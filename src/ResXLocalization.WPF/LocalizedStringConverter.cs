namespace RentADeveloper.ResXLocalization.WPF;

/// <summary>
/// Internal value converter used by <see cref="LocalizeExtension" />. It ignores the value it receives
/// (a <see cref="Localizer.CurrentCulture" /> change is only a trigger) and returns the localized string
/// produced by the captured factory. Binding to the ambient <see cref="Localizer.Current" /> - an
/// <see cref="INotifyPropertyChanged" /> source - is what makes WPF re-run the conversion, and keeps the
/// subscription leak-safe (WPF holds the binding target through a weak reference).
/// </summary>
/// <param name="valueFactory">Produces the localized string for the current culture on demand.</param>
internal sealed class LocalizedStringConverter(Func<String> valueFactory) : IValueConverter
{
    /// <summary>Returns the freshly resolved localized string, ignoring the trigger value.</summary>
    /// <param name="value">The bound <see cref="ILocalizer.CurrentCulture" /> value; only a re-conversion trigger.</param>
    /// <param name="targetType">The type of the binding target property.</param>
    /// <param name="parameter">An optional converter parameter; not used.</param>
    /// <param name="culture">The culture supplied by the binding; not used.</param>
    /// <returns>The localized string produced by the captured factory.</returns>
    public Object Convert(Object? value, Type targetType, Object? parameter, CultureInfo culture) =>
        valueFactory();

    /// <summary>Not supported; this converter is one-way.</summary>
    /// <param name="value">The value produced by the binding target.</param>
    /// <param name="targetType">The type to convert back to.</param>
    /// <param name="parameter">An optional converter parameter; not used.</param>
    /// <param name="culture">The culture supplied by the binding; not used.</param>
    /// <returns>Never returns; always throws.</returns>
    /// <exception cref="NotSupportedException">Always thrown; the converter is one-way.</exception>
    public Object ConvertBack(Object? value, Type targetType, Object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
