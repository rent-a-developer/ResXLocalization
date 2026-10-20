namespace RentADeveloper.ResXLocalization.MessageFormat;

/// <summary>
/// The ICU MessageFormat <see cref="IMessageFormatter" /> that
/// <see cref="MessageFormatLocalizerExtensions.UseMessageFormat" /> installs. It formats every resolved value
/// with one <see cref="Jeffijoe.MessageFormat.MessageFormatter" />, also a value without arguments.
/// </summary>
/// <param name="messageFormatter">
/// The formatter of the MessageFormat library, used as given for every culture and every thread.
/// </param>
internal sealed class IcuMessageFormatter(JeffijoeMessageFormatter messageFormatter) : IMessageFormatter
{
    /// <summary>
    /// The culture to format in when the lookup culture is the invariant culture: <c>en</c>, or the invariant
    /// culture itself where <c>en</c> cannot be created (globalization-invariant mode).
    /// </summary>
    private readonly CultureInfo englishCulture = ResolveEnglishCulture();

    /// <summary>The formatter of the MessageFormat library.</summary>
    private readonly JeffijoeMessageFormatter messageFormatter = messageFormatter;

    /// <summary>
    /// Formats <paramref name="pattern" /> as an ICU message, also when <paramref name="arguments" /> is empty.
    /// Every argument is passed under its <see cref="MessageArgument.Position" /> (<c>"0"</c>, <c>"1"</c>, and
    /// so on) and, if it has a <see cref="MessageArgument.Name" />, under that name as well; a name overwrites
    /// a position with the same text.
    /// </summary>
    /// <param name="pattern">The resolved resource value.</param>
    /// <param name="arguments">The arguments of the lookup; never <see langword="null" />.</param>
    /// <param name="culture">
    /// The culture to format in. The invariant culture formats as <c>en</c>, because the plural rules of the
    /// MessageFormat library for the invariant culture know only the category <c>other</c>.
    /// </param>
    /// <returns>The formatted message.</returns>
    /// <exception cref="Jeffijoe.MessageFormat.MessageFormatterException">
    /// <paramref name="pattern" /> is not a valid ICU message, references a variable
    /// <paramref name="arguments" /> does not supply, or uses an unsupported style. The localizer catches it,
    /// like any other exception of a formatter, and returns the failure text.
    /// </exception>
    /// <exception cref="FormatException">A <c>plural</c> argument is a string that doesn't represent a number.</exception>
    /// <exception cref="InvalidCastException">
    /// A <c>plural</c> argument is neither a number nor a string.
    /// </exception>
    public string Format(string pattern, MessageArguments arguments, CultureInfo culture)
    {
        var values = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var argument in arguments)
        {
            values[argument.Position.ToString(CultureInfo.InvariantCulture)] = argument.Value;
        }

        // A second pass, so that a name overwrites a position whatever the order of the arguments.
        foreach (var argument in arguments)
        {
            if (argument.Name is { } name)
            {
                values[name] = argument.Value;
            }
        }

        return this.messageFormatter.FormatMessage(
            pattern,
            values,
            culture.Name.Length == 0 ? this.englishCulture : culture
        );
    }

    /// <summary>
    /// Returns <c>en</c>, or the invariant culture when <c>en</c> cannot be created, as in
    /// globalization-invariant mode with only predefined cultures allowed.
    /// </summary>
    /// <returns>The culture that replaces the invariant culture when formatting.</returns>
    private static CultureInfo ResolveEnglishCulture()
    {
        try
        {
            return CultureInfo.GetCultureInfo("en");
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.InvariantCulture;
        }
    }
}
