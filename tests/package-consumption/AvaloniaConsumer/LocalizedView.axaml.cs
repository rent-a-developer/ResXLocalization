using Avalonia.Controls;

namespace AvaloniaConsumer;

public partial class LocalizedView : UserControl
{
    private readonly LocalizedViewModel viewModel = new();

    public LocalizedView()
    {
        this.InitializeComponent();

        // The view model the compiled argument binding of InvitedText reads.
        this.DataContext = this.viewModel;

        // The enum the {l:LocalizeEnum} markup extension localizes: it reads the target control's
        // DataContext, exactly as an item template would supply it.
        this.SortOrderText.DataContext = ConsumerSortOrder.Ascending;
    }

    /// <summary>Supplies the composite-format argument for the "{0} people invited" resource.</summary>
    /// <param name="count">The number to format into the resolved value.</param>
    public void SetInvitedCount(int count) => this.viewModel.InvitedCount = count;
}
