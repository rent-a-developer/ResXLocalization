namespace RentADeveloper.ResXLocalization;

/// <summary>
/// The default <see cref="IMessageFormatter" />: returns a value without arguments unchanged, and
/// formats a value with arguments with
/// <see cref="string.Format(IFormatProvider, string, object[])" /> in the lookup's culture. Argument
/// names are ignored; only positions count.
/// </summary>
public sealed class StringFormatMessageFormatter : IMessageFormatter
{
    /// <summary>The shared instance, and the default of <see cref="ILocalizer.MessageFormatter" />.</summary>
    public static readonly StringFormatMessageFormatter Instance = new();

    /// <summary>
    /// Returns <paramref name="pattern" /> unchanged when <paramref name="arguments" /> is empty;
    /// otherwise formats it as a composite format string with the argument values in
    /// <paramref name="culture" />.
    /// </summary>
    /// <param name="pattern">The resolved resource value.</param>
    /// <param name="arguments">The arguments of the lookup; never <see langword="null" />.</param>
    /// <param name="culture">The culture to format in.</param>
    /// <returns>The formatted string, or <paramref name="pattern" /> when there are no arguments.</returns>
    /// <exception cref="FormatException">
    /// <paramref name="pattern" /> is not a valid composite format string for
    /// <paramref name="arguments" />. The localizer catches it and returns the failure text.
    /// </exception>
    public string Format(string pattern, MessageArgument[] arguments, CultureInfo culture) =>
        arguments.Length == 0
            ? pattern
            : string.Format(culture, pattern, arguments.Select(argument => argument.Value).ToArray());
}
