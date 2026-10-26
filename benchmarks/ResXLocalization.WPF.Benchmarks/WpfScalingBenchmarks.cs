using System.Windows;
using System.Windows.Controls;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using RentADeveloper.ResXLocalization.Benchmarks;
using RentADeveloper.ResXLocalization.WPF.Benchmarks.Views;

namespace RentADeveloper.ResXLocalization.WPF.Benchmarks;

/// <summary>
/// Opening a view with <see cref="ElementCount" /> <c>{l:Localize}</c> elements, against <c>{x:Static}</c>, and
/// switching the culture while they are live, against the hand-written <see cref="TranslationSource" />. The
/// elements are <see cref="ElementCount" /> / 10 instances of a 10-element view in one window.
/// </summary>
/// <remarks>
/// Every iteration is one operation (<c>InvocationCount = 1</c>), which the culture switch needs for its
/// <c>[IterationSetup]</c> and which keeps the 10,000-element rows short. 5 measured iterations instead of the
/// configuration's 15, because an iteration of a 10,000-element row takes up to a second plus its setup and
/// cleanup.
/// </remarks>
[Config(typeof(BenchmarkConfig))]
[CategoriesColumn]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[InvocationCount(1)]
[IterationCount(5)]
public class WpfScalingBenchmarks
{
    private const string OpenCategory = "Open";

    private const string SwitchCategory = "Switch";

    private Window window = null!;

    [Params(100, 10_000)]
    public int ElementCount { get; set; }

    private int ViewCount => this.ElementCount / 10;

    [GlobalCleanup]
    public void Cleanup() => this.window.Close();

    [IterationCleanup]
    public void Idle() => WpfWindow.EmptyWindow(this.window);

    [Benchmark]
    [BenchmarkCategory(OpenCategory)]
    [STAThread]
    public StackPanel OpenLocalize() => WpfWindow.Open(this.window, this.ViewCount, static () => new LocalizeView());

    [Benchmark(Baseline = true)]
    [BenchmarkCategory(OpenCategory)]
    [STAThread]
    public StackPanel OpenStatic() => WpfWindow.Open(this.window, this.ViewCount, static () => new StaticView());

    [GlobalSetup]
    public void Setup()
    {
        Localizer.Current.ClearResourceManagers();
        BenchmarkCulture.Use(BenchmarkCulture.German);
        this.window = WpfWindow.Show();

        SelfCheck.Verify<StackPanel>(
            this,
            name =>
                ExpectedViewTexts.Repeat(
                    name.StartsWith(SwitchCategory, StringComparison.Ordinal)
                        ? ExpectedViewTexts.English
                        : ExpectedViewTexts.German,
                    this.ViewCount
                ),
            WpfWindow.GetTextsOfAllTextBlocks
        );
    }

    [IterationSetup(Targets = [nameof(SwitchLocalize)])]
    public void SetupLocalizeViews() => this.SetupViews(static () => new LocalizeView());

    [IterationSetup(Targets = [nameof(SwitchTranslationSource)])]
    public void SetupTranslationSourceViews() => this.SetupViews(static () => new TranslationSourceView());

    [Benchmark]
    [BenchmarkCategory(SwitchCategory)]
    [STAThread]
    public StackPanel SwitchLocalize() =>
        this.Switch(static () => Localizer.Current.CurrentCulture = BenchmarkCulture.English);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory(SwitchCategory)]
    [STAThread]
    public StackPanel SwitchTranslationSource() =>
        this.Switch(static () => TranslationSource.Instance.Culture = BenchmarkCulture.English);

    /// <summary>
    /// Builds the views of a culture switch in German, outside the measurement. The previous iteration's views
    /// are removed first, so that only these views are live when BenchmarkDotNet collects garbage before the
    /// iteration.
    /// </summary>
    /// <param name="createView">Creates one view.</param>
    private void SetupViews(Func<UIElement> createView)
    {
        this.window.Content = null;
        BenchmarkCulture.Use(BenchmarkCulture.German);
        _ = WpfWindow.Open(this.window, this.ViewCount, createView);
    }

    /// <summary>
    /// Switches the culture and processes the dispatcher's pending binding work, so every element shows the new
    /// text when this returns; the self-check confirms it for all of them.
    /// </summary>
    /// <param name="switchCulture">Switches the culture.</param>
    /// <returns>The panel with the views.</returns>
    private StackPanel Switch(Action switchCulture)
    {
        switchCulture();
        WpfWindow.Flush();
        return (StackPanel)this.window.Content;
    }
}
