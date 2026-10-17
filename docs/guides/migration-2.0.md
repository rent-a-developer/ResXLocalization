# Migrating to 2.0

2.0 is a major version; each section below is one breaking change, with what to do.

## A formatting failure returns the failure text instead of throwing

A resource value that cannot be formatted with the supplied arguments - `Broken {0`, or
`File {0} of {1}` with one argument - no longer throws `FormatException` from
`Localizer.Current.Get(key, args)` and its scoped and typed siblings. The lookup returns the failure
text (by default `!key!`) and raises `ILocalizer.LookupFailed` with
`LookupFailureReason.MessageFormatFailed`. Code that caught `FormatException` subscribes to the event
instead.

Before:

```csharp
try
{
    text = Localizer.Current.Get(AppStringsKeys.FilesCopied, copied);
}
catch (FormatException exception)
{
    logger.LogWarning(exception, "Broken translation");
}
```

After:

```csharp
Localizer.Current.LookupFailed += (_, args) =>
    logger.LogWarning(args.Failure.Exception, "Broken translation {Key} in {Culture}", args.Failure.Key, args.Failure.Culture);

text = Localizer.Current.Get(AppStringsKeys.FilesCopied, copied);
```

## A formatting failure in XAML renders the failure text

`{l:Localize}` with `LocalizeArgs` arguments whose resource value cannot be formatted renders the
failure text, in both frameworks, and raises `LookupFailed`. In 1.x, Avalonia reported a binding
error and kept the previous text, and WPF showed an empty text.

Before (1.x): an Avalonia binding error, or an empty WPF `TextBlock`.

After:

```xml
<!-- Resource "FilesCopied" = "Copied {0 files" -->
<TextBlock l:LocalizeArgs.Arg0="{Binding Copied}"
           Text="{l:Localize {x:Static res:AppStringsKeys.FilesCopied}}" />
<!-- renders "!FilesCopied!" and raises Localizer.Current.LookupFailed -->
```

## An empty arguments array no longer formats

`Localizer.Current.Get(key, args)` with an empty array (from 2.0, `Get(key, MessageArguments.FromValues())`) returns the resource value unformatted, exactly
like `Get(key)`: `{{x}}` stays `{{x}}`, and a value containing `{0}` no longer throws.

Before:

```csharp
// Resource "Template" = "Literal {{braces}} for {0}"
Localizer.Current.Get("Template", Array.Empty<object?>()); // 1.x: FormatException
```

After:

```csharp
Localizer.Current.Get("Template", MessageArguments.FromValues()); // "Literal {{braces}} for {0}"
```

## Custom `ILocalizer` implementations have new members

A custom `ILocalizer` implementation must implement:

- `ILocalizer.LookupFailed`, replacing `TranslationNotFound` (see below);
- `ILocalizer.MessageFormatter`;
- `ILocalizer.RefreshTriggeringRevision` - advance it and raise `PropertyChanged` on every change that
  can change a lookup's result; from 2.0 the XAML bindings re-resolve on it, not on `CurrentCulture`;
- `ILocalizer.Get(string key, MessageArguments? arguments = null, ResourceManager? resourceManager = null)`,
  the one entry point every lookup goes through, together with the other two `Get` overloads and
  `EnumKeyBuilder` (see
  [A custom `ILocalizer` implements three `Get` overloads](#a-custom-ilocalizer-implements-three-get-overloads-and-enumkeybuilder)).

It also replaces `MissingTranslationFormat` with `LookupFailureTextBuilder` (see below), raises
`LookupFailed` and builds the failure text for both `LookupFailureReason`s.

Before:

```csharp
public sealed class MyLocalizer : ILocalizer
{
    public event EventHandler<TranslationNotFoundEventArgs>? TranslationNotFound;
    public string MissingTranslationFormat { get; set; } = "!{0}!";
    // ...
}
```

After:

```csharp
public sealed class MyLocalizer : ILocalizer
{
    public event EventHandler<LookupFailedEventArgs>? LookupFailed;
    public IMessageFormatter MessageFormatter { get; set; } = StringFormatMessageFormatter.Instance;
    public int RefreshTriggeringRevision { get; private set; }
    public Func<LookupFailure, string> LookupFailureTextBuilder { get; set; } = failure => $"!{failure.Key}!";

    public string Get(string key, MessageArguments? arguments = null, ResourceManager? resourceManager = null) =>
        /* resolve, then MessageFormatter.Format(value, arguments, CurrentCulture);
           on a failure raise LookupFailed, then return LookupFailureTextBuilder(failure) */;
    // ...
}
```

## `MissingTranslationFormat` is replaced by `LookupFailureTextBuilder`

`ILocalizer.MissingTranslationFormat`, a composite format string, is replaced by
`ILocalizer.LookupFailureTextBuilder`, a `Func<LookupFailure, string>`. It produces the failure text for
a missing translation and, new in 2.0, for a formatting failure; `LookupFailure.Reason` tells them
apart. The compiler reports every use of the old property; code that read the format string calls the
function instead. The `ArgumentException` for a malformed format no longer exists. The documentation
calls the text the "failure text" instead of the "sentinel".

Before:

```csharp
Localizer.Current.MissingTranslationFormat = "[{0}]";
```

After:

```csharp
Localizer.Current.LookupFailureTextBuilder = failure => $"[{failure.Key}]";
```

See [Lookup modes and culture fallback](lookup-and-fallback.md#failure-text) for more examples.

## `TranslationNotFound` is replaced by `LookupFailed`

`ILocalizer.TranslationNotFound` and `TranslationNotFoundEventArgs` are replaced by
`ILocalizer.LookupFailed` and `LookupFailedEventArgs`. The new event reports every failed lookup - a key
that no resource file contains, and, new in 2.0, a value that cannot be formatted. Its
`LookupFailedEventArgs.Failure` is a `LookupFailure`, the same value `LookupFailureTextBuilder`
receives; `Failure.Reason` tells the two cases apart. To keep logging only missing translations, check
the reason.

Before:

```csharp
Localizer.Current.TranslationNotFound += (_, args) =>
    logger.LogWarning("No translation for {Key} in {Culture}", args.Key, args.Culture);
```

After:

```csharp
Localizer.Current.LookupFailed += (_, args) =>
{
    if (args.Failure.Reason == LookupFailureReason.TranslationNotFound)
    {
        logger.LogWarning("No translation for {Key} in {Culture}", args.Failure.Key, args.Failure.Culture);
    }
};
```

## Format arguments are built with `MessageArguments`

The `Get` overloads with `params object?[]` are removed. Arguments are a `MessageArguments`, built with
its static methods. A dictionary, tuple or anonymous object passed to the old overloads silently became
argument `{0}`; the new parameter accepts nothing but a `MessageArguments`.

Before:

```csharp
Localizer.Current.Get("FilePosition", a, b);
```

After:

```csharp
Localizer.Current.Get("FilePosition", MessageArguments.FromValues(a, b));
```

## A scoped lookup names the manager

The resource manager of a scoped lookup is an optional parameter after the arguments, so it is passed
by name or after the arguments.

Before:

```csharp
Localizer.Current.Get("Key", rm);
Localizer.Current.Get("Key", rm, a, b);
```

After:

```csharp
Localizer.Current.Get("Key", resourceManager: rm);
Localizer.Current.Get("Key", MessageArguments.FromValues(a, b), rm);
```

## An enum key prefix is named

The enum overload's optional parameters are `arguments`, `resourceManager` and `keyPrefix`, in that
order.

Before:

```csharp
Localizer.Current.Get(sortOrder, "Display_");
Localizer.Current.Get(sortOrder, rm, "Display_");
```

After:

```csharp
Localizer.Current.Get(sortOrder, keyPrefix: "Display_");
Localizer.Current.Get(sortOrder, resourceManager: rm, keyPrefix: "Display_");
```

## `null` no longer throws for a manager or a key prefix

A `null` resource manager means search-all, and a `null` key prefix means the default convention of
`ILocalizer.EnumKeyBuilder`. In 1.x both threw `ArgumentNullException`. This applies in code and in
XAML: `LocalizeEnumExtension.KeyPrefix` and `LocalizeEnumConverter.KeyPrefix` are `string?` and default
to `null`, and `KeyPrefix="{x:Null}"` no longer throws.

Before:

```csharp
Localizer.Current.Get("Key", (ResourceManager)null!);    // 1.x: ArgumentNullException
Localizer.Current.Get(sortOrder, (string)null!);         // 1.x: ArgumentNullException
```

After:

```csharp
Localizer.Current.Get("Key", resourceManager: null);     // search-all
Localizer.Current.Get(sortOrder, keyPrefix: null);       // the EnumKeyBuilder's default, Enum_
```

## Recompile assemblies that call `Get`

Every 1.x `Get` signature is removed, including `Get(string)` and `Get(ResourceKey)`, whose calls still
compile unchanged against 2.0. An assembly compiled against 1.x throws `MissingMethodException` at the
first `Get` call until it is recompiled.

## A custom `ILocalizer` implements three `Get` overloads and `EnumKeyBuilder`

A custom `ILocalizer` implementation replaces its `Get` overloads with exactly three, one per key kind,
and implements `EnumKeyBuilder`.

Before:

```csharp
public string Get(string key) => /* ... */;
public string Get(string key, ResourceManager resourceManager) => /* ... */;
public string Get(string key, params object?[] arguments) => /* ... */;
public string Get(string key, ResourceManager resourceManager, params object?[] arguments) => /* ... */;
public string Get(ResourceKey key) => /* ... */;
public string Get(ResourceKey key, params object?[] arguments) => /* ... */;
public string Get(Enum value, string keyPrefix = "Enum_") => /* ... */;
public string Get(Enum value, ResourceManager resourceManager, string keyPrefix = "Enum_") => /* ... */;
```

After:

```csharp
public Func<Enum, string?, string> EnumKeyBuilder { get; set; } =
    static (value, keyPrefix) => (keyPrefix ?? "Enum_") + value.GetType().Name + "_" + value;

public string Get(string key, MessageArguments? arguments = null, ResourceManager? resourceManager = null) =>
    /* the one lookup; null arguments means none, a null manager means search-all */;

public string Get(ResourceKey key, MessageArguments? arguments = null) =>
    this.Get(key.Name, arguments, key.Manager);

public string Get(Enum value, MessageArguments? arguments = null, ResourceManager? resourceManager = null, string? keyPrefix = null) =>
    this.Get(this.EnumKeyBuilder(value, keyPrefix), arguments, resourceManager);
```

## Optional: set the enum key prefix once

Not a break. An application that passes the same prefix on every enum lookup can make it the default
of `ILocalizer.EnumKeyBuilder` instead, for code and XAML alike.

Before:

```csharp
Localizer.Current.Get(sortOrder, "MyPrefix_");
```

```xml
<TextBlock Text="{l:LocalizeEnum KeyPrefix=MyPrefix_}" />
```

After:

```csharp
Localizer.Current.EnumKeyBuilder = static (value, keyPrefix) =>
    (keyPrefix ?? "MyPrefix_") + value.GetType().Name + "_" + value;

Localizer.Current.Get(sortOrder);
```

```xml
<TextBlock Text="{l:LocalizeEnum}" />
```

## Typed keys use the generated `ResourceManager`

Every generated `<Name>Keys` class has its own `ResourceManager` property, created from the name the
`.resx` is embedded under, and every key of the class uses it instead of the designer's
`<Name>.ResourceManager`. Lookups return the same text. Registering or scoping with the designer's
instance still works, but it is a second instance of the same file with its own cache; use
`<Name>Keys.ResourceManager`.

The generator reads that name from the analyzer config file the SDK writes into `obj/`, so every keys
class - with or without a designer - now depends on it. A project that sets
`GenerateMSBuildEditorConfigFile` to `false` gets `RXLGEN003` and no keys classes; remove that setting.

Before:

```csharp
ReferenceEquals(AppStringsKeys.Greeting.Manager, AppStrings.ResourceManager);   // true

Localizer.Current.RegisterResourceManager(AppStrings.ResourceManager);
Localizer.Current.Get("Greeting", resourceManager: AppStrings.ResourceManager);
```

After:

```csharp
ReferenceEquals(AppStringsKeys.Greeting.Manager, AppStringsKeys.ResourceManager); // true
ReferenceEquals(AppStringsKeys.Greeting.Manager, AppStrings.ResourceManager);     // false

Localizer.Current.RegisterResourceManager(AppStringsKeys.ResourceManager);
Localizer.Current.Get("Greeting", resourceManager: AppStringsKeys.ResourceManager);
```

## More `.resx` files produce keys classes

Every neutral `.resx` now produces a keys class: a file without a `.Designer.cs`, a neutral file with a
dot in its name, and the files of a project that uses the SDK's `GenerateResxSource`. `RXLGEN002` is no
longer reported. If a new `<Name>Keys` class collides with a type of your own, rename the `.resx`.

Before:

```text
Resources/Errors.resx (no designer)   ->  no ErrorsKeys, warning RXLGEN002
Resources/My.Strings.resx             ->  no keys class (taken for a satellite)
```

After:

```text
Resources/Errors.resx (no designer)   ->  ErrorsKeys
Resources/My.Strings.resx             ->  My_StringsKeys
```

## Two files that produce the same keys class are no longer merged

Two neutral `.resx` files that produce the same namespace and class name - for example two
`Strings.resx` in different folders whose designers declare the same namespace - were merged into one
partial class. They now report `RXLGEN004`, and neither class is generated. Rename one of the files, or
change its designer's namespace or its `CustomToolNamespace`.

Before:

```text
A/Strings.resx + B/Strings.resx, both designers in namespace MyApp  ->  one partial MyApp.StringsKeys
```

After:

```text
A/Strings.resx + B/Strings.resx, both designers in namespace MyApp  ->  RXLGEN004 at each file, no StringsKeys
B/Strings.resx renamed to B/MoreStrings.resx                         ->  MyApp.StringsKeys and MyApp.MoreStringsKeys
```

## A wrong base name fails at registration

`Localizer.RegisterResourceManager` checks a plain `ResourceManager` when it is registered. If no
resources exist under its base name - usually a hand-written base name with a typo - it throws an
`ArgumentException` that names the base name, and the manager is not registered. Before, the manager
was registered, and every search-all lookup that reached it threw `MissingManifestResourceException`.
A subclass of `ResourceManager` is registered without that check, as before.

Fix the base name, or register the generated `<Name>Keys.ResourceManager`.

Before:

```csharp
Localizer.Current.RegisterResourceManager(new ResourceManager("MyApp.Strngs", typeof(App).Assembly));  // registered
Localizer.Current.Get("Greeting");  // MissingManifestResourceException
```

After:

```csharp
Localizer.Current.RegisterResourceManager(new ResourceManager("MyApp.Strngs", typeof(App).Assembly));
// ArgumentException: No resources exist under the base name 'MyApp.Strngs'. ...

Localizer.Current.RegisterResourceManager(AppStringsKeys.ResourceManager);
```
