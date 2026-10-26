using System.Windows.Controls;

namespace RentADeveloper.ResXLocalization.WPF.Benchmarks.Views;

/// <summary>A view with 10 text elements. Every text is <c>{l:Localize}</c> with a typed key.</summary>
public partial class LocalizeView : UserControl
{
    public LocalizeView() => this.InitializeComponent();
}
