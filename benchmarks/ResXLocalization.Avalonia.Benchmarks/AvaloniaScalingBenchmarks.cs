using Avalonia.Controls;
using Avalonia.Threading;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using RentADeveloper.ResXLocalization.Avalonia.Benchmarks.Views;
using RentADeveloper.ResXLocalization.Benchmarks;

namespace RentADeveloper.ResXLocalization.Avalonia.Benchmarks;

/// <summary>
/// Opening a view with <see cref="ElementCount" /> <c>{l:Localize}</c> elements, against <c>{x:Static}</c>, and
/// switching the culture while they are live, against the hand-written <see cref="TranslationSource" />. The
/// elements are <see cref="ElementCount" /> / 10 instances of a 10-element view in one headless window.
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
public class AvaloniaScalingBenchmarks
{
    private const string OpenCategory = "Open";

    private const string SwitchCategory = "Switch";

    private Window window = null!;

    [Params(100, 10_000)]
    public int ElementCount { get; set; }

    private int ViewCount => this.ElementCount / 10;

    [GlobalCleanup]
    public void Cleanup() => AvaloniaSession.Run(this.window.Close);

    [IterationCleanup]
    public void Idle() => AvaloniaSession.EmptyWindow(this.window);

    [Benchmark]
    [BenchmarkCategory(OpenCategory)]
    public StackPanel OpenLocalize() => this.Open(static () => new LocalizeView());

    [Benchmark(Baseline = true)]
    [BenchmarkCategory(OpenCategory)]
    public StackPanel OpenStatic() => this.Open(static () => new StaticView());

    [GlobalSetup]
    public void Setup()
    {
        AvaloniaSession.Start();
        Localizer.Current.ClearResourceManagers();
        BenchmarkCulture.Use(BenchmarkCulture.German);
        this.window = AvaloniaSession.Run(AvaloniaSession.ShowWindow);

        SelfCheck.Verify<StackPanel>(
            this,
            name =>
                ExpectedViewTexts.Repeat(
                    name.StartsWith(SwitchCategory, StringComparison.Ordinal)
                        ? ExpectedViewTexts.English
                        : ExpectedViewTexts.German,
                    this.ViewCount
                ),
            AvaloniaSession.GetTextsOfAllTextBlocks
        );
    }

    [IterationSetup(Targets = [nameof(SwitchLocalize)])]
    public void SetupLocalizeViews() => this.SetupViews(static () => new LocalizeView());

    [IterationSetup(Targets = [nameof(SwitchTranslationSource)])]
    public void SetupTranslationSourceViews() => this.SetupViews(static () => new TranslationSourceView());

    [Benchmark]
    [BenchmarkCategory(SwitchCategory)]
    public StackPanel SwitchLocalize() =>
        this.Switch(static () => Localizer.Current.CurrentCulture = BenchmarkCulture.English);

    [Benchmark(Baseline = true)]
    [BenchmarkCategory(SwitchCategory)]
    public StackPanel SwitchTranslationSource() =>
        this.Switch(static () => TranslationSource.Instance.Culture = BenchmarkCulture.English);

    private StackPanel Open(Func<Control> createView) =>
        AvaloniaSession.Run(() => AvaloniaSession.Open(this.window, this.ViewCount, createView));

    /// <summary>
    /// Builds the views of a culture switch in German, outside the measurement. The previous iteration's views
    /// are removed first, so that only these views are live when BenchmarkDotNet collects garbage before the
    /// iteration. The culture is set on the UI thread, because views an earlier benchmark left may still be
    /// subscribed and update their texts; the <c>CurrentUICulture</c> it sets there ends with the call, and none
    /// of these views reads it.
    /// </summary>
    /// <param name="createView">Creates one view.</param>
    private void SetupViews(Func<Control> createView) =>
        AvaloniaSession.Run(() =>
        {
            this.window.Content = null;
            BenchmarkCulture.Use(BenchmarkCulture.German);
            _ = AvaloniaSession.Open(this.window, this.ViewCount, createView);
        });

    /// <summary>
    /// Switches the culture and processes the dispatcher's pending binding work, down to
    /// <see cref="DispatcherPriority.Normal" />, which is above layout and rendering, as in WPF. Avalonia updates
    /// a binding on the UI thread as soon as its source raises <c>PropertyChanged</c>, so every element shows the
    /// new text when this returns; the self-check confirms it for all of them.
    /// </summary>
    /// <param name="switchCulture">Switches the culture.</param>
    /// <returns>The panel with the views.</returns>
    private StackPanel Switch(Action switchCulture) =>
        AvaloniaSession.Run(() =>
        {
            switchCulture();
            Dispatcher.UIThread.RunJobs(DispatcherPriority.Normal);
            return (StackPanel)this.window.Content!;
        });
}
