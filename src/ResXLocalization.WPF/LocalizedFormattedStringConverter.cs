namespace RentADeveloper.ResXLocalization.WPF;

/// <summary>
/// Internal multi-value converter used by <see cref="LocalizeExtension" />. The first two bound
/// values - the ambient <see cref="Localizer.CurrentCulture" /> and the element's
/// <see cref="LocalizeArgs" /> args-version - are only re-conversion triggers; the third is the
/// target element itself, whose <see cref="LocalizeArgs" /> arguments are read freshly on every
/// conversion. Binding to the ambient <see cref="Localizer.Current" /> - an
/// <see cref="INotifyPropertyChanged" /> source - keeps the subscription leak-safe (WPF holds the
/// binding target through a weak reference).
/// </summary>
/// <param name="resolve">Resolves the localized string without composite formatting.</param>
/// <param name="resolveFormatted">Resolves the localized string formatted with the supplied arguments.</param>
internal sealed class LocalizedFormattedStringConverter(
    Func<String> resolve,
    Func<Object?[], String> resolveFormatted
)
    : IMultiValueConverter
{
    /// <summary>
    /// Returns the freshly resolved localized string: formatted with the target element's current
    /// <see cref="LocalizeArgs" /> arguments when at least one is set, otherwise resolved plainly so
    /// the no-argument behavior stays byte-for-byte unchanged.
    /// </summary>
    /// <param name="values">
    /// The bound values: culture trigger, args-version trigger, and the target element. When the
    /// third value is missing or not a <see cref="DependencyObject" /> (for example
    /// <see cref="DependencyProperty.UnsetValue" />), the string resolves without arguments.
    /// </param>
    /// <param name="targetType">The type of the binding target property.</param>
    /// <param name="parameter">An optional converter parameter; not used.</param>
    /// <param name="culture">
    /// The culture supplied by the binding; not used (the active culture is taken from
    /// <see cref="Localizer.Current" />).
    /// </param>
    /// <returns>
    /// The localized, possibly formatted string, or <see cref="DependencyProperty.UnsetValue" /> when
    /// the format string does not (yet) match the supplied arguments - routine while multi-argument
    /// bindings still deliver their values one by one - so the mismatch never throws into the
    /// property system mid-delivery and the text resolves cleanly on the next argument or culture
    /// change.
    /// </returns>
    public Object Convert(Object?[] values, Type targetType, Object? parameter, CultureInfo culture)
    {
        if (values.Length < 3 || values[2] is not DependencyObject element)
        {
            return resolve();
        }

        try
        {
            var arguments = LocalizeArgs.GetArguments(element);

            return arguments is null ? resolve() : resolveFormatted(arguments);
        }
        catch (FormatException)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    /// <summary>Not supported; this converter is one-way.</summary>
    /// <param name="value">The value produced by the binding target.</param>
    /// <param name="targetTypes">The types to convert back to.</param>
    /// <param name="parameter">An optional converter parameter; not used.</param>
    /// <param name="culture">The culture supplied by the binding; not used.</param>
    /// <returns>Never returns; always throws.</returns>
    /// <exception cref="NotSupportedException">Always thrown; the converter is one-way.</exception>
    public Object[] ConvertBack(Object? value, Type[] targetTypes, Object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
