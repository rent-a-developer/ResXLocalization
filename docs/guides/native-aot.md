# Native AOT and trimming

`ResXLocalization.Avalonia`, `ResXLocalization.Core` and `ResXLocalization.MessageFormat` publish with
`PublishAot=true` and produce no IL2xxx or IL3xxx diagnostic — not from the library, not from a package
in the closure, and not at your own call sites. There is no `[RequiresUnreferencedCode]` or `[RequiresDynamicCode]` on any
public member, so nothing about a trimmed publish is a warning you have to read and dismiss.

One annotation reaches your code: `MessageArguments.FromObject<T>` reads the public properties of `T`,
which carries `[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]`. Calling it
with a concrete type, such as an anonymous object, needs nothing. A generic method of your own that
forwards its type parameter to `FromObject` must carry the same annotation on that parameter, or the
trimmer reports IL2091 at that call.

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

Format arguments in XAML are trim safe when their bindings are compiled bindings: in Avalonia,
`Value="{Binding ...}"` of an `<l:Argument>` is a compiled binding under
`AvaloniaUseCompiledBindingsByDefault` or `x:CompileBindings="True"`, and a reflection binding
otherwise. See [Dynamic format arguments](format-arguments.md#worth-knowing).

## ICU MessageFormat

The opt-in package `ResXLocalization.MessageFormat` adds about 160 KB to a Native AOT binary, the
`MessageFormat` library included. It produces no IL diagnostic on `net8.0` or `net10.0`, because it
calls only the `IReadOnlyDictionary` overload of the library's `FormatMessage`; the library's overloads
that take an `object` read the properties of that object by reflection and are not called. See
[ICU MessageFormat](message-format.md).

`InvariantGlobalization=true` is often recommended for a small Native AOT binary, but it leaves a
localized application with the invariant culture only — with `PredefinedCulturesOnly=false` as well,
with named cultures whose numbers and dates format invariantly. See
[Globalization-invariant mode](message-format.md#globalization-invariant-mode).

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

This repository's own gate therefore publishes two package-only consumers natively and **runs** them,
asserting exact strings before and after a culture change. The first covers the typed, scoped and
search-all lookups, the enum convention, composite formatting, named arguments built by reflection with
`MessageArguments.FromObject`, and the fallback to the neutral culture. The second covers the ICU
formatter: `plural` patterns with a named argument from a compiled binding, in code and in XAML, and
the invariant culture formatted with English plural rules. See `scripts/verify-package-aot.ps1`. A check that only asserts the executable exists proves that the
linker ran, and nothing else.

## If a lookup returns the failure text only after publishing

In order of likelihood:

1. `SatelliteResourceLanguages` does not list the culture.
2. The publish output does not contain the culture's folder — check for `de/YourApp.resources.dll`
   next to the executable.
3. The key is missing from the neutral `.resx` as well, in which case it was never resolving and the
   just-in-time run was falling back to something you did not notice.
