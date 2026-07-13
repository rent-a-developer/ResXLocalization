using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using RentADeveloper.ResXLocalization.Avalonia.Sample.ViewModels;

namespace RentADeveloper.ResXLocalization.Avalonia.Sample.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        this.InitializeComponent();
        this.DataContext = new MainWindowViewModel(Localizer.Current);
        this.Closed += (_, _) => (this.DataContext as IDisposable)?.Dispose();
    }

    private void InitializeComponent() =>
        AvaloniaXamlLoader.Load(this);
}
