namespace RentADeveloper.ResXLocalization;

/// <summary>
/// The default <see cref="ILocalizer" /> implementation: an ambient, process-wide localizer that
/// resolves strings from registered <see cref="ResourceManager" /> instances and notifies bindings
/// when the culture changes. Access the shared instance through <see cref="Current" />.
/// </summary>
/// <remarks>
/// Like the UI frameworks it serves, this class is designed for UI-thread use and is not
/// synchronized: register resource managers once at startup and set <see cref="CurrentCulture" />,
/// <see cref="MessageFormatter" />, <see cref="LookupFailureTextBuilder" /> and <see cref="EnumKeyBuilder" />
/// from the UI thread, so change notifications reach bindings on the thread WPF and Avalonia expect.
/// </remarks>
public sealed class Localizer : ILocalizer
{
    /// <summary>
    /// The default of <see cref="EnumKeyBuilder" />: the convention
    /// <c>{keyPrefix ?? "Enum_"}{EnumTypeName}_{Value}</c>. One shared instance, so every new localizer
    /// starts with the same delegate.
    /// </summary>
    private static readonly Func<Enum, string?, string> DefaultEnumKeyBuilder = static (value, keyPrefix) =>
        (keyPrefix ?? "Enum_") + value.GetType().Name + "_" + value;

    /// <summary>
    /// The default of <see cref="LookupFailureTextBuilder" />: produces <c>!key!</c>. One shared instance,
    /// so every new localizer starts with the same delegate.
    /// </summary>
    private static readonly Func<LookupFailure, string> DefaultLookupFailureTextBuilder = static failure =>
        $"!{failure.Key}!";

    /// <summary>Backing field of <see cref="CurrentCulture" />.</summary>
    private CultureInfo currentCulture = CultureInfo.CurrentUICulture;

    /// <summary>
    /// The search set for key-only and enum lookups, in registration order. Mutations swap the whole
    /// immutable array (copy-on-write; see <see cref="RegisterResourceManager" />).
    /// </summary>
    private ImmutableArray<ResourceManager> resourceManagers = [];

    /// <summary>
    /// Initializes a new, isolated localizer with no registered resource managers. The XAML markup
    /// extensions always resolve through the ambient <see cref="Current" /> instance - create your own
    /// instance only where isolation matters, such as unit tests or code-behind consumers that inject
    /// <see cref="ILocalizer" />.
    /// </summary>
    public Localizer() { }

    /// <inheritdoc />
    public event EventHandler<CultureChangedEventArgs>? CultureChanged;

    /// <inheritdoc />
    public event EventHandler<LookupFailedEventArgs>? LookupFailed;

    /// <summary>
    /// Occurs when a property value changes, including <see cref="CurrentCulture" />,
    /// <see cref="RefreshTriggeringRevision" /> and the indexer, so that localized bindings re-resolve
    /// after a change that can change the result of a lookup.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Gets or sets the shared, ambient localizer instance used throughout the application. Register resource
    /// managers on it at startup and set <see cref="CurrentCulture" /> to switch languages. Applications
    /// may replace it at startup with their DI-owned implementation; markup extensions always resolve
    /// through the current value.
    /// </summary>
    /// <exception cref="ArgumentNullException">The assigned value is <see langword="null" />.</exception>
    public static ILocalizer Current
    {
        get;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            field = value;
        }
    } = new Localizer();

    /// <inheritdoc />
    public CultureInfo CurrentCulture
    {
        get => this.currentCulture;
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            if (Equals(this.currentCulture, value))
            {
                return;
            }

            var oldCulture = this.currentCulture;
            this.currentCulture = value;

            this.PropertyChanged?.Invoke(this, new(nameof(this.CurrentCulture)));
            this.AdvanceRefreshTriggeringRevision();
            this.CultureChanged?.Invoke(this, new(oldCulture, value));
        }
    }

    /// <inheritdoc />
    public Func<Enum, string?, string> EnumKeyBuilder
    {
        get;
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            if (ReferenceEquals(field, value))
            {
                return;
            }

            field = value;
            this.AdvanceRefreshTriggeringRevision();
        }
    } = DefaultEnumKeyBuilder;

    /// <inheritdoc />
    public Func<LookupFailure, string> LookupFailureTextBuilder
    {
        get;
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            if (ReferenceEquals(field, value))
            {
                return;
            }

            field = value;
            this.AdvanceRefreshTriggeringRevision();
        }
    } = DefaultLookupFailureTextBuilder;

    /// <inheritdoc />
    public IMessageFormatter MessageFormatter
    {
        get;
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            if (ReferenceEquals(field, value))
            {
                return;
            }

            field = value;
            this.AdvanceRefreshTriggeringRevision();
        }
    } = StringFormatMessageFormatter.Instance;

    /// <inheritdoc />
    public int RefreshTriggeringRevision { get; private set; }

    /// <inheritdoc />
    public string this[string key] => this.Get(key);

    /// <inheritdoc />
    public void ClearResourceManagers()
    {
        if (this.resourceManagers.IsEmpty)
        {
            return;
        }

        this.resourceManagers = [];
        this.AdvanceRefreshTriggeringRevision();
    }

    /// <inheritdoc />
    public string Get(string key, MessageArguments? arguments = null, ResourceManager? resourceManager = null)
    {
        arguments ??= MessageArguments.Empty;

        // One lookup uses one culture throughout: for the lookup, the format and the failure.
        var culture = this.currentCulture;

        if (string.IsNullOrEmpty(key))
        {
            return string.Empty;
        }

        var value = resourceManager is null
            ? this.FindInRegisteredResourceManagers(key, culture)
            : resourceManager.GetString(key, culture);

        if (value is null)
        {
            return this.FailLookup(
                new(LookupFailureReason.TranslationNotFound, key, culture, resourceManager, arguments, null, null)
            );
        }

        try
        {
            // Also without arguments: the formatter is the one hook for every resolved value.
            return this.MessageFormatter.Format(value, arguments, culture);
        }
        catch (Exception exception)
        {
            // Every exception type, deliberately: one bad translation in one language must not take
            // down a screen, and formatters from other packages throw their own exception types.
            return this.FailLookup(
                new(LookupFailureReason.MessageFormatFailed, key, culture, resourceManager, arguments, value, exception)
            );
        }
    }

    /// <inheritdoc />
    public string Get(ResourceKey key, MessageArguments? arguments = null)
    {
        // An uninitialized ResourceKey, such as default(ResourceKey), has no Manager. Without this check the
        // lookup below would receive a null manager and search all registered managers instead of throwing.
        ArgumentNullException.ThrowIfNull(key.Manager);

        return this.Get(key.Name, arguments, key.Manager);
    }

    /// <inheritdoc />
    public string Get(
        Enum value,
        MessageArguments? arguments = null,
        ResourceManager? resourceManager = null,
        string? keyPrefix = null
    )
    {
        ArgumentNullException.ThrowIfNull(value);

        return this.Get(this.BuildEnumKey(value, keyPrefix), arguments, resourceManager);
    }

    /// <inheritdoc />
    public IReadOnlyList<CultureInfo> GetAvailableCultures()
    {
        var cultures = new HashSet<CultureInfo>();

        foreach (var resourceManager in this.resourceManagers)
        {
            CollectAvailableCultures(resourceManager, cultures);
        }

        return SortCultures(cultures);
    }

    /// <inheritdoc />
    public IReadOnlyList<CultureInfo> GetAvailableCultures(ResourceManager resourceManager)
    {
        ArgumentNullException.ThrowIfNull(resourceManager);

        var cultures = new HashSet<CultureInfo>();
        CollectAvailableCultures(resourceManager, cultures);

        return SortCultures(cultures);
    }

    /// <inheritdoc />
    public void RegisterResourceManager(ResourceManager resourceManager)
    {
        ArgumentNullException.ThrowIfNull(resourceManager);

        if (this.resourceManagers.Contains(resourceManager))
        {
            return;
        }

        // Copy-on-write: register/unregister/clear swap the whole immutable array atomically, so a
        // lookup that reads the field once (foreach captures the struct value) never enumerates a
        // collection being mutated - even from a stray background thread - while keeping the
        // documented UI-thread contract for culture changes.
        this.resourceManagers = this.resourceManagers.Add(resourceManager);
        this.AdvanceRefreshTriggeringRevision();
    }

    /// <inheritdoc />
    public bool UnregisterResourceManager(ResourceManager resourceManager)
    {
        ArgumentNullException.ThrowIfNull(resourceManager);

        var updated = this.resourceManagers.Remove(resourceManager);

        if (updated == this.resourceManagers)
        {
            return false;
        }

        this.resourceManagers = updated;
        this.AdvanceRefreshTriggeringRevision();

        return true;
    }

    /// <summary>
    /// Probes every known culture for a resource set of <paramref name="resourceManager" /> and adds
    /// the hits to <paramref name="cultures" />. Neutral resources surface as the invariant culture.
    /// </summary>
    /// <param name="resourceManager">The resource manager to probe.</param>
    /// <param name="cultures">The set collecting the discovered cultures.</param>
    private static void CollectAvailableCultures(ResourceManager resourceManager, HashSet<CultureInfo> cultures)
    {
        foreach (var culture in CultureInfo.GetCultures(CultureTypes.AllCultures))
        {
            // tryParents: false - only a culture that ships its OWN resources counts; otherwise every
            // known culture would report a hit through fallback.
            var resourceSet = resourceManager.GetResourceSet(culture, createIfNotExists: true, tryParents: false);

            if (resourceSet is null)
            {
                continue;
            }

            // A ResourceManager caches fallback results: after a lookup in, say, "fr" that fell back
            // to the neutral resources, GetResourceSet("fr") returns that cached (parent) set even
            // with tryParents: false. A culture therefore only counts when its set is not simply its
            // parent's set surfacing through the cache.
            if (
                !culture.Equals(CultureInfo.InvariantCulture)
                && ReferenceEquals(
                    resourceSet,
                    resourceManager.GetResourceSet(culture.Parent, createIfNotExists: true, tryParents: false)
                )
            )
            {
                continue;
            }

            cultures.Add(culture);
        }
    }

    /// <summary>
    /// Orders discovered cultures by name; the invariant culture (empty name, representing the
    /// neutral resources) naturally sorts first.
    /// </summary>
    /// <param name="cultures">The cultures to sort.</param>
    /// <returns>The sorted list.</returns>
    private static IReadOnlyList<CultureInfo> SortCultures(HashSet<CultureInfo> cultures) =>
        [.. cultures.OrderBy(static culture => culture.Name, StringComparer.OrdinalIgnoreCase)];

    /// <summary>
    /// Advances <see cref="RefreshTriggeringRevision" /> (wrapping around harmlessly) and raises
    /// <see cref="PropertyChanged" /> for it and then for the indexer, so bound text re-resolves.
    /// </summary>
    private void AdvanceRefreshTriggeringRevision()
    {
        this.RefreshTriggeringRevision = unchecked(this.RefreshTriggeringRevision + 1);

        this.PropertyChanged?.Invoke(this, new(nameof(this.RefreshTriggeringRevision)));
        this.PropertyChanged?.Invoke(this, new("Item[]"));
    }

    /// <summary>
    /// Reports a failed lookup through <see cref="LookupFailed" />, then produces its failure text by
    /// calling <see cref="LookupFailureTextBuilder" />. Exceptions from event handlers and from the
    /// function propagate unchanged.
    /// </summary>
    /// <param name="failure">The failed lookup.</param>
    /// <returns>The failure text.</returns>
    /// <exception cref="InvalidOperationException">The function returned <see langword="null" />.</exception>
    private string FailLookup(LookupFailure failure)
    {
        this.LookupFailed?.Invoke(this, new(failure));

        return this.LookupFailureTextBuilder(failure)
            ?? throw new InvalidOperationException(
                $"{nameof(this.LookupFailureTextBuilder)} returned null for the lookup failure "
                    + $"{failure.Reason} of the key '{failure.Key}'. Return a string; an empty string shows nothing."
            );
    }

    /// <summary>
    /// Searches every registered resource manager in registration order and returns the first match,
    /// or <see langword="null" /> when no registered manager contains the key.
    /// </summary>
    /// <param name="key">The resource key to resolve.</param>
    /// <param name="culture">The culture of the lookup.</param>
    /// <returns>The first matching value, or <see langword="null" />.</returns>
    private string? FindInRegisteredResourceManagers(string key, CultureInfo culture)
    {
        foreach (var resourceManager in this.resourceManagers)
        {
            var value = resourceManager.GetString(key, culture);
            if (value is not null)
            {
                return value;
            }
        }

        return null;
    }
}
