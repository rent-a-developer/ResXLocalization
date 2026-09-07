# AGENTS.md

Machine-oriented guidance for this repository. [`CONTRIBUTING.md`](CONTRIBUTING.md) is the human document and
carries the rationale; this file is the commands, the constraints and the triggers.

Everything reusable lives in one place — see [`.agents/README.md`](.agents/README.md) for how the AI files fit
together.

## Project map

| Path | What it is |
| --- | --- |
| `src/ResXLocalization.Core` | The UI-agnostic engine: `ILocalizer`, `Localizer`, `ResourceKey`, the events. Both UI packages depend on it, and one copy satisfies both |
| `src/ResXLocalization.SourceGenerators` | The incremental generator that emits the typed `…Keys` classes. `netstandard2.0`. Ships **inside** the UI packages as an analyzer; it is not a package of its own |
| `src/ResXLocalization.Avalonia` | The Avalonia markup extensions and converter, plus the packaged generator and resx wiring |
| `src/ResXLocalization.WPF` | The same for WPF. Windows-only, no Native AOT |
| `samples/` | Two runnable showcases, one per UI framework. Excluded from coverage |
| `tests/` | Four suites: Core (both frameworks), the generator, and one sample-test suite per UI framework |
| `tests/package-consumption/` | Package-only consumers. The repository's `Directory.Build.*` deliberately does **not** apply there |
| `build/` | The shared resx and packaging targets, and the DocFX configuration |
| `scripts/` | Every check and workflow, as PowerShell 7 |

## Build and test

Requires the **.NET 10 SDK** (pinned in `global.json`). From the repository root:

```shell
# Build. Windows builds everything; Linux and macOS must use the filter, because WPF is Windows-only.
dotnet build ResXLocalization.slnx -c Release             # Windows
dotnet build ResXLocalization.NonWindows.slnf -c Release  # Linux/macOS

# Test. One command per solution; dotnet test discovers the suites and every target framework.
dotnet test ResXLocalization.slnx -c Release              # Windows
dotnet test ResXLocalization.NonWindows.slnf -c Release   # Linux/macOS, no WPF suite
```

Before committing:

```shell
pwsh -File scripts/pre-commit-gate.ps1        # checks; -Fix applies the tidying
```

The gate is the loop. It runs the public-API reminder, checks line endings, style, formatting and member
ordering, builds Release and runs the test suites. Before pushing a branch you want CI to go green on, run
`pwsh -File scripts/pre-release-gate.ps1`, which adds the documentation build, the pack, the Native AOT gate
and the package consumers.

## Code style, formatting and ordering

Four tools, one concern each; all of the C# ones are build errors, in `samples/` and `tests/` as much as in
`src/`. Apply them with `pwsh -File scripts/tidy-code.ps1 -Scope all`.

- **C# keywords, never BCL type names.** `string`, `int`, `bool`, `object?` — not `String`, `Int32`,
  `Boolean`, `Object?`.
- **`var` for every local declaration.** Two of the three cases are errors; the third, where nothing on the
  line names the type, is a judgement call and stays silent.
- **Always `this.`** for instance members, and no field begins with an underscore.
- **Expression bodies** wherever a member is a single expression; **file-scoped namespaces** with the usings
  outside; braces always.
- **Primary constructors**, with each parameter assigned to a `private readonly` field that members read
  through `this.`.
- **Line endings are LF everywhere**, in the repository and in the working tree. Never hand-convert them, and
  never compare a multi-line source literal against `Environment.NewLine`: the literal carries the file's
  bytes, `Environment.NewLine` carries the host's.
- **Two spaces** for `.csproj`, `.props`, `.targets`, `.slnx`, `.config`, `.xml` and `.json`.

`IDE0049` — the keyword-alias rule — is **not** reported by an ordinary build, and the member order is only
half-checked by the analyzers. Both details, and the XamlStyler settings that are switched off because they
change rendering rather than layout, are in
[`.agents/references/code-style.md`](.agents/references/code-style.md).

## The constraints that are not obvious

- **The generator's compiler floor is Roslyn 4.8.0**, set for that project in `Directory.Packages.props`. A
  generator built against a newer Roslyn does not load in an older compiler: `CS9057`, no generated keys, and
  a consumer build that fails on missing types. Every other Roslyn reference in the repository, including the
  generator's own test project, uses the current version.
- **The shipped Avalonia floor is 12.0.5**, also set per project in `Directory.Packages.props`. Avalonia
  12.1's XAML generator requires Roslyn 4.14, which the .NET 8 SDK cannot load. The samples and tests resolve
  12.1.0; the floor is a promise to consumers, not a stale version.
- **WPF re-invokes its own projects.** The markup compiler builds a temporary copy under a randomized
  `<Name>_<random>_wpftmp` name, and `_TargetAssemblyProjectName` carries the real one. The root
  `Directory.Build.props` resolves `_ConventionProjectName` from it, and every condition that has to hold in
  that pass tests that property rather than `MSBuildProjectName`.
- **Native AOT covers Core and Avalonia only.** WPF has no AOT support, and `src/Directory.Build.props`
  excludes it from `IsAotCompatible` deliberately.
- **The package consumers are outside everything.** `tests/package-consumption/` carries empty
  `Directory.Build.props` and `Directory.Build.targets` files that stop MSBuild's upward search, so those
  projects receive the library only from the packed packages. Do not add a project reference, a repository
  analyzer or a local generator import there.

## Triggers: when a change needs more than the gate

| Trigger | What to run | Reviewer |
| --- | --- | --- |
| One UI package changed | mirror it into the other, and mirror the tests | `ui_parity_reviewer` |
| Resource lookup, culture fallback, satellite discovery, the generator's output, or the packaging that carries them | `pwsh -File scripts/verify-package-aot.ps1 -Pack` | `aot_package_compat_reviewer` |
| The package contents, the `buildTransitive` wiring, or a dependency version | pack, then run each project under `tests/package-consumption` | — |

Nothing is trimmed on the just-in-time compiler, so **no test suite in this repository can see trimming
damage**. The AOT gate publishes a package-only consumer natively and runs it; that is the only check that
can. See [`docs/guides/native-aot.md`](docs/guides/native-aot.md).

Invoke a reviewer only when its scope applies. Both are read-only by construction — see
[`.agents/README.md`](.agents/README.md).

## Public API

The three runtime projects declare their public surface in `PublicAPI.Shipped.txt` and
`PublicAPI.Unshipped.txt`. An undeclared public member is `RS0016`; a declared one that is gone is `RS0017`.
Both are build errors.

```shell
pwsh -File scripts/update-public-api.ps1      # record the current surface in Unshipped
```

Review that diff line by line — a `*REMOVED*` entry is a break. The generator has no tracked public API; its
equivalent is analyzer release tracking in `AnalyzerReleases.*.md`.

**Never bump a version, and never date a changelog section.** The version in the repository-root
`Directory.Build.props`, the release date, promoting `Unshipped` to `Shipped`, and the tag are the
maintainer's, at release time.

## Commits and branches

[Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) with a lowercase, imperative summary:
`build: standardize repository tooling`. Types in use: `feat`, `fix`, `docs`, `test`, `build`, `ci`, `chore`.
A breaking change is `feat!:` or `fix!:` plus a `BREAKING CHANGE:` **footer** — never a `BREAKING CHANGE`
type.

Branches are `<type>/issue-<number>-<slug>`, with the types `feature`, `bugfix`, `hotfix`, `release` and
`chore`; drop the issue segment when there is no issue. The same policy applies to humans and to agents.

The [`commit`](.agents/skills/commit/SKILL.md) skill is explicit-invocation only: it inspects the diff, runs
the required checks, stages only what was authorized, and commits when asked. It never pushes.
