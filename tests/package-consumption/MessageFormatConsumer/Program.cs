// End-to-end consumption check for the ResXLocalization.MessageFormat NuGet package, next to
// ResXLocalization.Avalonia. The ICU formatter, its MessageFormat dependency and - proven at compile time
// by the StringsKeys class of a .resx without a designer - the packaged source generator must all arrive
// through the two PackageReferences. Exits non-zero when any check failed.
//
// This program is also the assertion set the Native AOT gate runs for the ICU formatter. Everything a
// trimmer can break and a JIT cannot is here: the plural rules of the MessageFormat library, the named
// argument delivered by a compiled binding, the German satellite, and the format culture of the invariant
// culture.

using System.Globalization;
using Avalonia.Threading;
using MessageFormatConsumer;
using MessageFormatConsumer.Resources;
using RentADeveloper.ResXLocalization;
using RentADeveloper.ResXLocalization.MessageFormat;

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

Localizer.Current.UseMessageFormat();
Localizer.Current.RegisterResourceManager(StringsKeys.ResourceManager);
Localizer.Current.CurrentCulture = new CultureInfo("en");

// --- The compiled XAML ------------------------------------------------------------------------------

var viewModel = new LocalizedViewModel { Count = 1 };
var localizedView = new LocalizedView { DataContext = viewModel };
Dispatcher.UIThread.RunJobs();

Check("compiled XAML plural, one", localizedView.PeopleInvitedText.Text ?? "<null>", "1 person invited");

viewModel.Count = 5;
Dispatcher.UIThread.RunJobs();
Check("compiled XAML plural, other", localizedView.PeopleInvitedText.Text ?? "<null>", "5 people invited");

Localizer.Current.CurrentCulture = new CultureInfo("de");
Dispatcher.UIThread.RunJobs();
Check(
    "compiled XAML plural after the live switch to de (satellite)",
    localizedView.PeopleInvitedText.Text ?? "<null>",
    "5 Personen eingeladen"
);

// --- Named arguments in code ------------------------------------------------------------------------

Localizer.Current.CurrentCulture = new CultureInfo("en");
Check(
    "named argument in code",
    Localizer.Current.Get(StringsKeys.PeopleInvited, MessageArguments.FromTuples(("Count", 1))),
    "1 person invited"
);

// The invariant culture has only CLDR's root plural rules; the ICU formatter formats it as en.
Localizer.Current.CurrentCulture = CultureInfo.InvariantCulture;
Check(
    "named argument in code, invariant culture formatted as en",
    Localizer.Current.Get(StringsKeys.PeopleInvited, MessageArguments.FromTuples(("Count", 1))),
    "1 person invited"
);

// --- An argument binding that has not resolved yet --------------------------------------------------
//
// A view whose data context arrives after it is created formats with a null argument first, which ICU
// renders without throwing - so no lookup failure is reported.

Localizer.Current.CurrentCulture = new CultureInfo("en");

var lookupFailures = 0;
void OnLookupFailed(object? sender, LookupFailedEventArgs e) => lookupFailures++;

Localizer.Current.LookupFailed += OnLookupFailed;
try
{
    var lateView = new LocalizedView();
    Dispatcher.UIThread.RunJobs();

    lateView.DataContext = new LocalizedViewModel { Count = 1 };
    Dispatcher.UIThread.RunJobs();
}
finally
{
    Localizer.Current.LookupFailed -= OnLookupFailed;
}

Check(
    "data context set after the view was created, lookup failures",
    lookupFailures.ToString(CultureInfo.InvariantCulture),
    "0"
);

// --- A value without arguments, which ICU parses as well --------------------------------------------

Check("ICU escaping without arguments in code", Localizer.Current.Get(StringsKeys.Ready), "It's ready");
Check("ICU escaping without arguments in compiled XAML", localizedView.ReadyText.Text ?? "<null>", "It's ready");

if (failures > 0)
{
    Console.Error.WriteLine($"{failures} package consumption check(s) FAILED.");

    return 1;
}

Console.WriteLine("All package consumption checks passed.");

return 0;
