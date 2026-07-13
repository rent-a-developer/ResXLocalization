namespace RentADeveloper.ResXLocalization;

/// <summary>
/// The default <see cref="ILocalizer" /> implementation: an ambient, process-wide localizer that
/// resolves strings from registered <see cref="ResourceManager" /> instances and notifies bindings
/// when the culture changes. Access the shared instance through <see cref="Current" />.
/// </summary>
/// <remarks>
/// Like the UI frameworks it serves, this class is designed for UI-thread use and is not
/// synchronized: register resource managers once at startup and set <see cref="CurrentCulture" />
/// from the UI thread, so change notifications reach bindings on the thread WPF and Avalonia expect.
/// </remarks>
public sealed class Localizer : ILocalizer
{
    /// <summary>
    /// Initializes a new, isolated localizer with no registered resource managers. The XAML markup
    /// extensions always resolve through the ambient <see cref="Current" /> instance - create your own
    /// instance only where isolation matters, such as unit tests or code-behind consumers that inject
    /// <see cref="ILocalizer" />.
    /// </summary>
    public Localizer()
    {
    }

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

            this.PropertyChanged?.Invoke(this, CurrentCultureChangedArgs);
            this.PropertyChanged?.Invoke(this, IndexerChangedArgs);
            this.CultureChanged?.Invoke(this, new(oldCulture, value));
        }
    }

    /// <inheritdoc />
    public String this[String key] => this.Get(key);

    /// <inheritdoc />
    public String MissingTranslationFormat
    {
        get;
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            // Validate eagerly with a probe key so a bad format (e.g. "{1}", which references a second
            // argument that never exists) fails right here, at the faulty assignment - not later, deep
            // inside the first cache miss where the FormatException would be far from its cause.
            try
            {
                _ = String.Format(CultureInfo.InvariantCulture, value, "probe");
            }
            catch (FormatException exception)
            {
                throw new ArgumentException(
                    "The missing-translation format must be a composite format string with at most one "
                    + "placeholder ({0}, the key).",
                    nameof(value),
                    exception
                );
            }

            field = value;
        }
    } = "!{0}!";

    /// <inheritdoc />
    public event EventHandler<CultureChangedEventArgs>? CultureChanged;

    /// <inheritdoc />
    public event EventHandler<TranslationNotFoundEventArgs>? TranslationNotFound;

    /// <summary>
    /// Occurs when a property value changes, including <see cref="CurrentCulture" /> and the indexer,
    /// so that localized bindings re-resolve after a culture change.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc />
    public String Get(String key)
    {
        if (String.IsNullOrEmpty(key))
        {
            return String.Empty;
        }

        return this.FindInRegisteredManagers(key) ?? this.Miss(key, null);
    }

    /// <inheritdoc />
    public String Get(String key, params Object?[] arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        if (String.IsNullOrEmpty(key))
        {
            return String.Empty;
        }

        var value = this.FindInRegisteredManagers(key);

        return value is null ? this.Miss(key, null) : this.FormatValue(value, arguments);
    }

    /// <inheritdoc />
    public String Get(Enum value, String keyPrefix = EnumKeyConvention.DefaultEnumKeyPrefix)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(keyPrefix);

        return this.Get(EnumKeyConvention.BuildEnumKey(value, keyPrefix));
    }

    /// <inheritdoc />
    public String Get(String key, ResourceManager resourceManager)
    {
        ArgumentNullException.ThrowIfNull(resourceManager);

        if (String.IsNullOrEmpty(key))
        {
            return String.Empty;
        }

        return resourceManager.GetString(key, this.currentCulture) ?? this.Miss(key, resourceManager);
    }

    /// <inheritdoc />
    public String Get(String key, ResourceManager resourceManager, params Object?[] arguments)
    {
        ArgumentNullException.ThrowIfNull(resourceManager);
        ArgumentNullException.ThrowIfNull(arguments);

        if (String.IsNullOrEmpty(key))
        {
            return String.Empty;
        }

        var value = resourceManager.GetString(key, this.currentCulture);

        return value is null ? this.Miss(key, resourceManager) : this.FormatValue(value, arguments);
    }

    /// <inheritdoc />
    public String Get(
        Enum value,
        ResourceManager resourceManager,
        String keyPrefix = EnumKeyConvention.DefaultEnumKeyPrefix
    )
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(resourceManager);
        ArgumentNullException.ThrowIfNull(keyPrefix);

        return this.Get(EnumKeyConvention.BuildEnumKey(value, keyPrefix), resourceManager);
    }

    /// <inheritdoc />
    public String Get(ResourceKey key) => this.Get(key.Name, key.Manager);

    /// <inheritdoc />
    public String Get(ResourceKey key, params Object?[] arguments) =>
        this.Get(key.Name, key.Manager, arguments);

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
    }

    /// <inheritdoc />
    public Boolean UnregisterResourceManager(ResourceManager resourceManager)
    {
        ArgumentNullException.ThrowIfNull(resourceManager);

        var updated = this.resourceManagers.Remove(resourceManager);

        if (updated == this.resourceManagers)
        {
            return false;
        }

        this.resourceManagers = updated;

        return true;
    }

    /// <inheritdoc />
    public void ClearResourceManagers() => this.resourceManagers = [];

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
            if (!culture.Equals(CultureInfo.InvariantCulture)
                && ReferenceEquals(
                    resourceSet,
                    resourceManager.GetResourceSet(culture.Parent, createIfNotExists: true, tryParents: false)
                ))
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
    /// Searches every registered resource manager in registration order and returns the first match,
    /// or <see langword="null" /> when no registered manager contains the key.
    /// </summary>
    /// <param name="key">The resource key to resolve.</param>
    /// <returns>The first matching value, or <see langword="null" />.</returns>
    private String? FindInRegisteredManagers(String key)
    {
        foreach (var resourceManager in this.resourceManagers)
        {
            var value = resourceManager.GetString(key, this.currentCulture);
            if (value is not null)
            {
                return value;
            }
        }

        return null;
    }

    /// <summary>Formats a resolved value with <see cref="String.Format(IFormatProvider, String, Object[])" /> in the current culture.</summary>
    /// <param name="value">The resolved resource value, used as the composite format string.</param>
    /// <param name="arguments">The format arguments.</param>
    /// <returns>The formatted string.</returns>
    private String FormatValue(String value, Object?[] arguments) =>
        String.Format(this.currentCulture, value, arguments);

    /// <summary>
    /// Handles a lookup no resource file could satisfy: raises <see cref="TranslationNotFound" />
    /// and produces the sentinel from <see cref="MissingTranslationFormat" />.
    /// </summary>
    /// <param name="key">The unresolved key.</param>
    /// <param name="resourceManager">
    /// The single manager of a scoped or typed lookup, or <see langword="null" /> for search-all.
    /// </param>
    /// <returns>The miss sentinel, by default <c>!key!</c>.</returns>
    private String Miss(String key, ResourceManager? resourceManager)
    {
        this.TranslationNotFound?.Invoke(this, new(key, this.currentCulture, resourceManager));

        return String.Format(CultureInfo.InvariantCulture, this.MissingTranslationFormat, key);
    }

    /// <summary>
    /// The search set for key-only and enum lookups, in registration order. Mutations swap the whole
    /// immutable array (copy-on-write; see <see cref="RegisterResourceManager" />).
    /// </summary>
    private ImmutableArray<ResourceManager> resourceManagers = [];

    /// <summary>Backing field of <see cref="CurrentCulture" />.</summary>
    private CultureInfo currentCulture = CultureInfo.CurrentUICulture;

    /// <summary>Cached event args announcing a <see cref="CurrentCulture" /> change.</summary>
    private static readonly PropertyChangedEventArgs CurrentCultureChangedArgs = new(nameof(CurrentCulture));

    /// <summary>
    /// Cached event args announcing an indexer change (<c>Item[]</c>), which makes bindings through
    /// the indexer re-resolve after a culture change.
    /// </summary>
    private static readonly PropertyChangedEventArgs IndexerChangedArgs = new("Item[]");
}
