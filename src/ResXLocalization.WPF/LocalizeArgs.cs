namespace RentADeveloper.ResXLocalization.WPF;

/// <summary>
/// Hosts the <c>Arg0</c>…<c>Arg8</c> attached properties that supply dynamic composite-format
/// arguments to <see cref="LocalizeExtension" />. Bind them on the element that carries the
/// localized property, for example
/// <c>&lt;TextBlock l:LocalizeArgs.Arg0="{Binding PeopleCount}" Text="{l:Localize {x:Static res:StringsKeys.PeopleInvited}}" /&gt;</c>
/// with a resource value such as <c>{0} people invited</c>; the rendered text re-formats live
/// whenever a bound argument or the culture of the ambient <see cref="Localizer.Current" /> changes.
/// </summary>
/// <remarks>
/// While no argument is set on the element, the resource resolves exactly as without this class -
/// no composite formatting runs, so resource values containing literal braces keep working. As soon
/// as at least one argument is set, the resolved value is treated as a composite format string:
/// the arguments <c>Arg0</c> up to the highest set slot are passed to the formatting
/// <c>ILocalizer.Get</c> overloads, with unset slots below the highest formatting as
/// <see langword="null" />. The arguments are per element - two localized properties on the same
/// element share the one argument set.
/// </remarks>
public static class LocalizeArgs
{
    /// <summary>Identifies the <c>LocalizeArgs.Arg0</c> attached property: format argument <c>{0}</c>.</summary>
    public static readonly DependencyProperty Arg0Property = RegisterArgument("Arg0");

    /// <summary>Identifies the <c>LocalizeArgs.Arg1</c> attached property: format argument <c>{1}</c>.</summary>
    public static readonly DependencyProperty Arg1Property = RegisterArgument("Arg1");

    /// <summary>Identifies the <c>LocalizeArgs.Arg2</c> attached property: format argument <c>{2}</c>.</summary>
    public static readonly DependencyProperty Arg2Property = RegisterArgument("Arg2");

    /// <summary>Identifies the <c>LocalizeArgs.Arg3</c> attached property: format argument <c>{3}</c>.</summary>
    public static readonly DependencyProperty Arg3Property = RegisterArgument("Arg3");

    /// <summary>Identifies the <c>LocalizeArgs.Arg4</c> attached property: format argument <c>{4}</c>.</summary>
    public static readonly DependencyProperty Arg4Property = RegisterArgument("Arg4");

    /// <summary>Identifies the <c>LocalizeArgs.Arg5</c> attached property: format argument <c>{5}</c>.</summary>
    public static readonly DependencyProperty Arg5Property = RegisterArgument("Arg5");

    /// <summary>Identifies the <c>LocalizeArgs.Arg6</c> attached property: format argument <c>{6}</c>.</summary>
    public static readonly DependencyProperty Arg6Property = RegisterArgument("Arg6");

    /// <summary>Identifies the <c>LocalizeArgs.Arg7</c> attached property: format argument <c>{7}</c>.</summary>
    public static readonly DependencyProperty Arg7Property = RegisterArgument("Arg7");

    /// <summary>Identifies the <c>LocalizeArgs.Arg8</c> attached property: format argument <c>{8}</c>.</summary>
    public static readonly DependencyProperty Arg8Property = RegisterArgument("Arg8");

    /// <summary>Gets the value of the <see cref="Arg0Property" /> attached property.</summary>
    /// <param name="element">The element holding the argument.</param>
    /// <returns>The argument value, or <see langword="null" /> when the argument is not set.</returns>
    public static Object? GetArg0(DependencyObject element) => GetArgument(element, Arg0Property);

    /// <summary>Gets the value of the <see cref="Arg1Property" /> attached property.</summary>
    /// <param name="element">The element holding the argument.</param>
    /// <returns>The argument value, or <see langword="null" /> when the argument is not set.</returns>
    public static Object? GetArg1(DependencyObject element) => GetArgument(element, Arg1Property);

    /// <summary>Gets the value of the <see cref="Arg2Property" /> attached property.</summary>
    /// <param name="element">The element holding the argument.</param>
    /// <returns>The argument value, or <see langword="null" /> when the argument is not set.</returns>
    public static Object? GetArg2(DependencyObject element) => GetArgument(element, Arg2Property);

    /// <summary>Gets the value of the <see cref="Arg3Property" /> attached property.</summary>
    /// <param name="element">The element holding the argument.</param>
    /// <returns>The argument value, or <see langword="null" /> when the argument is not set.</returns>
    public static Object? GetArg3(DependencyObject element) => GetArgument(element, Arg3Property);

    /// <summary>Gets the value of the <see cref="Arg4Property" /> attached property.</summary>
    /// <param name="element">The element holding the argument.</param>
    /// <returns>The argument value, or <see langword="null" /> when the argument is not set.</returns>
    public static Object? GetArg4(DependencyObject element) => GetArgument(element, Arg4Property);

    /// <summary>Gets the value of the <see cref="Arg5Property" /> attached property.</summary>
    /// <param name="element">The element holding the argument.</param>
    /// <returns>The argument value, or <see langword="null" /> when the argument is not set.</returns>
    public static Object? GetArg5(DependencyObject element) => GetArgument(element, Arg5Property);

    /// <summary>Gets the value of the <see cref="Arg6Property" /> attached property.</summary>
    /// <param name="element">The element holding the argument.</param>
    /// <returns>The argument value, or <see langword="null" /> when the argument is not set.</returns>
    public static Object? GetArg6(DependencyObject element) => GetArgument(element, Arg6Property);

    /// <summary>Gets the value of the <see cref="Arg7Property" /> attached property.</summary>
    /// <param name="element">The element holding the argument.</param>
    /// <returns>The argument value, or <see langword="null" /> when the argument is not set.</returns>
    public static Object? GetArg7(DependencyObject element) => GetArgument(element, Arg7Property);

    /// <summary>Gets the value of the <see cref="Arg8Property" /> attached property.</summary>
    /// <param name="element">The element holding the argument.</param>
    /// <returns>The argument value, or <see langword="null" /> when the argument is not set.</returns>
    public static Object? GetArg8(DependencyObject element) => GetArgument(element, Arg8Property);

    /// <summary>Sets the value of the <see cref="Arg0Property" /> attached property.</summary>
    /// <param name="element">The element to hold the argument.</param>
    /// <param name="value">The argument value; <see langword="null" /> is a valid, set value.</param>
    public static void SetArg0(DependencyObject element, Object? value) => element.SetValue(Arg0Property, value);

    /// <summary>Sets the value of the <see cref="Arg1Property" /> attached property.</summary>
    /// <param name="element">The element to hold the argument.</param>
    /// <param name="value">The argument value; <see langword="null" /> is a valid, set value.</param>
    public static void SetArg1(DependencyObject element, Object? value) => element.SetValue(Arg1Property, value);

    /// <summary>Sets the value of the <see cref="Arg2Property" /> attached property.</summary>
    /// <param name="element">The element to hold the argument.</param>
    /// <param name="value">The argument value; <see langword="null" /> is a valid, set value.</param>
    public static void SetArg2(DependencyObject element, Object? value) => element.SetValue(Arg2Property, value);

    /// <summary>Sets the value of the <see cref="Arg3Property" /> attached property.</summary>
    /// <param name="element">The element to hold the argument.</param>
    /// <param name="value">The argument value; <see langword="null" /> is a valid, set value.</param>
    public static void SetArg3(DependencyObject element, Object? value) => element.SetValue(Arg3Property, value);

    /// <summary>Sets the value of the <see cref="Arg4Property" /> attached property.</summary>
    /// <param name="element">The element to hold the argument.</param>
    /// <param name="value">The argument value; <see langword="null" /> is a valid, set value.</param>
    public static void SetArg4(DependencyObject element, Object? value) => element.SetValue(Arg4Property, value);

    /// <summary>Sets the value of the <see cref="Arg5Property" /> attached property.</summary>
    /// <param name="element">The element to hold the argument.</param>
    /// <param name="value">The argument value; <see langword="null" /> is a valid, set value.</param>
    public static void SetArg5(DependencyObject element, Object? value) => element.SetValue(Arg5Property, value);

    /// <summary>Sets the value of the <see cref="Arg6Property" /> attached property.</summary>
    /// <param name="element">The element to hold the argument.</param>
    /// <param name="value">The argument value; <see langword="null" /> is a valid, set value.</param>
    public static void SetArg6(DependencyObject element, Object? value) => element.SetValue(Arg6Property, value);

    /// <summary>Sets the value of the <see cref="Arg7Property" /> attached property.</summary>
    /// <param name="element">The element to hold the argument.</param>
    /// <param name="value">The argument value; <see langword="null" /> is a valid, set value.</param>
    public static void SetArg7(DependencyObject element, Object? value) => element.SetValue(Arg7Property, value);

    /// <summary>Sets the value of the <see cref="Arg8Property" /> attached property.</summary>
    /// <param name="element">The element to hold the argument.</param>
    /// <param name="value">The argument value; <see langword="null" /> is a valid, set value.</param>
    public static void SetArg8(DependencyObject element, Object? value) => element.SetValue(Arg8Property, value);

    /// <summary>
    /// The single "arguments changed" signal <see cref="LocalizeExtension" /> subscribes to: every
    /// <c>ArgN</c> change increments this version on the element, so one binding per localized
    /// property suffices instead of nine.
    /// </summary>
    internal static readonly DependencyProperty ArgsVersionProperty = DependencyProperty.RegisterAttached(
        "ArgsVersion",
        typeof(Int32),
        typeof(LocalizeArgs),
        new PropertyMetadata(0)
    );

    /// <summary>
    /// Reads the format arguments currently set on <paramref name="element" />, applying the
    /// trimming rules: arguments above the highest set slot are dropped, unset slots below it
    /// become <see langword="null" />.
    /// </summary>
    /// <param name="element">The element to read the arguments from.</param>
    /// <returns>
    /// The arguments to format with, or <see langword="null" /> when no argument is set on the
    /// element - the caller must then resolve without composite formatting.
    /// </returns>
    internal static Object?[]? GetArguments(DependencyObject element)
    {
        var lastSetIndex = -1;

        for (var index = ArgumentProperties.Length - 1; index >= 0; index--)
        {
            if (!ReferenceEquals(element.GetValue(ArgumentProperties[index]), UnsetSentinel.Value))
            {
                lastSetIndex = index;
                break;
            }
        }

        if (lastSetIndex < 0)
        {
            return null;
        }

        var arguments = new Object?[lastSetIndex + 1];

        for (var index = 0; index <= lastSetIndex; index++)
        {
            var value = element.GetValue(ArgumentProperties[index]);
            arguments[index] = ReferenceEquals(value, UnsetSentinel.Value) ? null : value;
        }

        return arguments;
    }

    /// <summary>Reads one argument slot, mapping the unset sentinel to <see langword="null" />.</summary>
    /// <param name="element">The element holding the argument.</param>
    /// <param name="property">The argument slot to read.</param>
    /// <returns>The argument value, or <see langword="null" /> when the argument is not set.</returns>
    private static Object? GetArgument(DependencyObject element, DependencyProperty property)
    {
        var value = element.GetValue(property);
        return ReferenceEquals(value, UnsetSentinel.Value) ? null : value;
    }

    /// <summary>
    /// Increments <see cref="ArgsVersionProperty" /> on the element whose argument changed, which is
    /// what re-triggers the multi-binding produced by <see cref="LocalizeExtension" />.
    /// </summary>
    /// <param name="element">The element whose argument changed.</param>
    /// <param name="e">The change data; not used.</param>
    private static void OnArgumentChanged(DependencyObject element, DependencyPropertyChangedEventArgs e) =>
        element.SetValue(ArgsVersionProperty, (Int32)element.GetValue(ArgsVersionProperty) + 1);

    /// <summary>
    /// Registers one argument slot. The default is a private sentinel (not <see langword="null" />),
    /// so "argument never set" and "argument bound to null" stay distinguishable, and every change
    /// bumps <see cref="ArgsVersionProperty" /> on the element.
    /// </summary>
    /// <param name="name">The attached property name, <c>Arg0</c>…<c>Arg8</c>.</param>
    /// <returns>The registered attached property.</returns>
    private static DependencyProperty RegisterArgument(String name) =>
        DependencyProperty.RegisterAttached(
            name,
            typeof(Object),
            typeof(LocalizeArgs),
            new PropertyMetadata(UnsetSentinel.Value, OnArgumentChanged)
        );

    /// <summary>The nine argument slots in slot order, for <see cref="GetArguments" />.</summary>
    private static readonly DependencyProperty[] ArgumentProperties =
    [
        Arg0Property,
        Arg1Property,
        Arg2Property,
        Arg3Property,
        Arg4Property,
        Arg5Property,
        Arg6Property,
        Arg7Property,
        Arg8Property
    ];

    /// <summary>
    /// Holds the "argument never set" default of the <c>ArgN</c> properties. A nested type keeps the
    /// sentinel initialized before the attached-property fields above it, regardless of textual order.
    /// </summary>
    private static class UnsetSentinel
    {
        /// <summary>The sentinel instance; compared by reference.</summary>
        internal static readonly Object Value = new();
    }
}
