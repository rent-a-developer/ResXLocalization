// End-to-end check for a WPF consumer whose RootNamespace is derived from its project name
// (Derived.$(MSBuildProjectName)). WPF compiles the XAML in a temporary copy of the project with a
// different name; the packaged build wiring must give the keys class of a .resx without a designer the
// same namespace there as in the real build - also for a LogicalName (Logical.resx) and a
// CustomToolNamespace (Named.resx) built from that RootNamespace - or this program and its XAML do not
// compile. See the comment "WPF's temporary markup-compile pass" in build/ResXLocalization.Resx.targets.
// Exits non-zero on the first failed check.

using System.Globalization;
using System.Windows.Threading;
using Derived.WpfDerivedNamespaceConsumer.Custom;
using Derived.WpfDerivedNamespaceConsumer.Resources;
using Derived.WpfDerivedNamespaceConsumer.Texts;
using RentADeveloper.ResXLocalization;

namespace Derived.WpfDerivedNamespaceConsumer;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
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

        Localizer.Current.CurrentCulture = new CultureInfo("en");

        Check("typed key in the derived namespace", Localizer.Current.Get(DesignerlessKeys.Title), "Designer-less");
        Check(
            "typed key whose LogicalName is built from RootNamespace",
            Localizer.Current.Get(LogicalKeys.Title),
            "Logical name"
        );
        Check(
            "typed key whose CustomToolNamespace is built from RootNamespace",
            Localizer.Current.Get(NamedKeys.Title),
            "Custom tool namespace"
        );

        var localizedView = new LocalizedView();
        Check("compiled XAML typed key in the derived namespace", localizedView.DesignerlessText.Text, "Designer-less");

        Localizer.Current.CurrentCulture = new CultureInfo("de");
        Dispatcher.CurrentDispatcher.Invoke(static () => { }, DispatcherPriority.Background);
        Check(
            "live switch, typed key in the derived namespace",
            Localizer.Current.Get(DesignerlessKeys.Title),
            "Ohne Designer"
        );
        Check("compiled XAML live switch", localizedView.DesignerlessText.Text, "Ohne Designer");
        Check(
            "live switch, typed key whose LogicalName is built from RootNamespace",
            Localizer.Current.Get(LogicalKeys.Title),
            "Logischer Name"
        );
        Check(
            "live switch, typed key whose CustomToolNamespace is built from RootNamespace",
            Localizer.Current.Get(NamedKeys.Title),
            "Eigener Namespace"
        );

        if (failures > 0)
        {
            Console.Error.WriteLine($"{failures} package consumption check(s) FAILED.");
            return 1;
        }

        Console.WriteLine("All package consumption checks passed.");
        return 0;
    }
}
