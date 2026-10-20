namespace RentADeveloper.ResXLocalization;

/// <summary>
/// Turns a resolved resource value and the arguments of its lookup - possibly none - into the final
/// string. Assign an implementation to <see cref="ILocalizer.MessageFormatter" /> to replace the default
/// <see cref="StringFormatMessageFormatter" />.
/// </summary>
/// <remarks>
/// <para>
/// The localizer calls <see cref="Format" /> for <b>every</b> lookup that resolves a resource value,
/// whether or not the lookup has arguments, so a formatter is the one hook every translated text passes
/// through. It is never called for a key no resource file contains, and never for a
/// <see langword="null" /> or empty key.
/// </para>
/// <para>
/// An implementation signals a malformed pattern or unusable arguments by <b>throwing</b>. The
/// localizer catches the exception, raises <see cref="ILocalizer.LookupFailed" /> and returns the
/// failure text. Because every lookup calls the formatter, a formatter that throws for values without
/// arguments turns plain texts into the failure text too.
/// </para>
/// </remarks>
public interface IMessageFormatter
{
    /// <summary>Formats a resolved resource value with the arguments of its lookup.</summary>
    /// <param name="pattern">The resolved resource value.</param>
    /// <param name="arguments">
    /// The arguments of the lookup; never <see langword="null" />. Read each argument's position from
    /// <see cref="MessageArgument.Position" />; an argument's index in the enumeration is not guaranteed to be
    /// the same as its position. They are empty (<see cref="MessageArguments.Count" />
    /// is 0) when the lookup had no arguments: a call to <c>ILocalizer.Get</c> without arguments (or with
    /// <see cref="MessageArguments.Empty" />), a lookup through the <see cref="ILocalizer" /> indexer
    /// (<c>Localizer.Current["Key"]</c>), or a XAML lookup through the <c>{l:Localize}</c> markup extension of
    /// the ResXLocalization.Avalonia and ResXLocalization.WPF packages without arguments.
    /// The default formatter then returns <paramref name="pattern" /> unchanged, and an implementation
    /// should do the same unless it deliberately handles values without arguments: formatting them would
    /// turn <c>{{x}}</c> into <c>{x}</c> and make every plain text containing <c>{0}</c> or a literal
    /// <c>{</c> fail.
    /// </param>
    /// <param name="culture">The localizer's <see cref="ILocalizer.CurrentCulture" /> at the time of the lookup.</param>
    /// <returns>The final string.</returns>
    string Format(string pattern, MessageArguments arguments, CultureInfo culture);
}
