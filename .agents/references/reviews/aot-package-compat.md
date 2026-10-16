# Native AOT, trimming and packaging compatibility review

Use this whenever a change touches resource lookup, culture fallback, satellite discovery, the source
generator's output, or the analyzer and `buildTransitive` wiring the packages carry. This is a
**standing** checklist: `ResXLocalization.Avalonia` and `ResXLocalization.Core` advertise Native AOT
support, and everything here exists to keep it from regressing.

Read [docs/guides/native-aot.md](../../../docs/guides/native-aot.md) before reviewing.

This is a **review**: report findings, cite file and line, do not edit files.

## Why this checklist exists at all

Trimming damage does not announce itself. A resource that is no longer found, a satellite assembly
that is no longer loaded, a typed key whose `ResourceManager` was trimmed away — each produces a
binary that builds, starts, and answers with the `!key!` sentinel or the wrong language. Nothing is
trimmed on the just-in-time compiler, so **every test suite in this repository passes throughout**.

The only check that can see it is `scripts/verify-package-aot.ps1`, which publishes a package-only
consumer natively and runs it. A green test run is not evidence here. Neither is a native binary that
exists: the gate runs it and asserts exact strings for exactly this reason.

## Blocking findings

- **A new suppressed `IL2xxx` or `IL3xxx` anywhere in `src/`.** These diagnostics are the only
  build-time proof that a reflection path survives trimming; suppressing one voids it. Restructure
  instead. There are no sanctioned suppressions in this repository today — a first one is a finding
  that needs the maintainer, not a reviewer.

  `[RequiresUnreferencedCode]` and `[RequiresDynamicCode]` are **not** suppressions: they propagate
  the requirement to the caller. But adding either to a public member is its own blocking finding —
  it changes what a consumer sees when they publish, and the packages promise they see nothing.

- **`IsAotCompatible` removed from `ResXLocalization.Core` or `ResXLocalization.Avalonia`**, or added
  to `ResXLocalization.WPF`. The first two set it through `src/Directory.Build.props`; WPF is
  excluded there deliberately, because WPF has no Native AOT.

- **A new reflection call** — `Type.GetType`, `Assembly.Load`, `Activator.CreateInstance`,
  `GetMethod`, `GetProperty`, `MakeGenericType` — reachable from a public entry point without a
  `[DynamicallyAccessedMembers]` annotation covering exactly what it asks for. Grep for these; the
  library's own lookups go through `ResourceManager`, which is annotated by the framework, and a new
  reflection site is a change of kind rather than of degree.

- **Anything that makes a satellite assembly optional.** The German satellite is what the AOT gate
  asserts on: a change that stops the consumer producing `de/AvaloniaConsumer.resources.dll`, or
  stops the library finding it, is the exact failure this gate exists for.

## The generator's output

The typed key classes are the other half of the contract, and they are generated at build time rather
than published, so trimming cannot reach them — but packaging can drop them.

- **Exactly one generator DLL per UI package**, at `analyzers/dotnet/cs`. Two copies are NU5118 at
  pack time; zero means the consumer gets no typed keys and a build failure that names a missing type
  rather than a missing generator.
- **`buildTransitive/<PackageId>.targets`** must be present in each UI package, and must be the same
  `build/ResXLocalization.Resx.targets` the repository imports itself. That file is what hands each
  `.resx` and `.Designer.cs` to the generator; without it the generator runs and emits nothing.
- **The generator's Roslyn floor stays at 4.8.0**, set for that project in
  `Directory.Packages.props`. A generator built against a newer Roslyn does not load in an older
  compiler: `CS9057`, no generated keys, and a consumer build that fails on missing types. Raising
  the floor is a breaking change for every consumer on the .NET 8 or 9 SDK.
- **No generator or compiler dependency in a runtime nuspec.** The `ProjectReference` to the
  generator carries `PrivateAssets="all"` and `ReferenceOutputAssembly="false"` for that reason;
  losing either leaks `Microsoft.CodeAnalysis.CSharp` into the package's dependency list.

## The build wiring on every supported SDK

`build/ResXLocalization.Resx.targets` runs inside every consumer's build, on whichever SDK that consumer
has - the .NET 8 SDK as much as the one this repository pins - and inside WPF's temporary markup-compile
pass, a copy of the project named `<Name>_<random>_wpftmp` that WPF builds before the real one. Neither
is visible to a test suite here, which builds on the pinned SDK with the repository's own
`Directory.Build.props`.

- **Anything the wiring or the generator derives from a project property must hold in the temporary
  pass, on every supported SDK.** `MSBuildProjectName`, and every `RootNamespace` computed from it,
  differ there; the whole mechanism, the fix and its limits are explained in the comment "WPF's temporary markup-compile pass" in `build/ResXLocalization.Resx.targets`. The fix
  changes only `ManifestResourceName`, `LogicalName` and `CustomToolNamespace` of the targets file's own
  copies of the `.resx` items. A change that removes it, widens it to `RootNamespace` or to other items
  or metadata, or adds a new dependency on a property derived from the project name, is a finding.
- **A claim about what an SDK does is checked, not assumed.** Cite the targets file or the task of each
  supported SDK that the claim is about, with file and line - for example
  `sdk/<version>/Sdks/Microsoft.NET.Sdk.WindowsDesktop/targets/Microsoft.WinFX.targets`. A claim you
  could not check is reported as unchecked, never as a fact.
- **What catches it:** the "Package consumers (.NET 8 SDK)" step of `scripts/pre-release-gate.ps1`,
  and CI's "Consume packages on .NET 8 SDK" job, through `WpfConsumer` and `WpfDerivedNamespaceConsumer`
  (the second also on the pinned SDK).

## What the tooling cannot tell you

- **`TrimmerSingleWarn`.** Left at its default, ILC collapses every diagnostic from an assembly into
  one `IL2104` line and the individual codes never appear. A change that stops
  `verify-package-aot.ps1` passing `-p:TrimmerSingleWarn=false` turns the gate into a check that
  counts zero because it cannot see.
- **`SatelliteResourceLanguages`.** A consumer that does not declare it ships no satellites at all
  and silently renders the neutral language. The README and the AOT guide both say so; a change that
  removes that instruction is a documentation finding with a runtime consequence.
- **The package source mapping** in `tests/package-consumption/nuget.config`. It binds
  `ResXLocalization.*` to the local feed. Without it, a missing local package resolves the published
  package of the same version from nuget.org and the whole gate passes against the last release.

## What to report

For each finding: the file and line, what the diagnostic or the packaging consequence is, and which
check would have caught it. Where the answer needs a build, a pack or the AOT gate run, say so and
name the command:

```text
pwsh -File scripts/verify-package-aot.ps1 -Pack                     # both frameworks, from a fresh pack
pwsh -File scripts/verify-package-aot.ps1 -Framework net8.0         # the LTS floor alone
pwsh -File scripts/pre-release-gate.ps1 -SkipNativeAot -SkipDocumentation   # every consumer, pinned SDK and .NET 8 SDK
```

Do not run them yourself, and do not fix what you found.
