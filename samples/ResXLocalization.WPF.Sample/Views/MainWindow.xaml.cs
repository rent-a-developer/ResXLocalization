using RentADeveloper.ResXLocalization.WPF.Sample.ViewModels;

namespace RentADeveloper.ResXLocalization.WPF.Sample.Views;

public partial class MainWindow
{
    public MainWindow()
    {
        this.InitializeComponent();
        this.DataContext = new MainWindowViewModel(Localizer.Current);
        this.Closed += (_, _) => (this.DataContext as IDisposable)?.Dispose();
    }
}
