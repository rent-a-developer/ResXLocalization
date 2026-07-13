namespace RentADeveloper.ResXLocalization.Avalonia;

/// <summary>
/// The observable behind the binding that <see cref="LocalizeExtension" /> produces: emits the
/// resolved localized string immediately on subscription and re-emits it on every culture change of
/// the ambient <see cref="Localizer.Current" />. Culture changes are observed through
/// <see cref="LocalizerWeakEvents" />, so a bound control can be collected while the process-wide
/// localizer lives on.
/// </summary>
/// <param name="valueFactory">Produces the localized string for the current culture on demand.</param>
internal sealed class LocalizedTextObservable(Func<String> valueFactory) : IObservable<Object?>
{
    /// <summary>
    /// Pushes the currently resolved string to <paramref name="observer" /> and keeps it updated on
    /// every culture change.
    /// </summary>
    /// <param name="observer">The observer receiving the localized string.</param>
    /// <returns>A subscription that stops the culture-change updates when disposed.</returns>
    public IDisposable Subscribe(IObserver<Object?> observer)
    {
        observer.OnNext(valueFactory());
        return new Subscription(valueFactory, observer);
    }

    /// <summary>
    /// One observer's subscription: weakly subscribes to
    /// <see cref="LocalizerWeakEvents.CultureChanged" /> and forwards every culture change as a
    /// freshly resolved string.
    /// </summary>
    private sealed class Subscription : IDisposable, IWeakEventSubscriber<CultureChangedEventArgs>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Subscription" /> class and subscribes it to
        /// culture changes of the ambient localizer.
        /// </summary>
        /// <param name="valueFactory">Produces the localized string for the current culture on demand.</param>
        /// <param name="observer">The observer receiving each freshly resolved string.</param>
        public Subscription(Func<String> valueFactory, IObserver<Object?> observer)
        {
            this.valueFactory = valueFactory;
            this.observer = observer;
            LocalizerWeakEvents.CultureChanged.Subscribe(Localizer.Current, this);
        }

        /// <summary>Unsubscribes from the culture-change weak event.</summary>
        public void Dispose() =>
            LocalizerWeakEvents.CultureChanged.Unsubscribe(Localizer.Current, this);

        /// <summary>Handles a culture change by re-resolving the string and emitting it to the observer.</summary>
        /// <param name="sender">The localizer that raised the event.</param>
        /// <param name="ev">The weak event delivering the notification.</param>
        /// <param name="e">The event data carrying the previous and current culture.</param>
        public void OnEvent(Object? sender, WeakEvent ev, CultureChangedEventArgs e) =>
            this.observer.OnNext(this.valueFactory());

        /// <summary>The observer receiving each freshly resolved string.</summary>
        private readonly IObserver<Object?> observer;

        /// <summary>Produces the localized string for the current culture on demand.</summary>
        private readonly Func<String> valueFactory;
    }
}
