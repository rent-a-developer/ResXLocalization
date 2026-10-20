using Avalonia.Controls;

namespace AvaloniaConsumer;

public partial class LocalizedView : UserControl
{
    private readonly LocalizedViewModel viewModel = new();

    public LocalizedView()
    {
        this.InitializeComponent();

        // The view model the compiled bindings of InvitedText and SortOrderText read.
        this.DataContext = this.viewModel;
    }

    /// <summary>Supplies the composite-format argument for the "{0} people invited" resource.</summary>
    /// <param name="count">The number to format into the resolved value.</param>
    public void SetInvitedCount(int count) => this.viewModel.InvitedCount = count;
}
