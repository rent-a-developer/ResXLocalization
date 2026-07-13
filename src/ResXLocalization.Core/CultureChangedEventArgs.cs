namespace RentADeveloper.ResXLocalization;

/// <summary>
/// Provides the previous and current culture for a <see cref="ILocalizer.CultureChanged" />
/// notification, raised after <see cref="ILocalizer.CurrentCulture" /> changes.
/// </summary>
public sealed class CultureChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CultureChangedEventArgs" /> class.
    /// </summary>
    /// <param name="oldCulture">The culture in effect before the change.</param>
    /// <param name="newCulture">The culture in effect after the change.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="oldCulture" /> or <paramref name="newCulture" /> is <see langword="null" />.
    /// </exception>
    public CultureChangedEventArgs(CultureInfo oldCulture, CultureInfo newCulture)
    {
        ArgumentNullException.ThrowIfNull(oldCulture);
        ArgumentNullException.ThrowIfNull(newCulture);

        this.OldCulture = oldCulture;
        this.NewCulture = newCulture;
    }

    /// <summary>Gets the culture that was in effect before the change.</summary>
    public CultureInfo OldCulture { get; }

    /// <summary>Gets the culture that is in effect after the change.</summary>
    public CultureInfo NewCulture { get; }
}
