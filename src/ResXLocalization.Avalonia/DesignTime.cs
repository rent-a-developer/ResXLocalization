using Avalonia.Controls;

namespace RentADeveloper.ResXLocalization.Avalonia;

/// <summary>
/// Hosts the <c>DesignTime.Culture</c> attached property, which selects the language the Avalonia XAML
/// previewer shows. Set it on the root element: <c>&lt;Window ... l:DesignTime.Culture="de"&gt;</c>.
/// </summary>
/// <remarks>
/// <para>
/// The property is for design time only and has no effect at run time. Inside the previewer it assigns
/// <see cref="ILocalizer.CurrentCulture" /> of the process-wide <see cref="Localizer.Current" />, so strings
/// that view models look up in code agree with the XAML. It overrides a culture selected by initialization
/// that the previewer executes, such as a culture assigned in <c>BuildAvaloniaApp</c>.
/// </para>
/// <para>
/// Without it, the previewer uses the machine's UI culture, unless initialization that the previewer
/// executes selects another culture.
/// </para>
/// <para>
/// An empty name and a name that <see cref="CultureInfo" /> rejects, such as a half-typed one, leave the
/// culture unchanged.
/// </para>
/// </remarks>
public sealed class DesignTime : AvaloniaObject
{
    /// <summary>
    /// Identifies the <c>DesignTime.Culture</c> attached property: the name of the culture the previewer
    /// shows, such as <c>de</c>.
    /// </summary>
    public static readonly AttachedProperty<string?> CultureProperty = AvaloniaProperty.RegisterAttached<
        DesignTime,
        AvaloniaObject,
        string?
    >("Culture");

    /// <summary>
    /// Hooks the change handler of <see cref="CultureProperty" /> once, for the lifetime of the process.
    /// </summary>
    static DesignTime() =>
        _ = CultureProperty.Changed.AddClassHandler<AvaloniaObject, string?>(
            static (_, e) => OnCultureChanged(e.NewValue.GetValueOrDefault())
        );

    private DesignTime() { }

    /// <summary>Gets the culture name set on <paramref name="element" /> for the previewer.</summary>
    /// <param name="element">The element to read the value from.</param>
    /// <returns>The culture name, or <see langword="null" /> if none is set.</returns>
    public static string? GetCulture(AvaloniaObject element) => element.GetValue(CultureProperty);

    /// <summary>
    /// Sets the culture name the previewer shows. Inside the previewer this assigns
    /// <see cref="ILocalizer.CurrentCulture" /> of <see cref="Localizer.Current" />; at run time it only stores
    /// the value.
    /// </summary>
    /// <param name="element">The element to set the value on, usually the root element of the view.</param>
    /// <param name="value">The culture name, such as <c>de</c>.</param>
    public static void SetCulture(AvaloniaObject element, string? value) => element.SetValue(CultureProperty, value);

    /// <summary>
    /// Assigns the new culture to <see cref="ILocalizer.CurrentCulture" /> of <see cref="Localizer.Current" />
    /// inside the previewer; does nothing at run time, for an empty name and for a name that
    /// <see cref="CultureInfo" /> rejects.
    /// </summary>
    /// <param name="newValue">The new culture name.</param>
    private static void OnCultureChanged(string? newValue)
    {
        if (!Design.IsDesignMode || newValue is not { Length: > 0 } name)
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
