using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using RentADeveloper.ResXLocalization.Avalonia.Sample.Views;

namespace RentADeveloper.ResXLocalization.Avalonia.Sample;

public class App : Application
{
    public override void Initialize() =>
        AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (this.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Localizer.Current is configured in Program.BuildAvaloniaApp before the app starts. The view model
            // takes it as a dependency so it never reaches for the singleton itself, which keeps it unit-testable.
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
