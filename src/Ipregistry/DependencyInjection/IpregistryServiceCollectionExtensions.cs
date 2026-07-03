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

using Ipregistry;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>Registers Ipregistry services in an <see cref="IServiceCollection"/>.</summary>
public static class IpregistryServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IIpregistryClient"/> backed by a named
    /// <see cref="HttpClient"/> managed by <c>IHttpClientFactory</c>. Configure
    /// resilience, proxies, or additional handlers on the returned
    /// <see cref="IHttpClientBuilder"/>.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configure">Configures the client; setting <see cref="IpregistryClientOptions.ApiKey"/> is required.</param>
    /// <example>
    /// <code>
    /// services.AddIpregistry(options =>
    /// {
    ///     options.ApiKey = builder.Configuration["Ipregistry:ApiKey"]!;
    ///     options.Cache = new InMemoryIpregistryCache();
    /// });
    /// </code>
    /// </example>
    public static IHttpClientBuilder AddIpregistry(this IServiceCollection services, Action<IpregistryClientOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<IpregistryClientOptions>()
            .Configure(configure)
            .Validate(
                static options => !string.IsNullOrWhiteSpace(options.ApiKey),
                "An Ipregistry API key is required. Get one at https://ipregistry.co.");

        return services.AddHttpClient<IIpregistryClient, IpregistryClient>(
            "ipregistry",
            static (httpClient, provider) =>
            {
                var options = provider.GetRequiredService<IOptions<IpregistryClientOptions>>().Value;
                if (options.Timeout > TimeSpan.Zero)
                {
                    httpClient.Timeout = options.Timeout;
                }

                return new IpregistryClient(options, httpClient);
            });
    }

    /// <summary>
    /// Registers <see cref="IIpregistryClient"/> authenticating with the given API
    /// key and default options.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="apiKey">The Ipregistry API key.</param>
    public static IHttpClientBuilder AddIpregistry(this IServiceCollection services, string apiKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(apiKey);
        return services.AddIpregistry(options => options.ApiKey = apiKey);
    }
}
