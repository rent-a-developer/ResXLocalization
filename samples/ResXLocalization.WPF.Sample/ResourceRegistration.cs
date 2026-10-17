using System.Runtime.CompilerServices;
using RentADeveloper.ResXLocalization.WPF.Sample.Resources;

namespace RentADeveloper.ResXLocalization.WPF.Sample;

/// <summary>
/// Selects the starting language and registers the sample's resource files with the ambient localizer when
/// the assembly loads - at run time and in the WPF designer.
/// </summary>
internal static class ResourceRegistration
{
    /// <summary>
    /// Runs once, before any other code of this assembly. The Visual Studio WPF designer runs no startup code
    /// (<c>App.OnStartup</c> is not called there), but it does run module initializers - as does Rider's
    /// previewer - so search-all lookups
    /// such as <c>{l:Localize Greeting}</c> resolve in the designer too instead of rendering the failure text.
    /// An application that does not care about the designer can do the same in <c>App.OnStartup</c>.
    /// </summary>
    [ModuleInitializer]
    internal static void Register()
    {
        // Pick the starting language explicitly so the sample always opens in English, regardless of the
        // machine's culture. The default would otherwise be CultureInfo.CurrentUICulture.
        Localizer.Current.CurrentCulture = new("en");

        // Register every resource file once, through the generated <Name>Keys.ResourceManager - the instance
        // the typed keys use. Only the search-all lookups consult this set; typed-key and scoped lookups need
        // no registration. Managers are searched in registration order, and the first match wins.
        //
        // BrandingStrings has an *internal* designer accessor, which plays no part here: the generated
        // BrandingStringsKeys class and its ResourceManager are public, so registration, typed keys and
        // XAML scoping via {x:Static res:BrandingStringsKeys.ResourceManager} all work.
        Localizer.Current.RegisterResourceManager(ApplicationStringsKeys.ResourceManager);
        Localizer.Current.RegisterResourceManager(SortingStringsKeys.ResourceManager);
        Localizer.Current.RegisterResourceManager(BrandingStringsKeys.ResourceManager);
    }
}
