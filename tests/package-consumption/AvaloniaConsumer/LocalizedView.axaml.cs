using Avalonia.Controls;
using RentADeveloper.ResXLocalization.Avalonia;

namespace AvaloniaConsumer;

public partial class LocalizedView : UserControl
{
    public LocalizedView()
    {
        this.InitializeComponent();

        // The enum the {l:LocalizeEnum} markup extension localizes: it reads the target control's
        // DataContext, exactly as an item template would supply it.
        this.SortOrderText.DataContext = ConsumerSortOrder.Ascending;
    }

    /// <summary>Supplies the composite-format argument for the "{0} people invited" resource.</summary>
    /// <param name="count">The number to format into the resolved value.</param>
    public void SetInvitedCount(Int32 count) => LocalizeArgs.SetArg0(this.InvitedText, count);
}
