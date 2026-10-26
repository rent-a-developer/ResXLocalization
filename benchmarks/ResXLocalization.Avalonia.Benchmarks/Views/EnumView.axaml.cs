using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace RentADeveloper.ResXLocalization.Avalonia.Benchmarks.Views;

/// <summary>A view with 10 text elements. Every text is <c>{l:LocalizeEnum}</c> of a different Priority member.</summary>
public partial class EnumView : UserControl
{
    public EnumView() => this.InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
