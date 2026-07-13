# ResXLocalization

**Type-safe `.resx` localization for Avalonia and WPF - switch language _live_, write _zero_ csproj boilerplate.**

ResXLocalization takes the `.resx` files you already know and adds:

- ⚡ **Live, no-reload language switching** - set one property and every bound string updates in place.
- 🔒 **Type-safe, compile-checked keys** - a Roslyn source generator turns every `.resx` into strongly-typed keys, so a renamed or deleted resource becomes a **compile error**, not a runtime surprise.
- 🗂️ **First-class multiple `.resx` support** - look up by typed key, scope to one file, or search across all registered files.
- 🔤 **Enum localization built in** - localize enum members by convention.
- 🧮 **Format arguments** - `Get(key, args…)` formats in the active culture.
- 🩺 **Missing-translation diagnostics** - a visible, configurable `!key!` sentinel plus a `TranslationNotFound` event.
- 🌐 **Language-picker ready** - `GetAvailableCultures()` discovers the cultures your app actually ships.
- 🚀 **Native AOT & trim clean** (Avalonia package) - zero `IL2026`/`IL3050`, no reflection over your resources.
- 📦 **One package, zero csproj boilerplate** - runtime engine, source generator, and MSBuild wiring ship together.

Normal .NET parent/neutral resource fallback applies. `TranslationNotFound` is raised only when a key
is unresolved across the complete fallback chain, not when a satellite entry falls back successfully.

Typed keys are generated for string entries in dot-free neutral `.resx` files that have a same-folder
classic `.Designer.cs` accessor. Satellite files, non-string entries, files without that designer, and
SDK `GenerateResxSource` accessors are not eligible.

Malformed eligible files produce the `RXLGEN001` build diagnostic. Accessor namespaces and types are
read from C# syntax, including file-scoped/global namespaces and imported or aliased
`System.Resources.ResourceManager` types.

## Quick start

```xml
<!-- Strongly-typed key, generated from your .resx, updates live on every culture switch. -->
<TextBlock Text="{l:Localize {x:Static res:AppStringsKeys.Greeting}}" />
```

```csharp
// Register your resources once at startup …
Localizer.Current.RegisterResourceManager(AppStrings.ResourceManager);

// … then switch the whole UI to German - every bound string re-resolves in place, no reload.
Localizer.Current.CurrentCulture = new CultureInfo("de");
```

The XAML namespace to map:

```text
Avalonia:  xmlns:l="clr-namespace:RentADeveloper.ResXLocalization.Avalonia;assembly=ResXLocalization.Avalonia"
WPF:       xmlns:l="clr-namespace:RentADeveloper.ResXLocalization.WPF;assembly=ResXLocalization.WPF"
```

## Two packages, one engine

| Package | UI framework | Targets | Native AOT |
| ------- | ------------ | ------- | ---------- |
| `ResXLocalization.Avalonia` | Avalonia 12 | `net8.0` · `net10.0` | ✅ |
| `ResXLocalization.WPF` | WPF | `net8.0-windows` · `net10.0-windows` | ❌ (WPF limitation) |

Both share the same UI-agnostic engine (`ILocalizer` / `Localizer.Current`, `ResourceKey`), the same source generator, and the same MSBuild wiring.

## Documentation

The full documentation - quick start, lookup modes, enum localization, multiple `.resx` files, Native AOT publishing, troubleshooting, and complete runnable sample apps for both frameworks - lives in the [project README](https://github.com/rent-a-developer/ResXLocalization#readme); the [API reference](https://rent-a-developer.github.io/ResXLocalization/) documents every type.

## License

Released under the [MIT License](https://github.com/rent-a-developer/ResXLocalization/blob/main/LICENSE.md). © 2026 David Liebeherr.

Avalonia is a registered trademark of AvaloniaUI OÜ, used in the package name with written permission. This project is not affiliated with or endorsed by AvaloniaUI OÜ.
