# ICU MessageFormat

The default formatter of this library is `StringFormatMessageFormatter`, which uses `string.Format`: a resource value with arguments is a composite format string, and its wording cannot depend on the arguments. The opt-in package `ResXLocalization.MessageFormat` replaces it with [ICU MessageFormat](https://unicode-org.github.io/icu/userguide/format_parse/messages/), whose `plural` and `select` choose the wording by the arguments:

```text
{Count, plural, one {# file deleted} other {# files deleted}}
{Gender, select, female {She} male {He} other {They}} replied.
```

The package formats with the [MessageFormat](https://github.com/jeffijoe/messageformat.net) library by Jeff Hansen (NuGet package `MessageFormat`,
namespace `Jeffijoe.MessageFormat`).

## Opting in

Add the package next to your UI package:

```shell
dotnet add package ResXLocalization.MessageFormat
```

Then install the ICU formatter once, at startup:

```csharp
using RentADeveloper.ResXLocalization.MessageFormat;

Localizer.Current.UseMessageFormat();
```

`UseMessageFormat()` assigns the package's ICU formatter to `Localizer.Current.MessageFormatter`, so it applies to every lookup in code
and in XAML, and makes bound text re-resolve like a language switch.

- **Avalonia:** call it in `BuildAvaloniaApp`, next to the registration of your resource files, so the
  Avalonia previewer formats with ICU too.
- **WPF:** call it in `App.OnStartup` for the running application. The WPF designer runs no startup code;
  to format with ICU there too, call it in a `[ModuleInitializer]` of the application assembly, as
  [Registering resource files](registration.md#wpf) shows for the registration.

Wherever `UseMessageFormat()` did not run, the default formatter is in place: a lookup without
arguments shows the resource value as written, for example `{Count, plural, one {# file deleted} other
{# files deleted}}`, and a lookup with arguments whose value `string.Format` cannot read returns the
failure text and raises `Localizer.Current.LookupFailed`.

## Every resolved value is ICU

After `UseMessageFormat()`, **every** resolved resource value is parsed as an ICU message, with or without
arguments: `Localizer.Current.Get(key)`, `Localizer.Current.Get(key, arguments: null)`,
`Localizer.Current.Get(key, MessageArguments.Empty)`, the indexer `Localizer.Current["Key"]` and
`{l:Localize Key}` alike. So, also without arguments:

- `It''s ready` renders `It's ready`;
- `'{literal}'` renders `{literal}`;
- `Hello {Name}`, looked up without arguments, returns the failure text and raises
  `Localizer.Current.LookupFailed`, because the variable `Name` is missing.

When you opt in, review every resource value for ICU syntax, including the texts without arguments - see
[Migrating resource values](#migrating-resource-values). A plain text needs no dummy argument and no
wrapper. The default formatter (`StringFormatMessageFormatter`) keeps returning a value without arguments
unchanged.

> [!NOTE]
> In Rider's WPF previewer, `{l:Localize}` and `{l:LocalizeEnum}` look up their text without arguments
> (see [Design time and previewers](design-time.md#riders-wpf-previewer)). With ICU installed there,
> escaping is interpreted, and a value that needs a variable renders the failure text.

## `plural` and `select`

| Key | `AppStrings.resx` (English) | `AppStrings.de.resx` (German) |
| --- | --- | --- |
| `FilesDeleted` | `{Count, plural, one {# file deleted} other {# files deleted}}` | `{Count, plural, one {# Datei gelöscht} other {# Dateien gelöscht}}` |

In code, build named arguments with `MessageArguments` and pass them to any `Localizer.Current.Get`
overload:

```csharp
Localizer.Current.Get(AppStringsKeys.FilesDeleted, MessageArguments.FromTuples(("Count", 1)));      // "1 file deleted"
Localizer.Current.Get(AppStringsKeys.FilesDeleted, MessageArguments.FromObject(new { Count = 5 })); // "5 files deleted"
```

In XAML, name the `<l:Argument>` children of `{l:Localize}`:

```xml
<TextBlock>
  <TextBlock.Text>
    <l:Localize Key="{x:Static res:AppStringsKeys.FilesDeleted}">
      <l:Argument Name="Count" Value="{Binding DeletedCount}" />
    </l:Localize>
  </TextBlock.Text>
</TextBlock>
```

Which plural categories exist - `zero`, `one`, `two`, `few`, `many`, `other` - depends on the language.
`=0` matches an exact value:
`{Count, plural, =0 {No files deleted} one {# file deleted} other {# files deleted}}` renders
`No files deleted` for 0. `selectordinal` chooses by ordinal rules:
`{0, selectordinal, one {#st} two {#nd} few {#rd} other {#th}} place` renders `2nd place` for 2.

`select` chooses by a value's text. A partition display that shows the volume label when there is one:

```text
{HasLabel, select, True {{Label} ({Letter}:)} other {Local Disk ({Letter}:)}}
```

```xml
<l:Localize Key="{x:Static res:AppStringsKeys.PartitionDisplay}">
  <l:Argument Name="Label" Value="{Binding VolumeLabel}" />
  <l:Argument Name="Letter" Value="{Binding DriveLetter}" />
  <l:Argument Name="HasLabel" Value="{Binding HasVolumeLabel}" />
</l:Localize>
```

It renders `Data (D:)` for a partition labeled `Data` on drive `D`, and `Local Disk (C:)` for an
unlabeled one on drive `C`. A `bool` matches the `select` keys `True` and `False` - .NET's text for a
`bool` - not `true` and `false`.

An enum label needs only one enum member and a `plural` pattern: give `{l:LocalizeEnum}` the same
`<l:Argument>` children, see [Localizing enum values](enums.md#with-format-arguments).

## Arguments

Every argument reaches ICU under its position - `"0"`, `"1"`, and so on - and, if it has a name, under its
name as well. A name overwrites a position with the same text:

| Arguments | ICU receives |
| --- | --- |
| `<l:Argument Value="{Binding Count}" />` with `Count` = 5 | `{ "0": 5 }`, so `{0, plural, ...}` works |
| `<l:Argument Value="{Binding VolumeLabel}" />` and `<l:Argument Name="Letter" Value="{Binding DriveLetter}" />` | `{ "0": "Data", "1": "D", "Letter": "D" }`, so the pattern may use `{0} ({Letter}:)` or `{0} ({1}:)` |
| `<l:Argument Value="{Binding A}" />` and `<l:Argument Name="0" Value="{Binding B}" />` | `{ "0": B, "1": B }`: the name wins, silently |

In code, `MessageArguments.FromTuples(("Count", 5))`, `MessageArguments.FromDictionary(dictionary)` and
`MessageArguments.FromObject(new { Count = 5 })` build named arguments, and
`MessageArguments.FromValues(5)` positional ones. Named arguments are reachable as `{0}`, `{1}` too, in the
order the method produced them; in a pattern used with named arguments, write the names.

**A missing variable always fails.** A pattern that references a variable the lookup does not supply
returns the failure text and raises `Localizer.Current.LookupFailed` with the reason
`LookupFailureReason.MessageFormatFailed`. The MessageFormat library has no setting that renders a
missing variable empty.

### `null`, strings and other values

A `null` value never fails:

| Pattern | Renders for `null` |
| --- | --- |
| `{0, plural, one {# file} other {# files}}` | `0 files` |
| `{0, selectordinal, one {#st} two {#nd} few {#rd} other {#th}}` | `0th` |
| `{0, select, a {A} other {O}}` | `O` - the `other` branch |
| `{0}`, `{0, number}`, `{0, date}`, `{0, time}` | an empty string |

An argument binding that is not resolved yet passes `null`, so a view that loads before its `DataContext`
briefly shows `0 people invited` for `{Count, plural, one {# person invited} other {# people invited}}`,
without `LookupFailed`; a view-model value that is `null` renders the same. The default formatter renders
every `null` argument as an empty string.

- **A numeric string** behaves like the number in `plural`: `"5"` renders `5 files` and `"1"` renders
  `1 file`, so the constant `Value="5"` in XAML works.
- **A non-numeric string** in `plural`, such as `"abc"`, fails with a `FormatException`: failure text
  and `LookupFailed`. `{0, number}` renders a string unchanged (`abc`), and `select` and `{0}` take any
  string.
- **A value that is neither a number nor a string**, such as a plain `object`, fails in `plural` with an
  `InvalidCastException`: failure text and `LookupFailed`.

## The format culture

ICU formats in the lookup's culture - `Localizer.Current.CurrentCulture` when the lookup runs - with one
exception: the invariant culture formats as `en`. The MessageFormat library chooses plural rules by the
culture's name, and for the invariant culture, whose name is empty, it uses CLDR's root rules, which know
only `other`: `{0, plural, one {# item} other {# items}}` would render `1 items`. The neutral resources
of most applications are English, so `en` is the better guess. Where `en` cannot be created, as in
[globalization-invariant mode](#globalization-invariant-mode), the invariant culture stays, with the root
rules.

Plural rules for every other culture come from CLDR rules generated into the MessageFormat library, which
follow the culture name's chain down to the root rules by themselves (`de-CH`, then `de`). A culture whose
name CLDR does not know at all gets the root rules: `other` only. Numbers and dates use .NET's data for
the culture:

| Culture | Plural category of `1` | `{0, number}` with `1234.5` |
| --- | --- | --- |
| `CultureInfo.InvariantCulture` (formats as `en`) | `one` | `1,234.5` |
| `en` | `one` | `1,234.5` |
| `de-DE` | `one` | `1.234,5` |
| `iu-Latn-CA` | `one` | the culture's own format |
| `zh-Hant-TW` | `other` - Chinese has no `one` | the culture's own format |

### Keep the thread culture in sync

A few places of the MessageFormat library do not use the lookup's culture but the thread's
`CultureInfo.CurrentCulture`, which `Localizer.Current.CurrentCulture` deliberately does not change:

- `#` in a `plural` branch renders the number in the thread culture (see
  [`#` uses the thread culture, without grouping](#-uses-the-thread-culture-without-grouping));
- `select` compares a number or date argument with its keys as the thread culture writes it, so with the
  thread culture `de-DE` the value 1.5 does not match the key `1.5`;
- decimal exact-value keys and offsets, such as `=1.5` and `offset:1.5`, are read in the thread culture,
  so with the thread culture `de-DE` the key `=1.5` does not match the value 1.5. Whole-number keys and
  offsets, such as `=0`, `=1` and `offset:1`, do not depend on any culture.

It is therefore recommended to keep `CultureInfo.CurrentCulture` in sync with
`Localizer.Current.CurrentCulture` yourself, for example in a `Localizer.Current.CultureChanged` handler
that you subscribe before the first assignment of `Localizer.Current.CurrentCulture`:

```csharp
Localizer.Current.CultureChanged += (_, e) =>
{
    CultureInfo.CurrentCulture = e.NewCulture; // the thread that switches the language, normally the UI thread
    CultureInfo.DefaultThreadCurrentCulture = e.NewCulture; // threads without a culture of their own
};

Localizer.Current.CurrentCulture = new CultureInfo("de-DE");
```

The thread culture also decides how the rest of your code formats numbers and dates, for example
`ToString()` and `string.Format` without a format provider, so this changes those to the selected
language too - in a localized application usually what you want.

## Styles

Numbers, dates and times are formatted with .NET's culture data, not with CLDR's, and only these styles
exist (examples in `en-US`):

| Pattern | Renders |
| --- | --- |
| `{0, number}` | the culture's number format, grouped: `1,234.5`; `1,234,567` for an integer |
| `{0, number, integer}` | rounded and **not** grouped: `1234568` for `1234567.6` |
| `{0, number, currency}` | .NET's `C` format with the culture's own currency: `$3.50`; `3,50 €` in `de-DE` |
| `{0, number, percent}` | .NET's `P0` format: `26%` for `0.256` |
| `{0, date}` and `{0, date, short}` | .NET's `d` format: `1/31/2026` |
| `{0, date, full}` | `Saturday, January 31, 2026` |
| `{0, time, short}` | `2:05 PM` |
| `{0, time}` and `{0, time, medium}` | `2:05:09 PM` |

A neutral culture such as `en` or `de` has no currency, so `{0, number, currency}` renders the generic
currency sign there: `¤3.50`. Use a specific culture such as `en-US` or `de-DE` where amounts of money
are shown.

Every other style returns the failure text and raises `LookupFailed` with an
`UnsupportedFormatStyleException`: `{0, date, medium}`, `{0, date, long}`, and number skeletons such as
`{0, number, ::currency/EUR}`. Add the styles you need with `CustomValueFormatter`, see
[Configuring the formatter of the MessageFormat library](#configuring-the-formatter-of-the-messageformat-library).

### `#` uses the thread culture, without grouping

In a `plural` branch, `#` renders the number with `CultureInfo.CurrentCulture` - the thread's culture,
which `Localizer.Current.CurrentCulture` deliberately does not change - and without grouping. With the
thread culture `en-US` and the lookup culture `de-DE`,
`{Count, plural, one {# Datei} other {# Dateien}}` renders `1234567 Dateien` for 1234567 and `1.5 Dateien`
for 1.5. Integer counts below 1,000 are therefore right in every culture, and fractional counts are right
when you [keep the thread culture in sync](#keep-the-thread-culture-in-sync).

For larger or fractional numbers, write `{Count, number}` in the branch instead of `#`; it uses the
lookup culture: `{Count, plural, one {{Count, number} Datei} other {{Count, number} Dateien}}` renders
`1.234.567 Dateien`. Nothing reports a `#` that renders differently from the lookup culture.

## Configuring the formatter of the MessageFormat library

`UseMessageFormat` takes an optional `Jeffijoe.MessageFormat.MessageFormatter`, the extension point of the
MessageFormat library: custom formatters (`Formatters`), custom plural rules (`CardinalPluralizers`,
`OrdinalPluralizers`), and `CustomValueFormatter` for further `number`, `date` and `time` styles. This one
adds `date, medium`:

```csharp
using System.Globalization;
using RentADeveloper.ResXLocalization;
using RentADeveloper.ResXLocalization.MessageFormat;
using JeffijoeMessageFormatter = Jeffijoe.MessageFormat.MessageFormatter;

var messageFormatter = new JeffijoeMessageFormatter(
    customValueFormatter: new Jeffijoe.MessageFormat.CustomValueFormatters
    {
        // Handles "date, medium"; returning false keeps the built-in behavior for every other style.
        Date = (CultureInfo culture, object? value, string? style, out string? formatted) =>
        {
            if (style == "medium" && value is DateTime date)
            {
                formatted = date.ToString("d MMM yyyy", culture);
                return true;
            }

            formatted = null;
            return false;
        },
    }
);

Localizer.Current.UseMessageFormat(messageFormatter);

// {0, date, medium} with new DateTime(2026, 1, 31) now renders "31 Jan 2026" in en.
```

- **It is used as given.** `UseMessageFormat` does not copy, wrap or reconfigure it. `null`, the default,
  means a new `MessageFormatter` with the library's defaults.
- **It is shared by every culture and every thread.** Formatting with it is thread-safe, but its
  configuration is not: `Formatters` is a list, the plural rules are plain dictionaries, and
  `CustomValueFormatter` has a setter. Configure it completely before calling `UseMessageFormat`, and do
  not modify it afterwards.
- **Its `Culture` property has no effect**, because every lookup passes its own culture.
- **Its parse cache** keeps one parsed pattern per distinct pattern text and is never trimmed. Resource
  values bound it; if you format generated patterns, pass `new JeffijoeMessageFormatter(useCache: false)`.
- **An exception your extensions throw** while formatting is a formatting failure like any other: the
  lookup returns the failure text and raises `LookupFailed`. Whether your extensions are trim and Native
  AOT safe is yours to check.
- **Use an alias.** Both `Jeffijoe.MessageFormat` and `RentADeveloper.ResXLocalization` declare an
  `IMessageFormatter`, so importing both namespaces in one file makes `IMessageFormatter` ambiguous. Import
  `Jeffijoe.MessageFormat.MessageFormatter` under an alias, as above.

## Globalization-invariant mode

`UseMessageFormat()` works in globalization-invariant mode and does not throw there. But an application
built with `InvariantGlobalization=true` - a common setting for Native AOT and for containers - or run
with `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` cannot create named cultures at all:
`new CultureInfo("de")` throws a `CultureNotFoundException`. Only the invariant culture is left, and it
formats with the root plural rules, `other` only.

With `PredefinedCulturesOnly=false` as well, named cultures can be created and their plural rules follow
their names (`de-CH` chooses `one` for 1), but numbers, dates and currencies format invariantly: `1,234.5`
in `de-DE`, and `3.50` for a currency. Neither setup is recommended for a localized application.

## Migrating resource values

Only a resource value that contains a brace (`{` or `}`), a doubled apostrophe (`''`) or an apostrophe
before a number sign (`'#`) can render differently under ICU, or fail. Every other value renders as
written, including texts with single apostrophes such as `It's ready` or `l'utilisateur`, and texts with
angle brackets. To find the values to review, search your `.resx` files for the regular expression
`[{}]|''|'#` - with the find-in-files search of your IDE, or with PowerShell:

```powershell
Get-ChildItem -Recurse -Filter *.resx | Select-String -Pattern "[{}]|''|'#"
```

Every value with arguments contains braces, so those values are among the matches too; the tables below
show what to look for.

ICU syntax is not compatible with composite format strings. These spellings fail under ICU - they return
the failure text and raise `LookupFailed`, so at least they are reported:

| Composite format | ICU |
| --- | --- |
| `{0:N0}` | `{0, number}` (grouped); `{0, number, integer}` rounds without grouping |
| `{1:d}` | `{1, date, short}` |
| `{0:C}` | `{0, number, currency}` |
| `{{literal}}` | `'{'literal'}'` |

ICU parses a resource value whenever it formats one, so a literal brace is syntax even in a text without
arguments: `Use { and } for blocks` fails, because `and` is read as a missing variable. Write
`Use '{' and '}' for blocks`.

These values change **silently** - nothing reports them, not even `LookupFailed`:

| Resource value | ICU renders, for the argument value "3" | The default formatter renders |
| --- | --- | --- |
| `'{0}' was not found` | `{0} was not found` | `'3' was not found` |
| `Datei '{0}' wurde gelöscht` | `Datei {0} wurde gelöscht` | `Datei '3' wurde gelöscht` |
| `Can't find '{Name}'` | `Can't find {Name}` | - |
| `'{0}'s file` | `{0}s file` | `'3's file` |
| `''{0}''` | `'3'` | `''3''` |
| `It''s {0}` | `It's 3` | `It''s 3` |
| `'#' in text {0}` | `# in text 3` | `'#' in text 3` |

- **`'{0}'` renders the literal text `{0}`**: an apostrophe before a brace starts quoted text. To put an
  argument in apostrophes, write `''{0}''`, which renders `'3'`.
- **`''` becomes one `'`.**
- **Apostrophes in ordinary text need no change**: elisions (`l'utilisateur {0}`), contractions
  (`Don't delete {0}`) and possessives (`It's {0}'s turn`) render as written, inside a `plural` branch
  too.

Angle brackets are plain text: `<b>{0}</b> saved` renders `<b>3</b> saved`, and `Press <Enter> to {0}`
renders `Press <Enter> to 3`. Some other ICU engines read tags as markup; this one does not.

## Using another ICU engine

The formatter is replaceable: `IMessageFormatter` and `ILocalizer.MessageFormatter` are public. If you
need what this engine does not offer - CLDR-exact number and date formats, number skeletons, `medium` and
`long` dates, durations, rich-text tags - implement `IMessageFormatter` over another engine and assign it
to `Localizer.Current.MessageFormatter` **instead of** calling `UseMessageFormat()`.

This adapter formats with [Rh.MessageFormat](https://github.com/Rheopyrin/Rh.Messageformat) (NuGet
package `Rh.MessageFormat`) and keeps the three rules of this package:

```csharp
using System.Globalization;
using RentADeveloper.ResXLocalization;
using Rh.MessageFormat.Caches;
using Rh.MessageFormat.Options;

public sealed class RhMessageFormatter : IMessageFormatter
{
    // One Rh.MessageFormat formatter per locale, created on first use. A locale Rh.MessageFormat has no
    // data for falls back to en instead of throwing an InvalidLocaleException.
    private readonly MessageFormatterCachedProvider formatters = new(
        new MessageFormatterOptions { DefaultFallbackLocale = "en" }
    );

    public string Format(string pattern, MessageArguments arguments, CultureInfo culture)
    {
        // 1. Every argument under its position, then every named argument under its name.
        var values = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var argument in arguments)
        {
            values[argument.Position.ToString(CultureInfo.InvariantCulture)] = argument.Value;
        }

        foreach (var argument in arguments)
        {
            if (argument.Name is { } name)
            {
                values[name] = argument.Value;
            }
        }

        // 2. Always parse, also without arguments: then the dictionary is empty.
        // 3. The dictionary overload of FormatMessage, never FormatMessage(string, object), which reads the
        //    object's properties by reflection and is not trim or Native AOT safe.
        // The invariant culture has no name, so it formats as en, as UseMessageFormat() does.
        var locale = culture.Name.Length == 0 ? "en" : culture.Name;
        return this.formatters.GetFormatter(locale).FormatMessage(pattern, values);
    }
}
```

```csharp
Localizer.Current.MessageFormatter = new RhMessageFormatter(); // instead of UseMessageFormat()
```

Do not import the namespace `Rh.MessageFormat.Abstractions.Interfaces` in the adapter's file: it declares
an `IMessageFormatter` of its own, which makes `IMessageFormatter` ambiguous.

Two warnings:

- **Engines do not accept the same dialect.** Tags, missing variables and styles differ between ICU
  engines, so resource values written for one engine can render differently, or fail, under another. For
  example, under Rh.MessageFormat `Hello {Name}` looked up without arguments renders `Hello ` instead of
  the failure text, and `{0, date, medium}` renders `Jan 31, 2026` instead of the failure text.
- **Such an adapter is your code.** ResXLocalization does not ship or support it, and its size and its
  Native AOT safety are yours to check. Rh.MessageFormat carries CLDR data for more than 200 locales:
  measured with Rh.MessageFormat 1.2.0, it adds about 15 MB to a Native AOT binary.
