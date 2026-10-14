namespace RentADeveloper.ResXLocalization;

/// <summary>
/// Resolves localized strings from one or more <see cref="ResourceManager" /> instances for a
/// runtime-selectable culture, and raises change notifications so bindings update live when the
/// culture changes - without reloading views.
/// </summary>
/// <remarks>
/// <para>
/// <b>Lookups.</b> Every <c>Get</c> overload and the indexer resolve the key in
/// <see cref="CurrentCulture" />, with .NET's own parent and neutral fallback, and pass the resolved value
/// to <see cref="MessageFormatter" /> together with the lookup's arguments - empty arguments when the
/// lookup has none. The default formatter (<see cref="StringFormatMessageFormatter"/>) returns a value without
/// arguments unchanged and formats one with arguments with
/// <see cref="string.Format(IFormatProvider, string, object[])" />. A <see langword="null" /> or empty key returns
/// <see cref="string.Empty" /> without calling the formatter and without raising an event.
/// </para>
/// <para>
/// <b>The failure text.</b> When no resource file contains the key, or the formatter throws, a lookup
/// raises <see cref="LookupFailed" /> and then returns the failure text produced by
/// <see cref="LookupFailureTextBuilder" />, by default <c>!key!</c>. The failure text never passes
/// through the formatter.
/// </para>
/// <para>
/// <b>Exceptions.</b> A lookup catches every exception the formatter throws. Exceptions from the
/// resource lookup itself, from event handlers and from <see cref="LookupFailureTextBuilder" /> propagate
/// unchanged; an event handler that throws prevents the failure text from being built.
/// </para>
/// <para>
/// <b>Threading.</b> Implementations are intended for UI-thread use: register resource managers at
/// startup and switch <see cref="CurrentCulture" /> from the UI thread, so change notifications reach
/// bindings on the thread WPF and Avalonia expect.
/// </para>
/// </remarks>
public interface ILocalizer : INotifyPropertyChanged
{
    /// <summary>
    /// Occurs after <see cref="CurrentCulture" /> changes, carrying the previous and current culture.
    /// Subscribe to refresh values that are not resolved through a binding.
    /// </summary>
    event EventHandler<CultureChangedEventArgs> CultureChanged;

    /// <summary>
    /// Occurs when a lookup fails, before it returns the failure text: no resource file contains the key,
    /// or <see cref="MessageFormatter" /> threw while formatting its value.
    /// <see cref="LookupFailure.Reason" /> tells the two apart. Subscribe to log missing or broken
    /// translations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Normal .NET resource fallback runs first, so a key that resolves from a parent or the neutral
    /// resources is not reported. A formatting failure is <b>not</b> specific to ICU MessageFormat: the
    /// default <see cref="StringFormatMessageFormatter" /> reports one for <c>Broken {0</c>, or for
    /// <c>File {0} of {1}</c> with one argument.
    /// </para>
    /// <para>Handlers must not throw (see <see cref="ILocalizer" />).</para>
    /// </remarks>
    event EventHandler<LookupFailedEventArgs> LookupFailed;

    /// <summary>
    /// Gets a value that changes whenever something changes that can change the result of a lookup -
    /// the culture, the formatter, the lookup-failure text builder, the enum key builder or the registered
    /// resource managers. Bind to it to re-resolve localized text; only whether the value differs is
    /// meaningful.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The value advances after each real change of <see cref="CurrentCulture" />,
    /// <see cref="MessageFormatter" />, <see cref="LookupFailureTextBuilder" /> or
    /// <see cref="EnumKeyBuilder" />, and after
    /// <see cref="RegisterResourceManager" />, <see cref="UnregisterResourceManager" /> and
    /// <see cref="ClearResourceManagers" /> when they change the registered set. Each advance raises
    /// <see cref="INotifyPropertyChanged.PropertyChanged" /> for this property and then for the indexer
    /// (<c>Item[]</c>). Strings previously returned by <c>Get</c> are not updated.
    /// </para>
    /// <para>
    /// Make these changes on the UI thread once views exist. In XAML, the trigger is
    /// <c>&lt;Binding Path="RefreshTriggeringRevision" Source="{x:Static core:Localizer.Current}" /&gt;</c>.
    /// </para>
    /// </remarks>
    int RefreshTriggeringRevision { get; }

    /// <summary>
    /// Gets or sets the culture used to resolve resources. Setting a different value raises
    /// <see cref="INotifyPropertyChanged.PropertyChanged" />, advances
    /// <see cref="RefreshTriggeringRevision" /> and raises <see cref="CultureChanged" />.
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
    /// Gets or sets the function that builds the resource key of an enum lookup from the enumeration value
    /// and an optional key prefix.
    /// </summary>
    /// <remarks>
    /// <para>The function receives, in order:</para>
    /// <list type="number">
    /// <item>
    /// <description>
    /// The enumeration value of the lookup (<see cref="Enum" />); never <see langword="null" />.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// The key prefix the lookup passed (<c>keyPrefix</c>, <c>KeyPrefix</c>; <see cref="string" />?), or
    /// <see langword="null" /> when it passed none. <see langword="null" /> means "use your own default"; a
    /// non-null value is an explicit override.
    /// </description>
    /// </item>
    /// </list>
    /// <para>
    /// It returns the resource key, never <see langword="null" /> or empty. The default builds
    /// <c>{keyPrefix ?? "Enum_"}{EnumTypeName}_{Value}</c>, for example
    /// <c>Enum_FileSortOrder_Ascending</c>. A custom convention might look like this:
    /// <c>Localizer.Current.EnumKeyBuilder = (value, keyPrefix) =&gt; (keyPrefix ?? "Text_") + value.GetType().Name + "." + value;</c>
    /// </para>
    /// <para>
    /// The builder applies to every enum lookup: <c>Get(Enum, …)</c>, <c>{l:LocalizeEnum}</c> and
    /// <c>LocalizeEnumConverter</c>. It must not throw or return <see langword="null" /> or an empty
    /// string: an exception it throws propagates out of the lookup, and a <see langword="null" /> or empty
    /// result makes the lookup throw <see cref="InvalidOperationException" />.
    /// </para>
    /// <para>
    /// Assigning a different delegate instance advances <see cref="RefreshTriggeringRevision" />, so bound
    /// text re-resolves; assigning the current instance again does nothing.
    /// </para>
    /// </remarks>
    /// <value>The enum key function. Defaults to the <c>Enum_{EnumTypeName}_{Value}</c> convention.</value>
    /// <exception cref="ArgumentNullException">The supplied value is <see langword="null" />.</exception>
    Func<Enum, string?, string> EnumKeyBuilder { get; set; }

    /// <summary>
    /// Gets or sets the function that produces the failure text: the text a lookup returns instead of a
    /// translation when no resource file contains the key or its value cannot be formatted.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The function receives a <see cref="LookupFailure" /> describing the failed lookup;
    /// <see cref="LookupFailure.Reason" /> tells the two cases apart. It returns the text shown instead
    /// of the translation; an empty string shows nothing.
    /// </para>
    /// <para>Examples:</para>
    /// <list type="bullet">
    /// <item><description><c>failure =&gt; $"[{failure.Key}]"</c></description></item>
    /// <item><description><c>failure =&gt; failure.Key[(failure.Key.LastIndexOf('.') + 1)..]</c></description></item>
    /// <item><description><c>failure =&gt; failure.Culture.TwoLetterISOLanguageName == "de" ? $"[fehlt: {failure.Key}]" : $"[missing: {failure.Key}]"</c></description></item>
    /// <item><description><c>failure =&gt; failure.Reason == LookupFailureReason.TranslationNotFound ? $"[missing: {failure.Key}]" : $"[broken: {failure.Key}]"</c></description></item>
    /// </list>
    /// <para>
    /// The function is called on every failed lookup, so it should be cheap. It must not throw or return
    /// <see langword="null" />: an exception it throws propagates out of the lookup, and a
    /// <see langword="null" /> result makes the lookup throw <see cref="InvalidOperationException" />.
    /// </para>
    /// <para>
    /// Assigning a different delegate instance advances <see cref="RefreshTriggeringRevision" />;
    /// assigning the current instance again does nothing.
    /// </para>
    /// </remarks>
    /// <value>The failure-text function. Defaults to <c>failure =&gt; $"!{failure.Key}!"</c>.</value>
    /// <exception cref="ArgumentNullException">The supplied value is <see langword="null" />.</exception>
    Func<LookupFailure, string> LookupFailureTextBuilder { get; set; }

    /// <summary>
    /// Gets or sets the formatter that turns each resolved resource value and the arguments of its lookup
    /// into the final string.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every lookup that resolves a value calls the formatter, also a lookup without arguments, so it is
    /// the one hook for all translated texts (see <see cref="ILocalizer" />).
    /// </para>
    /// <para>
    /// Assigning a different instance advances <see cref="RefreshTriggeringRevision" />; assigning the
    /// current instance again does nothing. Exceptions thrown by
    /// <see cref="INotifyPropertyChanged.PropertyChanged" /> handlers propagate from the assignment.
    /// </para>
    /// </remarks>
    /// <value>The formatter. Defaults to <see cref="StringFormatMessageFormatter.Instance" />.</value>
    /// <exception cref="ArgumentNullException">The supplied value is <see langword="null" />.</exception>
    IMessageFormatter MessageFormatter { get; set; }

    /// <summary>
    /// Gets the localized string for the specified key by searching every registered
    /// <see cref="ResourceManager" /> in registration order.
    /// </summary>
    /// <param name="key">The resource key to resolve.</param>
    /// <returns>
    /// The localized string, <see cref="string.Empty" /> for a <see langword="null" /> or empty key, or the
    /// failure text (see <see cref="ILocalizer" />).
    /// </returns>
    string this[string key] { get; }

    /// <summary>
    /// Removes every registered <see cref="ResourceManager" /> from the search-all set. Typed and
    /// scoped lookups are unaffected - they never consult the registration set. When at least one
    /// manager was registered, advances <see cref="RefreshTriggeringRevision" />.
    /// </summary>
    void ClearResourceManagers();

    /// <summary>
    /// Resolves the localized string for the specified key - searching every registered
    /// <see cref="ResourceManager" /> in registration order, or reading a single, explicit one - and
    /// passes it to <see cref="MessageFormatter" /> with the supplied arguments.
    /// </summary>
    /// <remarks>
    /// This is the lookup every other <c>Get</c> overload, the indexer and the XAML markup extensions go
    /// through. Every resolved value passes through <see cref="MessageFormatter" />; a formatting failure
    /// returns the failure text and raises <see cref="LookupFailed" /> (see <see cref="ILocalizer" />).
    /// </remarks>
    /// <param name="key">The resource key to resolve.</param>
    /// <param name="arguments">
    /// The arguments of the lookup, built with <see cref="MessageArguments" />, for example
    /// <c>MessageArguments.FromValues(3, 10)</c>; <see langword="null" /> or empty for none.
    /// </param>
    /// <param name="resourceManager">
    /// The single resource manager to read the key from, or <see langword="null" /> (the default) to
    /// search every registered resource manager in registration order. Pass it by name:
    /// <c>Get("Key", resourceManager: rm)</c>.
    /// </param>
    /// <returns>
    /// The localized (or formatted) string, <see cref="string.Empty" /> for a <see langword="null" /> or
    /// empty key, or the failure text (see <see cref="ILocalizer" />) in case the lookup failed.
    /// </returns>
    string Get(string key, MessageArguments? arguments = null, ResourceManager? resourceManager = null);

    /// <summary>
    /// Resolves the localized string for a typed <see cref="ResourceKey" />, which carries both the key
    /// name and the owning <see cref="ResourceManager" />, and passes it to <see cref="MessageFormatter" />
    /// with the supplied arguments.
    /// </summary>
    /// <remarks>
    /// Every resolved value passes through <see cref="MessageFormatter" />; a formatting failure returns
    /// the failure text and raises <see cref="LookupFailed" /> (see <see cref="ILocalizer" />).
    /// </remarks>
    /// <param name="key">The typed key identifying the resource and its resource manager.</param>
    /// <param name="arguments">
    /// The arguments of the lookup, built with <see cref="MessageArguments" />; <see langword="null" /> or
    /// empty for none.
    /// </param>
    /// <returns>The localized (or formatted) string, or the failure text (see <see cref="ILocalizer" />).</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="key" /> is a default or otherwise uninitialized <see cref="ResourceKey" />, whose
    /// <see cref="ResourceKey.Manager" /> is <see langword="null" />.
    /// </exception>
    string Get(ResourceKey key, MessageArguments? arguments = null);

    /// <summary>
    /// Resolves the localized string for an enumeration value: <see cref="EnumKeyBuilder" /> builds the
    /// resource key from the value, which is then resolved like a string key and passed to
    /// <see cref="MessageFormatter" /> with the supplied arguments.
    /// </summary>
    /// <remarks>
    /// Every resolved value passes through <see cref="MessageFormatter" />; a formatting failure returns
    /// the failure text and raises <see cref="LookupFailed" /> (see <see cref="ILocalizer" />).
    /// </remarks>
    /// <param name="value">The enumeration value to resolve.</param>
    /// <param name="arguments">
    /// The arguments of the lookup, built with <see cref="MessageArguments" />, for example
    /// <c>MessageArguments.FromValues(count)</c>; <see langword="null" /> or empty for none.
    /// </param>
    /// <param name="resourceManager">
    /// The single resource manager to read the key from, or <see langword="null" /> (the default) to
    /// search every registered resource manager in registration order.
    /// </param>
    /// <param name="keyPrefix">
    /// A key prefix passed to <see cref="EnumKeyBuilder" />, overriding only its default prefix (by default
    /// <c>Enum_</c>); <see langword="null" /> (the default) lets the builder decide. Pass it by name:
    /// <c>Get(sortOrder, keyPrefix: "Display_")</c>.
    /// </param>
    /// <returns>The localized (or formatted) string, or the failure text (see <see cref="ILocalizer" />).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value" /> is <see langword="null" />.</exception>
    /// <exception cref="InvalidOperationException">
    /// <see cref="EnumKeyBuilder" /> returned <see langword="null" /> or an empty string.
    /// </exception>
    string Get(
        Enum value,
        MessageArguments? arguments = null,
        ResourceManager? resourceManager = null,
        string? keyPrefix = null
    );

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

    /// <summary>
    /// Registers a <see cref="ResourceManager" /> to be searched by the key-only and enum lookups.
    /// Managers are searched in registration order on a first-match-wins basis; registering the same
    /// manager more than once has no effect. Adding a manager advances
    /// <see cref="RefreshTriggeringRevision" />.
    /// </summary>
    /// <param name="resourceManager">The resource manager to add to the search set.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="resourceManager" /> is <see langword="null" />.
    /// </exception>
    void RegisterResourceManager(ResourceManager resourceManager);

    /// <summary>
    /// Removes a previously registered <see cref="ResourceManager" /> from the search-all set, for
    /// example when the plugin or module that owns its strings is unloaded. Typed and scoped lookups
    /// are unaffected - they never consult the registration set. Removing a manager advances
    /// <see cref="RefreshTriggeringRevision" />.
    /// </summary>
    /// <param name="resourceManager">The resource manager to remove from the search set.</param>
    /// <returns>
    /// <see langword="true" /> when the manager was registered and has been removed;
    /// <see langword="false" /> when it was not registered.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="resourceManager" /> is <see langword="null" />.
    /// </exception>
    bool UnregisterResourceManager(ResourceManager resourceManager);
}
