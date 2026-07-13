namespace RentADeveloper.ResXLocalization;

/// <summary>
/// Provides the details of a lookup that no resource file could satisfy, raised through
/// <see cref="ILocalizer.TranslationNotFound" />. Subscribe to log missing translations during
/// development. Normal .NET parent/neutral resource fallback runs first, so this event means the
/// key was unresolved across the complete fallback chain; the lookup itself still returns the
/// missing-translation sentinel (see <see cref="ILocalizer.MissingTranslationFormat" />).
/// </summary>
public sealed class TranslationNotFoundEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TranslationNotFoundEventArgs" /> class.
    /// </summary>
    /// <param name="key">The resource key that could not be resolved.</param>
    /// <param name="culture">The culture the lookup ran against.</param>
    /// <param name="resourceManager">
    /// The single resource manager of a scoped or typed lookup, or <see langword="null" /> for a
    /// search-all lookup.
    /// </param>
    public TranslationNotFoundEventArgs(String key, CultureInfo culture, ResourceManager? resourceManager)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(culture);

        this.Key = key;
        this.Culture = culture;
        this.ResourceManager = resourceManager;
    }

    /// <summary>Gets the resource key that could not be resolved.</summary>
    public String Key { get; }

    /// <summary>Gets the culture the lookup ran against.</summary>
    public CultureInfo Culture { get; }

    /// <summary>
    /// Gets the single resource manager of a scoped or typed lookup, or <see langword="null" /> when
    /// the miss came from a search-all lookup across every registered resource manager.
    /// </summary>
    public ResourceManager? ResourceManager { get; }
}
