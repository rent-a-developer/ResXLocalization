namespace RentADeveloper.ResXLocalization;

/// <summary>
/// Resolves localized strings from one or more <see cref="ResourceManager" /> instances for a
/// runtime-selectable culture, and raises change notifications so bindings update live when the
/// culture changes - without reloading views.
/// </summary>
/// <remarks>
/// Implementations are intended for UI-thread use: register resource managers at startup and switch
/// <see cref="CurrentCulture" /> from the UI thread, so change notifications reach bindings on the
/// thread WPF and Avalonia expect.
/// </remarks>
public interface ILocalizer : INotifyPropertyChanged
{
    /// <summary>
    /// Gets or sets the culture used to resolve resources. Setting a different value raises
    /// <see cref="INotifyPropertyChanged.PropertyChanged" /> and <see cref="CultureChanged" />, causing
    /// bound localized values to re-resolve.
    /// </summary>
    /// <remarks>
    /// Setting this property changes only how this localizer resolves translations; it deliberately
    /// does not touch <see cref="CultureInfo.CurrentUICulture" /> or
    /// <see cref="CultureInfo.DefaultThreadCurrentUICulture" />. ResXLocalization is a translation
    /// engine, not a formatting engine - if number, date, or currency formatting should follow the
    /// selected language too, set those thread cultures yourself when handling
    /// <see cref="CultureChanged" />.
    /// </remarks>
    /// <value>The active culture. Defaults to <see cref="CultureInfo.CurrentUICulture" />.</value>
    /// <exception cref="ArgumentNullException">The supplied value is <see langword="null" />.</exception>
    CultureInfo CurrentCulture { get; set; }

    /// <summary>
    /// Gets the localized string for the specified key by searching every registered
    /// <see cref="ResourceManager" /> in registration order.
    /// </summary>
    /// <param name="key">The resource key to resolve.</param>
    /// <returns>
    /// The localized string for <paramref name="key" /> in <see cref="CurrentCulture" />;
    /// <see cref="String.Empty" /> if <paramref name="key" /> is <see langword="null" /> or empty;
    /// or the configured missing-translation sentinel (by default <c>!key!</c>) if no registered
    /// resource manager contains the key.
    /// </returns>
    String this[String key] { get; }

    /// <summary>
    /// Gets or sets the composite format string that produces the sentinel returned for a key no
    /// lookup could resolve, where <c>{0}</c> is the key. Defaults to <c>!{0}!</c>, which renders a
    /// missing <c>Greeting</c> as <c>!Greeting!</c>.
    /// </summary>
    /// <remarks>
    /// Configure this once at startup, before views load: changing it later does not re-resolve
    /// values that are already bound (only a culture change does that).
    /// </remarks>
    /// <value>The sentinel format. Defaults to <c>!{0}!</c>.</value>
    /// <exception cref="ArgumentNullException">The supplied value is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// The supplied value is not a valid composite format string for a single argument (the key) - for
    /// example <c>{1}</c>. It is validated on assignment so the defect surfaces here rather than on the
    /// first miss.
    /// </exception>
    String MissingTranslationFormat { get; set; }

    /// <summary>
    /// Occurs after <see cref="CurrentCulture" /> changes, carrying the previous and current culture.
    /// Subscribe to refresh values that are not resolved through a binding.
    /// </summary>
    event EventHandler<CultureChangedEventArgs> CultureChanged;

    /// <summary>
    /// Occurs whenever a lookup misses - no resource file could resolve the key for the current
    /// culture and the sentinel (see <see cref="MissingTranslationFormat" />) is about to be
    /// returned. Normal .NET resource fallback runs first; subscribe to log keys that remain
    /// unresolved across the complete parent and neutral fallback chain.
    /// </summary>
    event EventHandler<TranslationNotFoundEventArgs> TranslationNotFound;

    /// <summary>
    /// Resolves the localized string for the specified key by searching every registered
    /// <see cref="ResourceManager" /> in registration order.
    /// </summary>
    /// <param name="key">The resource key to resolve.</param>
    /// <returns>
    /// The localized string for <paramref name="key" /> in <see cref="CurrentCulture" />;
    /// <see cref="String.Empty" /> if <paramref name="key" /> is <see langword="null" /> or empty;
    /// or the configured missing-translation sentinel (by default <c>!key!</c>) if no registered
    /// resource manager contains the key.
    /// </returns>
    String Get(String key);

    /// <summary>
    /// Resolves the localized string for the specified key by searching every registered
    /// <see cref="ResourceManager" /> in registration order, then formats it as a composite format
    /// string with the supplied arguments in <see cref="CurrentCulture" /> - for example a resource
    /// value of <c>Downloading {0} of {1}…</c>.
    /// </summary>
    /// <param name="key">The resource key to resolve.</param>
    /// <param name="arguments">The values to format into the resolved string.</param>
    /// <returns>
    /// The formatted, localized string; <see cref="String.Empty" /> if <paramref name="key" /> is
    /// <see langword="null" /> or empty; or the configured missing-translation sentinel (by default
    /// <c>!key!</c>), without applying <paramref name="arguments" />, if no registered resource manager
    /// contains the key.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="arguments" /> is <see langword="null" />.</exception>
    /// <exception cref="FormatException">
    /// The resolved resource value is not a valid composite format string for
    /// <paramref name="arguments" />. That is a defect in the resource file you want to surface
    /// during development, not a missing translation, so it fails loudly.
    /// </exception>
    String Get(String key, params Object?[] arguments);

    /// <summary>
    /// Resolves the localized string for an enumeration value by mapping it to a resource key using
    /// the convention <c>{keyPrefix}{EnumTypeName}_{Value}</c>, then searching every registered
    /// <see cref="ResourceManager" /> in registration order.
    /// </summary>
    /// <param name="value">The enumeration value to resolve.</param>
    /// <param name="keyPrefix">The prefix prepended to the generated key. Defaults to <c>Enum_</c>.</param>
    /// <returns>
    /// The localized string for the generated key in <see cref="CurrentCulture" />, or the configured
    /// missing-translation sentinel (by default <c>!key!</c>) if no registered resource manager
    /// contains it.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="value" /> or <paramref name="keyPrefix" /> is <see langword="null" />.
    /// </exception>
    String Get(Enum value, String keyPrefix = EnumKeyConvention.DefaultEnumKeyPrefix);

    /// <summary>
    /// Resolves the localized string for the specified key from a single, explicit
    /// <see cref="ResourceManager" />.
    /// </summary>
    /// <param name="key">The resource key to resolve.</param>
    /// <param name="resourceManager">The resource manager to read the key from.</param>
    /// <returns>
    /// The localized string for <paramref name="key" /> in <see cref="CurrentCulture" />;
    /// <see cref="String.Empty" /> if <paramref name="key" /> is <see langword="null" /> or empty;
    /// or the configured missing-translation sentinel (by default <c>!key!</c>) if
    /// <paramref name="resourceManager" /> does not contain the key.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="resourceManager" /> is <see langword="null" />.
    /// </exception>
    String Get(String key, ResourceManager resourceManager);

    /// <summary>
    /// Resolves the localized string for the specified key from a single, explicit
    /// <see cref="ResourceManager" />, then formats it as a composite format string with the supplied
    /// arguments in <see cref="CurrentCulture" />.
    /// </summary>
    /// <param name="key">The resource key to resolve.</param>
    /// <param name="resourceManager">The resource manager to read the key from.</param>
    /// <param name="arguments">The values to format into the resolved string.</param>
    /// <returns>
    /// The formatted, localized string; <see cref="String.Empty" /> if <paramref name="key" /> is
    /// <see langword="null" /> or empty; or the configured missing-translation sentinel (by default
    /// <c>!key!</c>), without applying <paramref name="arguments" />, if
    /// <paramref name="resourceManager" /> does not contain the key.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="resourceManager" /> or <paramref name="arguments" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="FormatException">
    /// The resolved resource value is not a valid composite format string for
    /// <paramref name="arguments" />.
    /// </exception>
    String Get(String key, ResourceManager resourceManager, params Object?[] arguments);

    /// <summary>
    /// Resolves the localized string for an enumeration value from a single, explicit
    /// <see cref="ResourceManager" />, mapping the value to a key using the convention
    /// <c>{keyPrefix}{EnumTypeName}_{Value}</c>.
    /// </summary>
    /// <param name="value">The enumeration value to resolve.</param>
    /// <param name="resourceManager">The resource manager to read the generated key from.</param>
    /// <param name="keyPrefix">The prefix prepended to the generated key. Defaults to <c>Enum_</c>.</param>
    /// <returns>
    /// The localized string for the generated key in <see cref="CurrentCulture" />, or the configured
    /// missing-translation sentinel (by default <c>!key!</c>) if
    /// <paramref name="resourceManager" /> does not contain it.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="value" />, <paramref name="resourceManager" />, or
    /// <paramref name="keyPrefix" /> is <see langword="null" />.
    /// </exception>
    String Get(Enum value, ResourceManager resourceManager, String keyPrefix = EnumKeyConvention.DefaultEnumKeyPrefix);

    /// <summary>
    /// Resolves the localized string for a typed <see cref="ResourceKey" />, which carries both the
    /// key name and the owning <see cref="ResourceManager" />.
    /// </summary>
    /// <param name="key">The typed key identifying the resource and its resource manager.</param>
    /// <returns>
    /// The localized string for <paramref name="key" /> in <see cref="CurrentCulture" />, or the
    /// configured missing-translation sentinel (by default <c>!key!</c>) if the resource manager does
    /// not contain it.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="key" /> is a default or otherwise uninitialized <see cref="ResourceKey" />,
    /// whose <see cref="ResourceKey.Manager" /> is <see langword="null" />.
    /// </exception>
    String Get(ResourceKey key);

    /// <summary>
    /// Resolves the localized string for a typed <see cref="ResourceKey" />, then formats it as a
    /// composite format string with the supplied arguments in <see cref="CurrentCulture" />.
    /// </summary>
    /// <param name="key">The typed key identifying the resource and its resource manager.</param>
    /// <param name="arguments">The values to format into the resolved string.</param>
    /// <returns>
    /// The formatted, localized string, or the configured missing-translation sentinel (by default
    /// <c>!key!</c>), without applying <paramref name="arguments" />, if the resource manager does not
    /// contain the key.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="arguments" /> is <see langword="null" />, or <paramref name="key" /> is a
    /// default or otherwise uninitialized <see cref="ResourceKey" /> whose
    /// <see cref="ResourceKey.Manager" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="FormatException">
    /// The resolved resource value is not a valid composite format string for
    /// <paramref name="arguments" />.
    /// </exception>
    String Get(ResourceKey key, params Object?[] arguments);

    /// <summary>
    /// Registers a <see cref="ResourceManager" /> to be searched by the key-only and enum lookups.
    /// Managers are searched in registration order on a first-match-wins basis; registering the same
    /// manager more than once has no effect.
    /// </summary>
    /// <param name="resourceManager">The resource manager to add to the search set.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="resourceManager" /> is <see langword="null" />.
    /// </exception>
    void RegisterResourceManager(ResourceManager resourceManager);

    /// <summary>
    /// Removes a previously registered <see cref="ResourceManager" /> from the search-all set, for
    /// example when the plugin or module that owns its strings is unloaded. Typed and scoped lookups
    /// are unaffected - they never consult the registration set.
    /// </summary>
    /// <param name="resourceManager">The resource manager to remove from the search set.</param>
    /// <returns>
    /// <see langword="true" /> when the manager was registered and has been removed;
    /// <see langword="false" /> when it was not registered.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="resourceManager" /> is <see langword="null" />.
    /// </exception>
    Boolean UnregisterResourceManager(ResourceManager resourceManager);

    /// <summary>
    /// Removes every registered <see cref="ResourceManager" /> from the search-all set. Typed and
    /// scoped lookups are unaffected - they never consult the registration set.
    /// </summary>
    void ClearResourceManagers();

    /// <summary>
    /// Discovers the cultures for which any registered <see cref="ResourceManager" /> ships its own
    /// resources - the natural source for a language picker.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The neutral (fallback) resources embedded in the main assembly are reported as
    /// <see cref="CultureInfo.InvariantCulture" />, because a <see cref="ResourceManager" /> does not
    /// expose which language they are written in. Map it to the language you declared in
    /// <c>&lt;NeutralLanguage&gt;</c> when building a picker.
    /// </para>
    /// <para>
    /// Discovery probes every known culture for a satellite assembly, which is I/O-bound: call it
    /// once at startup and cache the result rather than per lookup or per binding.
    /// </para>
    /// </remarks>
    /// <returns>
    /// The union of the available cultures across every registered resource manager, sorted by
    /// culture name, with <see cref="CultureInfo.InvariantCulture" /> (the neutral resources) first
    /// when present. Empty when nothing is registered.
    /// </returns>
    IReadOnlyList<CultureInfo> GetAvailableCultures();

    /// <summary>
    /// Discovers the cultures for which a single <see cref="ResourceManager" /> ships its own
    /// resources. See <see cref="GetAvailableCultures()" /> for how neutral resources are reported
    /// and why the result should be cached.
    /// </summary>
    /// <param name="resourceManager">The resource manager to probe.</param>
    /// <returns>
    /// The available cultures, sorted by culture name, with
    /// <see cref="CultureInfo.InvariantCulture" /> (the neutral resources) first when present.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="resourceManager" /> is <see langword="null" />.
    /// </exception>
    IReadOnlyList<CultureInfo> GetAvailableCultures(ResourceManager resourceManager);
}
