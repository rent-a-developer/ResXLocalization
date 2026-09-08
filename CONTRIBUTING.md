# Contributing to ResXLocalization

Thank you for considering a contribution! This document explains how to propose changes and what a pull
request needs to be merged.

This project has a [Code of Conduct](CODE_OF_CONDUCT.md); by participating, you agree to abide by it.

## Before you start

- **Bugs:** open an issue with a minimal reproduction — usually a small `.resx` entry plus the XAML or C#
  lookup, and the culture you switched to. Say which package and which lookup mode is affected; the issue
  form asks for both.
- **Features and larger changes:** open an issue first and describe what you want to change and why, so the
  approach can be discussed before you invest time in an implementation.
- **Small fixes** — typos, documentation corrections, obvious one-liners — can go straight to a pull request.

## Prerequisites

| You need | For |
| --- | --- |
| **.NET 10 SDK** | Everything. It is pinned in `global.json` |
| **.NET 8 SDK** | Running the Core tests on `net8.0`, and reproducing the minimum-SDK consumer checks |
| **PowerShell 7** (`pwsh`) | Every script in `scripts/`. They declare `#requires -Version 7.0` and stop on the first line under an older host |
| **Windows** | The WPF projects, the WPF tests, and the full package and documentation build |
| **A C++ toolchain** | The Native AOT gate only. MSVC and the Windows SDK on Windows; `clang` and `zlib1g-dev` on Linux |

Restore the local tools once per clone:

```shell
dotnet tool restore --configfile NuGet.config
```

That installs CSharpier, DocFX, the ReSharper command-line tools and XamlStyler. **No script in this
repository installs a tool for you** — a formatter that installs software behind your back is a worse problem
than an unformatted file.

## Setting up

```shell
# Windows: build everything.
dotnet build ResXLocalization.slnx -c Release

# Linux/macOS: build everything except the Windows-only WPF projects.
dotnet build ResXLocalization.NonWindows.slnf -c Release
```

```shell
# Test. One command; dotnet test discovers the suites and every target framework, so the Core suite
# runs on net8.0 and net10.0 from here.
dotnet test ResXLocalization.slnx -c Release              # Windows
dotnet test ResXLocalization.NonWindows.slnf -c Release   # Linux/macOS, without the WPF suite
```

There are four suites: the Core engine (on both target frameworks), the source generator, and one
sample-driven suite per UI framework. The Avalonia suite runs headless; the WPF suite drives a real
dispatcher on its own thread and is Windows-only.

### One optional per-clone setting

```shell
git config --local blame.ignoreRevsFile .git-blame-ignore-revs
```

`.git-blame-ignore-revs` lists the repository-wide mechanical commits — a line-ending renormalization, a
formatting pass — so that `git blame` attributes a line to the change that wrote it rather than to the tool
that reformatted it. GitHub applies the file automatically; your local git does not until you tell it to.
It is optional, it is per clone, and no script sets it for you.

## The two gates

The names say when to run them.

```shell
pwsh -File scripts/pre-commit-gate.ps1        # before every commit
pwsh -File scripts/pre-release-gate.ps1       # before a push you want CI to go green on
```

**`pre-commit-gate.ps1`** is the per-commit loop: the public-API reminder, line endings, the full style,
formatting and ordering check, a Release build, and the test suites. By default it **writes nothing but build
output** — it does not edit your source files and it does not touch the git index; the tidiness step runs the
tools on a disposable copy of the tree and prints the diff that would fix it.

Pass `-Fix` to have it tidy your working tree first, then review what changed and include it in your commit.

Two checks are deliberately left out of it, because each takes minutes and neither applies to every change.
The script names them, with their trigger and their command, in its own summary output:

| Run it when the change touches | Command |
| --- | --- |
| Resource lookup, culture fallback, satellite discovery, the generated keys, or the packaging that carries them | `pwsh -File scripts/verify-package-aot.ps1 -Pack` |
| What the packages contain, the `buildTransitive` wiring, or a dependency version | `pwsh -File scripts/pre-release-gate.ps1 -SkipNativeAot -SkipDocumentation`, which packs and then runs every consumer, on both of AvaloniaConsumer's target frameworks |

**`pre-release-gate.ps1`** is everything CI checks that can honestly be checked on your machine, in CI's
order, stopping at the first failure: the ignored-revision check, line endings, tidiness, a Release build,
every test suite, the DocFX metadata and site build with `--warningsAsErrors`, the pack with package
validation, each Native AOT leg this host can run, and the package consumers against the packages just
packed. It ends in one line: `PASSED: All checks passed.` or `FAILED: Check <name> failed. See output.`

Its comment-based help lists every CI job it does **not** reproduce, and why. Read that before treating a
green run as a promise that CI will be green.

`-SkipNativeAot`, `-SkipConsumers` and `-SkipDocumentation` cover the checks with heavy prerequisites.

## Checking versus fixing

Every check in this repository reports by default and fixes only when asked. That is deliberate: a check that
rewrites your tree makes it impossible to tell what you wrote from what a tool wrote.

```shell
pwsh -File scripts/tidy-code.ps1                  # format the files git reports as changed
pwsh -File scripts/tidy-code.ps1 -Scope all       # style, then member ordering, then formatting, everywhere
pwsh -File scripts/tidy-code.ps1 -Scope all -Check # report only - writes nothing, touches no index
```

`-Check` never writes to your working tree at any scope. Where a tool has no verify mode — XamlStyler's
passive check rejects every LF file on Windows, and ReSharper has none at all — the tools run for real on a
disposable copy outside the repository and the diff from there is what you see.

## Style

Four tools, one concern each, and the C# ones are build errors rather than warnings:

| Concern | Tool |
| --- | --- |
| C# formatting | CSharpier |
| C# style | the Roslyn analyzers, through `dotnet format style` |
| C# member ordering | ReSharper applies it; NewStyleCop checks part of it |
| XAML and AXAML layout | XamlStyler |

Write `string`, not `String`. Use `var` for locals. Qualify instance members with `this.`. Use expression
bodies for single-expression members, file-scoped namespaces with the usings outside, and braces always.
Fields never begin with an underscore.

If the build is clean and `tidy-code.ps1 -Scope all -Check` passes, the style is fine. The details a tool
cannot tell you are in
[`.agents/references/code-style.md`](https://github.com/rent-a-developer/ResXLocalization/blob/main/.agents/references/code-style.md)
— an absolute link, because the documentation site publishes this page and not that one.

## Line endings

Every text file is LF, in the repository and in the working tree, on every OS. `.gitattributes` enforces this
whatever your `core.autocrlf` is set to, so there is nothing to configure, and CI fails if a wrongly stored
file lands anyway.

XamlStyler cannot write LF on Windows — it always writes the host newline — so `scripts/tidy-code.ps1`
rewrites exactly the XAML files it processed back to LF afterwards.

If some other tool writes CRLF, git still stores LF, but `git status` lists the file as modified while
`git diff` shows nothing. `pwsh -File scripts/verify-line-endings.ps1` reports both what git stored and what
is on disk, and says what fixes each. If you have set `core.safecrlf true`, git refuses to add such a file
with "CRLF would be replaced by LF"; run the tidy script first.

## Avalonia and WPF symmetry

`src/ResXLocalization.Avalonia` and `src/ResXLocalization.WPF` are deliberate mirrors: the same markup
extensions, the same converter, the same attached properties, the same names and defaults. A change to one
almost always needs the mirrored change in the other, and mirrored tests.

Where the frameworks genuinely differ the difference is expected to be local and explained — Avalonia binds
through an observable and weak events, WPF through a `MultiBinding` and its own weak binding-target
references, and WPF has no Native AOT. None of those is a reason for a different public API.

Anything that is not framework-specific belongs in `ResXLocalization.Core`, where both packages share it.

## Native AOT

`ResXLocalization.Avalonia` and `ResXLocalization.Core` publish with `PublishAot=true` and produce no IL2xxx
or IL3xxx diagnostic, from anywhere. Keep it that way: no reflection over resources, no new suppressed IL
diagnostic, and no `[RequiresUnreferencedCode]` or `[RequiresDynamicCode]` on a public member.

**Nothing is trimmed on the just-in-time compiler, so no test suite here can see trimming damage.** A
satellite that is no longer loaded or a resource name that no longer resolves produces a binary that builds,
starts and answers wrongly. `scripts/verify-package-aot.ps1` publishes a package-only consumer natively and
**runs** it, asserting exact strings; that is the only check that can see it.

The WPF package is exempt — WPF does not support Native AOT.

## Public API

The three runtime projects track their public surface with
[PublicApiAnalyzers](https://github.com/dotnet/roslyn/tree/main/src/RoslynAnalyzers/PublicApiAnalyzers). An
undeclared public member is `RS0016` and a declared one that is gone is `RS0017` — both build errors here.

```shell
pwsh -File scripts/update-public-api.ps1
```

That writes the missing entries into the project's `PublicAPI.Unshipped.txt`. **Review that diff line by
line**: it *is* the public-API change, and an entry starting with `*REMOVED*` is a break.

A public-surface change also needs XML documentation on the new members, the affected pages under `docs/`
updated, and a `CHANGELOG.md` entry.

**Do not bump a version.** The version in the repository-root `Directory.Build.props`, the release date,
promoting `Unshipped` to `Shipped`, and the tag are all the maintainer's, at release time. Describing the
change accurately under `## [Unreleased]` is what lets them choose the number.

## Commits and branches

[Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/), with a **lowercase, imperative**
summary:

```text
feat: add dynamic format arguments to the localize extension
fix: stop the enum converter caching the previous culture
build: standardize repository tooling
```

Types in use: `feat`, `fix`, `docs`, `test`, `build`, `ci`, `chore`. A breaking change is `feat!:` or `fix!:`
plus a `BREAKING CHANGE:` **footer** saying what breaks and what to do about it — `BREAKING CHANGE` is a
footer, never a type.

### Branches

```text
<type>/issue-<number>-<slug>       feature/issue-42-scoped-enum-lookup
<type>/<slug>                      chore/tidy-sample-resources
```

Types: `feature`, `bugfix`, `hotfix`, `release`, `chore`. Omit the issue segment when there is no issue. The
same policy applies to people and to AI agents.

## Changelog

`CHANGELOG.md` follows [Keep a Changelog](https://keepachangelog.com/). Add your entry under
`## [Unreleased]`, in the right category (`### Added`, `### Changed`, `### Fixed`, …). Write a breaking
change as `- **BREAKING:** …`.

Internal formatting and tooling work needs no entry: nothing about it reaches a consumer.

## Pull request checklist

The template in the repository states this as a conditional list. In short:

1. `pwsh -File scripts/pre-commit-gate.ps1` passes.
2. The Release build produces zero warnings.
3. New behaviour and fixed bugs are covered by tests, on both UI sides where both apply.
4. A change to one UI package is mirrored into the other, or the description says why not.
5. Public API changes are declared and reviewed; documentation and the changelog are updated.
6. The branch name follows the pattern above.

A maintainer will review your pull request, possibly request changes, and merge it once it is approved and CI
is green.

## For the maintainer: releasing

1. Land everything for the release on `main`, with the changelog entries under `## [Unreleased]`.
2. Move those entries into a new `## [x.y.z] - YYYY-MM-DD` section and add the link reference definition.
3. Set `<Version>` in the repository-root `Directory.Build.props` to the same number, and set
   `PackageValidationBaselineVersion` in `src/Directory.Build.props` to the version being replaced.
4. `pwsh -File scripts/update-public-api.ps1 -MarkShipped` — folds `Unshipped` into `Shipped` for each
   runtime project.
5. `pwsh -File scripts/pre-release-gate.ps1 -Version x.y.z`, on Windows. The `-Version` switch adds the two
   checks CI runs immediately before it publishes: that the declared version matches, and that the changelog
   holds exactly one dated, non-empty section for it.
6. Commit, then push the tag `vx.y.z`. Pushing the tag is what publishes: CI packs, verifies every gate,
   pushes to NuGet.org and creates the GitHub release from the changelog section.

Nothing about a release happens on a branch push, and nothing in this repository publishes anything locally.

## Questions

Not sure about something? Open an issue or email
[info@rent-a-developer.de](mailto:info@rent-a-developer.de).
