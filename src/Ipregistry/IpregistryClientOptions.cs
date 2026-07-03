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

namespace Ipregistry;

/// <summary>
/// Configures an <see cref="IpregistryClient"/>. All properties have sensible
/// defaults; only <see cref="ApiKey"/> is required.
/// </summary>
public sealed class IpregistryClientOptions
{
    /// <summary>The base URL of the Ipregistry API used unless overridden with <see cref="BaseUrl"/>.</summary>
    public const string DefaultBaseUrl = "https://api.ipregistry.co";

    /// <summary>
    /// The maximum number of IP addresses Ipregistry accepts in a single batch
    /// request. Batch lookups transparently split larger inputs into several
    /// requests so callers never have to.
    /// </summary>
    public const int ApiMaxBatchSize = 1024;

    /// <summary>
    /// The API key used to authenticate requests. You can obtain a key, along with
    /// a generous free tier, at https://ipregistry.co.
    /// </summary>
    public string ApiKey { get; set; } = "";

    /// <summary>
    /// The API base URL. Overriding it is mainly useful for testing or pointing at
    /// a private deployment. A trailing slash is ignored.
    /// </summary>
    public string BaseUrl { get; set; } = DefaultBaseUrl;

    /// <summary>
    /// The per-request timeout applied to the HTTP client the library creates and
    /// owns. It is ignored when the client is constructed with a caller-provided
    /// <see cref="HttpClient"/>, whose own timeout then applies. A value &lt;= 0
    /// disables the client-level timeout (rely on cancellation tokens instead).
    /// Defaults to 15 seconds.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// The maximum number of automatic retries performed in addition to the
    /// initial attempt. Set to 0 to disable retries. Defaults to 3.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// The base backoff between retries. Successive retries use an exponentially
    /// increasing delay (interval * 2^attempt). When a 429 response carries a
    /// Retry-After header, that value takes precedence. Defaults to 1 second.
    /// </summary>
    public TimeSpan RetryInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Whether 5xx responses (and transient network errors) are retried.
    /// Defaults to true.
    /// </summary>
    public bool RetryOnServerError { get; set; } = true;

    /// <summary>
    /// Whether 429 Too Many Requests responses are retried, honoring the
    /// Retry-After header when present. Ipregistry does not rate limit by default
    /// (it is opt-in per API key), so this defaults to false.
    /// </summary>
    public bool RetryOnTooManyRequests { get; set; }

    /// <summary>
    /// The maximum number of IP addresses sent in a single batch request. Batch
    /// lookups split larger inputs into this many addresses per request. Values
    /// are capped at <see cref="ApiMaxBatchSize"/> (the API limit); a value
    /// &lt;= 0 selects the default.
    /// </summary>
    public int MaxBatchSize { get; set; } = ApiMaxBatchSize;

    /// <summary>
    /// How many batch sub-requests are dispatched concurrently when a batch is
    /// large enough to be split into chunks. Set it to 1 for strictly sequential
    /// dispatch, which is gentler on a rate-limited API key. Defaults to 4.
    /// </summary>
    public int BatchConcurrency { get; set; } = 4;

    /// <summary>
    /// The cache used to memoize successful single and batch IP lookups. By
    /// default no cache is used so that data is never stale. Use
    /// <see cref="InMemoryIpregistryCache"/> or supply your own implementation.
    /// </summary>
    public IIpregistryCache? Cache { get; set; }

    /// <summary>The User-Agent header sent with requests, or null for the library default.</summary>
    public string? UserAgent { get; set; }

    internal IpregistryClientOptions Clone() => (IpregistryClientOptions)MemberwiseClone();
}
