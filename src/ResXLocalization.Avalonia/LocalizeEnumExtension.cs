namespace RentADeveloper.ResXLocalization.Avalonia;

/// <summary>
/// XAML markup extension that localizes the enumeration value held in the target control's
/// <c>DataContext</c>, re-resolving live on every culture change. Intended for item templates where
/// each item <em>is</em> an enumeration value, for example
/// <c>&lt;TextBlock Text="{l:LocalizeEnum}" /&gt;</c>. The value is mapped to a resource key using the
/// convention <c>{KeyPrefix}{EnumTypeName}_{Value}</c>.
/// </summary>
public sealed class LocalizeEnumExtension : MarkupExtension
{
    /// <summary>
    /// Gets or sets the prefix prepended to the generated resource key. Defaults to <c>Enum_</c>.
    /// </summary>
    public String KeyPrefix { get; set; } = EnumKeyConvention.DefaultEnumKeyPrefix;

    /// <summary>
    /// Gets or sets the resource manager that scopes the lookup to a single <c>.resx</c> file. When
    /// unset, the generated key is searched across all registered resource managers.
    /// </summary>
    public ResourceManager? ResourceManager { get; set; }

    /// <summary>
    /// Provides the value supplied to the target property: a binding that yields the localized text
    /// for the control's current <c>DataContext</c> enumeration value, updated live on every culture
    /// change.
    /// </summary>
    /// <param name="serviceProvider">The service provider supplied by the XAML loader.</param>
    /// <returns>
    /// An Avalonia binding that yields the localized enumeration text, or
    /// <see cref="String.Empty" /> when the target is not an <see cref="AvaloniaObject" />.
    /// </returns>
    public override Object ProvideValue(IServiceProvider serviceProvider)
    {
        // The target is the per-item control being built (e.g. the TextBlock in the template).
        var target = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
        if (target?.TargetObject is not AvaloniaObject control)
        {
            return String.Empty;
        }

        // Live stream of this control's DataContext (the enum value).
        var dataContext = control.GetObservable(StyledElement.DataContextProperty);

        return new LocalizedEnumObservable(dataContext, this.KeyPrefix, this.ResourceManager).ToBinding();
    }
}
