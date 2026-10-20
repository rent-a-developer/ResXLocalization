using System.ComponentModel;

namespace MessageFormatConsumer;

/// <summary>
/// The data context of <see cref="LocalizedView" />: the value its compiled argument binding reads.
/// </summary>
public sealed class LocalizedViewModel : INotifyPropertyChanged
{
    private int count;

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Gets or sets the number the ICU plural pattern of "PeopleInvited" selects its branch by.</summary>
    public int Count
    {
        get => this.count;
        set
        {
            this.count = value;
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(this.Count)));
        }
    }
}
