namespace RentADeveloper.ResXLocalization;

/// <summary>
/// Centralizes the convention that maps an enumeration value to a resource key:
/// <c>{keyPrefix}{EnumTypeName}_{Value}</c>, for example <c>Enum_Fruit_Apple</c>. Shared (via
/// <c>InternalsVisibleTo</c>) by the core enum lookups and the Avalonia/WPF enum markup extensions
/// and converters, so every enum lookup builds identical keys.
/// </summary>
internal static class EnumKeyConvention
{
    /// <summary>The default prefix (<c>Enum_</c>) prepended to generated enumeration resource keys.</summary>
    internal const string DefaultEnumKeyPrefix = "Enum_";

    /// <summary>Builds the resource key for an enumeration value.</summary>
    /// <param name="value">The enumeration value to build the key for.</param>
    /// <param name="keyPrefix">The prefix prepended to the generated key.</param>
    /// <returns>The key <c>{keyPrefix}{EnumTypeName}_{Value}</c>, for example <c>Enum_Fruit_Apple</c>.</returns>
    internal static string BuildEnumKey(Enum value, string keyPrefix) => keyPrefix + value.GetType().Name + "_" + value;
}
