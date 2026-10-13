# Dynamic format arguments

A resource value can be a composite format string, and the arguments can come straight from your
view model. Add the entry as usual:

| Key | `AppStrings.resx` (English) | `AppStrings.de.resx` (German) |
| --- | --- | --- |
| `PeopleInvited` | `{0} people invited` | `{0} Personen eingeladen` |

## In code

```csharp
Localizer.Current.Get(AppStringsKeys.PeopleInvited, peopleCount);
```

The resolved text and the arguments go to the localizer's formatter - by default `string.Format` in
the localizer's current culture, so a number or a date formats the way that culture writes it.

## In XAML

Bind the **`LocalizeArgs.Arg0`…`Arg8` attached properties** on the element that carries the localized
property — the same XAML in Avalonia and WPF:

```xml
<TextBlock l:LocalizeArgs.Arg0="{Binding PeopleCount}"
           Text="{l:Localize {x:Static res:AppStringsKeys.PeopleInvited}}" />
```

The rendered text re-formats **live** whenever a bound argument changes *and* whenever the language
switches: `PeopleCount = 5` renders `5 people invited`, and switching to German re-renders it in
place as `5 Personen eingeladen`.

## Worth knowing

- **Nine slots, `Arg0` through `Arg8`.** Arguments above the highest set slot are trimmed; a set slot
  with unset slots below it — say, only `Arg2` — formats the gaps as `null`, which renders empty.
- **Arguments are per element.** Two localized properties on the same element, for example a
  localized `Text` and a localized `ToolTip`, share the one argument set. Give each its own element
  when they need different arguments.
- **Per-argument format specifiers belong in the resource string** — `{0:N0} people invited` — where
  a translator can adjust them per language.
- **No arguments, no formatting.** Under the default formatter, a lookup without arguments returns the
  resource value unchanged - in code `Localizer.Current.Get(key, args)` with an empty array, like
  `Get(key)`, and in XAML an element without any `ArgN`. A resource value containing a literal `{` or
  `}` keeps working without `{{` escaping. The failure text of a missing key is never formatted.
- **Formatter exceptions are caught.** When a value cannot be formatted - `Broken {0`, or
  `File {0} of {1}` with one argument - the localizer raises `LookupFailed` and returns the
  failure text (by default `!key!`), in code and in XAML alike, provided the event handlers and
  `LookupFailureTextBuilder` complete successfully. An exception from an event handler or from
  `LookupFailureTextBuilder`, and a `null` result of the builder, propagate. See
  [Lookup modes and culture fallback](lookup-and-fallback.md#failure-text).
- **The formatter is replaceable.** Assign `Localizer.Current.MessageFormatter = ...`, usually at
  startup; changing it later re-resolves bound text like a language switch
  (`RefreshTriggeringRevision`). The default is `string.Format` in the localizer's current culture.
  The formatter sees **every** resolved value, in code and in XAML, also lookups without arguments
  (with an empty array), so it is the one place to post-process all translated texts; a missing key's
  failure text does not pass through it. A custom formatter should return the value unchanged when
  `arguments` is empty, unless it means to handle plain texts:

  ```csharp
  Localizer.Current.MessageFormatter = new MyFormatter();

  sealed class MyFormatter : IMessageFormatter
  {
      public string Format(string pattern, MessageArgument[] arguments, CultureInfo culture) =>
          arguments.Length == 0
              ? pattern // no arguments: do not format, so a literal { keeps working
              : string.Format(culture, pattern, arguments.Select(argument => argument.Value).ToArray());
  }
  ```
- **Keep it simple.** For a very complex string, compose the text in the view model with
  `Get(key, args…)` rather than wiring many argument slots.
- **Composite formatting does not pluralize.** `1 people invited` is on the resource author; use
  separate singular and plural resources where it matters.
