using Avalonia.Controls;

namespace MessageFormatConsumer;

public partial class LocalizedView : UserControl
{
    /// <summary>
    /// Creates the view without a data context: the caller assigns one, so a view can also be observed
    /// before its argument binding resolves.
    /// </summary>
    public LocalizedView() => this.InitializeComponent();
}
