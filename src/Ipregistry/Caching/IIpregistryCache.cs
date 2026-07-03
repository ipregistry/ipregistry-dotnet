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

using System.Diagnostics.CodeAnalysis;

namespace Ipregistry;

/// <summary>
/// Abstracts the storage used by an <see cref="IpregistryClient"/> to memoize IP
/// lookups. Implementations must be safe for concurrent use by multiple threads.
/// </summary>
/// <remarks>
/// Only successful single and batch IP lookups are cached. Origin lookups are
/// never cached, because the requester IP is only known from the response.
/// </remarks>
public interface IIpregistryCache
{
    /// <summary>Returns the cached value for <paramref name="key"/>, and whether it was present.</summary>
    bool TryGet(string key, [NotNullWhen(true)] out IpInfo? value);

    /// <summary>Stores <paramref name="value"/> under <paramref name="key"/>.</summary>
    void Set(string key, IpInfo value);

    /// <summary>Removes the entry for <paramref name="key"/>, if present.</summary>
    void Remove(string key);

    /// <summary>Removes every entry.</summary>
    void Clear();
}
