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

The resolved text and the arguments go to `string.Format` in the localizer's current culture, so a
number or a date formats the way that culture writes it.

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
- **No arguments set, no formatting.** An element without any `ArgN` resolves without composite
  formatting at all, so a resource value containing a literal `{` or `}` keeps working without `{{`
  escaping. The missing-key sentinel is likewise never formatted.
- **Keep it simple.** For a very complex string, compose the text in the view model with
  `Get(key, args…)` rather than wiring many argument slots.
- **Composite formatting does not pluralize.** `1 people invited` is on the resource author; use
  separate singular and plural resources where it matters.
