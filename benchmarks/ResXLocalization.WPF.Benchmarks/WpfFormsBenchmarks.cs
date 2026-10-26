using System.Windows;
using System.Windows.Controls;
using BenchmarkDotNet.Attributes;
using RentADeveloper.ResXLocalization.Benchmarks;
using RentADeveloper.ResXLocalization.WPF.Benchmarks.Views;

namespace RentADeveloper.ResXLocalization.WPF.Benchmarks;

/// <summary>
/// Opening 100 text elements that all use one form - 10 instances of a 10-element view as a window's content -
/// against the same layout with <c>{x:Static}</c> of the designer class's properties.
/// </summary>
/// <remarks>
/// 300 warmup iterations instead of the configuration's 3, where Avalonia needs 50: an iteration here is one
/// operation, and WPF ships as ReadyToRun code, which the tiered JIT recompiles to optimized code method by
/// method on a background thread - thousands of WPF methods. With 50 warmups the first row of the process still
/// measured about 2 ms more than the same row run last (Static: 3.4 ms against 1.4 ms); with 300 every row
/// measures the same wherever it runs. See also the call-counting delay in benchmarks/Directory.Build.props.
/// </remarks>
[Config(typeof(BenchmarkConfig))]
[WarmupCount(300)]
public class WpfFormsBenchmarks
{
    private const int ViewCount = 10;

    private Window window = null!;

    [GlobalCleanup]
    public void Cleanup() => this.window.Close();

    [IterationCleanup]
    public void Idle() => WpfWindow.EmptyWindow(this.window);

    [Benchmark]
    [STAThread]
    public StackPanel Localize() => WpfWindow.Open(this.window, ViewCount, static () => new LocalizeView());

    [Benchmark]
    [STAThread]
    public StackPanel LocalizeEnum() => WpfWindow.Open(this.window, ViewCount, static () => new EnumView());

    [Benchmark]
    [STAThread]
    public StackPanel LocalizeWithArguments() =>
        WpfWindow.Open(this.window, ViewCount, static () => new ArgumentsView());

    [GlobalSetup]
    public void Setup()
    {
        Localizer.Current.ClearResourceManagers();
        Localizer.Current.RegisterResourceManager(BenchmarkStringsKeys.ResourceManager);
        BenchmarkCulture.Use(BenchmarkCulture.German);
        this.window = WpfWindow.Show();

        SelfCheck.Verify<StackPanel>(
            this,
            static name =>
                ExpectedViewTexts.Repeat(
                    name switch
                    {
                        nameof(LocalizeWithArguments) => ExpectedViewTexts.GermanWithArguments,
                        nameof(LocalizeEnum) => ExpectedViewTexts.GermanEnum,
                        _ => ExpectedViewTexts.German,
                    },
                    ViewCount
                ),
            WpfWindow.GetTextsOfAllTextBlocks
        );
    }

    [Benchmark(Baseline = true)]
    [STAThread]
    public StackPanel Static() => WpfWindow.Open(this.window, ViewCount, static () => new StaticView());
}
