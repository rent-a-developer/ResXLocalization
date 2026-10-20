# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/) and
this project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

- `IMessageFormatter` and `ILocalizer.MessageFormatter`: one replaceable formatter that every resolved
  value passes through, with or without arguments. The default, `StringFormatMessageFormatter`, uses
  `string.Format` and does not format a value without arguments.
- `MessageArgument`, one argument of a lookup: its position, an optional name and its value.
- `ILocalizer.LookupFailed` with `LookupFailedEventArgs`, raised for every failed lookup: a key that no
  resource file contains, or a value the formatter cannot format.
- `LookupFailure` and `LookupFailureReason`, which describe a failed lookup to `LookupFailed` and to
  `ILocalizer.LookupFailureTextBuilder`.
- `ILocalizer.RefreshTriggeringRevision`, which changes on every change that can change a lookup's
  result (culture, formatter, lookup-failure text builder, enum key builder, registered resource
  managers).
- `MessageArguments`, the arguments of a lookup, built with `FromValues`, `FromTuples`,
  `FromDictionary` or `FromObject` and checked when they are built: positional, named or mixed.
- Enum lookups with format arguments: `Get(sortOrder, MessageArguments.FromValues(count))`.
- `ILocalizer.EnumKeyBuilder`, one replaceable enum key convention for code and XAML.
- Typed keys for every neutral `.resx`, with or without a `.Designer.cs`: files without a designer,
  neutral files with a dot in their name and projects using `GenerateResxSource` produce keys classes.
  Without a designer, the keys class takes its namespace from the file's `CustomToolNamespace` when it
  is set.
- The generated `<Name>Keys.ResourceManager` property: the resource manager of that `.resx`, which every
  key of the class uses. Register it for search-all lookups and pass it for scoped lookups.
- Generator warnings `RXLGEN003` (a `.resx` reached the generator without its MSBuild resource name)
  and `RXLGEN004` (two neutral `.resx` files would produce the same keys class).
- Format arguments on the binding, in both UI packages: `<l:Argument>` children of `{l:Localize}`, each
  with a constant or bound `Value` and an optional `Name`. There is no upper limit, each localized
  property has its own arguments, and arguments work in style setters, resources and templates.
- `{l:Localize}` re-resolves on every change of `ILocalizer.RefreshTriggeringRevision` - a new formatter,
  a new lookup-failure text builder, a registered or removed resource manager - not only on a language
  switch.
- Format arguments for enum labels in XAML (issue 11): `<l:Argument>` children of `{l:LocalizeEnum}`, and
  `MultiBinding` children of `LocalizeEnumConverter` from index 2 on.
- `LocalizeEnumExtension.Value`: the enum value to localize, a binding or a fixed member
  (`Value={x:Static m:FileSortOrder.Ascending}`), and the shorthand `{l:LocalizeEnum {Binding SortOrder}}`
  through the new constructor `LocalizeEnumExtension(object value)`.
- A bound key for `{l:Localize}`, in both UI packages: `{l:Localize {Binding Key}}` or
  `Key="{Binding Key}"` takes the key from the view model, as a `ResourceKey` or a `string`. A bound value
  that gives no key, such as `null`, renders an empty string instead of throwing.
- `LocalizeEnumConverter.ArgumentNames`: comma-separated names for the converter's arguments, for a
  formatter that understands named arguments.
- `{l:LocalizeEnum}` re-resolves on every change of `ILocalizer.RefreshTriggeringRevision`, and so does
  `LocalizeEnumConverter` with its new refresh-trigger child.
- The opt-in package `ResXLocalization.MessageFormat`: ICU MessageFormat (`plural`, `select`) through
  `Localizer.Current.UseMessageFormat()`, which parses every resolved value as an ICU message, with or
  without arguments, optionally with a configured `MessageFormatter` of the `MessageFormat` library. See
  [ICU MessageFormat](docs/guides/message-format.md).
- `DesignTime.Culture`, an attached property in both UI packages that selects the language of XAML
  previewers (`<Window ... l:DesignTime.Culture="de">`). It sets `Localizer.Current.CurrentCulture` inside
  a previewer and has no effect at run time. See [Design time and previewers](docs/guides/design-time.md).
- A workaround for a defect in Rider's WPF XAML previewer (RIDER-143207): localized strings, which
  rendered nothing there, now render their raw resource pattern under the default formatter, for example
  `{0} people invited`. See [Design time and previewers](docs/guides/design-time.md).

### Changed

- **BREAKING:** A formatting failure in `Get(key, args)` and its scoped and typed siblings returns the
  failure text and raises `ILocalizer.LookupFailed`; it no longer throws `FormatException`. See
  the [migration guide](docs/guides/migration-2.0.md).
- **BREAKING:** In XAML, a formatting failure in `{l:Localize}` renders the failure text instead of an
  Avalonia binding error or an empty WPF text. See the [migration guide](docs/guides/migration-2.0.md).
- **BREAKING:** `Get(key, args)` with an empty array returns the resource value unformatted: `{{x}}`
  stays `{{x}}`. See the [migration guide](docs/guides/migration-2.0.md).
- **BREAKING:** A custom `ILocalizer` implementation must implement `LookupFailed`,
  `MessageFormatter`, `RefreshTriggeringRevision` and `LookupFailureTextBuilder`. See the
  [migration guide](docs/guides/migration-2.0.md).
- **BREAKING:** `ILocalizer.MissingTranslationFormat` is replaced by `ILocalizer.LookupFailureTextBuilder`,
  a `Func<LookupFailure, string>` that also produces the failure text for a formatting failure. See the
  [migration guide](docs/guides/migration-2.0.md).
- **BREAKING:** `ILocalizer.TranslationNotFound` and `TranslationNotFoundEventArgs` are replaced by
  `ILocalizer.LookupFailed` and `LookupFailedEventArgs`, which report formatting failures too. See the
  [migration guide](docs/guides/migration-2.0.md).
- **BREAKING:** Format arguments are built with `MessageArguments`: `Get("Key", a, b)` becomes
  `Get("Key", MessageArguments.FromValues(a, b))`; the `params object?[]` overloads are removed. See the [migration guide](docs/guides/migration-2.0.md).
- **BREAKING:** A scoped lookup names the manager: `Get("Key", rm)` becomes
  `Get("Key", resourceManager: rm)`, and `Get("Key", rm, a, b)` becomes
  `Get("Key", MessageArguments.FromValues(a, b), rm)`.
  See the [migration guide](docs/guides/migration-2.0.md).
- **BREAKING:** An enum key prefix is named: `Get(sortOrder, "Display_")` becomes
  `Get(sortOrder, keyPrefix: "Display_")`. See the [migration guide](docs/guides/migration-2.0.md).
- **BREAKING:** A `null` resource manager means search-all and a `null` key prefix the default of
  `ILocalizer.EnumKeyBuilder`, instead of throwing `ArgumentNullException`;
  `LocalizeEnumExtension.KeyPrefix` and `LocalizeEnumConverter.KeyPrefix` are `string?` and default to
  `null`. See the [migration guide](docs/guides/migration-2.0.md).
- **BREAKING:** Every 1.x `Get` signature is removed, including `Get(string)` and `Get(ResourceKey)`;
  assemblies compiled against 1.x throw `MissingMethodException` until recompiled. See the [migration guide](docs/guides/migration-2.0.md).
- **BREAKING:** A custom `ILocalizer` implementation has exactly three `Get` overloads - string,
  `ResourceKey` and enum keys, each with an optional `MessageArguments` - and implements
  `EnumKeyBuilder`. See the [migration guide](docs/guides/migration-2.0.md).
- **BREAKING:** Typed keys use the generated `<Name>Keys.ResourceManager`, no longer the designer's
  `<Name>.ResourceManager`; `ReferenceEquals(AppStringsKeys.Greeting.Manager, AppStrings.ResourceManager)`
  is now `false`. See the [migration guide](docs/guides/migration-2.0.md).
- **BREAKING:** Files without a designer, neutral files with a dot in their name and projects using
  `GenerateResxSource` now produce keys classes, which can collide with types of your own. See the
  [migration guide](docs/guides/migration-2.0.md).
- **BREAKING:** Two neutral `.resx` files that produce the same keys class report `RXLGEN004` and
  generate neither class, instead of merging into one partial class. See the
  [migration guide](docs/guides/migration-2.0.md).

- **BREAKING:** `Localizer.RegisterResourceManager` throws `ArgumentException` naming the base name for a
  plain `ResourceManager` for which no resources exist under its base name, instead of registering it and
  letting every search-all lookup that reaches it throw `MissingManifestResourceException`. Fix the base
  name, or register `<Name>Keys.ResourceManager`. See the [migration guide](docs/guides/migration-2.0.md).
- **BREAKING:** `LocalizeExtension.Key` is an `object?` that takes a `string` or a `ResourceKey`; a key
  that is neither throws `InvalidOperationException` when the view loads instead of producing an empty
  key. See the [migration guide](docs/guides/migration-2.0.md).
- **BREAKING:** Avalonia: `{l:Localize}` with arguments in a `Style` setter or a resource is formatted;
  it resolved unformatted. See the [migration guide](docs/guides/migration-2.0.md).
- **BREAKING:** `LocalizeEnumConverter` reads child 0 of its `MultiBinding` as the refresh trigger
  (`RefreshTriggeringRevision` of `Localizer.Current`), child 1 as the enum value and children from
  index 2 on as format arguments; a `MultiBinding` with the enum first renders an empty string until it
  is rewritten. See the [migration guide](docs/guides/migration-2.0.md).

### Removed

- The generator warning `RXLGEN002` (a `.resx` without a recognized classic designer): a designer is no
  longer required.
- **BREAKING:** `LocalizeArgs` and its attached properties `Arg0`...`Arg8`, in both UI packages; give
  `{l:Localize}` `<l:Argument>` children instead. See the [migration guide](docs/guides/migration-2.0.md).
- **BREAKING:** `LocalizeExtension.ResourceKey`; set `Key` to the `ResourceKey` instead. See the [migration guide](docs/guides/migration-2.0.md).
- **BREAKING:** The `DataContext` mode of `{l:LocalizeEnum}`: it no longer reads the enum value from the
  target's `DataContext`; set `Value`, as `Value={Binding}` in item templates. See the
  [migration guide](docs/guides/migration-2.0.md).

## [1.1.0] - 2026-07-20

### Added

- Dynamic composite-format arguments for `{l:Localize}` in both UI packages: bind the new
  `LocalizeArgs.Arg0`…`Arg8` attached properties on the target element to supply `String.Format`
  arguments for the resolved resource value (e.g. `{0} people invited`). The rendered text
  re-formats live when a bound argument or the culture changes; elements without arguments resolve
  exactly as before.

## [1.0.0] - 2026-07-13

### Added

- Initial release of `ResXLocalization.Avalonia` and `ResXLocalization.WPF` (targeting .NET 8 LTS
  and .NET 10), both depending on the shared Core package and carrying the Roslyn source generator
  plus MSBuild wiring:
  - Live, no-reload language switching via `Localizer.Current.CurrentCulture`; the `CultureChanged`
    event carries a `CultureChangedEventArgs` reporting the previous and new culture.
  - Typed, compile-checked keys generated for string entries in eligible neutral `.resx` files with
    sibling classic designers (`{l:Localize {x:Static …Keys.…}}`).
  - Search-all, scoped, and typed lookup modes over any number of registered resource files, with
    `UnregisterResourceManager` / `ClearResourceManagers` for dynamic scenarios.
  - Enum localization by convention (`{l:LocalizeEnum}`, `LocalizeEnumConverter`).
  - Format-argument overloads: `Get(key, args…)` formats the resolved value in the current culture.
  - Miss diagnostics: the `TranslationNotFound` event and the configurable
    `MissingTranslationFormat` sentinel (default `!key!`).
  - Available-culture discovery for language pickers: `GetAvailableCultures()`.
  - Native AOT / trimming support for the Avalonia package and the shared engine.

[unreleased]: https://github.com/rent-a-developer/ResXLocalization/compare/v1.1.0...HEAD
[1.1.0]: https://github.com/rent-a-developer/ResXLocalization/releases/tag/v1.1.0
[1.0.0]: https://github.com/rent-a-developer/ResXLocalization/releases/tag/v1.0.0
