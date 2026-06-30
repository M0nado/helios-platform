# HELIOS project and package roles

`src/core/HELIOS.Platform/HELIOS.Platform.csproj` is the canonical HELIOS product project and the only project that should publish the `HELIOS.Platform` NuGet package. It owns product package identity, description, tags, and packable resources.

`HELIOS.Platform.csproj` at the repository root is a legacy compatibility/aggregation project for Windows/WPF-oriented builds that still need the historical root project path. It is intentionally marked `IsPackable=false` and uses the `HELIOS.Platform.LegacyAggregator` package id to avoid competing with the canonical product package.

Repository-wide package metadata that is not product-specific lives in `Directory.Build.props`. Shared NuGet versions live in `Directory.Packages.props`; project files should use versionless `PackageReference` entries. The package version audit script and GitHub Actions workflow fail if a project reintroduces inline package versions or references a package that has no central version.
