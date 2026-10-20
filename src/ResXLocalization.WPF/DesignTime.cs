namespace RentADeveloper.ResXLocalization.WPF;

/// <summary>
/// Hosts the <c>DesignTime.Culture</c> attached property, which selects the language a XAML designer
/// previews. Set it on the root element: <c>&lt;Window ... l:DesignTime.Culture="de"&gt;</c>.
/// </summary>
/// <remarks>
/// <para>
/// The property is for design time only and has no effect at run time. Inside the Visual Studio WPF
/// designer or Rider's WPF previewer it assigns <see cref="ILocalizer.CurrentCulture" /> of the process-wide
/// <see cref="Localizer.Current" />, so strings that view models look up in code agree with the XAML. It
/// overrides a culture selected by initialization that the designer executes, such as a
/// <c>[ModuleInitializer]</c> that selects English.
/// </para>
/// <para>
/// Without it, the designer uses the machine's UI culture, unless initialization that the designer executes
/// selects another culture. The designer does not call <c>Application.OnStartup</c>, so a culture selected
/// there does not apply at design time.
/// </para>
/// <para>
/// An empty name and a name that <see cref="CultureInfo" /> rejects, such as a half-typed one, leave the
/// culture unchanged.
/// </para>
/// </remarks>
public static class DesignTime
{
    /// <summary>
    /// Identifies the <c>DesignTime.Culture</c> attached property: the name of the culture a XAML designer
    /// previews, such as <c>de</c>.
    /// </summary>
    public static readonly DependencyProperty CultureProperty = DependencyProperty.RegisterAttached(
        "Culture",
        typeof(string),
        typeof(DesignTime),
        new PropertyMetadata(null, OnCultureChanged)
    );

    /// <summary>Gets the culture name set on <paramref name="element" /> for the XAML designer.</summary>
    /// <param name="element">The element to read the value from.</param>
    /// <returns>The culture name, or <see langword="null" /> if none is set.</returns>
    public static string? GetCulture(DependencyObject element) => (string?)element.GetValue(CultureProperty);

    /// <summary>
    /// Sets the culture name a XAML designer previews. Inside a designer this assigns
    /// <see cref="ILocalizer.CurrentCulture" /> of <see cref="Localizer.Current" />; at run time it only stores
    /// the value.
    /// </summary>
    /// <param name="element">The element to set the value on, usually the root element of the view.</param>
    /// <param name="value">The culture name, such as <c>de</c>.</param>
    public static void SetCulture(DependencyObject element, string? value) => element.SetValue(CultureProperty, value);

    /// <summary>
    /// Assigns the new culture to <see cref="ILocalizer.CurrentCulture" /> of <see cref="Localizer.Current" />
    /// inside a XAML designer; does nothing at run time, for an empty name and for a name that
    /// <see cref="CultureInfo" /> rejects.
    /// </summary>
    /// <param name="element">The element the value was set on.</param>
    /// <param name="e">The old and the new culture name.</param>
    private static void OnCultureChanged(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        if (!DesignMode.IsInDesignMode || e.NewValue is not string { Length: > 0 } name)
        {
            return;
        }

        try
        {
            Localizer.Current.CurrentCulture = new(name);
        }
        catch (CultureNotFoundException)
        {
            // A half-typed culture name must not take the preview down.
        }
    }
}
