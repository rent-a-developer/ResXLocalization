using System.Windows;
using RentADeveloper.ResXLocalization.WPF.Sample.Resources;
using RentADeveloper.ResXLocalization.WPF.Sample.Views;

namespace RentADeveloper.ResXLocalization.WPF.Sample;

public partial class App
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Register every resource file with the ambient localizer once, at startup. Only the key-only
        // ("search-all") lookups consult this set; typed-key and scoped lookups need no registration.
        // Managers are searched in registration order on a first-match-wins basis.
        //
        // BrandingStrings has an *internal* accessor: registering it here works because App is in the
        // same assembly, and so does the typed-key path ({l:Localize {x:Static res:BrandingStringsKeys.X}})
        // because the generated key class is always public. Only XAML scoping via
        // {x:Static res:BrandingStrings.ResourceManager} is unavailable for an internal accessor.
        Localizer.Current.RegisterResourceManager(ApplicationStrings.ResourceManager);
        Localizer.Current.RegisterResourceManager(SortingStrings.ResourceManager);
        Localizer.Current.RegisterResourceManager(BrandingStrings.ResourceManager);

        // Pick the starting language explicitly so the sample always opens in English, regardless of the
        // machine's culture. The default would otherwise be CultureInfo.CurrentUICulture.
        Localizer.Current.CurrentCulture = new("en");

        this.MainWindow = new MainWindow();
        this.MainWindow.Show();
    }
}
