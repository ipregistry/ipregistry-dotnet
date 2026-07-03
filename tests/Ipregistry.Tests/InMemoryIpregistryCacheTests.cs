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

namespace Ipregistry.Tests;

public sealed class InMemoryIpregistryCacheTests
{
    private sealed class FakeTimeProvider : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static IpInfo Info(string ip) => new() { Ip = ip };

    [Fact]
    public void SetAndGet_RoundTrips()
    {
        var cache = new InMemoryIpregistryCache();

        cache.Set("a", Info("1.1.1.1"));

        Assert.True(cache.TryGet("a", out var value));
        Assert.Equal("1.1.1.1", value.Ip);
        Assert.False(cache.TryGet("missing", out _));
    }

    [Fact]
    public void Entries_ExpireAfterTimeToLive()
    {
        var clock = new FakeTimeProvider();
        var cache = new InMemoryIpregistryCache(timeToLive: TimeSpan.FromMinutes(5), timeProvider: clock);

        cache.Set("a", Info("1.1.1.1"));
        clock.Now += TimeSpan.FromMinutes(4);
        Assert.True(cache.TryGet("a", out _));

        clock.Now += TimeSpan.FromMinutes(2);
        Assert.False(cache.TryGet("a", out _));
        Assert.Equal(0, cache.Count);
    }

    [Fact]
    public void Set_RefreshesExpiration()
    {
        var clock = new FakeTimeProvider();
        var cache = new InMemoryIpregistryCache(timeToLive: TimeSpan.FromMinutes(5), timeProvider: clock);

        cache.Set("a", Info("1.1.1.1"));
        clock.Now += TimeSpan.FromMinutes(4);
        cache.Set("a", Info("2.2.2.2"));
        clock.Now += TimeSpan.FromMinutes(4);

        Assert.True(cache.TryGet("a", out var value));
        Assert.Equal("2.2.2.2", value.Ip);
    }

    [Fact]
    public void EvictsLeastRecentlyUsed_WhenFull()
    {
        var cache = new InMemoryIpregistryCache(maxSize: 2);

        cache.Set("a", Info("1.1.1.1"));
        cache.Set("b", Info("2.2.2.2"));
        Assert.True(cache.TryGet("a", out _)); // touch "a" so "b" is the LRU entry
        cache.Set("c", Info("3.3.3.3"));

        Assert.True(cache.TryGet("a", out _));
        Assert.False(cache.TryGet("b", out _));
        Assert.True(cache.TryGet("c", out _));
        Assert.Equal(2, cache.Count);
    }

    [Fact]
    public void Remove_DeletesSingleEntry()
    {
        var cache = new InMemoryIpregistryCache();
        cache.Set("a", Info("1.1.1.1"));
        cache.Set("b", Info("2.2.2.2"));

        cache.Remove("a");
        cache.Remove("not-there"); // no-op

        Assert.False(cache.TryGet("a", out _));
        Assert.True(cache.TryGet("b", out _));
    }

    [Fact]
    public void Clear_DeletesEverything()
    {
        var cache = new InMemoryIpregistryCache();
        cache.Set("a", Info("1.1.1.1"));
        cache.Set("b", Info("2.2.2.2"));

        cache.Clear();

        Assert.Equal(0, cache.Count);
        Assert.False(cache.TryGet("a", out _));
    }

    [Fact]
    public async Task IsThreadSafe_UnderConcurrentAccess()
    {
        var cache = new InMemoryIpregistryCache(maxSize: 64);

        var tasks = Enumerable.Range(0, 8).Select(worker => Task.Run(() =>
        {
            for (var i = 0; i < 1000; i++)
            {
                var key = $"key-{i % 100}";
                cache.Set(key, Info($"10.0.0.{i % 256}"));
                cache.TryGet(key, out _);
                if (i % 50 == 0)
                {
                    cache.Remove(key);
                }
            }
        }));

        await Task.WhenAll(tasks);
        Assert.InRange(cache.Count, 0, 64);
    }
}
