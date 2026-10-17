using System.ComponentModel;

namespace AvaloniaConsumer;

/// <summary>
/// The data context of <see cref="LocalizedView" />: the value its compiled argument binding reads.
/// </summary>
public sealed class LocalizedViewModel : INotifyPropertyChanged
{
    private int invitedCount;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Gets or sets the number formatted into the "{0} people invited" resource.</summary>
    public int InvitedCount
    {
        get => this.invitedCount;
        set
        {
            this.invitedCount = value;
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.InvitedCount)));
        }
    }
}
