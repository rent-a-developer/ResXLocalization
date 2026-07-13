namespace RentADeveloper.ResXLocalization.Avalonia;

/// <summary>
/// The observable behind the binding that <see cref="LocalizeEnumExtension" /> produces: follows
/// the target control's <c>DataContext</c> (expected to be an enumeration value) and emits its
/// localized text, re-emitting whenever the value or the culture of the ambient
/// <see cref="Localizer.Current" /> changes. Culture changes are observed through
/// <see cref="LocalizerWeakEvents" />, so a bound control can be collected while the process-wide
/// localizer lives on.
/// </summary>
/// <param name="dataContextSource">The live stream of the control's <c>DataContext</c> values.</param>
/// <param name="keyPrefix">The prefix prepended to the generated resource key.</param>
/// <param name="resourceManager">
/// The resource manager that scopes the lookup to a single <c>.resx</c> file, or
/// <see langword="null" /> to search all registered resource managers.
/// </param>
internal sealed class LocalizedEnumObservable(
    IObservable<Object?> dataContextSource,
    String keyPrefix,
    ResourceManager? resourceManager
)
    : IObservable<Object?>
{
    /// <summary>
    /// Pushes the localized text for the control's current <c>DataContext</c> value to
    /// <paramref name="observer" /> and keeps it updated on every value or culture change.
    /// </summary>
    /// <param name="observer">The observer receiving the localized enumeration text.</param>
    /// <returns>A subscription that stops the updates when disposed.</returns>
    public IDisposable Subscribe(IObserver<Object?> observer) =>
        new Subscription(dataContextSource, keyPrefix, resourceManager, observer);

    /// <summary>
    /// One observer's subscription: tracks the latest <c>DataContext</c> enumeration value, weakly
    /// subscribes to <see cref="LocalizerWeakEvents.CultureChanged" />, and emits the freshly
    /// localized text on every change of either.
    /// </summary>
    private sealed class Subscription : IDisposable, IWeakEventSubscriber<CultureChangedEventArgs>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Subscription" /> class, subscribing it to
        /// the <c>DataContext</c> stream (which emits the current value immediately) and to culture
        /// changes of the ambient localizer.
        /// </summary>
        /// <param name="dataContextSource">The live stream of the control's <c>DataContext</c> values.</param>
        /// <param name="keyPrefix">The prefix prepended to the generated resource key.</param>
        /// <param name="resourceManager">
        /// The resource manager that scopes the lookup to a single <c>.resx</c> file, or
        /// <see langword="null" /> to search all registered resource managers.
        /// </param>
        /// <param name="observer">The observer receiving the localized enumeration text.</param>
        public Subscription(
            IObservable<Object?> dataContextSource,
            String keyPrefix,
            ResourceManager? resourceManager,
            IObserver<Object?> observer
        )
        {
            this.observer = observer;
            this.keyPrefix = keyPrefix;
            this.resourceManager = resourceManager;
            this.dataContextSubscription = dataContextSource.Subscribe(
                new AnonymousObserver<Object?>(value =>
                    {
                        this.currentValue = value as Enum;
                        this.Emit();
                    }
                )
            );
            LocalizerWeakEvents.CultureChanged.Subscribe(Localizer.Current, this);
        }

        /// <summary>Unsubscribes from the <c>DataContext</c> stream and the culture-change weak event.</summary>
        public void Dispose()
        {
            this.dataContextSubscription.Dispose();
            LocalizerWeakEvents.CultureChanged.Unsubscribe(Localizer.Current, this);
        }

        /// <summary>Handles a culture change by re-emitting the localized text.</summary>
        /// <param name="sender">The localizer that raised the event.</param>
        /// <param name="ev">The weak event delivering the notification.</param>
        /// <param name="e">The event data carrying the previous and current culture.</param>
        public void OnEvent(Object? sender, WeakEvent ev, CultureChangedEventArgs e) => this.Emit();

        /// <summary>
        /// Emits the localized text for the tracked enumeration value, or
        /// <see cref="String.Empty" /> when the current <c>DataContext</c> is not an enumeration
        /// value (for example while it is still <see langword="null" /> during template setup).
        /// </summary>
        private void Emit()
        {
            if (this.currentValue is null)
            {
                this.observer.OnNext(String.Empty);
                return;
            }

            var key = EnumKeyConvention.BuildEnumKey(this.currentValue, this.keyPrefix);
            this.observer.OnNext(
                this.resourceManager is null
                    ? Localizer.Current.Get(key)
                    : Localizer.Current.Get(key, this.resourceManager)
            );
        }

        /// <summary>The subscription following the control's <c>DataContext</c>.</summary>
        private readonly IDisposable dataContextSubscription;

        /// <summary>The prefix prepended to the generated resource key.</summary>
        private readonly String keyPrefix;

        /// <summary>The observer receiving the localized enumeration text.</summary>
        private readonly IObserver<Object?> observer;

        /// <summary>
        /// The resource manager that scopes the lookup to a single <c>.resx</c> file, or
        /// <see langword="null" /> to search all registered resource managers.
        /// </summary>
        private readonly ResourceManager? resourceManager;

        /// <summary>
        /// The latest <c>DataContext</c> enumeration value, or <see langword="null" /> when the
        /// <c>DataContext</c> is missing or not an enumeration value.
        /// </summary>
        private Enum? currentValue;
    }
}
