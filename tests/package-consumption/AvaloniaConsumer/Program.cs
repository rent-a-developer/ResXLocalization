// End-to-end consumption check for the ResXLocalization.Avalonia NuGet package. Everything below must
// arrive through the single PackageReference: the Core engine, the markup-extension assembly, and -
// proven at compile time by the StringsKeys, CatalogKeys and DesignerlessKeys classes - the packaged
// source generator plus the buildTransitive MSBuild wiring that feeds it, including the MSBuild resource
// name a .resx without a designer needs (Designerless.resx), also when LogicalName replaces it
// (Logical.resx) and when the DependentUpon convention names it after a class (Conventional.resx). Exits
// non-zero on the first failed check.
//
// This program is also the assertion set the Native AOT gate runs. Everything a trimmer can break and a
// JIT cannot is here: satellite discovery, resource lookup by name, the typed keys' captured resource
// managers, the enum key convention, composite formatting driven by an attached property, and named
// arguments read by reflection in MessageArguments.FromObject.

using System.Globalization;
using Avalonia.Controls;
using Avalonia.Threading;
using AvaloniaConsumer;
using AvaloniaConsumer.Convention;
using AvaloniaConsumer.Custom;
using AvaloniaConsumer.Resources;
using AvaloniaConsumer.Texts;
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
    Localizer.Current.Get("ScopedOnly", resourceManager: Catalog.ResourceManager),
    "Scoped to the catalog"
);
Check("search-all cannot reach the unregistered manager", Localizer.Current.Get("ScopedOnly"), "!ScopedOnly!");
Check("enum localization by convention", Localizer.Current.Get(ConsumerSortOrder.Ascending), "Ascending");
Check("typed key without a designer", Localizer.Current.Get(DesignerlessKeys.Title), "Designer-less");
Check(
    "scoped lookup into the generated resource manager",
    Localizer.Current.Get("Title", resourceManager: DesignerlessKeys.ResourceManager),
    "Designer-less"
);
Check("typed key whose resource name is a LogicalName", Localizer.Current.Get(LogicalKeys.Title), "Logical name");
Check(
    "typed key named by the DependentUpon convention",
    Localizer.Current.Get(ConventionalKeys.Title),
    "By convention"
);
Check(
    "typed key in the namespace CustomToolNamespace names",
    Localizer.Current.Get(NamedKeys.Title),
    "Custom tool namespace"
);
Check(
    "named arguments from an object's properties",
    Localizer.Current.Get(StringsKeys.ArgumentPair, MessageArguments.FromObject(new { A = "x", B = 2 })),
    "x 2"
);

// --- The compiled XAML, in English ------------------------------------------------------------------

var localizedView = new LocalizedView();

Check("compiled XAML typed key", localizedView.GreetingText.Text ?? "<null>", "Hello from the package!");
Check("compiled XAML scoped lookup", localizedView.ScopedText.Text ?? "<null>", "Scoped to the catalog");
Check("compiled XAML enum by convention", localizedView.SortOrderText.Text ?? "<null>", "Ascending");
Check("compiled XAML typed key without a designer", localizedView.DesignerlessText.Text ?? "<null>", "Designer-less");
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
Check("live switch, typed key without a designer", Localizer.Current.Get(DesignerlessKeys.Title), "Ohne Designer");
Check(
    "live switch, scoped lookup into the generated resource manager",
    Localizer.Current.Get("Title", resourceManager: DesignerlessKeys.ResourceManager),
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
Check(
    "live switch, typed key in the namespace CustomToolNamespace names",
    Localizer.Current.Get(NamedKeys.Title),
    "Eigener Namespace"
);
Check("compiled XAML live switch", localizedView.GreetingText.Text ?? "<null>", "Hallo aus dem Paket!");
Check("compiled XAML live switch, enum", localizedView.SortOrderText.Text ?? "<null>", "Aufsteigend");
Check(
    "compiled XAML live switch, typed key without a designer",
    localizedView.DesignerlessText.Text ?? "<null>",
    "Ohne Designer"
);
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
    Localizer.Current.Get("ScopedNeutralOnly", resourceManager: Catalog.ResourceManager),
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
