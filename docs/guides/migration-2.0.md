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

`Localizer.Current.Get(key, args)` with an empty array returns the resource value unformatted, exactly
like `Get(key)`: `{{x}}` stays `{{x}}`, and a value containing `{0}` no longer throws.

Before:

```csharp
// Resource "Template" = "Literal {{braces}} for {0}"
Localizer.Current.Get("Template", Array.Empty<object?>()); // 1.x: FormatException
```

After:

```csharp
Localizer.Current.Get("Template", Array.Empty<object?>()); // "Literal {{braces}} for {0}"
```

## Custom `ILocalizer` implementations have new members

A custom `ILocalizer` implementation must implement:

- `ILocalizer.LookupFailed`, replacing `TranslationNotFound` (see below);
- `ILocalizer.MessageFormatter`;
- `ILocalizer.RefreshTriggeringRevision` - advance it and raise `PropertyChanged` on every change that
  can change a lookup's result; from 2.0 the XAML bindings re-resolve on it, not on `CurrentCulture`;
- the hidden `ILocalizer.Get(string key, MessageArgument[] arguments, ResourceManager? resourceManager)`,
  the one entry point every lookup goes through.

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

    public string Get(string key, MessageArgument[] arguments, ResourceManager? resourceManager) =>
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
