# AGENTS.md

Machine-oriented guide for AI-assisted contributors. This distills [`CONTRIBUTING.md`](CONTRIBUTING.md)
into the conventions that are easy to get wrong; read `CONTRIBUTING.md` for the full rationale.

## Build & test

Requires the **.NET 10 SDK** (pinned in `global.json`). From the repository root:

```shell
# Build. Windows builds everything; Linux/macOS must use the filter (WPF is Windows-only).
dotnet build ResXLocalization.slnx -c Release             # Windows
dotnet build ResXLocalization.NonWindows.slnf -c Release  # Linux/macOS

# Test. The first three run on every OS; the WPF suite is Windows-only.
dotnet test tests/ResXLocalization.Core.Tests/ResXLocalization.Core.Tests.csproj -c Release
dotnet test tests/ResXLocalization.SourceGenerators.Tests/ResXLocalization.SourceGenerators.Tests.csproj -c Release
dotnet test tests/ResXLocalization.Avalonia.Sample.Tests/ResXLocalization.Avalonia.Sample.Tests.csproj -c Release
dotnet test tests/ResXLocalization.WPF.Sample.Tests/ResXLocalization.WPF.Sample.Tests.csproj -c Release   # Windows only

# Format before committing (CI fails otherwise).
scripts\formatCode.cmd   # Windows
scripts/formatCode.sh    # Linux/macOS
```

## Conventions the build enforces (get these right or CI fails)

- **BCL type names, not keyword aliases.** Write `String`, `Int32`, `Boolean`, `Object` - never
  `string`, `int`, `bool`, `object`. The whole codebase does this deliberately; `SA1121` is disabled
  to allow it.
- **Zero warnings.** `TreatWarningsAsErrors` + `AnalysisMode=All` + StyleCop + Roslynator +
  ErrorProne.NET. A warning is a build failure. Match the surrounding style; `.editorconfig` decides.
- **Avalonia/WPF symmetry.** `src/ResXLocalization.Avalonia` and `src/ResXLocalization.WPF` mirror
  each other (markup extensions, converters, behaviors). A change to one almost always needs the
  mirrored change in the other, plus mirrored tests.
- **Native AOT stays intact (Avalonia + Core).** No reflection over resources; no new `IL2026`/
  `IL3050`. The WPF package is exempt (WPF has no AOT).
- **Public API tracking.** When you add or change public API, the `PublicApiAnalyzers` build error
  tells you the exact line to add to that project's `PublicAPI.Unshipped.txt`. Add it. (On release the
  maintainer promotes `Unshipped` → `Shipped`.)
- **Docs + changelog.** Public API changes need XML doc comments; user-facing changes need a
  `README.md` update and a `CHANGELOG.md` entry under *Unreleased*. Do **not** bump version numbers.

## Repo shape

- `src/ResXLocalization.Core` - the ambient `Localizer` engine (`ILocalizer`, `ResourceKey`, events).
- `src/ResXLocalization.SourceGenerators` - incremental generator emitting typed `…Keys` classes from
  `.resx`. Targets `netstandard2.0`; pinned to Microsoft.CodeAnalysis **4.8.0** so it loads in the
  .NET 8/9 SDK compilers. Keep incremental-pipeline models value-equatable (see `EquatableArray.cs`).
- `src/ResXLocalization.Avalonia` / `src/ResXLocalization.WPF` - the two UI packages, each bundling
  Core + the generator.
- `samples/` - demonstration apps (excluded from coverage).
- `tests/package-consumption` - E2E consumers that get everything from the packed NuGet packages;
  the repo `Directory.Build.*` deliberately does not apply there.
