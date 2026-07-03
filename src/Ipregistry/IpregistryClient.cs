// Copyright 2019 Ipregistry (https://ipregistry.co).
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Net;
using System.Reflection;
using System.Text.Json;
using Ipregistry.Json;

namespace Ipregistry;

/// <summary>
/// The default <see cref="IIpregistryClient"/> implementation. Create one with an
/// API key, or register it in a service collection with
/// <c>services.AddIpregistry(...)</c>. A client is safe for concurrent use by
/// multiple threads.
/// </summary>
/// <remarks>
/// By default the client manages its own <see cref="HttpClient"/> with a
/// 15-second timeout, retries transient failures up to three times, and performs
/// no caching. Behavior is customized with <see cref="IpregistryClientOptions"/>.
/// </remarks>
public sealed class IpregistryClient : IIpregistryClient, IDisposable
{
    private static readonly string DefaultUserAgent =
        "IpregistryClient/DotNet/" + GetLibraryVersion();

    private readonly IpregistryClientOptions _options;
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private readonly IIpregistryCache _cache;
    private readonly string _baseUrl;
    private readonly string _userAgent;
    private readonly int _maxBatchSize;

    /// <summary>
    /// Creates a client authenticating with the given API key and default options.
    /// You can obtain a key, along with a generous free tier, at https://ipregistry.co.
    /// </summary>
    /// <param name="apiKey">The Ipregistry API key.</param>
    public IpregistryClient(string apiKey)
        : this(new IpregistryClientOptions { ApiKey = apiKey })
    {
    }

    /// <summary>Creates a client with the given options.</summary>
    /// <param name="options">The client configuration; <see cref="IpregistryClientOptions.ApiKey"/> is required.</param>
    public IpregistryClient(IpregistryClientOptions options)
        : this(options, httpClient: null)
    {
    }

    /// <summary>
    /// Creates a client that sends requests through a caller-provided
    /// <see cref="HttpClient"/>, giving full control over connection pooling,
    /// proxying, TLS, and instrumentation — typically one managed by
    /// <c>IHttpClientFactory</c>. The caller retains ownership: disposing the
    /// Ipregistry client does not dispose the HTTP client, and the HTTP client's
    /// own timeout applies instead of <see cref="IpregistryClientOptions.Timeout"/>.
    /// </summary>
    /// <param name="options">The client configuration; <see cref="IpregistryClientOptions.ApiKey"/> is required.</param>
    /// <param name="httpClient">The HTTP client to send requests through.</param>
    public IpregistryClient(IpregistryClientOptions options, HttpClient? httpClient)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            throw new ArgumentException(
                "An Ipregistry API key is required. Get one at https://ipregistry.co.",
                nameof(options));
        }

        _options = options.Clone();
        _cache = _options.Cache ?? NoopCache.Instance;
        _baseUrl = _options.BaseUrl.TrimEnd('/');
        _userAgent = string.IsNullOrEmpty(_options.UserAgent) ? DefaultUserAgent : _options.UserAgent;
        _maxBatchSize = _options.MaxBatchSize is > 0 and <= IpregistryClientOptions.ApiMaxBatchSize
            ? _options.MaxBatchSize
            : IpregistryClientOptions.ApiMaxBatchSize;

        if (httpClient is null)
        {
            _httpClient = new HttpClient
            {
                Timeout = _options.Timeout > TimeSpan.Zero ? _options.Timeout : System.Threading.Timeout.InfiniteTimeSpan,
            };
            _ownsHttpClient = true;
        }
        else
        {
            _httpClient = httpClient;
        }
    }

    /// <summary>The cache used by the client.</summary>
    public IIpregistryCache Cache => _cache;

    /// <inheritdoc />
    public async Task<IpInfo> LookupAsync(string ip, LookupOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ip))
        {
            throw new ArgumentException(
                "ip must not be empty; use LookupOriginAsync for the requester IP.", nameof(ip));
        }

        var query = options?.ToQueryString() ?? "";
        var key = CacheKey(ip, query);
        if (_cache.TryGet(key, out var cached))
        {
            return cached;
        }

        var data = await SendAsync(HttpMethod.Get, BuildUrl(ip, query), body: null, cancellationToken)
            .ConfigureAwait(false);

        var info = Deserialize(data, IpregistryJsonContext.Default.IpInfo);
        _cache.Set(key, info);
        return info;
    }

    /// <inheritdoc />
    public Task<IpInfo> LookupAsync(IPAddress address, LookupOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(address);
        return LookupAsync(address.ToString(), options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<RequesterIpInfo> LookupOriginAsync(LookupOptions? options = null, CancellationToken cancellationToken = default)
    {
        var query = options?.ToQueryString() ?? "";

        var data = await SendAsync(HttpMethod.Get, BuildUrl("", query), body: null, cancellationToken)
            .ConfigureAwait(false);

        return Deserialize(data, IpregistryJsonContext.Default.RequesterIpInfo);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IpInfoResult>> LookupBatchAsync(IEnumerable<string> ips, LookupOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ips);
        var allIps = ips as IReadOnlyList<string> ?? [.. ips];

        var query = options?.ToQueryString() ?? "";

        var cached = new IpInfo?[allIps.Count];
        List<string> misses = [];
        for (var i = 0; i < allIps.Count; i++)
        {
            if (_cache.TryGet(CacheKey(allIps[i], query), out var hit))
            {
                cached[i] = hit;
            }
            else
            {
                misses.Add(allIps[i]);
            }
        }

        var fresh = await ResolveMissesAsync(misses, query, cancellationToken).ConfigureAwait(false);

        var results = new IpInfoResult[allIps.Count];
        var next = 0;
        for (var i = 0; i < allIps.Count; i++)
        {
            if (cached[i] is { } hit)
            {
                results[i] = new IpInfoResult(hit);
                continue;
            }

            if (next >= fresh.Count)
            {
                // Defensive: the API returned fewer results than requested.
                results[i] = new IpInfoResult(new IpregistryApiException(
                    code: null, message: "missing result for requested IP address"));
                continue;
            }

            var result = fresh[next];
            next++;
            results[i] = result;
            if (result.Info is { } info)
            {
                _cache.Set(CacheKey(allIps[i], query), info);
            }
        }

        return results;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<IpInfoResult>> LookupBatchAsync(IEnumerable<IPAddress> addresses, LookupOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(addresses);
        List<string> ips = [];
        foreach (var address in addresses)
        {
            if (address is null)
            {
                throw new ArgumentException($"null IP address at index {ips.Count}.", nameof(addresses));
            }

            ips.Add(address.ToString());
        }

        return LookupBatchAsync(ips, options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UserAgentResult>> ParseUserAgentsAsync(IEnumerable<string> userAgents, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userAgents);
        string[] all = [.. userAgents];

        var body = JsonSerializer.SerializeToUtf8Bytes(all, IpregistryJsonContext.Default.StringArray);
        var data = await SendAsync(HttpMethod.Post, _baseUrl + "/user_agent", body, cancellationToken)
            .ConfigureAwait(false);

        return ParseResults(
            data,
            static element => new UserAgentResult(
                element.Deserialize(IpregistryJsonContext.Default.UserAgent)
                    ?? throw new IpregistryClientException("failed to decode response: null result entry")),
            static error => new UserAgentResult(error));
    }

    /// <summary>
    /// Releases resources held by the client. When the client owns its
    /// <see cref="HttpClient"/> (the default), it is disposed. A disposed client
    /// should no longer be used.
    /// </summary>
    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    /// <summary>
    /// Fetches fresh data for the cache-missed IP addresses. Sends a single
    /// request when the addresses fit within the API's per-request limit, and
    /// otherwise splits them into chunks dispatched with bounded concurrency. The
    /// returned results preserve the order of <paramref name="misses"/>.
    /// </summary>
    private async Task<IReadOnlyList<IpInfoResult>> ResolveMissesAsync(List<string> misses, string query, CancellationToken cancellationToken)
    {
        if (misses.Count == 0)
        {
            return [];
        }

        if (misses.Count <= _maxBatchSize)
        {
            return await SendBatchRequestAsync(misses, query, cancellationToken).ConfigureAwait(false);
        }

        List<List<string>> chunks = [];
        for (var start = 0; start < misses.Count; start += _maxBatchSize)
        {
            chunks.Add(misses.GetRange(start, Math.Min(_maxBatchSize, misses.Count - start)));
        }

        // On the first chunk failure the remaining in-flight requests are
        // cancelled and that error is rethrown, matching the Go client.
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var semaphore = new SemaphoreSlim(Math.Max(1, _options.BatchConcurrency));

        var tasks = chunks.Select(async chunk =>
        {
            await semaphore.WaitAsync(cts.Token).ConfigureAwait(false);
            try
            {
                return await SendBatchRequestAsync(chunk, query, cts.Token).ConfigureAwait(false);
            }
            catch
            {
                await cts.CancelAsync().ConfigureAwait(false);
                throw;
            }
            finally
            {
                semaphore.Release();
            }
        }).ToArray();

        IReadOnlyList<IpInfoResult>[] chunkResults;
        try
        {
            chunkResults = await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // A sibling chunk failed and cancelled the rest; surface its error
            // rather than the induced cancellation.
            var failure = tasks.Select(t => t.Exception?.GetBaseException())
                .FirstOrDefault(e => e is not null and not OperationCanceledException);
            throw failure ?? new IpregistryClientException("batch request failed");
        }

        List<IpInfoResult> merged = new(misses.Count);
        foreach (var chunkResult in chunkResults)
        {
            merged.AddRange(chunkResult);
        }

        return merged;
    }

    /// <summary>Performs a single POST batch request for the given addresses.</summary>
    private async Task<IReadOnlyList<IpInfoResult>> SendBatchRequestAsync(List<string> ips, string query, CancellationToken cancellationToken)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(ips.ToArray(), IpregistryJsonContext.Default.StringArray);
        var data = await SendAsync(HttpMethod.Post, BuildUrl("", query), body, cancellationToken)
            .ConfigureAwait(false);

        return ParseResults(
            data,
            static element => new IpInfoResult(
                element.Deserialize(IpregistryJsonContext.Default.IpInfo)
                    ?? throw new IpregistryClientException("failed to decode response: null result entry")),
            static error => new IpInfoResult(error));
    }

    /// <summary>
    /// Decodes the {"results": [...]} envelope, mapping each element to either a
    /// success or an error entry depending on whether it carries an error code.
    /// </summary>
    private static IReadOnlyList<TResult> ParseResults<TResult>(
        byte[] data,
        Func<JsonElement, TResult> success,
        Func<IpregistryApiException, TResult> failure)
    {
        try
        {
            using var document = JsonDocument.Parse(data);
            if (!document.RootElement.TryGetProperty("results"u8, out var resultsElement)
                || resultsElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            List<TResult> results = new(resultsElement.GetArrayLength());
            foreach (var element in resultsElement.EnumerateArray())
            {
                if (element.ValueKind == JsonValueKind.Object
                    && element.TryGetProperty("code"u8, out var codeElement)
                    && codeElement.ValueKind == JsonValueKind.String)
                {
                    var payload = element.Deserialize(IpregistryJsonContext.Default.ApiErrorPayload);
                    results.Add(failure(ToApiException(payload, statusCode: null)));
                }
                else
                {
                    results.Add(success(element));
                }
            }

            return results;
        }
        catch (JsonException exception)
        {
            throw new IpregistryClientException("failed to decode response", exception);
        }
    }

    /// <summary>
    /// Performs an HTTP request with automatic retries and returns the raw 2xx
    /// response body. Non-2xx responses are converted to
    /// <see cref="IpregistryApiException"/>; transport and I/O failures to
    /// <see cref="IpregistryClientException"/>. Caller-initiated cancellation
    /// propagates as <see cref="OperationCanceledException"/>.
    /// </summary>
    private async Task<byte[]> SendAsync(HttpMethod method, string url, byte[]? body, CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            using var request = new HttpRequestMessage(method, url);
            request.Headers.TryAddWithoutValidation("Authorization", "ApiKey " + _options.ApiKey);
            request.Headers.TryAddWithoutValidation("User-Agent", _userAgent);
            request.Headers.TryAddWithoutValidation("Accept", "application/json");
            if (body is not null)
            {
                request.Content = new ByteArrayContent(body);
                request.Content.Headers.TryAddWithoutValidation("Content-Type", "application/json");
            }

            HttpResponseMessage? response = null;
            try
            {
                byte[] data;
                try
                {
                    response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                        .ConfigureAwait(false);
                    data = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception exception) when (IsTransient(exception, cancellationToken))
                {
                    // Transport errors and per-request timeouts are retried up to
                    // MaxRetries regardless of the retry-on-status flags, matching
                    // the reference implementation.
                    if (attempt < _options.MaxRetries)
                    {
                        await BackoffAsync(attempt, retryAfter: null, cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    throw new IpregistryClientException("request failed", exception);
                }

                if (response.IsSuccessStatusCode)
                {
                    return data;
                }

                if (ShouldRetryStatus(response.StatusCode) && attempt < _options.MaxRetries)
                {
                    var retryAfter = ParseRetryAfter(response);
                    await BackoffAsync(attempt, retryAfter, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                throw ParseApiError(data, response.StatusCode);
            }
            finally
            {
                response?.Dispose();
            }
        }
    }

    /// <summary>
    /// Reports whether an exception thrown while sending is a retryable transport
    /// failure, as opposed to caller-initiated cancellation.
    /// </summary>
    private static bool IsTransient(Exception exception, CancellationToken cancellationToken) =>
        exception switch
        {
            OperationCanceledException => !cancellationToken.IsCancellationRequested,
            HttpRequestException or IOException => true,
            _ => false,
        };

    /// <summary>Reports whether a non-2xx status is eligible for retry given the client's configuration.</summary>
    private bool ShouldRetryStatus(HttpStatusCode status)
    {
        if (status == HttpStatusCode.TooManyRequests)
        {
            return _options.RetryOnTooManyRequests;
        }

        if ((int)status is >= 500 and < 600)
        {
            return _options.RetryOnServerError;
        }

        return false;
    }

    /// <summary>
    /// Waits before the next retry attempt, honoring an explicit Retry-After
    /// duration when positive and otherwise using exponential backoff.
    /// </summary>
    private async Task BackoffAsync(int attempt, TimeSpan? retryAfter, CancellationToken cancellationToken)
    {
        var delay = retryAfter ?? TimeSpan.Zero;
        if (delay <= TimeSpan.Zero)
        {
            var shift = Math.Min(attempt, 30);
            delay = _options.RetryInterval * (1L << shift);
        }

        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Extracts the Retry-After response header as a delay, supporting both the
    /// delta-seconds and HTTP-date forms. Returns null when absent or invalid.
    /// </summary>
    private static TimeSpan? ParseRetryAfter(HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter;
        if (retryAfter is null)
        {
            return null;
        }

        if (retryAfter.Delta is { } delta)
        {
            return delta > TimeSpan.Zero ? delta : null;
        }

        if (retryAfter.Date is { } date)
        {
            var delay = date - DateTimeOffset.UtcNow;
            return delay > TimeSpan.Zero ? delay : null;
        }

        return null;
    }

    /// <summary>
    /// Converts a non-2xx response body into an <see cref="IpregistryApiException"/>,
    /// falling back to a generic message when the body is not a recognizable error
    /// payload.
    /// </summary>
    private static IpregistryApiException ParseApiError(byte[] data, HttpStatusCode status)
    {
        ApiErrorPayload? payload = null;
        try
        {
            payload = JsonSerializer.Deserialize(data, IpregistryJsonContext.Default.ApiErrorPayload);
        }
        catch (JsonException)
        {
            // Fall through to the generic error below.
        }

        if (payload is null || string.IsNullOrEmpty(payload.Code))
        {
            return new IpregistryApiException(
                code: null, message: $"unexpected HTTP status {(int)status}", resolution: null, statusCode: status);
        }

        return ToApiException(payload, status);
    }

    private static IpregistryApiException ToApiException(ApiErrorPayload? payload, HttpStatusCode? statusCode) =>
        new(payload?.Code, payload?.Message ?? "", payload?.Resolution, statusCode);

    private static T Deserialize<T>(byte[] data, System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> typeInfo)
    {
        try
        {
            return JsonSerializer.Deserialize(data, typeInfo)
                ?? throw new IpregistryClientException("failed to decode response: empty body");
        }
        catch (JsonException exception)
        {
            throw new IpregistryClientException("failed to decode response", exception);
        }
    }

    /// <summary>
    /// Builds the request URL for a single-IP or origin lookup. An empty ip
    /// targets the origin (requester) endpoint.
    /// </summary>
    private string BuildUrl(string ip, string query)
    {
        var url = _baseUrl + "/" + Uri.EscapeDataString(ip);
        return query.Length > 0 ? url + "?" + query : url;
    }

    /// <summary>
    /// Derives a deterministic cache key from an IP address and its canonical
    /// query string, stable regardless of option ordering.
    /// </summary>
    private static string CacheKey(string ip, string query) =>
        query.Length == 0 ? ip : ip + ";" + query;

    private static string GetLibraryVersion()
    {
        var informational = typeof(IpregistryClient).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (string.IsNullOrEmpty(informational))
        {
            return "0.0.0";
        }

        // Strip any source-revision suffix (for example "1.0.0+abc123").
        var plus = informational.IndexOf('+', StringComparison.Ordinal);
        return plus >= 0 ? informational[..plus] : informational;
    }

    private sealed class NoopCache : IIpregistryCache
    {
        public static readonly NoopCache Instance = new();

        public bool TryGet(string key, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out IpInfo? value)
        {
            value = null;
            return false;
        }

        public void Set(string key, IpInfo value)
        {
        }

        public void Remove(string key)
        {
        }

        public void Clear()
        {
        }
    }
}
