using RentADeveloper.ResXLocalization;

_ = new RentADeveloper.ResXLocalization.Avalonia.LocalizeExtension("Probe");
_ = new RentADeveloper.ResXLocalization.WPF.LocalizeExtension("Probe");

// Load Core through its own type: neither constructor above has to touch it.
_ = Localizer.Current;

var coreAssemblies = AppDomain
    .CurrentDomain.GetAssemblies()
    .Where(static assembly => assembly.GetName().Name == "ResXLocalization.Core")
    .ToArray();

if (coreAssemblies.Length != 1)
{
    Console.Error.WriteLine("FAIL  unexpected duplicate assembly identity");
    return 1;
}

var core = coreAssemblies[0].GetName();
Console.WriteLine($"PASS  one resolved {core.Name} {core.Version}");
Console.WriteLine("PASS  Avalonia and WPF packages load together");
return 0;
