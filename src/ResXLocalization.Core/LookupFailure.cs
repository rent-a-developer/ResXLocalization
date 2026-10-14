namespace RentADeveloper.ResXLocalization;

/// <summary>
/// Describes a failed lookup. <see cref="ILocalizer.LookupFailed" /> reports it, and
/// <see cref="ILocalizer.LookupFailureTextBuilder" /> receives it to produce the failure text.
/// </summary>
/// <param name="Reason">
/// Why the lookup failed: <see cref="LookupFailureReason.TranslationNotFound" /> or
/// <see cref="LookupFailureReason.MessageFormatFailed" />.
/// </param>
/// <param name="Key">
/// The key as looked up, including an enum lookup's full convention key (for example
/// <c>Enum_FileSortOrder_Ascending</c>); never <see langword="null" /> or empty.
/// </param>
/// <param name="Culture">
/// The culture of the lookup - the <see cref="ILocalizer.CurrentCulture" /> read once at its start;
/// never <see langword="null" />.
/// </param>
/// <param name="ResourceManager">
/// The resource manager of a scoped or typed lookup (for a typed lookup,
/// <see cref="ResourceKey.Manager" />); <see langword="null" /> for a search-all lookup.
/// </param>
/// <param name="Arguments">
/// The arguments of the lookup; never <see langword="null" />, empty when the lookup had none.
/// </param>
/// <param name="Pattern">
/// For <see cref="LookupFailureReason.MessageFormatFailed" />, the resolved resource value that failed to
/// format; for <see cref="LookupFailureReason.TranslationNotFound" />, <see langword="null" />.
/// </param>
/// <param name="Exception">
/// For <see cref="LookupFailureReason.MessageFormatFailed" />, what the formatter threw; for
/// <see cref="LookupFailureReason.TranslationNotFound" />, <see langword="null" />.
/// </param>
public readonly record struct LookupFailure(
    LookupFailureReason Reason,
    string Key,
    CultureInfo Culture,
    ResourceManager? ResourceManager,
    MessageArguments Arguments,
    string? Pattern,
    Exception? Exception
);
