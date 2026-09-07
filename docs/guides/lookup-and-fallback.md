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
Localizer.Current.Get("Greeting", AppStrings.ResourceManager);

// Search-all: this key, in whichever registered file has it first.
Localizer.Current.Get("Greeting");
```

The same three modes exist in XAML:

```xml
<TextBlock Text="{l:Localize {x:Static res:AppStringsKeys.Greeting}}" />
<TextBlock Text="{l:Localize Key=Greeting, ResourceManager={x:Static res:AppStrings.ResourceManager}}" />
<TextBlock Text="{l:Localize Greeting}" />
```

## Registration is only for search-all

```csharp
Localizer.Current.RegisterResourceManager(AppStrings.ResourceManager);
```

Search-all inspects the registered managers **in registration order** and takes the first hit, so
registration order is what decides which file wins when two of them carry the same key name. Typed
and scoped lookups need no registration at all — they already name the file.

`UnregisterResourceManager` and `ClearResourceManagers` remove managers again, for an application
that loads resources dynamically.

> [!TIP]
> In Avalonia, put the registration and the initial `CurrentCulture` inside `BuildAvaloniaApp()`
> rather than `Main`. `BuildAvaloniaApp` runs at run time *and* under the XAML previewer, so
> search-all lookups resolve at design time too instead of showing the `!Greeting!` sentinel.

## Culture fallback

Fallback is .NET's own `ResourceManager` behaviour, unchanged. The localizer sits on top of it.

| Situation | Result | Raises `TranslationNotFound` |
| --- | --- | --- |
| `de-DE` entry exists | the `de-DE` value | No |
| Missing in `de-DE`, exists in `de` | the parent `de` value | No |
| Missing in every satellite, exists in the neutral resources | the neutral value | No |
| Missing across the complete fallback chain | `!key!` by default | Yes |

Because fallback runs first, a key that resolves from a parent or from the neutral file does **not**
count as missing. The sentinel and the event report keys that cannot be resolved at all — they are
not a report of incomplete per-language coverage.

## Missing-translation diagnostics

```csharp
// Change the sentinel, or make it invisible in production.
Localizer.MissingTranslationFormat = "«{0}»";

// Log every unresolvable key.
Localizer.Current.TranslationNotFound += (_, args) => logger.LogWarning("No translation for {Key}", args.Key);
```

The default sentinel is `!key!`, which is deliberately ugly: a missing translation should be
noticeable in a screenshot.

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

```csharp
services.AddSingleton<ILocalizer>(_ => Localizer.Current);
```

Alternatively, assign a container-owned implementation to `Localizer.Current` before any view is
created. The property rejects `null`, and the markup extensions always read its current value, so a
replacement is picked up everywhere.

## Lifetime and subscriptions

`Localizer.Current` lives for the whole process, so a strong `CultureChanged` subscription keeps its
subscriber alive for the whole process too. A short-lived subscriber — a view model owned by a
window, say — should unsubscribe when it is disposed. Both sample view models show the pattern.

The bindings themselves need no care: Avalonia's markup extensions subscribe through weak events,
and WPF binds to the singleton through WPF's own weak binding-target references, so a discarded
control stays collectable.
