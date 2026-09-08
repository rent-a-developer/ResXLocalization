# ResXLocalization

Type-safe `.resx` localization for [Avalonia](https://avaloniaui.net/) and
[WPF](https://learn.microsoft.com/dotnet/desktop/wpf/) — switch language **live**, no reload, no
restart.

Start at the [repository README](https://github.com/rent-a-developer/ResXLocalization#readme) for
installation and a quick start. These pages are the full documentation.

## Guides

| Guide | What it covers |
| --- | --- |
| [Lookup modes and culture fallback](guides/lookup-and-fallback.md) | Typed, scoped and search-all lookups, registration order, the fallback chain, missing-translation diagnostics, dependency injection and subscription lifetime |
| [Localizing enum values](guides/enums.md) | The naming convention, item templates, the converter, custom prefixes and scoping |
| [Dynamic format arguments](guides/format-arguments.md) | `Get(key, args…)` and the `LocalizeArgs.Arg0`…`Arg8` attached properties |
| [Native AOT and trimming](guides/native-aot.md) | What to declare, how to publish, and why the repository's own gate runs the native binary |

## Reference

| Page | What it covers |
| --- | --- |
| [Generated keys](reference/generated-keys.md) | Which `.resx` files qualify, what becomes a key, the generator diagnostics, and the Roslyn floor |

## API reference

The generated reference covers the runtime assemblies:

| Namespace | Assembly | Contains |
| --- | --- | --- |
| [`RentADeveloper.ResXLocalization`](xref:RentADeveloper.ResXLocalization) | `ResXLocalization.Core` | `ILocalizer`, `Localizer`, `ResourceKey` — the shared engine |
| [`RentADeveloper.ResXLocalization.Avalonia`](xref:RentADeveloper.ResXLocalization.Avalonia) | `ResXLocalization.Avalonia` | The Avalonia markup extensions and converter |
| [`RentADeveloper.ResXLocalization.WPF`](xref:RentADeveloper.ResXLocalization.WPF) | `ResXLocalization.WPF` | The WPF markup extensions and converter |

## The packages

| Package | UI framework | Targets | Native AOT |
| --- | --- | --- | --- |
| `ResXLocalization.Avalonia` | Avalonia 12 | `net8.0`, `net10.0` | Supported |
| `ResXLocalization.WPF` | WPF | `net8.0-windows`, `net10.0-windows` | Not supported — a WPF limitation |
| `ResXLocalization.Core` | none | `net8.0`, `net10.0` | Supported |

Install the package for your UI framework; it brings the engine, the source generator and the build
wiring with it. Core arrives as a dependency, and one copy of it satisfies both UI packages in an
application that uses them together.

## Project

- [Change log](../CHANGELOG.md)
- [Contributing](../CONTRIBUTING.md)
- [Code of conduct](../CODE_OF_CONDUCT.md)
- [Security policy](../SECURITY.md)
