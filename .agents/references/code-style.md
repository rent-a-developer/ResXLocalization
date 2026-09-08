# Code style details

Background for the rules in [AGENTS.md](../../AGENTS.md#code-style-formatting-and-ordering). Read this when a
tool does something you did not expect, or when you are about to write a type name in a place the build does
not check.

## The concerns, and the tool that owns each

| Concern | Tool | Configured in |
| --- | --- | --- |
| C# formatting — whitespace, line breaks, wrapping | **CSharpier** | `.editorconfig` (`max_line_length`, `indent_size`) |
| C# style — `var`, `=>`, `this.`, null checks, usings | **Roslyn analyzers** | `.editorconfig` |
| C# ordering — types and their members | **ReSharper** applies it, **NewStyleCop** checks *part* of it | `ResXLocalization.slnx.DotSettings` and `stylecop.json` |
| XAML and AXAML layout | **XamlStyler** | `Settings.XamlStyler` |

The word *part* is load-bearing. StyleCop checks kind, access, constant, static and readonly — `SA1201`,
`SA1202`, `SA1203`, `SA1204`, `SA1214`. It has no notion of alphabetical order **within** one of those
groups, which the ReSharper file layout applies and nothing checks. A member that is in the right group but
the wrong place inside it compiles, passes the analyzers, and is only visible by running the pipeline and
looking at what it moves.

Each tool owns its concern completely, and the C# ones are build errors rather than warnings, in `samples/`
and `tests/` as much as in `src/`. Two different mechanisms, both in the repository-root
`Directory.Build.props`: `EnforceCodeStyleInBuild=true` with `TreatWarningsAsErrors=true` covers style and
ordering, and the `CSharpier.MsBuild` package covers formatting. It runs in check mode, so a build never
rewrites your files — an unformatted file fails the build and names itself. `-p:CSharpier_Bypass=true` skips
it.

Run them all with one command:

```powershell
pwsh -File scripts/tidy-code.ps1 -Scope all
```

## Where the build misses a BCL type name

The rule is: write `string`, `object?`, `int`, `bool`, `nint`, `nuint` — never `String`, `Object?`, `Int32`,
`Boolean`, `IntPtr`, `UIntPtr`. `dotnet_style_predefined_type_for_*` is `true:error`, and `IDE0049` is the
diagnostic.

**`IDE0049` is not reported by an ordinary build.** Measured on this repository: with
`EnforceCodeStyleInBuild=true` the build is green while
`dotnet format style --diagnostics IDE0049 --severity error` reports every occurrence. So the style pipeline
is what enforces this rule, not the compiler — which is why CI runs `tidy-code.ps1 -Check` rather than
relying on the build alone, and why an editor that only builds will not tell you.

Two further blind spots, both in the analyzer itself:

- **`nint`/`nuint` are not on its list.** They arrived in C# 9 as their own feature and only became aliases
  for `IntPtr`/`UIntPtr` in C# 11; the analyzer was never extended. `IntPtr` is invisible to it.
- **It never looks inside `nameof(...)`.** With good reason: `nameof(int)` does not compile at all, so a
  blanket skip is the safe choice. Where `nameof` names a CLR type on purpose, leave it and say why in a
  comment.

A third gap is not the analyzer's fault: **`tests/package-consumption/` is outside the solution**, and its
deliberately empty `Directory.Build.props` gives it no style gate at all. CSharpier and XamlStyler still
format those files — neither needs a project — but the style rules there are on the author.

## `var`, in all three cases

```ini
csharp_style_var_for_built_in_types = true:error
csharp_style_var_when_type_is_apparent = true:error
csharp_style_var_elsewhere = true:silent
```

The third one is silent on purpose. Every local declaration under `src/` already uses `var` — 69 of them,
none with an explicit type — so the preference matches what the code does; it stays silent because a
declaration whose type nothing on the line reveals is a readability judgement rather than something a build
should reject.

## Primary constructor parameters

A primary constructor parameter is assigned to a `private readonly` backing field, and members read
`this.field` rather than the parameter. Nothing enforces it — a parameter is not an instance member, so
`dotnet_style_qualification_for_field` cannot see it — but a captured parameter compiles to a field with no
`readonly`, and using one directly silently drops the guarantee that it cannot be reassigned.

## Member order, in one place

```text
constants, fields, constructors, finalizers, delegates, events, enums, interfaces, properties, indexers,
conversion operators, operators, methods, nested structs and classes
```

Within a group: public, internal, protected internal, protected, private protected, private; static before
instance; readonly before mutable; then alphabetical by name.

Explicit interface implementations are the exception worth knowing. ReSharper ranks one below private,
because in C# it carries no access modifier. StyleCop counts an explicit property, indexer or method as
public and wants it first in its group — but counts an explicit **event** as private. That is why
`ResXLocalization.slnx.DotSettings` has one "Explicit interface …" entry per kind and none for events:
giving events one puts an explicit event ahead of a public one and breaks `SA1202`.

## XAML

XamlStyler owns `.xaml` and `.axaml`, and CSharpier is kept away from them by `.csharpierignore`.

Three of its settings are switched **off**, and they are not formatting: `ReorderGridChildren` and
`ReorderCanvasChildren` change which child is drawn on top when two overlap, and `ReorderSetters` changes
which setter wins when a style sets the same property twice. Movement that changes what the user sees is not
formatting.

XamlStyler writes the host operating system's newline and has no setting for it, so on Windows every file it
touches comes back CRLF. `scripts/tidy-code.ps1` normalizes exactly the files it processed, byte by byte, and
stages nothing.

## XML and configuration

Two-space indentation for `.csproj`, `.props`, `.targets`, `.slnx`, `.config`, `.xml` and `.json`. Nothing in
the tidy pipeline applies it — CSharpier is kept away by `.csharpierignore`, `dotnet format` does not touch
XML whitespace, and the ReSharper profile only reorders C# members — so it is an editor rule, and
`.editorconfig` is where it is stated.
