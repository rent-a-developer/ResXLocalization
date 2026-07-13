namespace RentADeveloper.ResXLocalization.WPF;

/// <summary>
/// Multi-value converter that localizes an enumeration value bound as a real value (rather than as a
/// <c>DataContext</c>). Use it in a <c>MultiBinding</c> whose first binding is the enumeration value
/// and whose second binding is <see cref="Localizer.CurrentCulture" />, so the result re-converts on
/// every culture change. The value is mapped to a resource key using the convention
/// <c>{KeyPrefix}{EnumTypeName}_{Value}</c>.
/// </summary>
public sealed class LocalizeEnumConverter : IMultiValueConverter
{
    /// <summary>Initializes a new instance of the <see cref="LocalizeEnumConverter" /> class.</summary>
    public LocalizeEnumConverter()
    {
    }

    /// <summary>
    /// Gets or sets the prefix prepended to the generated resource key. Defaults to <c>Enum_</c>.
    /// </summary>
    /// <exception cref="ArgumentNullException">The supplied value is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">
    /// The converter is the shared <see cref="Default" /> instance, which is read-only.
    /// </exception>
    public String KeyPrefix
    {
        get;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            this.ThrowIfShared();
            field = value;
        }
    } = EnumKeyConvention.DefaultEnumKeyPrefix;

    /// <summary>
    /// Gets or sets the resource manager that scopes the lookup to a single <c>.resx</c> file. When
    /// unset, the generated key is searched across all registered resource managers.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The converter is the shared <see cref="Default" /> instance, which is read-only.
    /// </exception>
    public ResourceManager? ResourceManager
    {
        get;
        set
        {
            this.ThrowIfShared();
            field = value;
        }
    }

    /// <summary>
    /// Converts the bound enumeration value to its localized string.
    /// </summary>
    /// <param name="values">
    /// The bound values. The first element is expected to be the <see cref="Enum" /> value to
    /// localize; any further bindings (such as the current culture) exist only to trigger
    /// re-conversion.
    /// </param>
    /// <param name="targetType">The type of the binding target property.</param>
    /// <param name="parameter">An optional converter parameter; not used.</param>
    /// <param name="culture">
    /// The culture supplied by the binding; not used (the active culture is
    /// taken from <see cref="Localizer.Current" />).
    /// </param>
    /// <returns>
    /// The localized string for the enumeration value, or <see cref="String.Empty" /> when no value
    /// is supplied or the first value is not an <see cref="Enum" />.
    /// </returns>
    public Object Convert(Object?[] values, Type targetType, Object? parameter, CultureInfo culture)
    {
        if (values.Length == 0 || values[0] is not Enum enumValue)
        {
            return String.Empty;
        }

        var key = EnumKeyConvention.BuildEnumKey(enumValue, this.KeyPrefix);

        return this.ResourceManager is null
            ? Localizer.Current.Get(key)
            : Localizer.Current.Get(key, this.ResourceManager);
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

    /// <summary>
    /// Gets the shared, search-all converter instance with default settings. Reference it from XAML
    /// as <c>{x:Static l:LocalizeEnumConverter.Default}</c> when no file scoping is required. The
    /// shared instance is read-only - create your own converter to customize
    /// <see cref="KeyPrefix" /> or <see cref="ResourceManager" />.
    /// </summary>
    public static LocalizeEnumConverter Default { get; } = new(isSharedInstance: true);

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalizeEnumConverter" /> class, optionally
    /// marked as the read-only shared instance. Used only to create <see cref="Default" />.
    /// </summary>
    /// <param name="isSharedInstance">
    /// <see langword="true" /> to make the instance read-only, rejecting property assignments.
    /// </param>
    private LocalizeEnumConverter(Boolean isSharedInstance) => this.isSharedInstance = isSharedInstance;

    /// <summary>Guards property setters against mutating the shared <see cref="Default" /> instance.</summary>
    /// <exception cref="InvalidOperationException">
    /// This converter is the shared <see cref="Default" /> instance, which is read-only.
    /// </exception>
    private void ThrowIfShared()
    {
        if (this.isSharedInstance)
        {
            throw new InvalidOperationException(
                "The shared LocalizeEnumConverter.Default instance is read-only; " +
                "create your own LocalizeEnumConverter to customize KeyPrefix or ResourceManager."
            );
        }
    }

    /// <summary>Indicates whether this is the read-only shared <see cref="Default" /> instance.</summary>
    private readonly Boolean isSharedInstance;
}
