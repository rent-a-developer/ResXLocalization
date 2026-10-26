using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using RentADeveloper.ResXLocalization.Benchmarks;

namespace RentADeveloper.ResXLocalization.Avalonia.Benchmarks;

/// <summary>
/// The one headless Avalonia session of the process, shared by every benchmark class, and the helpers the
/// benchmark bodies use on its dispatcher thread.
/// </summary>
internal static class AvaloniaSession
{
    private static HeadlessUnitTestSession? session;

    /// <summary>
    /// Empties <paramref name="window" />, collects the garbage and runs the dispatcher's pending work, outside
    /// the measurement, as an application's dispatcher is idle between two user actions; without this, the views
    /// an earlier operation created would still be subscribed to <c>Localizer.Current</c> in the next one.
    /// </summary>
    /// <param name="window">The window to empty.</param>
    [SuppressMessage(
        "Major Code Smell",
        "S1215:\"GC.Collect\" should not be called",
        Justification = "Weak subscriptions are only removed once their subscribers were collected."
    )]
    public static void EmptyWindow(Window window) =>
        Run(() =>
        {
            window.Content = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Dispatcher.UIThread.RunJobs();
        });

    /// <summary>Reads the text of every <see cref="TextBlock" /> below <paramref name="root" />, on the UI thread.</summary>
    /// <param name="root">The panel a benchmark returned.</param>
    /// <returns>The texts, in logical-tree order.</returns>
    public static IReadOnlyList<string> GetTextsOfAllTextBlocks(ILogical root) =>
        Run(() =>
            (IReadOnlyList<string>)
                [
                    .. root.GetLogicalDescendants()
                        .OfType<TextBlock>()
                        .Select(static textBlock => textBlock.Text ?? string.Empty),
                ]
        );

    /// <summary>
    /// Puts <paramref name="viewCount" /> new views into a <see cref="StackPanel" /> and makes it the content of
    /// <paramref name="window" />. Avalonia applies a binding's value as soon as it is bound, so the texts are set
    /// when this returns.
    /// </summary>
    /// <param name="window">The window.</param>
    /// <param name="viewCount">The number of views.</param>
    /// <param name="createView">Creates one view.</param>
    /// <returns>The panel with the views.</returns>
    public static StackPanel Open(Window window, int viewCount, Func<Control> createView)
    {
        var panel = new StackPanel();

        for (var index = 0; index < viewCount; index++)
        {
            panel.Children.Add(createView());
        }

        window.Content = panel;
        return panel;
    }

    /// <summary>
    /// Runs <paramref name="body" /> on the session's dispatcher thread and waits for it. The call flows the
    /// caller's execution context, and with it <see cref="System.Globalization.CultureInfo.CurrentUICulture" />,
    /// so the culture is set on the calling thread: a culture set inside <paramref name="body" /> ends with it.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="body">The work to run on the UI thread.</param>
    /// <returns>The result of <paramref name="body" />.</returns>
    public static T Run<T>(Func<T> body) => session!.Dispatch(body, CancellationToken.None).GetAwaiter().GetResult();

    /// <summary>Runs <paramref name="body" /> on the session's dispatcher thread and waits for it.</summary>
    /// <param name="body">The work to run on the UI thread.</param>
    public static void Run(Action body) => session!.Dispatch(body, CancellationToken.None).GetAwaiter().GetResult();

    /// <summary>
    /// Shows a headless window: the window every benchmark of a class makes its views the content of. Its
    /// <c>DataContext</c> is the one the argument bindings read.
    /// </summary>
    /// <returns>The shown window.</returns>
    public static Window ShowWindow()
    {
        var window = new Window
        {
            DataContext = ArgumentValues.Instance,
            Height = 600,
            Width = 800,
        };
        window.Show();
        Dispatcher.UIThread.RunJobs(DispatcherPriority.Normal);
        return window;
    }

    /// <summary>Starts the session, once per process; each class's <c>[GlobalSetup]</c> calls it.</summary>
    public static void Start() => session ??= HeadlessUnitTestSession.StartNew(typeof(BenchmarkApp));
}
