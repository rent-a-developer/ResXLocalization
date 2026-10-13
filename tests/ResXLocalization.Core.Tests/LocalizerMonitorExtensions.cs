using System.ComponentModel;
using AwesomeAssertions.Events;

namespace RentADeveloper.ResXLocalization.Core.Tests;

/// <summary>
/// Reads the failed lookups an AwesomeAssertions event monitor recorded on a localizer. Compiled into the
/// UI test suites too.
/// </summary>
internal static class LocalizerMonitorExtensions
{
    /// <summary>
    /// Gets every event recorded so far, in the order it was raised: <c>PropertyChanged(Name)</c> for a
    /// property change, otherwise the event name.
    /// </summary>
    /// <typeparam name="T">The monitored localizer type.</typeparam>
    /// <param name="monitor">The monitor of the localizer under test.</param>
    /// <returns>The event log.</returns>
    public static string[] EventLog<T>(this IMonitor<T> monitor)
        where T : ILocalizer =>
        [
            .. monitor
                .OccurredEvents.OrderBy(occurred => occurred.Sequence)
                .Select(occurred =>
                    occurred.Parameters[1] is PropertyChangedEventArgs args
                        ? $"PropertyChanged({args.PropertyName})"
                        : occurred.EventName
                ),
        ];

    /// <summary>Gets the failure of every <see cref="ILocalizer.LookupFailed" /> recorded so far.</summary>
    /// <typeparam name="T">The monitored localizer type.</typeparam>
    /// <param name="monitor">The monitor of the localizer under test.</param>
    /// <returns>The failed lookups, in order.</returns>
    public static LookupFailure[] LookupFailures<T>(this IMonitor<T> monitor)
        where T : ILocalizer =>
        [
            .. monitor
                .OccurredEvents.Where(occurred => occurred.EventName == nameof(ILocalizer.LookupFailed))
                .Select(occurred => ((LookupFailedEventArgs)occurred.Parameters[1]).Failure),
        ];
}
