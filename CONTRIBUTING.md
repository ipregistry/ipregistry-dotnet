# Contributing

Thanks for your interest in improving the Ipregistry .NET client!

## Prerequisites

- [.NET SDK 10](https://dotnet.microsoft.com/download) (the library also targets .NET 8; the SDK can build both).

## Building and testing

```bash
dotnet build Ipregistry.slnx
dotnet test tests/Ipregistry.Tests
```

Unit tests use a fake HTTP handler and never touch the network. System tests run against the live API, consume
credits, and require an API key:

```bash
IPREGISTRY_API_KEY=YOUR_API_KEY dotnet test tests/Ipregistry.SystemTests
```

## Code style

Formatting is enforced in CI. Before pushing, run:

```bash
dotnet format Ipregistry.slnx
```

## Pull requests

- Keep changes focused; one topic per pull request.
- Add or update tests for any behavior change.
- Update `CHANGELOG.md` under the `[Unreleased]` section.

## Releasing (maintainers)

1. Move the `[Unreleased]` notes into a new `## [X.Y.Z] - YYYY-MM-DD` section in `CHANGELOG.md` and merge to `main`.
2. Run the **Release** workflow from the Actions tab with the version number. It validates the version, runs unit and
   live system tests, pushes the package to NuGet, and creates the tag and GitHub release with the changelog notes.

Publishing uses [NuGet Trusted Publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing)
rather than a long-lived API key. One-time setup: on nuget.org, under **Trusted Publishing**, add a policy with
repository owner `ipregistry`, repository `ipregistry-dotnet`, and workflow file `release.yml`, then set the
`NUGET_USER` repository secret to the nuget.org profile name owning that policy.
