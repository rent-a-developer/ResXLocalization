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
- The generated `<Name>Keys.ResourceManager` property: the resource manager of that `.resx`, which every
  key of the class uses. Register it for search-all lookups and pass it for scoped lookups.
- Generator warnings `RXLGEN003` (a `.resx` reached the generator without its MSBuild resource name)
  and `RXLGEN004` (two neutral `.resx` files would produce the same keys class).

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

### Removed

- The generator warning `RXLGEN002` (a `.resx` without a recognized classic designer): a designer is no
  longer required.

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
