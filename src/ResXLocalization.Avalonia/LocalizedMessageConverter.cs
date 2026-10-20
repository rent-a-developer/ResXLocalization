namespace RentADeveloper.ResXLocalization.Avalonia;

/// <summary>
/// The multi-value converter of the <see cref="MultiBinding" /> that <see cref="LocalizedMultiBindingBuilder" />
/// builds: turns its values into one call of <c>ILocalizer.Get(string, MessageArguments?, ResourceManager?)</c>
/// on <see cref="Localizer.Current" />. Value 0 is the refresh trigger and is ignored; value 1 is the key, or
/// the enumeration value whose key <see cref="ILocalizer.EnumKeyBuilder" /> builds; values 2 and up are the
/// argument values.
/// </summary>
/// <param name="argumentNames">The argument names, one per argument value, in order.</param>
/// <param name="resourceManager">
/// The resource manager that scopes a <see cref="string" /> key, or <see langword="null" /> to search all
/// registered resource managers.
/// </param>
/// <param name="isEnumKey">
/// <see langword="true" /> when value 1 is an enumeration value whose key <see cref="ILocalizer.EnumKeyBuilder" />
/// builds.
/// </param>
/// <param name="enumKeyPrefix">
/// The key prefix passed to <see cref="ILocalizer.EnumKeyBuilder" />, or <see langword="null" /> to let the
/// builder decide.
/// </param>
internal sealed class LocalizedMessageConverter(
    string?[] argumentNames,
    ResourceManager? resourceManager,
    bool isEnumKey,
    string? enumKeyPrefix
) : IMultiValueConverter
{
    /// <summary>The argument names, one per argument value, in order.</summary>
    private readonly string?[] argumentNames = argumentNames;

    /// <summary>The key prefix passed to <see cref="ILocalizer.EnumKeyBuilder" />.</summary>
    private readonly string? enumKeyPrefix = enumKeyPrefix;

    /// <summary>Whether value 1 is an enumeration value rather than a key.</summary>
    private readonly bool isEnumKey = isEnumKey;

    /// <summary>The resource manager that scopes a <see cref="string" /> key.</summary>
    private readonly ResourceManager? resourceManager = resourceManager;

    /// <summary>Returns the localized, formatted text for the current values.</summary>
    /// <param name="values">The refresh trigger, the key or the enumeration value, and the argument values.</param>
    /// <param name="targetType">The type of the binding target property; not used.</param>
    /// <param name="parameter">An optional converter parameter; not used.</param>
    /// <param name="culture">
    /// The culture supplied by the binding; not used (the lookup uses the culture of
    /// <see cref="Localizer.Current" />).
    /// </param>
    /// <returns>
    /// <list type="bullet">
    /// <item><description>The localized text.</description></item>
    /// <item><description>
    /// The failure text (see <see cref="ILocalizer.LookupFailureTextBuilder" />) when the lookup fails.
    /// </description></item>
    /// <item><description>
    /// An empty string when value 1 gives no key to look up. When value 1 is a key (<c>isEnumKey</c> is
    /// <see langword="false" />), that is the case when it is neither a non-empty <see cref="string" /> nor a
    /// <see cref="ResourceKey" /> that has a resource manager. When value 1 is an enumeration value
    /// (<c>isEnumKey</c> is <see langword="true" />), that is the case when it is not an <see cref="Enum" />.
    /// Examples are <see langword="null" /> and a binding that has no value yet.
    /// </description></item>
    /// </list>
    /// </returns>
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        // Never throws for a key value: a key that cannot be looked up renders empty.
        var (key, keyResourceManager) = (this.isEnumKey, values[1]) switch
        {
            (true, Enum value) => (Localizer.Current.BuildEnumKey(value, this.enumKeyPrefix), this.resourceManager),
            (false, string { Length: > 0 } name) => (name, this.resourceManager),
            (false, ResourceKey { Manager: not null } resourceKey) => (resourceKey.Name, resourceKey.Manager),
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
