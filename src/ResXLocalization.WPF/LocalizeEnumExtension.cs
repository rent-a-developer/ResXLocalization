namespace RentADeveloper.ResXLocalization.WPF;

/// <summary>
/// XAML markup extension that localizes an enumeration value, re-resolving automatically on every change of
/// the value and of <see cref="ILocalizer.RefreshTriggeringRevision" /> of <see cref="Localizer.Current" />.
/// The value is mapped to a resource key by <see cref="ILocalizer.EnumKeyBuilder" />, by default
/// <c>{KeyPrefix}{EnumTypeName}_{Value}</c>. Usage in XAML: <c>{l:LocalizeEnum Value={Binding SortOrder}}</c>
/// (or the shorthand <c>{l:LocalizeEnum {Binding SortOrder}}</c>) for a bound value,
/// <c>{l:LocalizeEnum Value={Binding}}</c> in an item template whose item is the enumeration value, or
/// <c>{l:LocalizeEnum Value={x:Static m:FileSortOrder.Ascending}}</c> for a fixed member. When the resource
/// value is a composite format string, supply its arguments as <see cref="Argument" /> child elements:
/// <c>&lt;l:LocalizeEnum Value="{Binding SortOrder}" KeyPrefix="Count_"&gt;&lt;l:Argument Value="{Binding FileCount}" /&gt;&lt;/l:LocalizeEnum&gt;</c>.
/// </summary>
[ContentProperty(nameof(Arguments))]
[MarkupExtensionReturnType(typeof(string))]
[XamlSetMarkupExtension(nameof(ReceiveMarkupExtension))]
public sealed class LocalizeEnumExtension : MarkupExtension
{
    /// <summary>Initializes a new instance of the <see cref="LocalizeEnumExtension" /> class.</summary>
    public LocalizeEnumExtension() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalizeEnumExtension" /> class and sets
    /// <see cref="Value" />: <c>{l:LocalizeEnum {Binding SortOrder}}</c> is
    /// <c>{l:LocalizeEnum Value={Binding SortOrder}}</c>.
    /// </summary>
    /// <param name="value">The enumeration value: a binding or a constant <see cref="Enum" /> member.</param>
    public LocalizeEnumExtension(object value) => this.Value = value;

    /// <summary>
    /// Gets the format arguments, in declaration order: the <see cref="Argument" /> child elements. The first
    /// argument is <c>{0}</c>, the second <c>{1}</c>, and so on, whether it has a name or not. The enumeration
    /// value itself is not an argument; bind it as an <see cref="Argument" /> when the text needs it.
    /// </summary>
    public Collection<Argument> Arguments { get; } = [];

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
    /// Gets or sets the enumeration value to localize: a plain binding, such as <c>{Binding SortOrder}</c> or
    /// <c>{Binding}</c> in an item template, or a constant member written as
    /// <c>{x:Static m:FileSortOrder.Ascending}</c>. A bound value that is not an <see cref="Enum" />, such as
    /// <see langword="null" />, renders an empty string.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Provides the value supplied to the target property: a binding whose value is the localized string for
    /// <see cref="Value" />, formatted with <see cref="Arguments" />, updated live on every change of the
    /// value, of the arguments and of <see cref="ILocalizer.RefreshTriggeringRevision" />.
    /// </summary>
    /// <param name="serviceProvider">The service provider supplied by the XAML loader.</param>
    /// <returns>
    /// The binding expression of a WPF multi-binding that yields the localized string, or the multi-binding
    /// itself when the target is not a dependency property, such as a <see cref="Setter" /> value.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// <see cref="Value" /> is <see langword="null" />, a constant that is not an <see cref="Enum" />, or a
    /// binding that is not a plain <see cref="Binding" />; or an <see cref="Argument" /> has no
    /// <see cref="Argument.Value" />, an empty <see cref="Argument.Name" />, the same name as another one, or a
    /// binding that is not a plain <see cref="Binding" />.
    /// </exception>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        this.ValidateValue();
        LocalizedMultiBindingBuilder.ValidateArguments(this.Arguments);

        return LocalizedMultiBindingBuilder
            .Build(this.Value!, this.Arguments, this.ResourceManager, isEnumKey: true, enumKeyPrefix: this.KeyPrefix)
            .ProvideValue(serviceProvider);
    }

    /// <summary>
    /// Called by the XAML reader for every markup extension written for a property of a
    /// <see cref="LocalizeEnumExtension" />. Assigns a binding written for <see cref="Value" /> instead of
    /// applying it; every other markup extension, such as <c>{x:Static}</c> or <c>{x:Null}</c>, stays unhandled
    /// and provides its value as usual.
    /// </summary>
    /// <param name="targetObject">The object whose property is being set.</param>
    /// <param name="eventArgs">The property and the markup extension written for it.</param>
    private static void ReceiveMarkupExtension(object targetObject, XamlSetMarkupExtensionEventArgs eventArgs)
    {
        if (
            targetObject is LocalizeEnumExtension extension
            && eventArgs.Member.Name == nameof(Value)
            && eventArgs.MarkupExtension is BindingBase binding
        )
        {
            extension.Value = binding;
            eventArgs.Handled = true;
        }
    }

    /// <summary>Checks that <see cref="Value" /> is a plain binding or an <see cref="Enum" /> member.</summary>
    /// <exception cref="InvalidOperationException">
    /// <see cref="Value" /> is <see langword="null" />, a constant that is not an <see cref="Enum" />, or a
    /// binding that is not a plain <see cref="Binding" />.
    /// </exception>
    private void ValidateValue()
    {
        switch (this.Value)
        {
            case null:
                throw new InvalidOperationException(
                    "{l:LocalizeEnum} has no Value. Set it to a binding, as Value={Binding ...}, "
                        + "or to a fixed member, as Value={x:Static ...}."
                );

            case Binding:
            case Enum:
                return;

            // WPF's MultiBinding accepts only plain Binding children.
            case BindingBase:
                throw new InvalidOperationException(
                    $"Value of {{l:LocalizeEnum}} is a {this.Value.GetType().FullName}. "
                        + "WPF supports only a plain {Binding} here."
                );

            default:
                throw new InvalidOperationException(
                    $"Value of {{l:LocalizeEnum}} must be a binding or an Enum, but is a {this.Value.GetType().FullName}. "
                        + "A fixed member is written Value={x:Static ...}."
                );
        }
    }
}
