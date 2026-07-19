namespace RentADeveloper.ResXLocalization.Avalonia;

/// <summary>
/// The observable behind the binding that <see cref="LocalizeExtension" /> produces when it knows
/// its target element: emits the resolved localized string - formatted with the
/// <see cref="LocalizeArgs" /> arguments currently set on the element - and re-emits it whenever an
/// argument or the culture of the ambient <see cref="Localizer.Current" /> changes. Culture changes
/// are observed through <see cref="LocalizerWeakEvents" />, so a bound control can be collected
/// while the process-wide localizer lives on.
/// </summary>
/// <param name="element">The target element carrying the <see cref="LocalizeArgs" /> arguments.</param>
/// <param name="resolve">Resolves the localized string without composite formatting.</param>
/// <param name="resolveFormatted">Resolves the localized string formatted with the supplied arguments.</param>
internal sealed class LocalizedFormattedTextObservable(
    AvaloniaObject element,
    Func<String> resolve,
    Func<Object?[], String> resolveFormatted
)
    : IObservable<Object?>
{
    /// <summary>
    /// Pushes the currently resolved string to <paramref name="observer" /> and keeps it updated on
    /// every argument or culture change.
    /// </summary>
    /// <param name="observer">The observer receiving the localized string.</param>
    /// <returns>A subscription that stops the updates when disposed.</returns>
    public IDisposable Subscribe(IObserver<Object?> observer) =>
        new Subscription(element, resolve, resolveFormatted, observer);

    /// <summary>
    /// One observer's subscription: follows the element's args-version property (whose stream emits
    /// immediately, producing the initial value), weakly subscribes to
    /// <see cref="LocalizerWeakEvents.CultureChanged" />, and emits the freshly resolved string on
    /// every change of either.
    /// </summary>
    private sealed class Subscription : IDisposable, IWeakEventSubscriber<CultureChangedEventArgs>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Subscription" /> class, subscribing it to the
        /// element's args-version stream and to culture changes of the ambient localizer.
        /// </summary>
        /// <param name="element">The target element carrying the <see cref="LocalizeArgs" /> arguments.</param>
        /// <param name="resolve">Resolves the localized string without composite formatting.</param>
        /// <param name="resolveFormatted">Resolves the localized string formatted with the supplied arguments.</param>
        /// <param name="observer">The observer receiving each freshly resolved string.</param>
        public Subscription(
            AvaloniaObject element,
            Func<String> resolve,
            Func<Object?[], String> resolveFormatted,
            IObserver<Object?> observer
        )
        {
            this.element = element;
            this.resolve = resolve;
            this.resolveFormatted = resolveFormatted;
            this.observer = observer;
            this.argsVersionSubscription = element
                .GetObservable(LocalizeArgs.ArgsVersionProperty)
                .Subscribe(new AnonymousObserver<Int32>(_ => this.Emit()));
            LocalizerWeakEvents.CultureChanged.Subscribe(Localizer.Current, this);
        }

        /// <summary>Unsubscribes from the args-version stream and the culture-change weak event.</summary>
        public void Dispose()
        {
            this.argsVersionSubscription.Dispose();
            LocalizerWeakEvents.CultureChanged.Unsubscribe(Localizer.Current, this);
        }

        /// <summary>Handles a culture change by re-resolving the string and emitting it to the observer.</summary>
        /// <param name="sender">The localizer that raised the event.</param>
        /// <param name="ev">The weak event delivering the notification.</param>
        /// <param name="e">The event data carrying the previous and current culture.</param>
        public void OnEvent(Object? sender, WeakEvent ev, CultureChangedEventArgs e) => this.Emit();

        /// <summary>
        /// Emits the freshly resolved string: formatted with the element's current
        /// <see cref="LocalizeArgs" /> arguments when at least one is set, otherwise resolved plainly
        /// so the no-argument behavior stays byte-for-byte unchanged.
        /// </summary>
        private void Emit()
        {
            Object? value;

            try
            {
                var arguments = LocalizeArgs.GetArguments(this.element);
                value = arguments is null ? this.resolve() : this.resolveFormatted(arguments);
            }
            catch (FormatException exception)
            {
                // Mirrors WPF, whose binding engine catches converter exceptions and reports a binding
                // error: a format string that does not (yet) match the supplied arguments - routine
                // while multi-argument bindings still deliver their values one by one - must not throw
                // into the property system mid-delivery. It surfaces as a binding error instead and
                // resolves cleanly on the next argument or culture change.
                value = new BindingNotification(exception, BindingErrorType.Error);
            }

            this.observer.OnNext(value);
        }

        /// <summary>The subscription following the element's args-version property.</summary>
        private readonly IDisposable argsVersionSubscription;

        /// <summary>The target element carrying the <see cref="LocalizeArgs" /> arguments.</summary>
        private readonly AvaloniaObject element;

        /// <summary>The observer receiving each freshly resolved string.</summary>
        private readonly IObserver<Object?> observer;

        /// <summary>Resolves the localized string without composite formatting.</summary>
        private readonly Func<String> resolve;

        /// <summary>Resolves the localized string formatted with the supplied arguments.</summary>
        private readonly Func<Object?[], String> resolveFormatted;
    }
}
