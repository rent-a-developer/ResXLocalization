using System.Windows.Controls;

namespace RentADeveloper.ResXLocalization.WPF.Benchmarks.Views;

/// <summary>A view with 10 text elements. Every text is <c>{x:Static}</c> of the designer class's property: the baseline of the forms and of opening a view.</summary>
public partial class StaticView : UserControl
{
    public StaticView() => this.InitializeComponent();
}
