[<img src="https://cdn.ipregistry.co/icons/favicon-96x96.png" alt="Ipregistry" width="64"/>](https://ipregistry.co/)
# Ipregistry .NET Client Library

[![License](http://img.shields.io/:license-apache-blue.svg)](LICENSE)
[![NuGet](https://img.shields.io/nuget/v/Ipregistry.svg)](https://www.nuget.org/packages/Ipregistry)
[![CI](https://github.com/ipregistry/ipregistry-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/ipregistry/ipregistry-dotnet/actions/workflows/ci.yml)

This is the official .NET client library for the [Ipregistry](https://ipregistry.co) IP geolocation and threat data
API, allowing you to look up your own IP address or specified ones. Responses return multiple data points including
carrier, company, currency, location, time zone, threat information, and more. The library can also parse raw
User-Agent strings.

It works from any .NET language — C#, F#, and VB.NET — and is:

- **Async end to end** — every operation is a cancellable `Task`.
- **`HttpClient`-friendly** — bring your own `HttpClient` / `IHttpClientFactory`, or let the library manage one.
- **DI-ready** — one-line registration with `services.AddIpregistry(...)`.
- **Resilient** — automatic retries with exponential backoff and `Retry-After` support.
- **Cacheable** — optional in-memory LRU + TTL cache, or plug in your own.
- **Trimming- and Native AOT-compatible** — JSON handling is source-generated.

## Getting started

You'll need an Ipregistry API key, which you can get along with 100,000 free lookups by signing up for a free account
at [https://ipregistry.co](https://ipregistry.co).

### Installation

```bash
dotnet add package Ipregistry
```

Requires .NET 8 or later.

### Single IP lookup

```csharp
using Ipregistry;

using var client = new IpregistryClient("YOUR_API_KEY");

// Look up data for a given IPv4 or IPv6 address.
// On the server side, retrieve the client IP from the request headers.
var info = await client.LookupAsync("54.85.132.205");
Console.WriteLine(info.Location.Country.Name);
Console.WriteLine(info.Connection.Asn);
Console.WriteLine(info.Security.IsVpn);
```

Nested objects such as `Location`, `Security`, or `Currency` are always non-null, so drilling into them never throws
— fields the API did not return simply hold `null` (strings, `Asn`, `Latitude`, ...) or their default value.

### Origin IP lookup

To look up the IP address the request is sent from — no argument needed — use `LookupOriginAsync`. It returns a
`RequesterIpInfo`, which additionally carries parsed User-Agent data:

```csharp
var origin = await client.LookupOriginAsync();
Console.WriteLine($"{origin.Ip} {origin.Location.Country.Name} {origin.UserAgent?.Name}");
```

### Batch IP lookup

`LookupBatchAsync` resolves many IP addresses in one call. Each entry may independently succeed or fail (for example
on an invalid address), so results are inspected element by element:

```csharp
var results = await client.LookupBatchAsync("8.8.8.8", "1.1.1.1", "not-an-ip");
foreach (var result in results)
{
    if (result.IsSuccess)
        Console.WriteLine($"{result.Value.Ip}: {result.Value.Location.Country.Name}");
    else
        Console.WriteLine($"failed: {result.Error.Message}");
}
```

Inputs larger than the API limit (1024 addresses) are transparently split into several requests dispatched with
bounded concurrency, and results come back in input order.

### User-Agent parsing

```csharp
var agents = await client.ParseUserAgentsAsync(Request.Headers.UserAgent.ToString());
Console.WriteLine(agents[0].Value.Name); // e.g. "Chrome"
```

There is also a lightweight local heuristic to skip lookups for automated traffic:

```csharp
if (!UserAgents.IsBot(userAgentHeader)) { /* look up the IP */ }
```

### ASP.NET Core / dependency injection

The library integrates with `IHttpClientFactory` and the options pattern:

```csharp
builder.Services.AddIpregistry(options =>
{
    options.ApiKey = builder.Configuration["Ipregistry:ApiKey"]!;
    options.Cache = new InMemoryIpregistryCache();
});
```

Then inject `IIpregistryClient` anywhere:

```csharp
app.MapGet("/whois/{ip}", async (string ip, IIpregistryClient ipregistry) =>
{
    var info = await ipregistry.LookupAsync(ip);
    return new { info.Ip, Country = info.Location.Country.Name, info.Security.IsVpn };
});
```

`AddIpregistry` returns an `IHttpClientBuilder`, so you can chain standard resilience or handler configuration
(for example `.AddStandardResilienceHandler()` from `Microsoft.Extensions.Http.Resilience`).

### F#

The API is `task`-friendly and null-annotated, so it composes naturally from F#:

```fsharp
open Ipregistry

use client = new IpregistryClient("YOUR_API_KEY")

task {
    let! info = client.LookupAsync "54.85.132.205"
    printfn $"{info.Location.Country.Name}"

    let! results = client.LookupBatchAsync [ "8.8.8.8"; "1.1.1.1" ]
    for result in results do
        match result.Error with
        | null -> printfn $"{result.Value.Ip} -> {result.Value.Location.Country.Name}"
        | error -> printfn $"failed -> {error.Message}"
}
```

Runnable C# and F# samples live in [`samples/`](samples).

## Configuration

All settings are optional except the API key:

```csharp
using var client = new IpregistryClient(new IpregistryClientOptions
{
    ApiKey = "YOUR_API_KEY",
    Timeout = TimeSpan.FromSeconds(15),          // per-request timeout (owned HttpClient only)
    MaxRetries = 3,                              // retries in addition to the initial attempt
    RetryInterval = TimeSpan.FromSeconds(1),     // base backoff, doubled per attempt
    RetryOnServerError = true,                   // retry 5xx and transient network errors
    RetryOnTooManyRequests = false,              // retry 429, honoring Retry-After
    MaxBatchSize = 1024,                         // addresses per batch request (API max)
    BatchConcurrency = 4,                        // concurrent chunk requests for large batches
    Cache = new InMemoryIpregistryCache(         // no caching by default
        maxSize: 4096, timeToLive: TimeSpan.FromMinutes(10)),
});
```

To control the HTTP layer yourself (proxy, TLS, pooling, instrumentation), pass your own `HttpClient`; the library
then never disposes it and your client's timeout applies:

```csharp
var client = new IpregistryClient(options, httpClient);
```

### Per-request options

```csharp
var info = await client.LookupAsync("8.8.8.8", new LookupOptions
{
    // https://ipregistry.co/docs/filtering-selecting-fields
    Fields = "location.country.name,security",
    Hostname = true, // reverse-DNS resolution
});
```

## Error handling

All failures derive from `IpregistryException`:

- `IpregistryApiException` — the API reported an error. Branch on the typed `ErrorCode`
  (for example `IpregistryErrorCode.InvalidIpAddress`, `InsufficientCredits`, `TooManyRequests`) or inspect the raw
  `Code`, `Resolution`, and HTTP `StatusCode`.
- `IpregistryClientException` — a client-side failure such as a network error or an undecodable response; the cause
  is in `InnerException`.

Cancellation is idiomatic: a canceled `CancellationToken` surfaces as `OperationCanceledException`, never wrapped.

```csharp
try
{
    var info = await client.LookupAsync(ip, cancellationToken: ct);
}
catch (IpregistryApiException e) when (e.ErrorCode == IpregistryErrorCode.InvalidIpAddress)
{
    // handle bad input
}
catch (IpregistryException e)
{
    // handle any other Ipregistry failure
}
```

In batch lookups, per-entry failures are returned (never thrown) as `IpregistryApiException` values on
`IpInfoResult.Error`.

## Development

```bash
dotnet build Ipregistry.slnx
dotnet test tests/Ipregistry.Tests           # unit tests, no network
IPREGISTRY_API_KEY=YOUR_API_KEY \
  dotnet test tests/Ipregistry.SystemTests   # system tests against the live API (consumes credits)
```

System tests skip cleanly when `IPREGISTRY_API_KEY` is not set.

## Other Languages

Official Ipregistry client libraries are also available for
[Go](https://github.com/ipregistry/ipregistry-go),
[Java](https://github.com/ipregistry/ipregistry-java),
[JavaScript/TypeScript](https://github.com/ipregistry/ipregistry-javascript),
[PHP](https://github.com/ipregistry/ipregistry-php), and
[Python](https://github.com/ipregistry/ipregistry-python).

## License

[Apache 2.0](LICENSE)
