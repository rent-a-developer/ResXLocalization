namespace RentADeveloper.ResXLocalization.WPF.Sample.Tests;

/// <summary>
/// A single, shared STA thread that owns a WPF <see cref="Dispatcher" /> and the one process-wide
/// <see cref="Application" />. WPF objects have thread affinity and the localizer is a process-wide
/// singleton, so every test body is marshalled onto this one thread via <see cref="Invoke" />. This keeps
/// all controls, bindings, and culture switches on a single, message-pumped thread.
/// </summary>
internal static class WpfThread
{
    /// <summary>Runs the supplied action synchronously on the shared WPF dispatcher thread.</summary>
    /// <param name="action">The test body to execute on the WPF thread.</param>
    public static void Invoke(Action action) =>
        SharedDispatcher.Invoke(action);

    private static Dispatcher StartDispatcher()
    {
        using var ready = new ManualResetEventSlim(false);
        Dispatcher? dispatcher = null;

        var thread = new Thread(() =>
            {
                try
                {
                    // One Application for the whole test run registers the WPF pack:// scheme. The sample's own
                    // pack URIs are assembly-qualified (…;component/…), so they resolve without touching
                    // Application.ResourceAssembly (which the test host has already set and forbids changing).
                    if (Application.Current is null)
                    {
                        _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                    }

                    dispatcher = Dispatcher.CurrentDispatcher;
                }
                finally
                {
                    // Always release the starter, even on failure, so the main thread never deadlocks waiting.
                    ready.Set();
                }

                Dispatcher.Run();
            }
        )
        {
            IsBackground = true,
            Name = "WpfTestThread"
        };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        ready.Wait();

        return dispatcher!;
    }

    private static readonly Dispatcher SharedDispatcher = StartDispatcher();
}
