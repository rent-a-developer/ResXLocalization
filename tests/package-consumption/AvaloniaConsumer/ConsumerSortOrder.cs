namespace AvaloniaConsumer;

/// <summary>
/// An enumeration the consumer localizes by convention, to prove that
/// <c>{keyPrefix}{EnumTypeName}_{Value}</c> still resolves once the application is trimmed and
/// compiled ahead of time. The key names in Strings.resx are derived from this type's name.
/// </summary>
public enum ConsumerSortOrder
{
    /// <summary>Ascending order.</summary>
    Ascending,

    /// <summary>Descending order.</summary>
    Descending,
}
