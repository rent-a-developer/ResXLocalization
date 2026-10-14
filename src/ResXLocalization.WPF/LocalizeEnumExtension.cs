namespace RentADeveloper.ResXLocalization.WPF;

/// <summary>
/// XAML markup extension that localizes the enumeration value held in the target control's
/// <c>DataContext</c>, re-resolving live on every culture change. Intended for item templates where
/// each item <em>is</em> an enumeration value, for example
/// <c>&lt;TextBlock Text="{l:LocalizeEnum}" /&gt;</c>. The value is mapped to a resource key by
/// <see cref="ILocalizer.EnumKeyBuilder" />, by default <c>{KeyPrefix}{EnumTypeName}_{Value}</c>.
/// </summary>
[MarkupExtensionReturnType(typeof(string))]
public sealed class LocalizeEnumExtension : MarkupExtension
{
    /// <summary>
    /// Gets or sets the key prefix passed to <see cref="ILocalizer.EnumKeyBuilder" />.
    /// <see langword="null" /> (the default) means "the <see cref="ILocalizer.EnumKeyBuilder" /> decides",
    /// which by default is <c>Enum_</c>; a value overrides the prefix for this extension.
    /// </summary>
    public string? KeyPrefix { get; set; }

    /// <summary>
    /// Gets or sets the resource manager that scopes the lookup to a single <c>.resx</c> file. When
    /// unset, the generated key is searched across all registered resource managers.
    /// </summary>
    public ResourceManager? ResourceManager { get; set; }

    /// <summary>
    /// Provides the value supplied to the target property: a multi-binding that yields the localized
    /// text for the control's current <c>DataContext</c> enumeration value, updated live on every
    /// culture change.
    /// </summary>
    /// <param name="serviceProvider">The service provider supplied by the XAML loader.</param>
    /// <returns>A WPF multi-binding that yields the localized enumeration text.</returns>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var converter = new LocalizeEnumConverter
        {
            KeyPrefix = this.KeyPrefix,
            ResourceManager = this.ResourceManager,
        };

        // The empty-path binding reads the target control's DataContext (the enum value); the second
        // binding to the ambient culture re-triggers the conversion on every language switch. Because that
        // source is the process-wide singleton, WPF keeps the binding target only weakly.
        var multiBinding = new MultiBinding { Converter = converter, Mode = BindingMode.OneWay };
        multiBinding.Bindings.Add(new Binding());
        multiBinding.Bindings.Add(new Binding(nameof(ILocalizer.CurrentCulture)) { Source = Localizer.Current });

        return multiBinding.ProvideValue(serviceProvider);
    }
}
