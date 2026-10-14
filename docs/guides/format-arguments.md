# Dynamic format arguments

A resource value can be a composite format string, and the arguments can come straight from your
view model. Add the entry as usual:

| Key | `AppStrings.resx` (English) | `AppStrings.de.resx` (German) |
| --- | --- | --- |
| `PeopleInvited` | `{0} people invited` | `{0} Personen eingeladen` |

## In code

Arguments are a `MessageArguments`, built with one of its static methods:

```csharp
Localizer.Current.Get(AppStringsKeys.PeopleInvited, MessageArguments.FromValues(peopleCount));
```

Every `Get` overload - string keys, typed keys and enum values - takes the arguments the same way, as
its optional second parameter.

The resolved text and the arguments go to the localizer's formatter - by default `string.Format` in
the localizer's current culture, so a number or a date formats the way that culture writes it.

### Building arguments

Each method returns a `MessageArguments` whose arguments have the positions 0, 1, 2, … in the order you pass
them:

- **`MessageArguments.FromValues(a, b, …)`** - positional arguments, for `{0}`, `{1}`, … This is the one to use with the
  default formatter: `Get("FilePosition", MessageArguments.FromValues(3, 10))` renders `File {0} of {1}` as
  `File 3 of 10`.
- **`MessageArguments.FromTuples(("Count", 5), ("Total", 10))`** - named arguments, in declaration order.
  A `null` name makes that argument positional only, so named and unnamed arguments can be mixed:
  `MessageArguments.FromTuples((null, file), ("Total", 10))`.
- **`MessageArguments.FromDictionary(dictionary)`** - named arguments from an existing
  `IReadOnlyDictionary<string, object?>`; positions follow its enumeration order.
- **`MessageArguments.FromObject(new { Count = 5, Total = 10 })`** - named arguments from the public instance properties
  of the object's static type, in declaration order. It is trim and Native AOT safe; a value typed
  `object` has no properties and yields no arguments.

The methods reject an empty string as a name and two arguments with the same name (compared
ordinally), so a `MessageArguments` is always valid. It cannot be changed after it is built;
`MessageArguments.Empty` stands for no arguments. An argument's position is its
`MessageArgument.Position`: its index in the enumeration of a `MessageArguments` is not guaranteed to be
the same, so a custom formatter reads `Position`.

**The `MessageArguments.FromValues(dictionary)` trap.** `FromValues` takes `params object?[]`, so
`MessageArguments.FromValues(dictionary)` is *one* positional argument whose value is the dictionary itself. Use
`MessageArguments.FromDictionary(dictionary)` for named arguments.

**Names matter only to a formatter that understands them.** The default `string.Format` formatter
ignores names and uses positions, so `MessageArguments.FromObject(new { Count = 1.5, Where = "x" })` fills `{0}` and `{1}`
exactly like `MessageArguments.FromValues(1.5, "x")`. A formatter that knows names (ICU MessageFormat) finds a named
argument both under its position and under its name.

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
  resource value unchanged - in code `Localizer.Current.Get(key, MessageArguments.FromValues())` or
  `Get(key, null)`, like `Get(key)`, and in XAML an element without any `ArgN`. A resource value containing a literal `{` or
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
  (with empty arguments), so it is the one place to post-process all translated texts; a missing key's
  failure text does not pass through it. A custom formatter should return the value unchanged when
  `arguments` is empty, unless it means to handle plain texts:

  ```csharp
  Localizer.Current.MessageFormatter = new MyFormatter();

  sealed class MyFormatter : IMessageFormatter
  {
      public string Format(string pattern, MessageArguments arguments, CultureInfo culture) =>
          arguments.Count == 0
              ? pattern // no arguments: do not format, so a literal { keeps working
              : string.Format(
                  culture,
                  pattern,
                  // Order by Position: the index in the enumeration is not guaranteed to be the position.
                  arguments.OrderBy(argument => argument.Position).Select(argument => argument.Value).ToArray()
              );
  }
  ```
- **Keep it simple.** For a very complex string, compose the text in the view model with
  `Get(key, MessageArguments.FromValues(…))` rather than wiring many argument slots.
- **Composite formatting does not pluralize.** `1 people invited` is on the resource author; use
  separate singular and plural resources where it matters.
