namespace RentADeveloper.ResXLocalization.WPF;

/// <summary>
/// XAML markup extension that produces a binding to a localized string, re-resolving automatically
/// whenever <see cref="Localizer.Current" /> changes culture. Usage in XAML:
/// <c>{l:Localize {x:Static res:StringsKeys.Greeting}}</c> for a typed key,
/// <c>{l:Localize SomeKey}</c> for a key-only lookup across all registered resource managers, or
/// <c>{l:Localize Key=SomeKey, ResourceManager={x:Static res:Strings.ResourceManager}}</c> to scope
/// the lookup to one file. When the resource value is a composite format string, supply its
/// arguments by binding the <see cref="LocalizeArgs" /> attached properties on the target element.
/// </summary>
[MarkupExtensionReturnType(typeof(String))]
public sealed class LocalizeExtension : MarkupExtension
{
    /// <summary>Initializes a new instance of the <see cref="LocalizeExtension" /> class.</summary>
    public LocalizeExtension()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalizeExtension" /> class from a single positional
    /// argument. A typed <see cref="RentADeveloper.ResXLocalization.ResourceKey" /> (for example from
    /// <c>{x:Static res:StringsKeys.Greeting}</c>) becomes <see cref="ResourceKey" />; a
    /// <see cref="String" /> becomes <see cref="Key" />; any other value leaves <see cref="Key" /> empty.
    /// A single object-typed constructor is used instead of overloads so WPF's positional-argument
    /// resolution is never ambiguous.
    /// </summary>
    /// <param name="key">A <see cref="RentADeveloper.ResXLocalization.ResourceKey" /> or a key string.</param>
    public LocalizeExtension(Object key)
    {
        if (key is ResourceKey resourceKey)
        {
            this.ResourceKey = resourceKey;
        }
        else
        {
            this.Key = key as String ?? String.Empty;
        }
    }

    /// <summary>
    /// Gets or sets the resource key to resolve. Used when <see cref="ResourceKey" /> is not set. If
    /// <see cref="ResourceManager" /> is also set, the lookup is scoped to that file; otherwise every
    /// registered resource manager is searched.
    /// </summary>
    public String Key { get; set; } = String.Empty;

    /// <summary>
    /// Gets or sets a typed, file-scoped key. When set, it takes precedence over <see cref="Key" />
    /// and <see cref="ResourceManager" /> and resolves through its own resource manager.
    /// </summary>
    public ResourceKey? ResourceKey { get; set; }

    /// <summary>
    /// Gets or sets the resource manager that scopes a <see cref="Key" /> lookup to a single
    /// <c>.resx</c> file. Ignored when <see cref="ResourceKey" /> is set; when both this and
    /// <see cref="ResourceKey" /> are unset, the key is searched across all registered resource managers.
    /// </summary>
    public ResourceManager? ResourceManager { get; set; }

    /// <summary>
    /// Provides the value supplied to the target property: a multi-binding whose value is the
    /// localized string for the configured key, updated live on every culture change and on every
    /// change of the target element's <see cref="LocalizeArgs" /> arguments.
    /// </summary>
    /// <param name="serviceProvider">The service provider supplied by the XAML loader.</param>
    /// <returns>A WPF multi-binding that yields the localized string.</returns>
    public override Object ProvideValue(IServiceProvider serviceProvider)
    {
        Func<String> resolve;
        Func<Object?[], String> resolveFormatted;

        if (this.ResourceKey.HasValue)
        {
            var resourceKey = this.ResourceKey.Value;
            resolve = () => Localizer.Current.Get(resourceKey);
            resolveFormatted = arguments => Localizer.Current.Get(resourceKey, arguments);
        }
        else if (this.ResourceManager is not null)
        {
            var key = this.Key;
            var resourceManager = this.ResourceManager;
            resolve = () => Localizer.Current.Get(key, resourceManager);
            resolveFormatted = arguments => Localizer.Current.Get(key, resourceManager, arguments);
        }
        else
        {
            var key = this.Key;
            resolve = () => Localizer.Current.Get(key);
            resolveFormatted = arguments => Localizer.Current.Get(key, arguments);
        }

        // Three constant children, regardless of how many arguments are used:
        // 1. The ambient localizer's CurrentCulture re-triggers the conversion on every language switch.
        //    Because that source is the process-wide singleton (an INotifyPropertyChanged), WPF keeps the
        //    binding target only weakly.
        // 2. The element's LocalizeArgs args-version re-triggers the conversion whenever any argument
        //    changes - crucial because ProvideValue runs during XAML instantiation, before argument
        //    bindings deliver values, so the arguments must be re-read later, not captured here.
        // 3. The target element itself, from which the converter reads the current arguments.
        // RelativeSource.Self resolves per applied instance, so the extension also works inside
        // deferred item and data templates.
        var multiBinding = new MultiBinding
        {
            Converter = new LocalizedFormattedStringConverter(resolve, resolveFormatted),
            Mode = BindingMode.OneWay
        };
        multiBinding.Bindings.Add(new Binding(nameof(ILocalizer.CurrentCulture)) { Source = Localizer.Current });
        multiBinding.Bindings.Add(new Binding
        {
            RelativeSource = RelativeSource.Self,
            Path = new PropertyPath(LocalizeArgs.ArgsVersionProperty)
        }
        );
        multiBinding.Bindings.Add(new Binding { RelativeSource = RelativeSource.Self });

        return multiBinding.ProvideValue(serviceProvider);
    }
}
