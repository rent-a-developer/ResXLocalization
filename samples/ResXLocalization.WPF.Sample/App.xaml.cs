using System.Windows;
using RentADeveloper.ResXLocalization.WPF.Sample.Views;

namespace RentADeveloper.ResXLocalization.WPF.Sample;

public partial class App
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // The starting language and the resource registration live in ResourceRegistration, a module
        // initializer, so that they apply in the WPF designer too.
        this.MainWindow = new MainWindow();
        this.MainWindow.Show();
    }
}
