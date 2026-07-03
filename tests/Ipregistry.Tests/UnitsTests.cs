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

using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Ipregistry.Tests;

public sealed class ErrorCodeTests
{
    [Theory]
    [InlineData("INVALID_API_KEY", IpregistryErrorCode.InvalidApiKey)]
    [InlineData("invalid_api_key", IpregistryErrorCode.InvalidApiKey)]
    [InlineData(" TOO_MANY_REQUESTS ", IpregistryErrorCode.TooManyRequests)]
    [InlineData("RESERVED_IP_ADDRESS", IpregistryErrorCode.ReservedIpAddress)]
    [InlineData("SOMETHING_NEW", IpregistryErrorCode.Unknown)]
    [InlineData("", IpregistryErrorCode.Unknown)]
    [InlineData(null, IpregistryErrorCode.Unknown)]
    public void Parse_MapsRawCodes(string? raw, IpregistryErrorCode expected)
    {
        Assert.Equal(expected, IpregistryErrorCodes.Parse(raw));
    }

    [Fact]
    public void ApiException_FormatsMessage()
    {
        var exception = new IpregistryApiException("INVALID_IP_ADDRESS", "The IP is invalid.", "Use a valid IP.");
        Assert.Equal("ipregistry: The IP is invalid. (INVALID_IP_ADDRESS): Use a valid IP.", exception.Message);

        var bare = new IpregistryApiException(null, "");
        Assert.Equal("ipregistry: API error", bare.Message);
    }
}

public sealed class UserAgentsTests
{
    [Theory]
    [InlineData("Googlebot/2.1 (+http://www.google.com/bot.html)", true)]
    [InlineData("Mozilla/5.0 (compatible; Baiduspider/2.0)", true)]
    [InlineData("Yahoo! Slurp", true)]
    [InlineData("ROBOT crawler", true)]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsBot_DetectsCrawlers(string? userAgent, bool expected)
    {
        Assert.Equal(expected, UserAgents.IsBot(userAgent));
    }
}

public sealed class LookupOptionsTests
{
    [Fact]
    public void ToQueryString_IsCanonicalAndEscaped()
    {
        var options = new LookupOptions { Fields = "a,b", Hostname = false };
        options.AdditionalParameters["zeta"] = "z value";
        options.AdditionalParameters["alpha"] = "1";

        Assert.Equal("alpha=1&fields=a%2Cb&hostname=false&zeta=z%20value", options.ToQueryString());
    }

    [Fact]
    public void ToQueryString_IsEmptyWithoutParameters()
    {
        Assert.Equal("", new LookupOptions().ToQueryString());
    }

    [Fact]
    public void DedicatedProperties_OverrideAdditionalParameters()
    {
        var options = new LookupOptions { Fields = "security" };
        options.AdditionalParameters["fields"] = "ignored";

        Assert.Equal("fields=security", options.ToQueryString());
    }
}

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddIpregistry_RegistersAWorkingClient()
    {
        var services = new ServiceCollection();
        services.AddIpregistry(options =>
        {
            options.ApiKey = "test-key";
            options.Cache = new InMemoryIpregistryCache();
        });

        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IIpregistryClient>();

        Assert.IsType<IpregistryClient>(client);
        Assert.IsType<InMemoryIpregistryCache>(((IpregistryClient)client).Cache);
    }

    [Fact]
    public void AddIpregistry_WithApiKeyOverload_Works()
    {
        var services = new ServiceCollection();
        services.AddIpregistry("test-key");

        using var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetRequiredService<IIpregistryClient>());
    }

    [Fact]
    public void AddIpregistry_RejectsMissingApiKey()
    {
        var services = new ServiceCollection();
        services.AddIpregistry(_ => { });

        using var provider = services.BuildServiceProvider();
        Assert.ThrowsAny<Microsoft.Extensions.Options.OptionsValidationException>(
            () => provider.GetRequiredService<IIpregistryClient>());
    }
}
