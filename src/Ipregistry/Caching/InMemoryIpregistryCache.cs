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
/// A thread-safe, in-process <see cref="IIpregistryCache"/> with time-based
/// expiration and a bounded size using least-recently-used eviction. Without
/// arguments it holds up to 4096 entries for 10 minutes each.
/// </summary>
public sealed class InMemoryIpregistryCache : IIpregistryCache
{
    /// <summary>The default maximum number of entries (4096).</summary>
    public const int DefaultMaxSize = 4096;

    /// <summary>The default time-to-live of an entry (10 minutes).</summary>
    public static readonly TimeSpan DefaultTimeToLive = TimeSpan.FromMinutes(10);

#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif
    private readonly int _maxSize;
    private readonly TimeSpan _timeToLive;
    private readonly TimeProvider _timeProvider;
    private readonly LinkedList<CacheEntry> _lru = new(); // first = most recently used
    private readonly Dictionary<string, LinkedListNode<CacheEntry>> _items = new();

    /// <summary>Creates a cache.</summary>
    /// <param name="maxSize">
    /// The maximum number of entries the cache holds before it starts evicting the
    /// least recently used entry. A value &lt;= 0 leaves the default (4096).
    /// </param>
    /// <param name="timeToLive">
    /// How long an entry stays valid after being written. A null or non-positive
    /// value leaves the default (10 minutes).
    /// </param>
    /// <param name="timeProvider">The clock used for expiration; defaults to <see cref="TimeProvider.System"/>.</param>
    public InMemoryIpregistryCache(int maxSize = 0, TimeSpan? timeToLive = null, TimeProvider? timeProvider = null)
    {
        _maxSize = maxSize > 0 ? maxSize : DefaultMaxSize;
        _timeToLive = timeToLive > TimeSpan.Zero ? timeToLive.Value : DefaultTimeToLive;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// The current number of entries, including any not yet evicted but possibly
    /// stale. It is primarily useful in tests.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _lru.Count;
            }
        }
    }

    /// <inheritdoc />
    public bool TryGet(string key, [NotNullWhen(true)] out IpInfo? value)
    {
        ArgumentNullException.ThrowIfNull(key);

        lock (_lock)
        {
            if (!_items.TryGetValue(key, out var node))
            {
                value = null;
                return false;
            }

            if (_timeProvider.GetUtcNow() > node.Value.ExpiresAt)
            {
                RemoveNode(node);
                value = null;
                return false;
            }

            _lru.Remove(node);
            _lru.AddFirst(node);
            value = node.Value.Value;
            return true;
        }
    }

    /// <inheritdoc />
    public void Set(string key, IpInfo value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        lock (_lock)
        {
            var expiresAt = _timeProvider.GetUtcNow() + _timeToLive;
            if (_items.TryGetValue(key, out var node))
            {
                node.Value = new CacheEntry(key, value, expiresAt);
                _lru.Remove(node);
                _lru.AddFirst(node);
                return;
            }

            node = _lru.AddFirst(new CacheEntry(key, value, expiresAt));
            _items[key] = node;

            if (_lru.Count > _maxSize && _lru.Last is { } oldest)
            {
                RemoveNode(oldest);
            }
        }
    }

    /// <inheritdoc />
    public void Remove(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        lock (_lock)
        {
            if (_items.TryGetValue(key, out var node))
            {
                RemoveNode(node);
            }
        }
    }

    /// <inheritdoc />
    public void Clear()
    {
        lock (_lock)
        {
            _lru.Clear();
            _items.Clear();
        }
    }

    // Must be called with the lock held.
    private void RemoveNode(LinkedListNode<CacheEntry> node)
    {
        _lru.Remove(node);
        _items.Remove(node.Value.Key);
    }

    private readonly record struct CacheEntry(string Key, IpInfo Value, DateTimeOffset ExpiresAt);
}
