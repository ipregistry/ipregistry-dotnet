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

using Xunit;

namespace Ipregistry.SystemTests;

/// <summary>
/// A fact that runs against the live Ipregistry API. It is skipped unless the
/// IPREGISTRY_API_KEY environment variable is set, because each successful
/// lookup consumes credits.
/// </summary>
public sealed class SystemTestFactAttribute : FactAttribute
{
    public SystemTestFactAttribute()
    {
        if (string.IsNullOrEmpty(SystemTests.ApiKey))
        {
            Skip = "Set IPREGISTRY_API_KEY to run system tests against the live API.";
        }
    }
}

public sealed class SystemTests : IDisposable
{
    internal static readonly string? ApiKey = Environment.GetEnvironmentVariable("IPREGISTRY_API_KEY");

    private readonly IpregistryClient _client = new(ApiKey ?? "unused");
    private readonly CancellationTokenSource _timeout = new(TimeSpan.FromSeconds(30));

    public void Dispose()
    {
        _client.Dispose();
        _timeout.Dispose();
    }

    [SystemTestFact]
    public async Task Lookup_ReturnsDataForWellKnownAddress()
    {
        var info = await _client.LookupAsync("8.8.8.8", cancellationToken: _timeout.Token);

        Assert.Equal("8.8.8.8", info.Ip);
        Assert.Equal(IpType.IPv4, info.Type);
        Assert.False(string.IsNullOrEmpty(info.Location.Country.Code));
        Assert.NotNull(info.Connection.Asn);
    }

    [SystemTestFact]
    public async Task Lookup_SupportsIPv6()
    {
        var info = await _client.LookupAsync("2001:4860:4860::8888", cancellationToken: _timeout.Token);

        Assert.Equal(IpType.IPv6, info.Type);
        Assert.False(string.IsNullOrEmpty(info.Location.Country.Code));
    }

    [SystemTestFact]
    public async Task Lookup_WithFieldSelection_ReturnsFilteredPayload()
    {
        var info = await _client.LookupAsync(
            "8.8.8.8",
            new LookupOptions { Fields = "location.country.code" },
            _timeout.Token);

        Assert.False(string.IsNullOrEmpty(info.Location.Country.Code));
        Assert.Null(info.Connection.Asn); // filtered out
    }

    [SystemTestFact]
    public async Task LookupOrigin_ReturnsRequesterData()
    {
        var origin = await _client.LookupOriginAsync(cancellationToken: _timeout.Token);

        Assert.False(string.IsNullOrEmpty(origin.Ip));
    }

    [SystemTestFact]
    public async Task LookupBatch_ReportsPerEntryFailures()
    {
        string[] ips = ["8.8.8.8", "1.1.1.1", "not-an-ip"];
        var results = await _client.LookupBatchAsync(ips, cancellationToken: _timeout.Token);

        Assert.Equal(ips.Length, results.Count);
        Assert.True(results[0].IsSuccess);
        Assert.Equal("8.8.8.8", results[0].Value.Ip);
        Assert.True(results[1].IsSuccess);
        Assert.False(results[2].IsSuccess);
        Assert.NotNull(results[2].Error);
    }

    [SystemTestFact]
    public async Task Lookup_InvalidIp_ThrowsApiException()
    {
        var exception = await Assert.ThrowsAsync<IpregistryApiException>(
            () => _client.LookupAsync("invalid", cancellationToken: _timeout.Token));

        Assert.Equal(IpregistryErrorCode.InvalidIpAddress, exception.ErrorCode);
    }

    [SystemTestFact]
    public async Task Lookup_InvalidApiKey_ThrowsApiException()
    {
        using var client = new IpregistryClient("invalid-api-key");

        var exception = await Assert.ThrowsAsync<IpregistryApiException>(
            () => client.LookupAsync("8.8.8.8", cancellationToken: _timeout.Token));

        Assert.Equal(IpregistryErrorCode.InvalidApiKey, exception.ErrorCode);
    }

    [SystemTestFact]
    public async Task ParseUserAgents_ReturnsStructuredData()
    {
        const string chrome =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36";

        var results = await _client.ParseUserAgentsAsync([chrome], _timeout.Token);

        var result = Assert.Single(results);
        Assert.True(result.IsSuccess);
        Assert.Equal(chrome, result.Value.Header);
        Assert.False(string.IsNullOrEmpty(result.Value.Name));
    }
}
