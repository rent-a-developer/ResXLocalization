namespace RentADeveloper.ResXLocalization.Avalonia.Sample.Tests;

/// <summary>
/// Guards the leak-safety contract: because the engine subscribes to <c>CultureChanged</c> weakly, the
/// process-lifetime <see cref="Localizer.Current" /> singleton must not keep bound-then-discarded controls
/// alive. If this regresses, every localized control in a long-running app would leak.
/// </summary>
public class MemoryLeakTests
{
    [AvaloniaFact]
    public void BoundControls_AreCollected_WhenDiscarded()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        // Sanity: the binding really subscribes and resolves, so the leak check below is not vacuous.
        var probe = TestSupport.BindLocalizedText(new(ApplicationStringsKeys.Greeting));
        probe.Text.Should().Be("Hello and welcome!");

        var references = CreateAndAbandonBoundControls(200);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        references.Should().NotContain(reference => reference.IsAlive);
    }

    [AvaloniaFact]
    public void DisposedViewModel_IsCollected_WhileAmbientLocalizerLives()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();
        var reference = CreateAndDisposeViewModel();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        reference.IsAlive.Should().BeFalse();
        Localizer.Current.Should().NotBeNull();
    }

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
            var textBlock = new TextBlock();
            textBlock.Bind(
                TextBlock.TextProperty,
                (BindingBase)new LocalizeExtension(ApplicationStringsKeys.Greeting).ProvideValue(null!)
            );
            references.Add(new(textBlock));
        }

        return references;
    }
}
