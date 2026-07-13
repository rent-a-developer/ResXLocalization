# Contributing to ResXLocalization

Thank you for considering a contribution! This document explains how to propose changes and what a
pull request needs to be merged.

Please note that this project has a [Code of Conduct](CODE_OF_CONDUCT.md); by participating, you
agree to abide by it.

## Before you start

- **Bugs:** open an issue with a minimal reproduction (a small `.resx` + XAML/C# snippet is usually
  enough). If you can, say which package (`ResXLocalization.Avalonia` / `ResXLocalization.WPF`) and
  which lookup mode (typed / scoped / search-all / enum) is affected.
- **Features and larger changes:** open an issue first and describe what you want to change and why,
  so we can discuss the approach before you invest time in an implementation.
- **Small fixes** (typos, doc corrections, obvious one-liners) can go straight to a pull request.

## Development setup

You need the **.NET 10 SDK** (see `global.json`). Everything builds from the repository root:

```shell
# Windows: build everything.
dotnet build ResXLocalization.slnx -c Release

# Linux/macOS: build everything except the Windows-only WPF projects.
dotnet build ResXLocalization.NonWindows.slnf -c Release

# Run the test suites (the first three work on every OS).
dotnet test tests/ResXLocalization.Core.Tests/ResXLocalization.Core.Tests.csproj -c Release
dotnet test tests/ResXLocalization.SourceGenerators.Tests/ResXLocalization.SourceGenerators.Tests.csproj -c Release
dotnet test tests/ResXLocalization.Avalonia.Sample.Tests/ResXLocalization.Avalonia.Sample.Tests.csproj -c Release
dotnet test tests/ResXLocalization.WPF.Sample.Tests/ResXLocalization.WPF.Sample.Tests.csproj -c Release   # Windows only
```

Format the code before committing - CI enforces it:

```shell
scripts\formatCode.cmd    # Windows
scripts/formatCode.sh     # Linux/macOS
```

## Pull request checklist

1. **Zero warnings.** The build treats warnings as errors and runs a strict analyzer set
   (`AnalysisMode=All`, StyleCop, Roslynator, ErrorProne.NET). `dotnet build -c Release` must
   succeed cleanly.
2. **Tests pass - and new behavior is tested.** All four suites must be green (Core engine, source
   generator, Avalonia, WPF). Bug fixes should include a test that fails without the fix; features
   need coverage for the new behavior (both the Avalonia and the WPF side, if applicable - the two
   engines deliberately mirror each other).
3. **Keep the Avalonia/WPF symmetry.** A change to a markup extension, converter, or behavior in one
   UI package almost always needs the mirrored change in the other.
4. **Keep Native AOT support intact (Avalonia/Core).** No reflection over resources, no new
   `IL2026`/`IL3050` warnings. The WPF package is exempt (WPF does not support AOT).
5. **Declare public API changes.** The libraries track their public surface with
   [PublicApiAnalyzers](https://github.com/dotnet/roslyn/tree/main/src/RoslynAnalyzers/PublicApiAnalyzers):
   when you add or change public API, the build tells you exactly which line to add to the
   project's `PublicAPI.Unshipped.txt`. That file is part of the review. (On release, the maintainer
   promotes `PublicAPI.Unshipped.txt` entries into `PublicAPI.Shipped.txt`.)
6. **Update the documentation.** Public API changes need XML doc comments and, where user-facing, a
   matching update to `README.md`.
7. **Update `CHANGELOG.md`** under the *Unreleased* heading, following
   [Keep a Changelog](https://keepachangelog.com/). Do **not** bump version numbers - versioning
   ([SemVer](https://semver.org/)) and releases are handled by the maintainer.
8. **Match the existing code style.** It is enforced by `.editorconfig` on build; if the build is
   clean, the style is fine.

## A note on `String` vs. `string`

This codebase deliberately uses the BCL type names (`String`, `Int32`, `Boolean`, …) instead of the
C# keyword aliases - the maintainer strongly prefers seeing the actual type. This deviates from the
common C# convention, it is a conscious choice, and it is enforced by the build
(`dotnet_style_predefined_type_*` = error). Please follow it in contributions rather than debating
it in pull requests.

A maintainer will review your pull request, possibly request changes, and merge it once it is
approved and CI is green.

## Questions

Not sure about something? Open an issue or email
[info@rent-a-developer.de](mailto:info@rent-a-developer.de).
