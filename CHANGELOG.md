# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [2.0.0] - 2026-07-04

Versioning starts at 2.0.0 because the NuGet package ID previously hosted an
unofficial community wrapper whose 1.0.x version numbers cannot be reused.

### Added

- Initial release of the official Ipregistry .NET client library.
- Single IP lookup (`LookupAsync`), with `string` and `IPAddress` overloads.
- Origin IP lookup (`LookupOriginAsync`) returning parsed User-Agent data.
- Batch IP lookup (`LookupBatchAsync`) with per-entry results, transparent
  chunking beyond the 1024-address API limit, and bounded request concurrency.
- User-Agent parsing (`ParseUserAgentsAsync`) and the `UserAgents.IsBot` heuristic.
- Automatic retries with exponential backoff, `Retry-After` support, and
  configurable retry policies for 5xx and 429 responses.
- Optional response caching through `IIpregistryCache`, with a bundled
  thread-safe LRU + TTL `InMemoryIpregistryCache`.
- Typed error model: `IpregistryApiException` (with `IpregistryErrorCode`) and
  `IpregistryClientException`.
- Field selection, hostname resolution, and arbitrary query parameters via
  `LookupOptions`.
- ASP.NET Core integration: `services.AddIpregistry(...)` backed by
  `IHttpClientFactory`, with options validation.
- Multi-targeting for .NET 8 and .NET 10, nullable annotations, and
  trimming/Native AOT compatibility via source-generated JSON.

[Unreleased]: https://github.com/ipregistry/ipregistry-dotnet/compare/v2.0.0...HEAD
[2.0.0]: https://github.com/ipregistry/ipregistry-dotnet/releases/tag/v2.0.0
