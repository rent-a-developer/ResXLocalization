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
  result (culture, formatter, lookup-failure text builder, registered resource managers).

### Changed

- **BREAKING:** A formatting failure in `Get(key, args)` and its scoped and typed siblings returns the
  failure text and raises `ILocalizer.LookupFailed`; it no longer throws `FormatException`. See
  the [migration guide](docs/guides/migration-2.0.md).
- **BREAKING:** In XAML, a formatting failure in `{l:Localize}` renders the failure text instead of an
  Avalonia binding error or an empty WPF text. See the [migration guide](docs/guides/migration-2.0.md).
- **BREAKING:** `Get(key, args)` with an empty array returns the resource value unformatted: `{{x}}`
  stays `{{x}}`. See the [migration guide](docs/guides/migration-2.0.md).
- **BREAKING:** A custom `ILocalizer` implementation must implement `LookupFailed`,
  `MessageFormatter`, `RefreshTriggeringRevision`, `LookupFailureTextBuilder` and the hidden
  `Get(string key, MessageArgument[] arguments, ResourceManager? resourceManager)`. See the
  [migration guide](docs/guides/migration-2.0.md).
- **BREAKING:** `ILocalizer.MissingTranslationFormat` is replaced by `ILocalizer.LookupFailureTextBuilder`,
  a `Func<LookupFailure, string>` that also produces the failure text for a formatting failure. See the
  [migration guide](docs/guides/migration-2.0.md).
- **BREAKING:** `ILocalizer.TranslationNotFound` and `TranslationNotFoundEventArgs` are replaced by
  `ILocalizer.LookupFailed` and `LookupFailedEventArgs`, which report formatting failures too. See the
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
