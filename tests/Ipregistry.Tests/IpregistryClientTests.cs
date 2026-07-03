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
using Xunit;

namespace Ipregistry.Tests;

public sealed class IpregistryClientTests
{
    private const string SampleIpInfoJson = """
        {
          "ip": "8.8.8.8",
          "type": "IPv4",
          "connection": { "asn": 15169, "organization": "Google LLC", "type": "hosting" },
          "location": {
            "continent": { "code": "NA", "name": "North America" },
            "country": { "code": "US", "name": "United States", "area": 9629091, "population": 331002651 },
            "region": { "code": "US-CA", "name": "California" },
            "city": "Mountain View",
            "latitude": 37.405992,
            "longitude": -122.078515,
            "in_eu": false
          },
          "security": { "is_vpn": false, "is_threat": false },
          "time_zone": { "id": "America/Los_Angeles", "offset": -28800 }
        }
        """;

    private static (IpregistryClient Client, FakeHttpMessageHandler Handler) CreateClient(
        Action<IpregistryClientOptions>? configure = null)
    {
        var handler = new FakeHttpMessageHandler();
        var options = new IpregistryClientOptions
        {
            ApiKey = "test-key",
            RetryInterval = TimeSpan.FromMilliseconds(1),
        };
        configure?.Invoke(options);
        var client = new IpregistryClient(options, new HttpClient(handler));
        return (client, handler);
    }

    [Fact]
    public void Constructor_RequiresApiKey()
    {
        Assert.Throws<ArgumentException>(() => new IpregistryClient(""));
        Assert.Throws<ArgumentException>(() => new IpregistryClient("   "));
        Assert.Throws<ArgumentNullException>(() => new IpregistryClient((IpregistryClientOptions)null!));
    }

    [Fact]
    public async Task Lookup_ParsesResponse_AndSendsExpectedRequest()
    {
        var (client, handler) = CreateClient();
        handler.Enqueue(HttpStatusCode.OK, SampleIpInfoJson);

        var info = await client.LookupAsync("8.8.8.8");

        Assert.Equal("8.8.8.8", info.Ip);
        Assert.Equal(IpType.IPv4, info.Type);
        Assert.Equal(15169, info.Connection.Asn);
        Assert.Equal("US", info.Location.Country.Code);
        Assert.Equal("Mountain View", info.Location.City);
        Assert.Equal(37.405992, info.Location.Latitude);
        Assert.Equal(-28800, info.TimeZone.Offset);
        Assert.False(info.Security.IsVpn);
        // Omitted nested objects are non-null with default values.
        Assert.NotNull(info.Carrier);
        Assert.Null(info.Carrier.Name);
        Assert.NotNull(info.Currency.Format.Negative);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("https://api.ipregistry.co/8.8.8.8", request.Uri!.ToString());
        Assert.Equal("ApiKey test-key", request.Authorization);
        Assert.StartsWith("IpregistryClient/DotNet/", request.UserAgent);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Lookup_RejectsEmptyIp(string? ip)
    {
        var (client, _) = CreateClient();
        await Assert.ThrowsAsync<ArgumentException>(() => client.LookupAsync(ip!));
    }

    [Fact]
    public async Task Lookup_WithIPAddress_UsesStringForm()
    {
        var (client, handler) = CreateClient();
        handler.Enqueue(HttpStatusCode.OK, """{"ip": "2001:db8::1", "type": "IPv6"}""");

        var info = await client.LookupAsync(IPAddress.Parse("2001:db8::1"));

        Assert.Equal("2001:db8::1", info.Ip);
        Assert.Contains("2001", handler.Requests[0].Uri!.AbsolutePath, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Lookup_AppliesOptionsAsQueryParameters()
    {
        var (client, handler) = CreateClient();
        handler.Enqueue(HttpStatusCode.OK, "{}");

        var options = new LookupOptions { Fields = "location.country.name,security", Hostname = true };
        options.AdditionalParameters["key2"] = "value 2";
        await client.LookupAsync("8.8.8.8", options);

        var query = handler.Requests[0].Uri!.Query;
        Assert.Contains("fields=location.country.name%2Csecurity", query, StringComparison.Ordinal);
        Assert.Contains("hostname=true", query, StringComparison.Ordinal);
        Assert.Contains("key2=value%202", query, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Lookup_MapsApiError()
    {
        var (client, handler) = CreateClient();
        handler.Enqueue(HttpStatusCode.BadRequest, """
            {"code": "INVALID_IP_ADDRESS", "message": "The IP is invalid.", "resolution": "Use a valid IP."}
            """);

        var exception = await Assert.ThrowsAsync<IpregistryApiException>(() => client.LookupAsync("invalid"));

        Assert.Equal("INVALID_IP_ADDRESS", exception.Code);
        Assert.Equal(IpregistryErrorCode.InvalidIpAddress, exception.ErrorCode);
        Assert.Equal("Use a valid IP.", exception.Resolution);
        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
        Assert.Contains("The IP is invalid.", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Lookup_MapsUnrecognizedErrorBody()
    {
        var (client, handler) = CreateClient(options => options.RetryOnServerError = false);
        handler.Enqueue(HttpStatusCode.BadGateway, "<html>bad gateway</html>");

        var exception = await Assert.ThrowsAsync<IpregistryApiException>(() => client.LookupAsync("8.8.8.8"));

        Assert.Null(exception.Code);
        Assert.Equal(IpregistryErrorCode.Unknown, exception.ErrorCode);
        Assert.Contains("502", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Lookup_RetriesServerErrors_ThenSucceeds()
    {
        var (client, handler) = CreateClient();
        handler
            .Enqueue(HttpStatusCode.InternalServerError, """{"code": "INTERNAL", "message": "boom"}""")
            .Enqueue(HttpStatusCode.ServiceUnavailable, "oops")
            .Enqueue(HttpStatusCode.OK, SampleIpInfoJson);

        var info = await client.LookupAsync("8.8.8.8");

        Assert.Equal("8.8.8.8", info.Ip);
        Assert.Equal(3, handler.Requests.Count);
    }

    [Fact]
    public async Task Lookup_DoesNotRetryServerErrors_WhenDisabled()
    {
        var (client, handler) = CreateClient(options => options.RetryOnServerError = false);
        handler.Enqueue(HttpStatusCode.InternalServerError, """{"code": "INTERNAL", "message": "boom"}""");

        await Assert.ThrowsAsync<IpregistryApiException>(() => client.LookupAsync("8.8.8.8"));

        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Lookup_StopsRetrying_AfterMaxRetries()
    {
        var (client, handler) = CreateClient(options => options.MaxRetries = 2);
        handler.Enqueue(HttpStatusCode.InternalServerError, """{"code": "INTERNAL", "message": "boom"}""");

        var exception = await Assert.ThrowsAsync<IpregistryApiException>(() => client.LookupAsync("8.8.8.8"));

        Assert.Equal(IpregistryErrorCode.Internal, exception.ErrorCode);
        Assert.Equal(3, handler.Requests.Count); // initial attempt + 2 retries
    }

    [Fact]
    public async Task Lookup_DoesNotRetry429_ByDefault()
    {
        var (client, handler) = CreateClient();
        handler.Enqueue(HttpStatusCode.TooManyRequests, """{"code": "TOO_MANY_REQUESTS", "message": "slow down"}""");

        var exception = await Assert.ThrowsAsync<IpregistryApiException>(() => client.LookupAsync("8.8.8.8"));

        Assert.Equal(IpregistryErrorCode.TooManyRequests, exception.ErrorCode);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Lookup_Retries429_WhenEnabled_HonoringRetryAfter()
    {
        var (client, handler) = CreateClient(options => options.RetryOnTooManyRequests = true);
        handler
            .Enqueue(HttpStatusCode.TooManyRequests, "{}", ("Retry-After", "0"))
            .Enqueue(HttpStatusCode.OK, SampleIpInfoJson);

        var info = await client.LookupAsync("8.8.8.8");

        Assert.Equal("8.8.8.8", info.Ip);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task Lookup_RetriesTransportErrors()
    {
        var (client, handler) = CreateClient();
        handler
            .EnqueueTransportError(new HttpRequestException("connection reset"))
            .Enqueue(HttpStatusCode.OK, SampleIpInfoJson);

        var info = await client.LookupAsync("8.8.8.8");

        Assert.Equal("8.8.8.8", info.Ip);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task Lookup_WrapsTransportErrors_AfterExhaustingRetries()
    {
        var (client, handler) = CreateClient(options => options.MaxRetries = 1);
        handler.EnqueueTransportError(new HttpRequestException("connection reset"));

        var exception = await Assert.ThrowsAsync<IpregistryClientException>(() => client.LookupAsync("8.8.8.8"));

        Assert.IsType<HttpRequestException>(exception.InnerException);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task Lookup_PropagatesCancellation_AsOperationCanceledException()
    {
        var (client, _) = CreateClient();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.LookupAsync("8.8.8.8", cancellationToken: cts.Token));
    }

    [Fact]
    public async Task Lookup_ThrowsClientException_OnMalformedJson()
    {
        var (client, handler) = CreateClient();
        handler.Enqueue(HttpStatusCode.OK, "{not json");

        await Assert.ThrowsAsync<IpregistryClientException>(() => client.LookupAsync("8.8.8.8"));
    }

    [Fact]
    public async Task Lookup_UsesCache()
    {
        var (client, handler) = CreateClient(options => options.Cache = new InMemoryIpregistryCache());
        handler.Enqueue(HttpStatusCode.OK, SampleIpInfoJson);

        var first = await client.LookupAsync("8.8.8.8");
        var second = await client.LookupAsync("8.8.8.8");

        Assert.Same(first, second);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Lookup_CacheKeyIncludesOptions()
    {
        var (client, handler) = CreateClient(options => options.Cache = new InMemoryIpregistryCache());
        handler
            .Enqueue(HttpStatusCode.OK, SampleIpInfoJson)
            .Enqueue(HttpStatusCode.OK, SampleIpInfoJson);

        await client.LookupAsync("8.8.8.8");
        await client.LookupAsync("8.8.8.8", new LookupOptions { Fields = "security" });

        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task LookupOrigin_TargetsRootEndpoint_AndParsesUserAgent()
    {
        var (client, handler) = CreateClient();
        handler.Enqueue(HttpStatusCode.OK, """
            {
              "ip": "203.0.113.10",
              "type": "IPv4",
              "user_agent": { "header": "Mozilla/5.0", "name": "Chrome", "os": { "name": "Windows" } }
            }
            """);

        var origin = await client.LookupOriginAsync();

        Assert.Equal("203.0.113.10", origin.Ip);
        Assert.NotNull(origin.UserAgent);
        Assert.Equal("Chrome", origin.UserAgent.Name);
        Assert.Equal("Windows", origin.UserAgent.OperatingSystem.Name);
        Assert.Equal("https://api.ipregistry.co/", handler.Requests[0].Uri!.ToString());
    }

    [Fact]
    public async Task LookupBatch_MixesSuccessesAndPerEntryErrors_PreservingOrder()
    {
        var (client, handler) = CreateClient();
        handler.Enqueue(HttpStatusCode.OK, """
            {
              "results": [
                {"ip": "8.8.8.8", "type": "IPv4"},
                {"code": "INVALID_IP_ADDRESS", "message": "invalid", "resolution": "fix it"},
                {"ip": "1.1.1.1", "type": "IPv4"}
              ]
            }
            """);

        var results = await client.LookupBatchAsync(["8.8.8.8", "not-an-ip", "1.1.1.1"]);

        Assert.Equal(3, results.Count);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("8.8.8.8", results[0].Value.Ip);
        Assert.False(results[1].IsSuccess);
        Assert.Equal(IpregistryErrorCode.InvalidIpAddress, results[1].Error!.ErrorCode);
        Assert.Throws<IpregistryApiException>(() => results[1].Value);
        Assert.True(results[2].IsSuccess);
        Assert.Equal("1.1.1.1", results[2].Value.Ip);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("""["8.8.8.8","not-an-ip","1.1.1.1"]""", request.Body);
    }

    [Fact]
    public async Task LookupBatch_ServesCachedEntriesLocally_AndCachesFreshOnes()
    {
        var (client, handler) = CreateClient(options => options.Cache = new InMemoryIpregistryCache());
        handler
            .Enqueue(HttpStatusCode.OK, SampleIpInfoJson)
            .Enqueue(HttpStatusCode.OK, """{"results": [{"ip": "1.1.1.1", "type": "IPv4"}]}""");

        await client.LookupAsync("8.8.8.8"); // populates the cache

        var results = await client.LookupBatchAsync(["8.8.8.8", "1.1.1.1"]);

        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal("""["1.1.1.1"]""", handler.Requests[1].Body); // only the miss is requested
        Assert.Equal("8.8.8.8", results[0].Value.Ip);
        Assert.Equal("1.1.1.1", results[1].Value.Ip);

        // The batch response populated the cache for the second address.
        var again = await client.LookupAsync("1.1.1.1");
        Assert.Equal("1.1.1.1", again.Ip);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task LookupBatch_SplitsLargeInputsIntoChunks_PreservingOrder()
    {
        var (client, handler) = CreateClient(options =>
        {
            options.MaxBatchSize = 2;
            options.BatchConcurrency = 1; // deterministic request order
        });
        handler.Enqueue(request =>
        {
            // Echo back one result per requested IP.
            var body = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            var ips = System.Text.Json.JsonSerializer.Deserialize<string[]>(body)!;
            var entries = string.Join(",", ips.Select(ip => $$"""{"ip": "{{ip}}"}"""));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent($$"""{"results": [{{entries}}]}""", System.Text.Encoding.UTF8, "application/json"),
            };
        });

        string[] ips = ["10.0.0.1", "10.0.0.2", "10.0.0.3", "10.0.0.4", "10.0.0.5"];
        var results = await client.LookupBatchAsync(ips);

        Assert.Equal(3, handler.Requests.Count); // 2 + 2 + 1
        Assert.Equal(ips, results.Select(result => result.Value.Ip));
    }

    [Fact]
    public async Task LookupBatch_FailsWholeRequest_WhenAChunkFails()
    {
        var (client, handler) = CreateClient(options =>
        {
            options.MaxBatchSize = 1;
            options.BatchConcurrency = 1;
            options.MaxRetries = 0;
        });
        handler
            .Enqueue(HttpStatusCode.OK, """{"results": [{"ip": "10.0.0.1"}]}""")
            .Enqueue(HttpStatusCode.Forbidden, """{"code": "INVALID_API_KEY", "message": "bad key"}""");

        var exception = await Assert.ThrowsAsync<IpregistryApiException>(
            () => client.LookupBatchAsync(["10.0.0.1", "10.0.0.2"]));

        Assert.Equal(IpregistryErrorCode.InvalidApiKey, exception.ErrorCode);
    }

    [Fact]
    public async Task LookupBatch_WithIPAddresses_ConvertsToStrings()
    {
        var (client, handler) = CreateClient();
        handler.Enqueue(HttpStatusCode.OK, """{"results": [{"ip": "8.8.8.8"}]}""");

        var results = await client.LookupBatchAsync([IPAddress.Parse("8.8.8.8")]);

        Assert.Single(results);
        Assert.Equal("""["8.8.8.8"]""", handler.Requests[0].Body);
    }

    [Fact]
    public async Task ParseUserAgents_SendsPost_AndMapsPerEntryResults()
    {
        var (client, handler) = CreateClient();
        handler.Enqueue(HttpStatusCode.OK, """
            {
              "results": [
                {"header": "Mozilla/5.0", "name": "Firefox", "device": {"type": "desktop"}},
                {"code": "BAD_REQUEST", "message": "unparseable"}
              ]
            }
            """);

        var results = await client.ParseUserAgentsAsync("Mozilla/5.0", "garbage");

        Assert.Equal(2, results.Count);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("Firefox", results[0].Value.Name);
        Assert.Equal("desktop", results[0].Value.Device.Type);
        Assert.False(results[1].IsSuccess);
        Assert.Equal(IpregistryErrorCode.BadRequest, results[1].Error!.ErrorCode);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://api.ipregistry.co/user_agent", request.Uri!.ToString());
        Assert.Equal("""["Mozilla/5.0","garbage"]""", request.Body);
    }

    [Fact]
    public async Task BaseUrl_TrailingSlashIsIgnored()
    {
        var (client, handler) = CreateClient(options => options.BaseUrl = "https://example.test/api/");
        handler.Enqueue(HttpStatusCode.OK, "{}");

        await client.LookupAsync("8.8.8.8");

        Assert.Equal("https://example.test/api/8.8.8.8", handler.Requests[0].Uri!.ToString());
    }

    [Fact]
    public async Task CustomUserAgent_IsSent()
    {
        var (client, handler) = CreateClient(options => options.UserAgent = "MyApp/2.0");
        handler.Enqueue(HttpStatusCode.OK, "{}");

        await client.LookupAsync("8.8.8.8");

        Assert.Equal("MyApp/2.0", handler.Requests[0].UserAgent);
    }
}
