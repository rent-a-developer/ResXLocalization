namespace RentADeveloper.ResXLocalization.Avalonia;

/// <summary>
/// The multi-value converter of the <see cref="MultiBinding" /> that <see cref="LocalizedMultiBindingBuilder" />
/// builds: turns its values into one call of <c>ILocalizer.Get(string, MessageArguments?, ResourceManager?)</c>
/// on <see cref="Localizer.Current" />. Value 0 is the refresh trigger and is ignored; value 1 is the key;
/// values 2 and up are the argument values.
/// </summary>
/// <param name="argumentNames">The argument names, one per argument value, in order.</param>
/// <param name="resourceManager">
/// The resource manager that scopes a <see cref="string" /> key, or <see langword="null" /> to search all
/// registered resource managers.
/// </param>
internal sealed class LocalizedMessageConverter(string?[] argumentNames, ResourceManager? resourceManager)
    : IMultiValueConverter
{
    /// <summary>The argument names, one per argument value, in order.</summary>
    private readonly string?[] argumentNames = argumentNames;

    /// <summary>The resource manager that scopes a <see cref="string" /> key.</summary>
    private readonly ResourceManager? resourceManager = resourceManager;

    /// <summary>Returns the localized, formatted text for the current values.</summary>
    /// <param name="values">The refresh trigger, the key, and the argument values.</param>
    /// <param name="targetType">The type of the binding target property; not used.</param>
    /// <param name="parameter">An optional converter parameter; not used.</param>
    /// <param name="culture">
    /// The culture supplied by the binding; not used (the lookup uses the culture of
    /// <see cref="Localizer.Current" />).
    /// </param>
    /// <returns>
    /// The localized text, or the failure text (see <see cref="ILocalizer.LookupFailureTextBuilder" />) when
    /// the lookup fails. When value 1 is neither a non-empty <see cref="string" /> nor a
    /// <see cref="ResourceKey" /> with a resource manager, there is no key to look up, and the result is an
    /// empty string.
    /// </returns>
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        // Never throws for a key value: a key that cannot be looked up renders empty.
        var (key, keyResourceManager) = values[1] switch
        {
            string { Length: > 0 } name => (name, this.resourceManager),
            ResourceKey { Manager: not null } resourceKey => (resourceKey.Name, resourceKey.Manager),
            _ => ((string?)null, (ResourceManager?)null),
        };

        if (key is null)
        {
            return string.Empty;
        }

        // Every argument value becomes (Name, Value); FromTuples gives each its index as position.
        var arguments = MessageArguments.FromTuples([
            .. values
                .Skip(2)
                .Select(
                    (value, index) => (this.argumentNames[index], value == AvaloniaProperty.UnsetValue ? null : value)
                ),
        ]);

        return Localizer.Current.Get(key, arguments, keyResourceManager);
    }
}
