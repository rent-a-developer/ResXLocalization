using Avalonia;
using RentADeveloper.ResXLocalization.Avalonia.Sample.Resources;

namespace RentADeveloper.ResXLocalization.Avalonia.Sample;

internal static class Program
{
    [STAThread]
    public static void Main(String[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    /// <summary>Referenced by Main and by the Avalonia design-time tooling.</summary>
    /// <returns>The configured application builder.</returns>
    private static AppBuilder BuildAvaloniaApp()
    {
        // Register every resource file with the ambient localizer once. This runs from Main at runtime
        // AND from the Avalonia XAML previewer at design time, so search-all lookups ({l:Localize Greeting})
        // resolve in the previewer too instead of rendering the miss sentinel. Only the key-only
        // ("search-all") lookups consult this set; typed-key and scoped lookups need no registration.
        // Managers are searched in registration order on a first-match-wins basis.
        //
        // BrandingStrings has an *internal* accessor: registering it here works because Program is in the
        // same assembly, and so does the typed-key path ({l:Localize {x:Static res:BrandingStringsKeys.X}})
        // because the generated key class is always public. Only XAML scoping via
        // {x:Static res:BrandingStrings.ResourceManager} is unavailable for an internal accessor.
        Localizer.Current.RegisterResourceManager(ApplicationStrings.ResourceManager);
        Localizer.Current.RegisterResourceManager(SortingStrings.ResourceManager);
        Localizer.Current.RegisterResourceManager(BrandingStrings.ResourceManager);

        // Pick the starting language explicitly so the sample always opens in English, regardless of the
        // machine's culture. The default would otherwise be CultureInfo.CurrentUICulture.
        Localizer.Current.CurrentCulture = new("en");

        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}
