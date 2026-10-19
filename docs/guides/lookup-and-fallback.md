# Lookup modes and culture fallback

Three ways to ask for a string, one fallback story, and one way to find out when a key does not
resolve.

## The three lookup modes

| Mode | What you pass | When to use it |
| --- | --- | --- |
| **Typed** | a generated `ResourceKey` | Almost always. The key name is compile-checked, and the key carries its own `ResourceManager`, so the lookup goes straight to one file |
| **Scoped** | a key name and a `ResourceManager` | A key that is not eligible for generation, or a name assembled at run time, in a known file |
| **Search-all** | a key name | A key that could live in any of several files, and you would rather not say which |

```csharp
// Typed: the generated key knows which .resx it came from.
Localizer.Current.Get(AppStringsKeys.Greeting);

// Scoped: this key, in this file.
Localizer.Current.Get("Greeting", resourceManager: AppStringsKeys.ResourceManager);

// Search-all: this key, in whichever registered file has it first.
Localizer.Current.Get("Greeting");
```

`ILocalizer` has one `Get` per key kind - a string key, a typed `ResourceKey` and an enum value - and
each takes optional [format arguments](format-arguments.md) as its second parameter:

```csharp
string Get(string key, MessageArguments? arguments = null, ResourceManager? resourceManager = null);
string Get(ResourceKey key, MessageArguments? arguments = null);
string Get(Enum value, MessageArguments? arguments = null, ResourceManager? resourceManager = null, string? keyPrefix = null);
```

A scoped lookup therefore names the manager: `Get("Greeting", resourceManager: rm)`, or
`Get("Greeting", MessageArguments.FromValues(a), rm)` with arguments. Without a manager, or with
`null`, the lookup searches all registered managers.

The same three modes exist in XAML:

```xml
<TextBlock Text="{l:Localize {x:Static res:AppStringsKeys.Greeting}}" />
<TextBlock Text="{l:Localize Key=Greeting, ResourceManager={x:Static res:AppStringsKeys.ResourceManager}}" />
<TextBlock Text="{l:Localize Greeting}" />
```

## A key from the view model

When the view model decides *which* message to show - e.g. a status that can be "Connecting", "Connected"
or "Failed" - bind the key instead of writing it in the XAML:

```xml
<TextBlock Text="{l:Localize {Binding StatusKey}}" />            <!-- a ResourceKey: recommended -->
<TextBlock Text="{l:Localize Key={Binding StatusKeyName}}" />    <!-- a string key: the escape hatch -->

<l:Localize Key="{Binding StatusKey}">                            <!-- a bound key with arguments -->
  <l:Argument Value="{Binding Count}" />
</l:Localize>
```

```csharp
public ResourceKey StatusKey => this.IsConnected ? AppStringsKeys.Connected : AppStringsKeys.Connecting;
```

Bind a `ResourceKey`. The view model then picks one of the generated keys, so renaming or deleting a
resource is still a build error. Binding a `string` key works too, but a typo in that string is not
caught by the compiler: the lookup fails, and the text shows the [failure text](#failure-text)
(`!key!` by default).

`{l:Localize}` reads the bound value again whenever the view model reports a change of the bound
property through `INotifyPropertyChanged`, and whenever `Localizer.Current.RefreshTriggeringRevision`
changes - for example on a language switch. Depending on the bound value, the text is:

| Bound value | Text shown |
| --- | --- |
| a non-empty `string` | the translation of that key, looked up exactly like a key written in the XAML: in the file that `ResourceManager` names if `ResourceManager` is set, otherwise in all registered files (search-all) |
| a `ResourceKey` | the translation of that key, looked up in the file the `ResourceKey` belongs to; `ResourceManager` is ignored |
| an empty string (`""`) | an empty string - the same result as `Localizer.Current.Get("")` in code |
| nothing yet, because the element has no `DataContext` yet | an empty string |
| `null` | an empty string. A view model can therefore leave the key property `null` while it has nothing to show - for example before the first connection attempt, when there is no status yet - and the element simply stays empty |
| `default(ResourceKey)`, or a value of any other type, such as an `int` | an empty string; `Localizer.Current.LookupFailed` is not raised |

No bound value makes `{l:Localize}` throw an exception: a value that is neither a non-empty `string`
nor a `ResourceKey` always renders an empty string. Format arguments (`<l:Argument>` children)
work with a bound key exactly as with a key written in the XAML.

Worth knowing:

- **WPF accepts a plain `{Binding}` only.** In WPF, a `MultiBinding` or `PriorityBinding` as the key is
  rejected with an `InvalidOperationException` when the view loads. Avalonia accepts every binding kind.
- **Missing `x:DataType` inference.** Rider and Visual Studio with ReSharper may not infer the
  `x:DataType` for a binding nested inside `{l:Localize}`. Code completion and go-to-definition navigation
  for the bound property then do not work in the editor; the binding itself still works at run time.
  To get them back, add a `d:DataContext` to the view, or write `{CompiledBinding ...}` instead of
  `{Binding ...}`.

## Registration is only for search-all

```csharp
Localizer.Current.RegisterResourceManager(AppStringsKeys.ResourceManager);
```

Search-all inspects the registered managers **in registration order** and takes the first hit, so
registration order is what decides which file wins when two of them carry the same key name. Typed
and scoped lookups need no registration at all — they already name the file.

`UnregisterResourceManager` and `ClearResourceManagers` remove managers again, for an application
that loads resources dynamically.

Register the generated `<Name>Keys.ResourceManager`, the instance the typed keys use. Where to
register, class libraries, resources from other assemblies and the WPF designer are covered in
[Registering resource files](registration.md).

> [!WARNING]
> A designer property such as `AppStrings.Greeting` - in code, or through
> `{x:Static res:AppStrings.Greeting}` - reads in `CultureInfo.CurrentUICulture`, which the localizer
> deliberately never changes. Such values do not follow `Localizer.CurrentCulture` or live switching,
> and can show a different language from the localized bindings next to them. Read strings through
> typed keys, `Get` or the markup extensions.

> [!TIP]
> In Avalonia, put the registration and the initial `CurrentCulture` inside `BuildAvaloniaApp()`
> rather than `Main`. `BuildAvaloniaApp` runs at run time *and* under the XAML previewer, so
> search-all lookups resolve at design time too instead of showing the `!Greeting!` failure text.

## Culture fallback

Fallback is .NET's own `ResourceManager` behaviour, unchanged. The localizer sits on top of it.

| Situation | Result | Raises `LookupFailed` |
| --- | --- | --- |
| `de-DE` entry exists | the `de-DE` value | No |
| Missing in `de-DE`, exists in `de` | the parent `de` value | No |
| Missing in every satellite, exists in the neutral resources | the neutral value | No |
| Missing across the complete fallback chain | `!key!` by default | Yes |

Because fallback runs first, a key that resolves from a parent or from the neutral file does **not**
count as missing. The failure text and the event report keys that cannot be resolved at all — they
are not a report of incomplete per-language coverage.

## Missing-translation diagnostics

```csharp
// Log every key that cannot be resolved, and every value that cannot be formatted.
Localizer.Current.LookupFailed += (_, args) =>
{
    var failure = args.Failure;

    if (failure.Reason == LookupFailureReason.TranslationNotFound)
    {
        logger.LogWarning("No translation for {Key} in {Culture}", failure.Key, failure.Culture);
    }
    else
    {
        logger.LogWarning(failure.Exception, "Cannot format {Key} in {Culture}: {Pattern}", failure.Key, failure.Culture, failure.Pattern);
    }
};
```

`LookupFailed` reports both kinds of failure, and `LookupFailure.Reason` tells them apart:
`TranslationNotFound` for a key that no resource file contains, `MessageFormatFailed` for a key that
was found but whose value could not be formatted - for example `Broken {0`, or `File {0} of {1}` with
one argument. Both lookups return the failure text.

## Failure text

The failure text is what a lookup returns instead of a translation when it cannot produce one: the key
is in no resource file, or its value could not be formatted. It is produced by the function
`LookupFailureTextBuilder`, which receives the same `LookupFailure` that `LookupFailed` reports - the
reason, the key, the culture, the resource manager of a scoped or typed lookup, the arguments, and for a
formatting failure the pattern and the exception.

```csharp
// Wrap the key differently, or make it invisible in production.
Localizer.Current.LookupFailureTextBuilder = failure => $"«{failure.Key}»";

// Show only the last segment of a dotted key.
Localizer.Current.LookupFailureTextBuilder = failure => failure.Key[(failure.Key.LastIndexOf('.') + 1)..];

// Tell the two reasons apart.
Localizer.Current.LookupFailureTextBuilder = failure =>
    failure.Reason == LookupFailureReason.TranslationNotFound
        ? $"[missing: {failure.Key}]"
        : $"[broken: {failure.Key}]";
```

The default produces `!key!`, which is deliberately ugly: a missing translation should be noticeable
in a screenshot. A formatting failure uses the same function.

The function is called on every failed lookup, so keep it cheap. It must not throw or return `null`:
an exception it throws propagates out of the lookup, and a `null` result throws
`InvalidOperationException`. An empty string is fine and shows nothing. Changing the function
re-resolves bound text.

## Refreshing localized text

`ILocalizer.RefreshTriggeringRevision` changes whenever something changes that can change the result
of a lookup: the culture, the formatter, the lookup-failure text builder, the enum key builder
(`EnumKeyBuilder`, see [Localizing enum values](enums.md#changing-the-key-convention)), or the registered
resource managers (a manager registered or unregistered, or the registrations cleared). For the
formatter and the two builders, assigning a different instance counts; assigning the current instance
again does nothing. Only whether the value
differs is meaningful, never the number itself.

Bound text re-resolves on each of these changes. Strings fetched in code with `Get` are not
updated; re-fetch them when the localizer raises
`PropertyChanged(nameof(ILocalizer.RefreshTriggeringRevision))`. Make these changes on the UI thread
once views exist.

## Available cultures

```csharp
foreach (var culture in Localizer.Current.GetAvailableCultures())
{
    // The invariant culture stands for the neutral resources compiled into the assembly.
}
```

`GetAvailableCultures()` reports the cultures the application actually ships, by asking each
registered resource manager which satellite assemblies exist. It is what a language picker binds to.

## Dependency injection

The recommended form hands the ambient instance to the container:

```csharp
services.AddSingleton<ILocalizer>(_ => Localizer.Current);
```

Alternatively, assign a container-owned implementation to `Localizer.Current` before any view is
created. The property rejects `null`, and the markup extensions always read its current value, so a
replacement is picked up everywhere.

Registrations belong to the instance they were made on and do not move with the assignment. An
application that assigns its own instance to `Localizer.Current` must register its resource managers
on **that** instance. A registration in a `[ModuleInitializer]` of the executable always lands in the
default instance, because it runs before `Main`.

## Lifetime and subscriptions

`Localizer.Current` lives for the whole process, so a strong `CultureChanged` subscription keeps its
subscriber alive for the whole process too. A short-lived subscriber — a view model owned by a
window, say — should unsubscribe when it is disposed. Both sample view models show the pattern.

The bindings themselves need no care: Avalonia's markup extensions subscribe through weak events,
and WPF binds to the singleton through WPF's own weak binding-target references, so a discarded
control stays collectable.
