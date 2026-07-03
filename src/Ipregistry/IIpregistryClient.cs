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

namespace Ipregistry;

/// <summary>
/// Sends requests to the Ipregistry API. The default implementation is
/// <see cref="IpregistryClient"/>; depend on this interface to substitute a fake
/// in tests.
/// </summary>
public interface IIpregistryClient
{
    /// <summary>
    /// Returns the data associated with the given IP address. To look up the
    /// requester's own IP, use <see cref="LookupOriginAsync"/> instead.
    /// When a cache is configured, a hit is returned without contacting the API.
    /// </summary>
    /// <param name="ip">A non-empty IPv4 or IPv6 address.</param>
    /// <param name="options">Optional per-request parameters.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="ArgumentException">When <paramref name="ip"/> is null, empty, or whitespace.</exception>
    /// <exception cref="IpregistryApiException">When the API reports a failure.</exception>
    /// <exception cref="IpregistryClientException">On network or decoding failures.</exception>
    Task<IpInfo> LookupAsync(string ip, LookupOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the data associated with the given IP address. It is a typed
    /// convenience over <see cref="LookupAsync(string, LookupOptions?, CancellationToken)"/>
    /// for callers that already hold an <see cref="IPAddress"/>.
    /// </summary>
    /// <param name="address">The IP address to look up.</param>
    /// <param name="options">Optional per-request parameters.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<IpInfo> LookupAsync(IPAddress address, LookupOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the data associated with the IP address the request originates
    /// from, enriched with parsed User-Agent data. Origin lookups are never
    /// cached, because the requester IP is only known from the response.
    /// </summary>
    /// <param name="options">Optional per-request parameters.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<RequesterIpInfo> LookupOriginAsync(LookupOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves several IP addresses in one call. The returned list preserves the
    /// order of <paramref name="ips"/>, and each entry may independently succeed
    /// or fail. A thrown exception indicates the whole request failed (for example
    /// authentication or a network error), not the failure of an individual entry.
    /// Inputs larger than <see cref="IpregistryClientOptions.MaxBatchSize"/> are
    /// transparently split into several requests dispatched with bounded concurrency.
    /// Entries already present in the cache are served locally; only the remainder
    /// are requested from the API, and freshly resolved entries are cached.
    /// </summary>
    /// <param name="ips">The IPv4 or IPv6 addresses to look up.</param>
    /// <param name="options">Optional per-request parameters.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<IReadOnlyList<IpInfoResult>> LookupBatchAsync(IEnumerable<string> ips, LookupOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// The <see cref="IPAddress"/> variant of
    /// <see cref="LookupBatchAsync(IEnumerable{string}, LookupOptions?, CancellationToken)"/>.
    /// </summary>
    /// <param name="addresses">The IP addresses to look up.</param>
    /// <param name="options">Optional per-request parameters.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<IReadOnlyList<IpInfoResult>> LookupBatchAsync(IEnumerable<IPAddress> addresses, LookupOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Parses one or more raw User-Agent strings (such as the User-Agent header of
    /// an incoming HTTP request) into structured data. Results preserve the order
    /// of the input, and each entry may independently succeed or fail.
    /// </summary>
    /// <param name="userAgents">The raw User-Agent strings to parse.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<IReadOnlyList<UserAgentResult>> ParseUserAgentsAsync(IEnumerable<string> userAgents, CancellationToken cancellationToken = default);
}
