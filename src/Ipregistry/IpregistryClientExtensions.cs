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

/// <summary>Convenience overloads for <see cref="IIpregistryClient"/>.</summary>
public static class IpregistryClientExtensions
{
    /// <summary>
    /// Resolves several IP addresses in one call. See
    /// <see cref="IIpregistryClient.LookupBatchAsync(IEnumerable{string}, LookupOptions?, CancellationToken)"/>.
    /// </summary>
    /// <param name="client">The client to send the request through.</param>
    /// <param name="ips">The IPv4 or IPv6 addresses to look up.</param>
    public static Task<IReadOnlyList<IpInfoResult>> LookupBatchAsync(this IIpregistryClient client, params string[] ips)
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.LookupBatchAsync(ips, options: null);
    }

    /// <summary>
    /// Parses one or more raw User-Agent strings into structured data. See
    /// <see cref="IIpregistryClient.ParseUserAgentsAsync(IEnumerable{string}, CancellationToken)"/>.
    /// </summary>
    /// <param name="client">The client to send the request through.</param>
    /// <param name="userAgents">The raw User-Agent strings to parse.</param>
    public static Task<IReadOnlyList<UserAgentResult>> ParseUserAgentsAsync(this IIpregistryClient client, params string[] userAgents)
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.ParseUserAgentsAsync(userAgents, CancellationToken.None);
    }
}
