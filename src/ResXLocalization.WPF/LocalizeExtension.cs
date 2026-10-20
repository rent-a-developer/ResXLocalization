namespace RentADeveloper.ResXLocalization.WPF;

/// <summary>
/// XAML markup extension that produces a binding to a localized string, re-resolving automatically on every
/// change of <see cref="ILocalizer.RefreshTriggeringRevision" /> of <see cref="Localizer.Current" /> - a
/// language switch, a new formatter, a new lookup-failure text builder or a change of the registered resource
/// managers. Usage in XAML: <c>{l:Localize {x:Static res:StringsKeys.Greeting}}</c> for a typed key,
/// <c>{l:Localize Greeting}</c> for a key-only lookup across all registered resource managers, or
/// <c>{l:Localize Key=Greeting, ResourceManager={x:Static res:StringsKeys.ResourceManager}}</c> to scope the
/// lookup to one file. A key chosen by the view model is bound: <c>{l:Localize {Binding StatusKey}}</c> or
/// <c>Key="{Binding StatusKey}"</c>; bind a <see cref="RentADeveloper.ResXLocalization.ResourceKey" /> to keep
/// the key compile-checked. When the resource value is a composite format string, supply its arguments as
/// <see cref="Argument" /> child elements:
/// <c>&lt;l:Localize Key="{x:Static res:StringsKeys.PeopleInvited}"&gt;&lt;l:Argument Value="{Binding PeopleCount}" /&gt;&lt;/l:Localize&gt;</c>.
/// </summary>
[ContentProperty(nameof(Arguments))]
[MarkupExtensionReturnType(typeof(string))]
[XamlSetMarkupExtension(nameof(ReceiveMarkupExtension))]
public sealed class LocalizeExtension : MarkupExtension
{
    /// <summary>Initializes a new instance of the <see cref="LocalizeExtension" /> class.</summary>
    public LocalizeExtension() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalizeExtension" /> class and sets <see cref="Key" />:
    /// <c>{l:Localize Greeting}</c> is <c>{l:Localize Key=Greeting}</c>, and <c>{l:Localize {Binding StatusKey}}</c>
    /// is <c>{l:Localize Key={Binding StatusKey}}</c>. A single object-typed constructor is used instead of
    /// overloads so WPF's positional-argument resolution is never ambiguous.
    /// </summary>
    /// <param name="key">
    /// The key: a <see cref="string" />, a <see cref="RentADeveloper.ResXLocalization.ResourceKey" />, or a
    /// plain <see cref="Binding" /> that yields one of them.
    /// </param>
    public LocalizeExtension(object key) => this.Key = key;

    /// <summary>
    /// Gets the format arguments, in declaration order: the <see cref="Argument" /> child elements. The first
    /// argument is <c>{0}</c>, the second <c>{1}</c>, and so on, whether it has a name or not.
    /// </summary>
    public Collection<Argument> Arguments { get; } = [];

    /// <summary>
    /// Gets or sets the key to resolve: a <see cref="string" />, scoped to <see cref="ResourceManager" /> if
    /// that is set and otherwise searched across all registered resource managers, or a
    /// <see cref="RentADeveloper.ResXLocalization.ResourceKey" />, which resolves through its own resource
    /// manager; or a plain <see cref="Binding" />, such as <c>Key="{Binding StatusKey}"</c>, whose value is
    /// resolved by the same rules on every update. Prefer binding a
    /// <see cref="RentADeveloper.ResXLocalization.ResourceKey" /> chosen by the view model, which keeps the key
    /// compile-checked; a bound <see cref="string" /> key is an escape hatch where a typo silently becomes the
    /// failure text. A bound value that gives no key, such as <see langword="null" />, an empty string or a
    /// value of another type, renders an empty string.
    /// </summary>
    public object? Key { get; set; }

    /// <summary>
    /// Gets or sets the resource manager that scopes a <see cref="string" /> <see cref="Key" /> to a single
    /// <c>.resx</c> file. Ignored when <see cref="Key" /> is a
    /// <see cref="RentADeveloper.ResXLocalization.ResourceKey" />; when unset, a <see cref="string" /> key is
    /// searched across all registered resource managers.
    /// </summary>
    public ResourceManager? ResourceManager { get; set; }

    /// <summary>
    /// Provides the value supplied to the target property: a multi-binding whose value is the localized
    /// string for <see cref="Key" />, formatted with <see cref="Arguments" />, updated live on every change of
    /// the arguments and of <see cref="ILocalizer.RefreshTriggeringRevision" />. It needs no target element,
    /// so it also works in a style setter, in a resource and in a template.
    /// </summary>
    /// <param name="serviceProvider">The service provider supplied by the XAML loader.</param>
    /// <returns>
    /// The value of the WPF multi-binding for the target: the binding expression for a dependency property,
    /// the multi-binding itself for a <c>Setter</c>. Inside a XAML designer that supplies the target property
    /// as a CLR property instead of a dependency property, such as Rider's WPF previewer, a string instead,
    /// without validation: the lookup of a constant <see cref="Key" /> without arguments, or an empty string
    /// when <see cref="Key" /> is <see langword="null" /> or a binding.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// <see cref="Key" /> is <see langword="null" />, a constant that is neither a <see cref="string" /> nor a
    /// <see cref="RentADeveloper.ResXLocalization.ResourceKey" />, or a binding that is not a plain
    /// <see cref="Binding" />; or an <see cref="Argument" /> has no <see cref="Argument.Value" />, an empty
    /// <see cref="Argument.Name" />, the same name as another one, or a binding that is not a plain
    /// <see cref="Binding" />.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <see cref="Key" /> is a <see cref="RentADeveloper.ResXLocalization.ResourceKey" /> without a resource
    /// manager.
    /// </exception>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        // Before the validation: Rider's previewer often delivers Key as null, and an exception takes the
        // preview down.
        if (DesignMode.ShouldReturnDesignTimeText(serviceProvider))
        {
            return this.GetDesignTimeText();
        }

        this.ValidateKey();
        LocalizedMultiBindingBuilder.ValidateArguments(this.Arguments);

        return LocalizedMultiBindingBuilder
            .Build(this.Key!, this.Arguments, this.ResourceManager)
            .ProvideValue(serviceProvider);
    }

    /// <summary>
    /// Called by the XAML reader for every markup extension written for a property of a
    /// <see cref="LocalizeExtension" />. Assigns a binding written for <see cref="Key" /> instead of applying
    /// it; every other markup extension, such as <c>{x:Static}</c> or <c>{x:Null}</c>, stays unhandled and
    /// provides its value as usual. Also called for the constructor argument, with the member name
    /// <c>_PositionalParameters</c>; that case stays unhandled, because a binding passed to the constructor
    /// already arrives as a <see cref="Binding" />.
    /// </summary>
    /// <param name="targetObject">The object whose property is being set.</param>
    /// <param name="eventArgs">The property and the markup extension written for it.</param>
    private static void ReceiveMarkupExtension(object targetObject, XamlSetMarkupExtensionEventArgs eventArgs)
    {
        if (
            targetObject is LocalizeExtension extension
            && eventArgs.Member.Name == nameof(Key)
            && eventArgs.MarkupExtension is BindingBase binding
        )
        {
            extension.Key = binding;
            eventArgs.Handled = true;
        }
    }

    /// <summary>
    /// Gets the design-time text: the lookup of a constant <see cref="Key" /> without arguments - under the
    /// default formatter the resource value as written, such as <c>{0} people invited</c> - or an empty string
    /// for any other <see cref="Key" />, because there is no key to look up.
    /// </summary>
    /// <returns>The design-time text.</returns>
    private string GetDesignTimeText() =>
        this.Key switch
        {
            ResourceKey { Manager: not null } resourceKey => Localizer.Current.Get(resourceKey),
            string { Length: > 0 } key => Localizer.Current.Get(key, resourceManager: this.ResourceManager),
            _ => string.Empty,
        };

    /// <summary>
    /// Checks that <see cref="Key" /> is a <see cref="string" />, a usable resource key or a plain binding.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// <see cref="Key" /> is <see langword="null" />, a constant that is neither a <see cref="string" /> nor a
    /// <see cref="RentADeveloper.ResXLocalization.ResourceKey" />, or a binding that is not a plain
    /// <see cref="Binding" />.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// <see cref="Key" /> is a <see cref="RentADeveloper.ResXLocalization.ResourceKey" /> without a resource
    /// manager.
    /// </exception>
    private void ValidateKey()
    {
        switch (this.Key)
        {
            case null:
                throw new InvalidOperationException(
                    "{l:Localize} has no Key. Set the key as {l:Localize Greeting} or as Key=\"...\"."
                );

            case string:
                return;

            case ResourceKey { Manager: null }:
                throw new ArgumentNullException(
                    message: "The ResourceKey assigned to Key of {l:Localize} has no ResourceManager.",
                    innerException: null
                );

            case ResourceKey:
            case Binding:
                return;

            // WPF's MultiBinding accepts only plain Binding children.
            case BindingBase:
                throw new InvalidOperationException(
                    $"Key of {{l:Localize}} is a {this.Key.GetType().FullName}. WPF supports only a plain {{Binding}} here."
                );

            default:
                throw new InvalidOperationException(
                    $"Key of {{l:Localize}} must be a string, a ResourceKey or a binding, but is a {this.Key.GetType().FullName}."
                );
        }
    }
}
