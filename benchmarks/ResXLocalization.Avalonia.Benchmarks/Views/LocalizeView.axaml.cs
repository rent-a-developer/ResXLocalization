using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace RentADeveloper.ResXLocalization.Avalonia.Benchmarks.Views;

/// <summary>A view with 10 text elements. Every text is <c>{l:Localize}</c> with a typed key.</summary>
public partial class LocalizeView : UserControl
{
    public LocalizeView() => this.InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
