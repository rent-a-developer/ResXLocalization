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

Give `{l:Localize}` one **`<l:Argument>` child** per argument - the same XAML in Avalonia and WPF:

```xml
<TextBlock>
  <TextBlock.Text>
    <l:Localize Key="{x:Static res:AppStringsKeys.PeopleInvited}">
      <l:Argument Value="{Binding PeopleCount}" />
    </l:Localize>
  </TextBlock.Text>
</TextBlock>
```

The rendered text re-formats **live** whenever a bound argument changes *and* whenever the language
switches: `PeopleCount = 5` renders `5 people invited`, and switching to German re-renders it in
place as `5 Personen eingeladen`.

`Arguments` is the content property of `{l:Localize}`, so the `<l:Argument>` children need no wrapper;
the explicit form is `<l:Localize.Arguments>`. The first `<l:Argument>` is `{0}`, the second `{1}`, and
so on. There is no upper limit, and each localized property has its own arguments - a localized `Text`
and a localized `ToolTip` on the same element can format different values.

`{l:LocalizeEnum}` takes the same `<l:Argument>` children, and `LocalizeEnumConverter` takes its
arguments as extra `MultiBinding` children; see [Localizing enum values](enums.md#with-format-arguments).

### Bound and constant values

`Value` takes a binding or a constant:

```xml
<l:Localize Key="Summary">
  <l:Argument Value="{Binding VolumeLabel}" />
  <l:Argument Value="{x:Static app:AppInfo.ProductName}" />
  <l:Argument Value="Text" />
  <l:Argument Value="{x:Null}" />
</l:Localize>
```

A constant is a literal, `{x:Static}`, `{StaticResource}`, `{x:Null}`, or a typed property element such
as `<x:Int32>` in Avalonia or `<system:Int32>` in WPF. A value that is not resolved yet - for example
before the `DataContext` arrives - is passed as `null`, and the default formatter (`StringFormatMessageFormatter`) renders `null` as empty.

### Named arguments

An argument can have a name, in XAML and in code alike:

```xml
<l:Localize Key="FilePosition">
  <l:Argument Value="{Binding Index}" />
  <l:Argument Name="Total" Value="{Binding Count}" />
</l:Localize>
```

```csharp
Localizer.Current.Get("FilePosition", MessageArguments.FromTuples((null, index), ("Total", count)));
```

Both become `MessageArgument`s - a position, an optional name and the resolved value - which is what a
custom formatter receives. The position of an `<l:Argument>` is its index among the children, whether it
has a name or not; the default formatter ignores names, so `File {0} of {1}` renders `File 3 of 10`
for `Index` 3 and `Count` 10. A formatter that understands names (ICU MessageFormat) finds a named
argument under its name too.

## Worth knowing

- **Arguments always need element syntax.** A localized text with arguments is written as a
  `<l:Localize>` element with `<l:Argument>` children; the attribute form `{l:Localize Key}` has no
  arguments.
- **Order is meaning.** Under the default formatter, reordering the `<l:Argument>` elements changes the
  output, no matter whether names are set on the arguments.
- **`Value="5"` is a string.** The default formatter renders it like the number 5; a constant that must
  be a number is written `{x:Static}`, `<x:Int32>` (Avalonia) or `<system:Int32>` (WPF).
- **`{x:Null}` passes `null`; a forgotten `Value` throws.** `<l:Argument />` without a `Value`, an empty-string
  `Name`, and two arguments with the same `Name` are rejected with an `InvalidOperationException` when
  the view loads.
- **Supported binding kinds.** An argument binding behaves like an ordinary binding in the same place.
  Avalonia: `{Binding}`, `{CompiledBinding}`, `{ReflectionBinding}`, `#element` and `$parent` paths,
  `MultiBinding`, `{DynamicResource}` and `{TemplateBinding}`. WPF: a plain `{Binding}`, including
  `ElementName` and `RelativeSource`; a `MultiBinding` or `PriorityBinding` is rejected with an
  `InvalidOperationException`, and `{DynamicResource}` fails with WPF's own `XamlParseException`.
- **Missing `x:DataType` inference.** Rider and Visual Studio with ReSharper may not infer `x:DataType`
  for a binding nested in `<l:Localize>`, so completion and navigation are lost there; the binding works
  at run time. Add a `d:DataContext`, or write `{CompiledBinding ...}`.
- **Trim safety is your binding setting.** In Avalonia, `Value="{Binding ...}"` is a compiled binding
  when `AvaloniaUseCompiledBindingsByDefault` is `true` (or under `x:CompileBindings="True"`), and a
  reflection binding otherwise. Compiled bindings are trim and Native AOT safe.
- **No target element needed.** Arguments live on the binding, so `{l:Localize}` with arguments works as
  a `Style` setter value, in a resource and in a `DataTemplate`, and re-renders on every refresh there.
- **Per-argument format specifiers belong in the resource string** — `{0:N0} people invited` — where
  a translator can adjust them per language.
- **No arguments, no formatting.** Under the default formatter, a lookup without arguments returns the
  resource value unchanged - in code `Localizer.Current.Get(key, MessageArguments.FromValues())` or
  `Get(key, null)`, like `Get(key)`, and in XAML a `{l:Localize}` without `<l:Argument>` children.
   A resource value containing a literal `{` or `}` keeps working without `{{` escaping. The failure
   text of a missing key is never formatted.
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
  `Get(key, MessageArguments.FromValues(…))` rather than writing many `<l:Argument>` elements.
- **Composite formatting does not pluralize.** `1 people invited` is on the resource author; use
  separate singular and plural resources where it matters.
