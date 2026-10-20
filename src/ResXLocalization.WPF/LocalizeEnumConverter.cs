namespace RentADeveloper.ResXLocalization.WPF;

/// <summary>
/// Multi-value converter that localizes an enumeration value in a hand-written <c>MultiBinding</c>, for places
/// where a markup extension such as <see cref="LocalizeEnumExtension" /> is not accepted, for example a
/// <c>Binding</c>-typed property. The children of the <c>MultiBinding</c> are: 0, the refresh trigger
/// <c>&lt;Binding Path="RefreshTriggeringRevision" Source="{x:Static core:Localizer.Current}" /&gt;</c>, which
/// re-runs the conversion on every change of <see cref="ILocalizer.RefreshTriggeringRevision" />; 1, the
/// enumeration value; 2 and up, the format arguments, in order. The value is mapped to a resource key by
/// <see cref="ILocalizer.EnumKeyBuilder" />, by default <c>{KeyPrefix}{EnumTypeName}_{Value}</c>.
/// </summary>
/// <remarks>
/// A <c>MultiBinding</c> whose first child is the enumeration value, followed by <c>CurrentCulture</c>, renders
/// an empty string; the enumeration value must be child 1, after the refresh trigger.
/// </remarks>
public sealed class LocalizeEnumConverter : IMultiValueConverter
{
    /// <summary>Indicates whether this is the read-only shared <see cref="Default" /> instance.</summary>
    private readonly bool isSharedInstance;

    /// <summary>The argument names parsed from <see cref="ArgumentNames" />, one per argument, in order.</summary>
    private string?[] argumentNames = [];

    /// <summary>Initializes a new instance of the <see cref="LocalizeEnumConverter" /> class.</summary>
    public LocalizeEnumConverter() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalizeEnumConverter" /> class, optionally
    /// marked as the read-only shared instance. Used only to create <see cref="Default" />.
    /// </summary>
    /// <param name="isSharedInstance">
    /// <see langword="true" /> to make the instance read-only, rejecting property assignments.
    /// </param>
    private LocalizeEnumConverter(bool isSharedInstance) => this.isSharedInstance = isSharedInstance;

    /// <summary>
    /// Gets the shared, search-all converter instance with default settings. Reference it from XAML
    /// as <c>{x:Static l:LocalizeEnumConverter.Default}</c> when no file scoping is required. The
    /// shared instance is read-only - create your own converter to customize
    /// <see cref="KeyPrefix" />, <see cref="ResourceManager" /> or <see cref="ArgumentNames" />.
    /// </summary>
    public static LocalizeEnumConverter Default { get; } = new(isSharedInstance: true);

    /// <summary>
    /// Gets or sets the names of the format arguments, as one comma-separated string: entry <c>i</c> names the
    /// argument at child <c>2 + i</c>. For example, <c>ArgumentNames="Count,Total"</c> names child 2
    /// <c>Count</c> and child 3 <c>Total</c>. Entries are trimmed; an empty entry leaves that argument unnamed
    /// (<c>",Total"</c> names only the second). <see langword="null" /> (the default) or an empty string means
    /// no names. Every argument keeps its position whether it is named or not, and names matter only to a
    /// formatter that understands them, such as ICU MessageFormat; the default formatter ignores them. Extra
    /// names are ignored, and arguments without an entry are unnamed.
    /// </summary>
    /// <exception cref="ArgumentException">Two entries have the same name, compared ordinally.</exception>
    /// <exception cref="InvalidOperationException">
    /// The converter is the shared <see cref="Default" /> instance, which is read-only.
    /// </exception>
    public string? ArgumentNames
    {
        get;
        set
        {
            this.ThrowIfShared();

            string?[] names = string.IsNullOrEmpty(value)
                ? []
                :
                [
                    .. value
                        .Split(',')
                        .Select(static name => name.Trim())
                        .Select(static name => name.Length == 0 ? null : name),
                ];

            var duplicate = names
                .OfType<string>()
                .GroupBy(static name => name, StringComparer.Ordinal)
                .FirstOrDefault(static group => group.Skip(1).Any())
                ?.Key;
            if (duplicate is not null)
            {
                throw new ArgumentException(
                    $"ArgumentNames contains the name '{duplicate}' twice. Argument names must be unique.",
                    nameof(value)
                );
            }

            this.argumentNames = names;
            field = value;
        }
    }

    /// <summary>
    /// Gets or sets the key prefix passed to <see cref="ILocalizer.EnumKeyBuilder" />.
    /// <see langword="null" /> (the default) means "the <see cref="ILocalizer.EnumKeyBuilder" /> decides",
    /// which by default is <c>Enum_</c>; a value overrides the prefix for this converter.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The converter is the shared <see cref="Default" /> instance, which is read-only.
    /// </exception>
    public string? KeyPrefix
    {
        get;
        set
        {
            this.ThrowIfShared();
            field = value;
        }
    }

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

    /// <summary>Converts the enumeration value to its localized, formatted string.</summary>
    /// <param name="values">
    /// The bound values: 0, the refresh trigger, whose value is ignored; 1, the <see cref="Enum" /> value to
    /// localize; 2 and up, the format arguments, named by <see cref="ArgumentNames" />.
    /// </param>
    /// <param name="targetType">The type of the binding target property.</param>
    /// <param name="parameter">An optional converter parameter; not used.</param>
    /// <param name="culture">
    /// The culture supplied by the binding; not used (the active culture is
    /// taken from <see cref="Localizer.Current" />).
    /// </param>
    /// <returns>
    /// The localized string for the enumeration value, or <see cref="string.Empty" /> when there are fewer than
    /// two values or value 1 is not an <see cref="Enum" />.
    /// </returns>
    public object Convert(object?[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[1] is not Enum enumValue)
        {
            return string.Empty;
        }

        // Only two values means there are no arguments; every further value becomes (Name, Value), positioned from child 2 on.
        var arguments =
            values.Length == 2
                ? null
                : MessageArguments.FromTuples([
                    .. values
                        .Skip(2)
                        .Select(
                            (value, index) =>
                                (
                                    index < this.argumentNames.Length ? this.argumentNames[index] : null,
                                    value == DependencyProperty.UnsetValue ? null : value
                                )
                        ),
                ]);

        return Localizer.Current.Get(
            Localizer.Current.BuildEnumKey(enumValue, this.KeyPrefix),
            arguments,
            this.ResourceManager
        );
    }

    /// <summary>Not supported; this converter is one-way.</summary>
    /// <param name="value">The value produced by the binding target.</param>
    /// <param name="targetTypes">The types to convert back to.</param>
    /// <param name="parameter">An optional converter parameter; not used.</param>
    /// <param name="culture">The culture supplied by the binding; not used.</param>
    /// <returns>Never returns; always throws.</returns>
    /// <exception cref="NotSupportedException">Always thrown; the converter is one-way.</exception>
    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    /// <summary>Guards property setters against mutating the shared <see cref="Default" /> instance.</summary>
    /// <exception cref="InvalidOperationException">
    /// This converter is the shared <see cref="Default" /> instance, which is read-only.
    /// </exception>
    private void ThrowIfShared()
    {
        if (this.isSharedInstance)
        {
            throw new InvalidOperationException(
                "The shared LocalizeEnumConverter.Default instance is read-only; "
                    + "create your own LocalizeEnumConverter to customize KeyPrefix, ResourceManager or ArgumentNames."
            );
        }
    }
}
