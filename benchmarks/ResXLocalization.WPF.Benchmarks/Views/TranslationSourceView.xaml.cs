using System.Windows.Controls;

namespace RentADeveloper.ResXLocalization.WPF.Benchmarks.Views;

/// <summary>A view with 10 text elements. Every text binds the indexer of the hand-written TranslationSource: the baseline of the culture switch.</summary>
public partial class TranslationSourceView : UserControl
{
    public TranslationSourceView() => this.InitializeComponent();
}
