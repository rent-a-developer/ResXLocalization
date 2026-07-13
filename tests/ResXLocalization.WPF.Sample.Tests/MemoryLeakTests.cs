namespace RentADeveloper.ResXLocalization.WPF.Sample.Tests;

/// <summary>
/// Guards the leak-safety contract: <c>{l:Localize}</c> binds to the process-lifetime
/// <see cref="Localizer.Current" /> singleton, and WPF holds the binding target through a weak reference,
/// so bound-then-discarded controls must still be collectable. If this regresses, every localized control
/// in a long-running app would leak.
/// </summary>
public class MemoryLeakTests
{
    [Fact]
    public void BoundControls_AreCollected_WhenDiscarded() => WpfThread.Invoke(static () =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            // Sanity: the binding really subscribes and resolves, so the leak check below is not vacuous.
            var probe = TestSupport.BindLocalizedText(new(ApplicationStringsKeys.Greeting));
            probe.Text.Should().Be("Hello and welcome!");

            var references = CreateAndAbandonBoundControls(200);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            references.Should().NotContain(static reference => reference.IsAlive);
        }
    );

    [Fact]
    public void DisposedViewModel_IsCollected_WhileAmbientLocalizerLives() => WpfThread.Invoke(static () =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();
            var reference = CreateAndDisposeViewModel();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            reference.IsAlive.Should().BeFalse();
            Localizer.Current.Should().NotBeNull();
        }
    );

    private static WeakReference CreateAndDisposeViewModel()
    {
        var viewModel = new MainWindowViewModel(Localizer.Current);
        var reference = new WeakReference(viewModel);
        viewModel.Dispose();
        return reference;
    }

    private static List<WeakReference> CreateAndAbandonBoundControls(Int32 count)
    {
        var references = new List<WeakReference>(count);
        for (var index = 0; index < count; index++)
        {
            var textBlock = TestSupport.BindLocalizedText(new(ApplicationStringsKeys.Greeting));
            references.Add(new(textBlock));
        }

        return references;
    }
}
