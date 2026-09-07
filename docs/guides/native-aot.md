# Native AOT and trimming

`ResXLocalization.Avalonia` and `ResXLocalization.Core` publish with `PublishAot=true` and produce no
IL2xxx or IL3xxx diagnostic — not from the library, not from a package in the closure, and not at
your own call sites. There is no `[RequiresUnreferencedCode]` or `[RequiresDynamicCode]` on any
public member, so nothing about a trimmed publish is a warning you have to read and dismiss.

**`ResXLocalization.WPF` is out of scope.** WPF is Windows-only and does not support Native AOT; that
is a WPF limitation, not one of this library.

## What you have to declare

One property, and it is the one that matters:

```xml
<PropertyGroup>
  <!-- The cultures you ship. Each one builds a satellite assembly. -->
  <SatelliteResourceLanguages>en;de</SatelliteResourceLanguages>
</PropertyGroup>
```

A satellite assembly is a separate file, and a publish that does not know a culture ships has no
reason to keep it. Without this property a trimmed application silently falls back to the neutral
resources for every culture — it starts, it renders, and it is in the wrong language.

Two more are worth setting for an Avalonia application in general:

```xml
<PropertyGroup>
  <!-- Lets the trim and AOT analyzers check your own code during an ordinary build. -->
  <IsAotCompatible>true</IsAotCompatible>

  <!-- Compiled bindings rather than reflection bindings. -->
  <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
</PropertyGroup>
```

## Publishing

```shell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishAot=true
```

Read the log rather than the exit code. If you want the individual diagnostics rather than one
collapsed line per assembly, add `-p:TrimmerSingleWarn=false`: left at its default, the compiler
reports "assembly produced trim warnings" and hides the codes inside it.

## Why the check runs the binary

Trimming damage does not announce itself. A resource that is no longer found, a satellite that is no
longer loaded, a typed key whose resource manager was trimmed away — each of them produces a binary
that builds, starts, and answers wrongly. Nothing is trimmed on the just-in-time compiler, so a test
suite passes throughout.

This repository's own gate therefore publishes a package-only consumer natively and **runs** it,
asserting exact strings before and after a culture change, through the typed, scoped and search-all
lookups, the enum convention, composite formatting, and the fallback to the neutral culture. See
`scripts/verify-package-aot.ps1`. A check that only asserts the executable exists proves that the
linker ran, and nothing else.

## If a lookup returns the sentinel only after publishing

In order of likelihood:

1. `SatelliteResourceLanguages` does not list the culture.
2. The publish output does not contain the culture's folder — check for `de/YourApp.resources.dll`
   next to the executable.
3. The key is missing from the neutral `.resx` as well, in which case it was never resolving and the
   just-in-time run was falling back to something you did not notice.
