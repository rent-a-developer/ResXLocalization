<div align="center">

![logo](assets/logo-40.png)

# ResXLocalization

**Type-safe `.resx` localization for [Avalonia](https://avaloniaui.net/) and [WPF](https://learn.microsoft.com/dotnet/desktop/wpf/) - switch language _live_, no reload, no restart.**

[![CI](https://github.com/rent-a-developer/ResXLocalization/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/rent-a-developer/ResXLocalization/actions/workflows/ci.yml)
[![Coverage](https://codecov.io/gh/rent-a-developer/ResXLocalization/branch/main/graph/badge.svg)](https://codecov.io/gh/rent-a-developer/ResXLocalization)
[![NuGet: Avalonia](https://img.shields.io/nuget/v/ResXLocalization.Avalonia?logo=nuget&label=ResXLocalization.Avalonia)](https://www.nuget.org/packages/ResXLocalization.Avalonia)
[![NuGet: WPF](https://img.shields.io/nuget/v/ResXLocalization.WPF?logo=nuget&label=ResXLocalization.WPF)](https://www.nuget.org/packages/ResXLocalization.WPF)
[![.NET 8 | 10](https://img.shields.io/badge/.NET-8.0%20%7C%2010.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Native AOT (Avalonia)](https://img.shields.io/badge/Native%20AOT-Avalonia-2ea44f)](https://learn.microsoft.com/dotnet/core/deploying/native-aot/)
[![API docs](https://img.shields.io/badge/API%20docs-GitHub%20Pages-2b6cb0)](https://rent-a-developer.github.io/ResXLocalization/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE.md)

</div>

---

Localizing a XAML app with plain `.resx` usually means string-typed keys that break silently at
runtime, and a language change that only takes effect after a restart or window reload.
ResXLocalization fixes both: you keep the `.resx` files and editors you already use, and get
**compile-checked keys** (a renamed or deleted resource becomes a build error, not a runtime
surprise) and **instant, in-place language switching** - in **Avalonia** and **WPF** alike.

```xml
<!-- Strongly-typed key, generated from your .resx, updates live on every culture switch. -->
<TextBlock Text="{l:Localize {x:Static res:AppStringsKeys.Greeting}}" />
```

```csharp
// Switch the whole UI to German - every bound string re-resolves in place, no reload.
Localizer.Current.CurrentCulture = new CultureInfo("de");
```

<div align="center">
  <img src="assets/sample.gif" width="760"
       alt="The ResXLocalization sample app: a showcase window with a language selector at the top and, in each row, a XAML markup form on the left and its live-rendered localized string on the right." />
  <p><sub>The included sample apps (<a href="samples/ResXLocalization.Avalonia.Sample">Avalonia</a> · <a href="samples/ResXLocalization.WPF.Sample">WPF</a>) - pick a language and every binding updates live, no reload.</sub></p>
</div>

## Highlights

- ⚡ **Live, no-reload language switching** - set one property and every bound string updates in place. No window reload, no view rebuild, no restart.
- 🔒 **Type-safe, compile-checked keys** - a source generator turns every `.resx` into strongly-typed keys with full IntelliSense.
- 📦 **Zero configuration** - install one NuGet package and build; typed keys are generated automatically, nothing else to set up.
- 🚀 **Native AOT & trim clean (Avalonia)** - no reflection over your resources; publishes with `PublishAot=true` out of the box. *(WPF is Windows-only and does not support Native AOT.)*
- 🗂️ **First-class multiple `.resx`** - look up by **typed key**, **scope** to one file, or **search across all** registered files in a defined order.
- 🔤 **Enum localization built in** - localize enum members by naming convention, in item templates and as bound values. See [Localizing enums](#localizing-enums).
- 🧮 **Format arguments** - `Get(key, args…)` formats the translation in the active culture.
- 🩺 **Missing-translation diagnostics** - a visible `!key!` sentinel (configurable) plus a `TranslationNotFound` event for logging and coverage reports.
- 🌐 **Language-picker ready** - `GetAvailableCultures()` discovers the cultures your app actually ships.
- 🧩 **MVVM-friendly** - an injectable `ILocalizer` service with `INotifyPropertyChanged`, markup extensions for XAML, and a clean code-behind API.
- 🪶 **Leak-safe** - discarded controls stay collectable (Avalonia uses weak events; WPF binds to the singleton through WPF's own weak binding-target references).

---

## Table of Contents

- [Requirements](#requirements)
- [Installation](#installation)
- [Quick start](#quick-start)
- [Localizing enums](#localizing-enums)
- [Guides and API reference](#guides-and-api-reference)
- [Troubleshooting](#troubleshooting)
- [The sample applications](#the-sample-applications)
- [Building from source](#building-from-source)
- [Versioning](#versioning)
- [Contributing](#contributing)
- [License](#license)
- [Author](#author)

---

## Requirements

- An app targeting **.NET 8** or later, built with the **.NET 8 SDK or later**.
- For **Avalonia**: **Avalonia 12**. For **WPF**: **Windows**.
- **`.resx`** resource files with the standard sibling `*.Designer.cs` accessor, as generated by
  Visual Studio's or Rider's classic resx tooling. SDK-only `GenerateResxSource` accessors are not
  eligible; see [Troubleshooting](#troubleshooting).

## Installation

Install the package for your UI framework:

```shell
# Avalonia
dotnet add package ResXLocalization.Avalonia

# WPF
dotnet add package ResXLocalization.WPF
```

|                | [`ResXLocalization.Avalonia`](https://www.nuget.org/packages/ResXLocalization.Avalonia) | [`ResXLocalization.WPF`](https://www.nuget.org/packages/ResXLocalization.WPF) |
| -------------- | ---------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------ |
| **Targets**    | `net8.0` · `net10.0`                                                                     | `net8.0-windows` · `net10.0-windows`                                           |
| **Platforms**  | cross-platform (Avalonia 12)                                                             | Windows only                                                                   |
| **Native AOT** | ✅ Fully supported                                                                        | ❌ Not supported (WPF limitation)                                               |

That's it - the source generator and the build wiring for your `.resx` files are included; there is
nothing else to configure.

A few project properties are recommended. For **Avalonia** (required if you publish with Native AOT):

```xml
<PropertyGroup>
  <!-- The cultures you ship. Each one builds a satellite assembly; required for AOT. -->
  <SatelliteResourceLanguages>en;de</SatelliteResourceLanguages>

  <!-- Lets the analyzers verify AOT-safety on a normal build. -->
  <IsAotCompatible>true</IsAotCompatible>

  <!-- Recommended for Avalonia apps in general. -->
  <AvaloniaUseCompiledBindingsByDefault>true</AvaloniaUseCompiledBindingsByDefault>
</PropertyGroup>
```

For **WPF**, only the cultures you ship are worth declaring (WPF does not support AOT):

```xml
<PropertyGroup>
  <SatelliteResourceLanguages>en;de</SatelliteResourceLanguages>
</PropertyGroup>
```

## Quick start

### 1. Add your strings

Add a `.resx` file the normal way - for example `Resources/AppStrings.resx` (your neutral/default language) - and a satellite file per culture, e.g. `Resources/AppStrings.de.resx`:

| Key           | `AppStrings.resx` (English) | `AppStrings.de.resx` (German) |
| ------------- | --------------------------- | ----------------------------- |
| `WindowTitle` | `My Application`            | `Meine Anwendung`             |
| `Greeting`    | `Hello and welcome!`        | `Hallo und willkommen!`       |

### 2. Let the generator create typed keys

On every build, the source generator inspects each `.resx` and emits a typed key class named `<FileName>Keys` in the same namespace as your resource file. For `AppStrings.resx` you get:

```csharp
// <auto-generated/>
namespace YourApp.Resources;

public static partial class AppStringsKeys
{
    public static readonly ResourceKey Greeting    = new("Greeting",    AppStrings.ResourceManager);
    public static readonly ResourceKey WindowTitle = new("WindowTitle", AppStrings.ResourceManager);
    // …one ResourceKey per string entry, sorted, with sanitized member names.
}
```

Each `ResourceKey` carries **both** the key name and the `ResourceManager` it belongs to - that is what makes typed lookups direct and collision-free.

> [!NOTE]
> Only **string** entries become keys. Binary resources, images, colors, and any entry carrying a `type`/`mimetype` are skipped, and resource names that aren't valid C# identifiers are sanitized into valid member names.

### 3. Register your resources at startup

Register each resource file once, then choose the starting culture.

**Avalonia** (`Program.cs`):

```csharp
using System.Globalization;
using RentADeveloper.ResXLocalization;
using YourApp.Resources;

Localizer.Current.RegisterResourceManager(AppStrings.ResourceManager);
Localizer.Current.CurrentCulture = new CultureInfo("en");
```

**WPF** (`App.xaml.cs`):

```csharp
using System.Globalization;
using System.Windows;
using RentADeveloper.ResXLocalization;
using YourApp.Resources;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Localizer.Current.RegisterResourceManager(AppStrings.ResourceManager);
        Localizer.Current.CurrentCulture = new CultureInfo("en");
    }
}
```

Registration is only needed for **search-all** lookups; typed and scoped lookups need no registration.

> [!TIP]
> In Avalonia, put the registration (and the initial `CurrentCulture`) inside **`BuildAvaloniaApp()`** rather than `Main`. `BuildAvaloniaApp` runs at runtime *and* under the XAML previewer, so search-all lookups like `{l:Localize Greeting}` resolve at design time too instead of showing the `!Greeting!` sentinel.

### 4. Use it in XAML

Add the namespaces and bind with the `{l:Localize}` markup extension.

**Avalonia:**

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:l="clr-namespace:RentADeveloper.ResXLocalization.Avalonia;assembly=ResXLocalization.Avalonia"
        xmlns:res="clr-namespace:YourApp.Resources"
        Title="{l:Localize {x:Static res:AppStringsKeys.WindowTitle}}">

  <TextBlock Text="{l:Localize {x:Static res:AppStringsKeys.Greeting}}" />

</Window>
```

**WPF:**

```xml
<Window xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:l="clr-namespace:RentADeveloper.ResXLocalization.WPF;assembly=ResXLocalization.WPF"
        xmlns:res="clr-namespace:YourApp.Resources"
        Title="{l:Localize {x:Static res:AppStringsKeys.WindowTitle}}">

  <TextBlock Text="{l:Localize {x:Static res:AppStringsKeys.Greeting}}" />

</Window>
```

- `xmlns:l` points at the **framework package** (Avalonia: `assembly=ResXLocalization.Avalonia`; WPF: `assembly=ResXLocalization.WPF`).
- `xmlns:res` points at the namespace of **your** generated keys / resource accessors.

### 5. Switch language - live

```csharp
Localizer.Current.CurrentCulture = new CultureInfo("de");
```

Every `{l:Localize}` binding re-resolves **immediately**. No reload, no flicker.

## Localizing enums

Enum members are localized **by naming convention**: add one string entry per member to any of your
`.resx` files, named `Enum_<EnumTypeName>_<MemberName>`. No attributes on the enum, no extra code.

```csharp
public enum FileSortOrder { Unsorted, Ascending, Descending }
```

| Key                            | `AppStrings.resx` (English) | `AppStrings.de.resx` (German) |
| ------------------------------ | --------------------------- | ----------------------------- |
| `Enum_FileSortOrder_Unsorted`  | `Unsorted`                  | `Unsortiert`                  |
| `Enum_FileSortOrder_Ascending` | `Ascending (A-Z)`           | `Aufsteigend (A-Z)`           |
| `Enum_FileSortOrder_Descending`| `Descending (Z-A)`          | `Absteigend (Z-A)`            |

Like every other lookup, enum labels update live when the culture changes. The same three usages
work identically in Avalonia and WPF:

### In item templates: `{l:LocalizeEnum}`

Inside a `ComboBox`/`ListBox` item template each item *is* the enum value (the `DataContext`), so
`{l:LocalizeEnum}` localizes it directly:

```xml
<ComboBox ItemsSource="{Binding FileSortOrders}"
          SelectedItem="{Binding SelectedFileSortOrder}">
  <ComboBox.ItemTemplate>
    <DataTemplate>
      <TextBlock Text="{l:LocalizeEnum}" />
    </DataTemplate>
  </ComboBox.ItemTemplate>
</ComboBox>
```

### As a bound value: `LocalizeEnumConverter`

When the enum is a bound property rather than the `DataContext`, use `LocalizeEnumConverter` in a
`MultiBinding`. The second binding - to the current culture - re-triggers the conversion on every
language switch:

```xml
<!-- xmlns:core="clr-namespace:RentADeveloper.ResXLocalization;assembly=ResXLocalization.Core" -->
<TextBlock>
  <TextBlock.Text>
    <MultiBinding Converter="{x:Static l:LocalizeEnumConverter.Default}">
      <Binding Path="SelectedFileSortOrder" />
      <Binding Path="CurrentCulture" Source="{x:Static core:Localizer.Current}" />
    </MultiBinding>
  </TextBlock.Text>
</TextBlock>
```

### In code

```csharp
Localizer.Current.Get(FileSortOrder.Ascending);   // "Ascending (A-Z)" / "Aufsteigend (A-Z)"
```

### Custom prefix and scoping

The markup extension, the converter, and the code API all accept a **`KeyPrefix`** (default
`Enum_`) and an optional **`ResourceManager`** that scopes the lookup to one `.resx` file - useful
for giving the same enum different label sets, or for keeping enum labels in their own file:

```xml
<TextBlock Text="{l:LocalizeEnum KeyPrefix=Display_,
                                 ResourceManager={x:Static res:SortingStrings.ResourceManager}}" />
```

```csharp
Localizer.Current.Get(FileSortOrder.Ascending, SortingStrings.ResourceManager, "Display_");
```

To customize the converter, declare your own instance in resources
(`LocalizeEnumConverter.Default` is read-only):

```xml
<l:LocalizeEnumConverter x:Key="DisplayEnumConverter" KeyPrefix="Display_" />
```

---

## Guides and API reference

The [API reference](https://rent-a-developer.github.io/ResXLocalization/) documents every public type and member. The sections below cover the operational details that go beyond the quick start.

### Namespaces

In C# you almost always need only the first namespace:

| Namespace                                  | Contains                                                                       |
| ------------------------------------------ | ------------------------------------------------------------------------------ |
| `RentADeveloper.ResXLocalization`          | `ILocalizer`, `Localizer`, `ResourceKey` - the shared engine                   |
| `RentADeveloper.ResXLocalization.Avalonia` | Avalonia `LocalizeExtension`, `LocalizeEnumExtension`, `LocalizeEnumConverter` |
| `RentADeveloper.ResXLocalization.WPF`      | WPF `LocalizeExtension`, `LocalizeEnumExtension`, `LocalizeEnumConverter`      |

### Dependency injection

Register the ambient instance so it can be injected as `ILocalizer`:

```csharp
services.AddSingleton<ILocalizer>(_ => Localizer.Current);
```

Alternatively, assign a DI-owned implementation to `Localizer.Current` before creating views. The
property rejects `null`, and the markup extensions always use its current value.

### Lookups and fallback

Prefer generated `ResourceKey` values: they bind a compile-checked name directly to its resource
manager. Scoped string lookups accept a key and manager, while search-all lookups inspect registered
managers in registration order.

Normal .NET `ResourceManager` fallback applies:

| Situation | Result | `TranslationNotFound` |
| --- | --- | --- |
| `de-DE` entry exists | `de-DE` value | No |
| Missing in `de-DE`, exists in `de` | Parent `de` value | No |
| Missing in satellite, exists in neutral resources | Neutral value | No |
| Missing across the complete fallback chain | `!key!` by default | Yes |

`Localizer.MissingTranslationFormat` changes the sentinel. Because fallback runs first, a key that resolves
from a parent or neutral value does not count as missing - the sentinel and `Localizer.TranslationNotFound`
report unresolvable keys, not incomplete per-language coverage.

Formatting overloads (`Get(key, args…)`) pass the resolved text and arguments to `String.Format`
using the localizer's current culture.

`Localizer.Current` lives for the whole process, so a strong `CultureChanged` subscription keeps its
subscriber alive. Short-lived subscribers - a view model owned by a window, for example - should
unsubscribe when they are disposed (the sample view models show the pattern).

## Troubleshooting

**A string shows up as `!key!`.** The key could not be resolved in the current culture's complete
fallback chain. Check that the key exists in the neutral `.resx`, and - for search-all lookups like
`{l:Localize Greeting}` or `{l:LocalizeEnum}` - that the resource manager was registered via
`RegisterResourceManager`. Subscribe to `TranslationNotFound` to log every miss.

**No typed `…Keys` class is generated.**

- Ensure the neutral filename is dot-free and a same-folder classic `.Designer.cs` exists.
- Use the .NET 8 SDK or later. An older compiler may reject the generator with `CS9057`.
- SDK `GenerateResxSource` output lives under `obj` and does not qualify; use
  `PublicResXFileCodeGenerator` or `ResXFileCodeGenerator`.
- `RXLGEN001` identifies malformed eligible `.resx` XML and points at the source file.
- `RXLGEN002` identifies a missing or unrecognized same-folder classic accessor.

**Native AOT publishing (Avalonia).** Publish with `PublishAot=true` and list the shipped cultures
in `SatelliteResourceLanguages` so the satellite assemblies are retained. WPF does not support
Native AOT.

**Avalonia version resolution.** The Avalonia package declares 12.0.5 as its minimum so it still
builds under the .NET 8 SDK; applications building with a current SDK resolve Avalonia 12.1+
normally.

## The sample applications

Two complete, runnable showcases exercise **every** feature and combination - a scrolling window with a live language `ComboBox`:

- **Avalonia:** [`samples/ResXLocalization.Avalonia.Sample`](samples/ResXLocalization.Avalonia.Sample)
- **WPF:** [`samples/ResXLocalization.WPF.Sample`](samples/ResXLocalization.WPF.Sample)

Run them:

```shell
dotnet run --project samples/ResXLocalization.Avalonia.Sample
dotnet run --project samples/ResXLocalization.WPF.Sample   # Windows only
```

## Building from source

You need the **.NET 10 SDK** (see `global.json`); the produced packages target .NET 8 and .NET 10.

```powershell
# Build everything (must be 0 warnings / 0 errors - warnings are promoted to errors).
dotnet build ResXLocalization.slnx -c Release                # Windows (includes WPF)
dotnet build ResXLocalization.NonWindows.slnf -c Release     # Linux/macOS (skips WPF)
```

See [CONTRIBUTING.md](CONTRIBUTING.md) for running the test suites, packing the NuGet packages, and formatting the code before committing.

## Versioning

This project follows [Semantic Versioning](https://semver.org/). See the [CHANGELOG](CHANGELOG.md) for the history of changes.

## Contributing

Contributions are welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) first. In short: open an issue to discuss larger changes, keep the build warning-free, update the `CHANGELOG.md`, and make sure the tests pass.

## License

Released under the [MIT License](LICENSE.md). © 2026 David Liebeherr.

Thank you Mike James from AvaloniaUI OÜ for the written permission to use `Avalonia` in the name of the `ResXLocalization.Avalonia` NuGet package.

Avalonia is a registered trademark of AvaloniaUI OÜ. This project is not affiliated with or endorsed by AvaloniaUI OÜ.

## Author

**David Liebeherr** - [rent-a-developer](https://github.com/rent-a-developer)
📧 [info@rent-a-developer.de](mailto:info@rent-a-developer.de)

If this library saves you time, a ⭐ on [GitHub](https://github.com/rent-a-developer/ResXLocalization) is appreciated!
