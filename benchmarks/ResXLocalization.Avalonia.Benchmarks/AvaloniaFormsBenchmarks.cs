using Avalonia.Controls;
using BenchmarkDotNet.Attributes;
using RentADeveloper.ResXLocalization.Avalonia.Benchmarks.Views;
using RentADeveloper.ResXLocalization.Benchmarks;

namespace RentADeveloper.ResXLocalization.Avalonia.Benchmarks;

/// <summary>
/// Opening 100 text elements that all use one form - 10 instances of a 10-element view as a headless window's
/// content - against the same layout with <c>{x:Static}</c> of the designer class's properties.
/// </summary>
/// <remarks>
/// 50 warmup iterations instead of the configuration's 3: an iteration here is one operation, and the tiered JIT
/// promotes a method only after 30 calls, so with fewer warmups the measured rows ran partly unoptimized code
/// and the first rows looked slower than the later ones (see also the call-counting delay in
/// benchmarks/Directory.Build.props).
/// </remarks>
[Config(typeof(BenchmarkConfig))]
[WarmupCount(50)]
public class AvaloniaFormsBenchmarks
{
    private const int ViewCount = 10;

    private Window window = null!;

    [GlobalCleanup]
    public void Cleanup() => AvaloniaSession.Run(this.window.Close);

    [IterationCleanup]
    public void Idle() => AvaloniaSession.EmptyWindow(this.window);

    [Benchmark]
    public StackPanel Localize() => this.Open(static () => new LocalizeView());

    [Benchmark]
    public StackPanel LocalizeEnum() => this.Open(static () => new EnumView());

    [Benchmark]
    public StackPanel LocalizeWithArguments() => this.Open(static () => new ArgumentsView());

    [GlobalSetup]
    public void Setup()
    {
        AvaloniaSession.Start();
        Localizer.Current.ClearResourceManagers();
        Localizer.Current.RegisterResourceManager(BenchmarkStringsKeys.ResourceManager);
        BenchmarkCulture.Use(BenchmarkCulture.German);
        this.window = AvaloniaSession.Run(AvaloniaSession.ShowWindow);

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
            AvaloniaSession.GetTextsOfAllTextBlocks
        );
    }

    [Benchmark(Baseline = true)]
    public StackPanel Static() => this.Open(static () => new StaticView());

    private StackPanel Open(Func<Control> createView) =>
        AvaloniaSession.Run(() => AvaloniaSession.Open(this.window, ViewCount, createView));
}
