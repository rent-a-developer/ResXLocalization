using Avalonia;
using RentADeveloper.ResXLocalization.Avalonia.Sample.Resources;

namespace RentADeveloper.ResXLocalization.Avalonia.Sample;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    /// <summary>Referenced by Main and by the Avalonia design-time tooling.</summary>
    /// <returns>The configured application builder.</returns>
    private static AppBuilder BuildAvaloniaApp()
    {
        // Register every resource file with the ambient localizer once, through the generated
        // <Name>Keys.ResourceManager - the instance the typed keys use. This runs from Main at run time AND
        // from the Avalonia XAML previewer at design time, so search-all lookups ({l:Localize Greeting})
        // resolve in the previewer too instead of rendering the failure text. Only the search-all lookups
        // consult this set; typed-key and scoped lookups need no registration. Managers are searched in
        // registration order, and the first match wins.
        //
        // BrandingStrings has an *internal* designer accessor, which plays no part here: the generated
        // BrandingStringsKeys class and its ResourceManager are public, so registration, typed keys and
        // XAML scoping via {x:Static res:BrandingStringsKeys.ResourceManager} all work.
        Localizer.Current.RegisterResourceManager(ApplicationStringsKeys.ResourceManager);
        Localizer.Current.RegisterResourceManager(SortingStringsKeys.ResourceManager);
        Localizer.Current.RegisterResourceManager(BrandingStringsKeys.ResourceManager);

        // Pick the starting language explicitly so the sample always opens in English, regardless of the
        // machine's culture. The default would otherwise be CultureInfo.CurrentUICulture.
        Localizer.Current.CurrentCulture = new("en");

        return AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace();
    }
}
