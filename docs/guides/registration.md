# Registering resource files

A search-all lookup - a key without a resource manager - searches the resource files you registered.
This page covers what to register, where, and in which order.

## Registration is only for search-all

Registration is needed only for lookups that name no resource manager:

```csharp
Localizer.Current.Get("Greeting");               // search-all
Localizer.Current.Get(FileSortOrder.Ascending);  // search-all, enum key
```

```xml
<TextBlock Text="{l:Localize Greeting}" />
<TextBlock Text="{l:LocalizeEnum}" />
```

Typed and scoped lookups need none - they already name the file:

```csharp
Localizer.Current.Get(AppStringsKeys.Greeting);
Localizer.Current.Get("Greeting", resourceManager: AppStringsKeys.ResourceManager);
```

With nothing registered, every search-all lookup returns the failure text, `!Greeting!` by default.

## Register the generated resource manager

```csharp
Localizer.Current.RegisterResourceManager(AppStringsKeys.ResourceManager);
```

`AppStringsKeys.ResourceManager` is the instance every typed key of `AppStrings.resx` uses (see
[Generated keys](../reference/generated-keys.md)). Registering the designer's
`AppStrings.ResourceManager` works too, but it is a second instance of the same file with its own
cache, and `UnregisterResourceManager` finds only the instance that was registered.

`RegisterResourceManager` checks a hand-written `ResourceManager` once, when it is registered: if no
resources exist under its base name, it throws an `ArgumentException` that names the base name, and
the manager is not registered. A subclass of `ResourceManager` is registered without that check.

## Order

Search-all searches the registered files **in registration order**, and the first match wins. When two
files carry the same key name, the one registered first decides. Registering the same instance twice
has no effect.

To keep a file out of search-all - an internal branding file, say - do not register it. Its typed keys
and scoped lookups still work.

## Where to register

### Avalonia

Register in `BuildAvaloniaApp`. It runs at run time and in the Avalonia previewer, so search-all
lookups resolve in the previewer too.

```csharp
internal static class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    private static AppBuilder BuildAvaloniaApp()
    {
        Localizer.Current.RegisterResourceManager(AppStringsKeys.ResourceManager);
        Localizer.Current.CurrentCulture = new CultureInfo("en");

        return AppBuilder.Configure<App>().UsePlatformDetect();
    }
}
```

### WPF

At run time, `App.OnStartup` is enough:

```csharp
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        Localizer.Current.RegisterResourceManager(AppStringsKeys.ResourceManager);
        Localizer.Current.CurrentCulture = new CultureInfo("en");
    }
}
```

The WPF designer runs no startup code: `App.OnStartup` is not called there, so search-all lookups render
the failure text in the designer. A module initializer does run there - in the Visual Studio designer and
in Rider's previewer. To make search-all lookups work in the designer too, register in a
`[ModuleInitializer]` of the application assembly instead:

```csharp
using System.Runtime.CompilerServices;

internal static class ResourceRegistration
{
    [ModuleInitializer]
    internal static void Register()
    {
        Localizer.Current.CurrentCulture = new CultureInfo("en");
        Localizer.Current.RegisterResourceManager(AppStringsKeys.ResourceManager);
    }
}
```

A module initializer runs before `Main`, so it always registers into the default `Localizer.Current`
(see [Dependency injection](#dependency-injection)).

### Views in a class library

The WPF designer loads the library that contains the view, not the executable, so the application's
module initializer does not run for the library's previews. Give the library its own module initializer
that registers only the library's own files.

Under the JIT, a library's module initializer runs before the application's when the application's
initializer references the library's types, so the library's files come first in the search order. A
file registered by both is harmless: registering the same instance twice has no effect.

`CA2255` reports a `[ModuleInitializer]` in a library. Suppress it there, on the method, with a
justification:

```csharp
[ModuleInitializer]
[SuppressMessage("Usage", "CA2255", Justification = "Registers this library's resources for the WPF designer.")]
internal static void Register() => Localizer.Current.RegisterResourceManager(LibraryStringsKeys.ResourceManager);
```

## Dependency injection

Register into the instance your application uses for lookups. With
`services.AddSingleton<ILocalizer>(_ => Localizer.Current)` that is `Localizer.Current`, and nothing
else is needed. An application that assigns its own instance to `Localizer.Current` registers on that
instance; see [Lookup modes and culture fallback](lookup-and-fallback.md#dependency-injection).

## Resources in other assemblies

| The other assembly | Register |
| --- | --- |
| A library of yours with the UI package, so it has its own keys classes | `Lib.Resources.StringsKeys.ResourceManager` |
| A library with a public designer | `Lib.Resources.Strings.ResourceManager` |
| Neither - for example a NuGet package or DLL with internal designers only | `new ResourceManager("<embedded name>", typeof(<any public type of that assembly>).Assembly)` |

For the last case, `RegisterResourceManager` rejects a wrong embedded name at startup. An assembly with
no public type at all can be reached with a constant type name:

```csharp
var assembly = Type.GetType("Vendor.Texts.SomeType, Vendor.Texts", throwOnError: true)!.Assembly;
Localizer.Current.RegisterResourceManager(new ResourceManager("Vendor.Texts.Strings", assembly));
```

That works under Native AOT without trim warnings, and so do that assembly's satellites for the cultures
you list in `SatelliteResourceLanguages` (see
[Native AOT and trimming](native-aot.md#what-you-have-to-declare)). `Assembly.Load("Vendor.Texts")`
throws under Native AOT; do not use it.

## A forgotten registration

A file that was never registered shows as the failure text in every search-all lookup for its keys.
Subscribe to `LookupFailed` to log every miss (see
[Missing-translation diagnostics](lookup-and-fallback.md#missing-translation-diagnostics)).

## Why the library registers nothing itself

Nothing registers a resource file automatically - neither the library nor the source generator:

- A generated module initializer would register into whatever `Localizer.Current` is when the assembly
  is first touched. In an executable that is before `Main`, so an application that assigns its own
  `ILocalizer` to `Localizer.Current` at startup would always lose the registrations.
- Module initializers of several assemblies run in touch order under the JIT, but eagerly and in
  dependency order under Native AOT, so the search order across assemblies would differ between a JIT
  run and a native one.
- The generator sees one project at a time and cannot see the resources of referenced projects, and
  automatic discovery would also pick up third-party files nobody meant to search.

Registration in your own code has none of these problems, and the search order is where you write it.
