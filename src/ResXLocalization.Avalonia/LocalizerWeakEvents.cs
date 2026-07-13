namespace RentADeveloper.ResXLocalization.Avalonia;

/// <summary>
/// Hosts the <see cref="WeakEvent" /> registration for <see cref="ILocalizer.CultureChanged" />.
/// The localized-value observables subscribe through it, so the long-lived ambient localizer never
/// keeps their (per-control) subscriptions - and thus the bound controls - alive.
/// </summary>
internal static class LocalizerWeakEvents
{
    /// <summary>The weak-event wrapper around <see cref="ILocalizer.CultureChanged" />.</summary>
    internal static readonly WeakEvent<ILocalizer, CultureChangedEventArgs> CultureChanged =
        WeakEvent.Register<ILocalizer, CultureChangedEventArgs>(
            static (localizer, handler) => localizer.CultureChanged += handler,
            static (localizer, handler) => localizer.CultureChanged -= handler
        );
}
