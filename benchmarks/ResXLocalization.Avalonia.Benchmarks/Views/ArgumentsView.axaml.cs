using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace RentADeveloper.ResXLocalization.Avalonia.Benchmarks.Views;

/// <summary>A view with 10 text elements. Every text is <c>{l:Localize}</c> with a typed key and two <c>&lt;l:Argument&gt;</c> children bound to the DataContext.</summary>
public partial class ArgumentsView : UserControl
{
    public ArgumentsView() => this.InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
