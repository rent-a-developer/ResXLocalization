namespace RentADeveloper.ResXLocalization;

/// <summary>
/// Builds the resource key of an enum lookup through <see cref="ILocalizer.EnumKeyBuilder" />. Shared
/// (via <c>InternalsVisibleTo</c>) by the core enum lookup and the Avalonia/WPF enum markup extensions
/// and converters, so every enum lookup applies the same rules to the builder's result.
/// </summary>
internal static class EnumKeyBuilding
{
    /// <summary>
    /// Calls <see cref="ILocalizer.EnumKeyBuilder" /> of <paramref name="localizer" /> and checks the
    /// result, so every caller gets the same rules.
    /// </summary>
    /// <param name="localizer">The localizer whose builder builds the key.</param>
    /// <param name="value">The enumeration value to build the key for.</param>
    /// <param name="keyPrefix">The key prefix the lookup passed, or <see langword="null" /> for none.</param>
    /// <returns>The resource key; never <see langword="null" /> or empty.</returns>
    /// <exception cref="InvalidOperationException">
    /// The builder returned <see langword="null" /> or an empty string.
    /// </exception>
    internal static string BuildEnumKey(this ILocalizer localizer, Enum value, string? keyPrefix)
    {
        var key = localizer.EnumKeyBuilder(value, keyPrefix);

        return string.IsNullOrEmpty(key)
            ? throw new InvalidOperationException(
                $"{nameof(ILocalizer.EnumKeyBuilder)} returned null or an empty string for the enum value "
                    + $"{value.GetType().FullName}.{value}. Return a non-empty resource key."
            )
            : key;
    }
}
