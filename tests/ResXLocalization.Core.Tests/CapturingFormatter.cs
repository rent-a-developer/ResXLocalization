namespace RentADeveloper.ResXLocalization.Core.Tests;

/// <summary>
/// An <see cref="IMessageFormatter" /> that records every call in order and otherwise behaves like the
/// default formatter: it returns the pattern unchanged without arguments and formats it with
/// <see cref="string.Format(IFormatProvider, string, object[])" /> otherwise, so installing it changes no
/// output. Compiled into the UI test suites too.
/// </summary>
internal sealed class CapturingFormatter : IMessageFormatter
{
    /// <summary>Gets the recorded calls, in order.</summary>
    public List<(string Pattern, MessageArgument[] Arguments, CultureInfo Culture)> Calls { get; } = [];

    /// <inheritdoc />
    public string Format(string pattern, MessageArgument[] arguments, CultureInfo culture)
    {
        this.Calls.Add((pattern, arguments, culture));

        return arguments.Length == 0
            ? pattern
            : string.Format(culture, pattern, arguments.Select(argument => argument.Value).ToArray());
    }
}
