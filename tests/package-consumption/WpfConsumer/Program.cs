using System.Globalization;
using System.Windows.Threading;
using RentADeveloper.ResXLocalization;
using RentADeveloper.ResXLocalization.WPF;
using WpfConsumer.Convention;
using WpfConsumer.Custom;
using WpfConsumer.Resources;

namespace WpfConsumer;

internal static class Program
{
    [STAThread]
    private static Int32 Main()
    {
        var failures = 0;

        void Check(String description, String actual, String expected)
        {
            if (actual == expected)
            {
                Console.WriteLine($"PASS  {description}");
            }
            else
            {
                Console.Error.WriteLine($"FAIL  {description}: expected \"{expected}\", got \"{actual}\"");
                failures++;
            }
        }

        Localizer.Current.RegisterResourceManager(Strings.ResourceManager);
        Localizer.Current.CurrentCulture = new CultureInfo("en");

        Check(
            "typed key (generated StringsKeys)",
            Localizer.Current.Get(StringsKeys.Greeting),
            "Hello from the package!"
        );
        Check("search-all lookup", Localizer.Current.Get("Greeting"), "Hello from the package!");
        Check("typed key without a designer", Localizer.Current.Get(DesignerlessKeys.Title), "Designer-less");
        Check(
            "typed key whose resource name is a LogicalName",
            Localizer.Current.Get(LogicalKeys.Title),
            "Logical name"
        );
        Check(
            "typed key named by the DependentUpon convention",
            Localizer.Current.Get(ConventionalKeys.Title),
            "By convention"
        );

        var localizedView = new LocalizedView();
        Check("compiled XAML initial value", localizedView.GreetingText.Text, "Hello from the package!");
        Check("compiled XAML typed key without a designer", localizedView.DesignerlessText.Text, "Designer-less");

        Localizer.Current.CurrentCulture = new CultureInfo("de");
        Dispatcher.CurrentDispatcher.Invoke(static () => { }, DispatcherPriority.Background);
        Check("live switch to de (satellite)", Localizer.Current.Get(StringsKeys.Greeting), "Hallo aus dem Paket!");
        Check("compiled XAML live switch", localizedView.GreetingText.Text, "Hallo aus dem Paket!");
        Check(
            "live switch, typed key without a designer",
            Localizer.Current.Get(DesignerlessKeys.Title),
            "Ohne Designer"
        );
        Check(
            "compiled XAML live switch, typed key without a designer",
            localizedView.DesignerlessText.Text,
            "Ohne Designer"
        );
        Check(
            "live switch, typed key whose resource name is a LogicalName",
            Localizer.Current.Get(LogicalKeys.Title),
            "Logischer Name"
        );
        Check(
            "live switch, typed key named by the DependentUpon convention",
            Localizer.Current.Get(ConventionalKeys.Title),
            "Nach Konvention"
        );

        var cultures = Localizer.Current.GetAvailableCultures();
        Check(
            "culture discovery",
            String.Join(",", cultures.Select(static culture => culture.Name.Length == 0 ? "<neutral>" : culture.Name)),
            "<neutral>,de"
        );

        var extension = new LocalizeExtension(StringsKeys.Greeting);
        Check("markup extension instantiation", extension.ResourceKey?.Name ?? "<null>", "Greeting");

        if (failures > 0)
        {
            Console.Error.WriteLine($"{failures} package consumption check(s) FAILED.");
            return 1;
        }

        Console.WriteLine("All package consumption checks passed.");
        return 0;
    }
}
