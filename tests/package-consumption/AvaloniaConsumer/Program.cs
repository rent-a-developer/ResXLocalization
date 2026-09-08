// End-to-end consumption check for the ResXLocalization.Avalonia NuGet package. Everything below must
// arrive through the single PackageReference: the Core engine, the markup-extension assembly, and -
// proven at compile time by the StringsKeys and CatalogKeys classes - the packaged source generator plus
// the buildTransitive MSBuild wiring that feeds it. Exits non-zero on the first failed check.
//
// This program is also the assertion set the Native AOT gate runs. Everything a trimmer can break and a
// JIT cannot is here: satellite discovery, resource lookup by name, the typed keys' captured resource
// managers, the enum key convention, and composite formatting driven by an attached property.

using System.Globalization;
using Avalonia.Controls;
using Avalonia.Threading;
using AvaloniaConsumer;
using AvaloniaConsumer.Resources;
using RentADeveloper.ResXLocalization;
using RentADeveloper.ResXLocalization.Avalonia;

var failures = 0;

void Check(string description, string actual, string expected)
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

// Strings is registered; Catalog deliberately is NOT, so a search-all lookup cannot reach it and only
// the scoped overloads can.
Localizer.Current.RegisterResourceManager(Strings.ResourceManager);
Localizer.Current.CurrentCulture = new CultureInfo("en");

// --- The engine, in English -------------------------------------------------------------------------

Check("typed key (generated StringsKeys)", Localizer.Current.Get(StringsKeys.Greeting), "Hello from the package!");
Check("search-all lookup", Localizer.Current.Get("Greeting"), "Hello from the package!");
Check(
    "scoped lookup into an unregistered resource manager",
    Localizer.Current.Get("ScopedOnly", Catalog.ResourceManager),
    "Scoped to the catalog"
);
Check("search-all cannot reach the unregistered manager", Localizer.Current.Get("ScopedOnly"), "!ScopedOnly!");
Check("enum localization by convention", Localizer.Current.Get(ConsumerSortOrder.Ascending), "Ascending");

// --- The compiled XAML, in English ------------------------------------------------------------------

var localizedView = new LocalizedView();

Check("compiled XAML typed key", localizedView.GreetingText.Text ?? "<null>", "Hello from the package!");
Check("compiled XAML scoped lookup", localizedView.ScopedText.Text ?? "<null>", "Scoped to the catalog");
Check("compiled XAML enum by convention", localizedView.SortOrderText.Text ?? "<null>", "Ascending");
Check(
    "compiled XAML composite format, no argument set",
    localizedView.InvitedText.Text ?? "<null>",
    "{0} people invited"
);

// An argument change, with the culture unchanged: the rendered text must re-format.
localizedView.SetInvitedCount(3);
Dispatcher.UIThread.RunJobs();
Check(
    "compiled XAML composite format after an argument change",
    localizedView.InvitedText.Text ?? "<null>",
    "3 people invited"
);

// --- The live switch to German ----------------------------------------------------------------------

Localizer.Current.CurrentCulture = new CultureInfo("de");
Dispatcher.UIThread.RunJobs();

Check("live switch to de (satellite)", Localizer.Current.Get(StringsKeys.Greeting), "Hallo aus dem Paket!");
Check("live switch, enum by convention", Localizer.Current.Get(ConsumerSortOrder.Ascending), "Aufsteigend");
Check("compiled XAML live switch", localizedView.GreetingText.Text ?? "<null>", "Hallo aus dem Paket!");
Check("compiled XAML live switch, enum", localizedView.SortOrderText.Text ?? "<null>", "Aufsteigend");
Check(
    "compiled XAML live switch, composite format keeps its argument",
    localizedView.InvitedText.Text ?? "<null>",
    "3 Personen eingeladen"
);

// --- Culture fallback, which is what a trimmed satellite would break silently ------------------------
//
// Neither key exists in the German files. The neutral value has to come back - not the sentinel, and not
// an empty string.

Check(
    "typed key falls back to the neutral culture",
    Localizer.Current.Get(StringsKeys.NeutralOnly),
    "Only in the neutral file"
);
Check(
    "scoped key falls back to the neutral culture",
    Localizer.Current.Get("ScopedNeutralOnly", Catalog.ResourceManager),
    "Catalog value with no German translation"
);
Check("compiled XAML typed fallback", localizedView.FallbackText.Text ?? "<null>", "Only in the neutral file");
Check(
    "compiled XAML scoped fallback",
    localizedView.ScopedFallbackText.Text ?? "<null>",
    "Catalog value with no German translation"
);
Check("compiled XAML scoped lookup after the switch", localizedView.ScopedText.Text ?? "<null>", "Nur im Katalog");

// --- Discovery and the markup extension -------------------------------------------------------------
//
// Culture discovery sees the neutral resources (invariant) and the German satellite.

var cultures = Localizer.Current.GetAvailableCultures();
Check(
    "culture discovery",
    string.Join(",", cultures.Select(culture => culture.Name.Length == 0 ? "<neutral>" : culture.Name)),
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
