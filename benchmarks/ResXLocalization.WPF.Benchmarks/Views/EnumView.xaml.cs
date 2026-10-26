using System.Windows.Controls;

namespace RentADeveloper.ResXLocalization.WPF.Benchmarks.Views;

/// <summary>A view with 10 text elements. Every text is <c>{l:LocalizeEnum}</c> of a different Priority member.</summary>
public partial class EnumView : UserControl
{
    public EnumView() => this.InitializeComponent();
}
