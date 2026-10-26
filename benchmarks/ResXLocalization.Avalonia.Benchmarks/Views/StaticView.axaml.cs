using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace RentADeveloper.ResXLocalization.Avalonia.Benchmarks.Views;

/// <summary>A view with 10 text elements. Every text is <c>{x:Static}</c> of the designer class's property: the baseline of the forms and of opening a view.</summary>
public partial class StaticView : UserControl
{
    public StaticView() => this.InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
