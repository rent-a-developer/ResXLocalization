using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using RentADeveloper.ResXLocalization.Benchmarks;

namespace RentADeveloper.ResXLocalization.WPF.Benchmarks;

/// <summary>
/// The window the WPF benchmarks make their views the content of, and the helpers the benchmark bodies use.
/// WPF has no headless mode, so it is a real window, shown without activation outside the visible screen
/// area. Every member runs on the benchmark thread, which is an STA thread because <c>Main</c> and the
/// benchmarks are <c>[STAThread]</c>.
/// </summary>
internal static class WpfWindow
{
    /// <summary>
    /// Empties <paramref name="window" />, collects the garbage and lets the dispatcher go idle, outside the
    /// measurement. WPF's weak event managers remove the listeners of collected bindings only when the dispatcher
    /// is idle, which an application's dispatcher is between two user actions; without this, every view an
    /// earlier operation created would still be a listener of <c>Localizer.Current</c> in the next one.
    /// </summary>
    /// <param name="window">The window to empty.</param>
    [SuppressMessage(
        "Major Code Smell",
        "S1215:\"GC.Collect\" should not be called",
        Justification = "The weak event managers can only remove listeners whose bindings were collected."
    )]
    public static void EmptyWindow(Window window)
    {
        window.Content = null;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        Flush(DispatcherPriority.ApplicationIdle);
    }

    /// <summary>
    /// Processes the dispatcher's pending binding work. The frame ends at <see cref="DispatcherPriority.DataBind" />,
    /// which is above layout and rendering, so those are not part of a measurement.
    /// </summary>
    public static void Flush() => Flush(DispatcherPriority.DataBind);

    /// <summary>Reads the text of every <see cref="TextBlock" /> below <paramref name="root" />.</summary>
    /// <param name="root">The panel a benchmark returned.</param>
    /// <returns>The texts, in logical-tree order.</returns>
    public static IReadOnlyList<string> GetTextsOfAllTextBlocks(DependencyObject root) =>
        [.. GetLogicalDescendants(root).OfType<TextBlock>().Select(static textBlock => textBlock.Text)];

    /// <summary>
    /// Puts <paramref name="viewCount" /> new views into a <see cref="StackPanel" />, makes it the content of
    /// <paramref name="window" /> and processes the pending binding work, so the texts are set when this returns.
    /// </summary>
    /// <param name="window">The window.</param>
    /// <param name="viewCount">The number of views.</param>
    /// <param name="createView">Creates one view.</param>
    /// <returns>The panel with the views.</returns>
    public static StackPanel Open(Window window, int viewCount, Func<UIElement> createView)
    {
        var panel = new StackPanel();

        for (var index = 0; index < viewCount; index++)
        {
            _ = panel.Children.Add(createView());
        }

        window.Content = panel;
        Flush();
        return panel;
    }

    /// <summary>
    /// Shows a window outside the visible screen area, without activating it. Its <c>DataContext</c> is the one
    /// the argument bindings read.
    /// </summary>
    /// <returns>The shown window.</returns>
    public static Window Show()
    {
        var window = new Window
        {
            DataContext = ArgumentValues.Instance,
            Height = 600,
            Left = -32_000,
            ShowActivated = false,
            ShowInTaskbar = false,
            Top = -32_000,
            Width = 800,
            WindowStartupLocation = WindowStartupLocation.Manual,
            WindowStyle = WindowStyle.None,
        };
        window.Show();
        Flush();
        return window;
    }

    private static void Flush(DispatcherPriority priority)
    {
        var frame = new DispatcherFrame();
        _ = Dispatcher.CurrentDispatcher.BeginInvoke(priority, new Action(() => frame.Continue = false));
        Dispatcher.PushFrame(frame);
    }

    private static IEnumerable<DependencyObject> GetLogicalDescendants(DependencyObject root)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
        {
            yield return child;

            foreach (var descendant in GetLogicalDescendants(child))
            {
                yield return descendant;
            }
        }
    }
}
