// End-to-end consumption check for the ResXLocalization.Avalonia NuGet package. Everything below
// must arrive through the single PackageReference: the Core engine, the markup-extension assembly,
// and - proven at compile time by the StringsKeys class - the packaged source generator plus the
// buildTransitive MSBuild wiring that feeds it. Exits non-zero on the first failed check.

using System.Globalization;
using Avalonia.Controls;
using Avalonia.Threading;
using AvaloniaConsumer.Resources;
using RentADeveloper.ResXLocalization;
using RentADeveloper.ResXLocalization.Avalonia;

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

// Typed key emitted by the packaged source generator (compiles only if the wiring works).
Check("typed key (generated StringsKeys)", Localizer.Current.Get(StringsKeys.Greeting), "Hello from the package!");
Check("search-all lookup", Localizer.Current.Get("Greeting"), "Hello from the package!");
var localizedView = new AvaloniaConsumer.LocalizedView();
var localizedText = (TextBlock)localizedView.Content!;
Check("compiled XAML initial value", localizedText.Text ?? "<null>", "Hello from the package!");

// Live switch through the German satellite assembly.
Localizer.Current.CurrentCulture = new CultureInfo("de");
Dispatcher.UIThread.RunJobs();
Check("live switch to de (satellite)", Localizer.Current.Get(StringsKeys.Greeting), "Hallo aus dem Paket!");
Check("compiled XAML live switch", localizedText.Text ?? "<null>", "Hallo aus dem Paket!");

// Culture discovery sees the neutral resources (invariant) and the German satellite.
var cultures = Localizer.Current.GetAvailableCultures();
Check(
    "culture discovery",
    String.Join(",", cultures.Select(culture => culture.Name.Length == 0 ? "<neutral>" : culture.Name)),
    "<neutral>,de"
);

// The Avalonia markup-extension assembly is loadable and functional.
var extension = new LocalizeExtension(StringsKeys.Greeting);
Check("markup extension instantiation", extension.ResourceKey?.Name ?? "<null>", "Greeting");

if (failures > 0)
{
    Console.Error.WriteLine($"{failures} package consumption check(s) FAILED.");
    return 1;
}

Console.WriteLine("All package consumption checks passed.");
return 0;
