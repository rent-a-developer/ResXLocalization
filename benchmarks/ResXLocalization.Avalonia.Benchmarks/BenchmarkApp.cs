using Avalonia;
using Avalonia.Headless;
using Avalonia.Themes.Fluent;

namespace RentADeveloper.ResXLocalization.Avalonia.Benchmarks;

/// <summary>
/// The headless application every Avalonia benchmark runs in. It has the Fluent theme, as a desktop application
/// does, so that every created control gets its theme styles, as WPF's controls get their default theme styles.
/// Templates are only applied by layout, which no benchmark runs.
/// </summary>
public sealed class BenchmarkApp : Application
{
    /// <summary>The entry point <c>HeadlessUnitTestSession.StartNew</c> calls to configure the application.</summary>
    /// <returns>The configured application builder.</returns>
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<BenchmarkApp>().UseHeadless(new() { UseHeadlessDrawing = true });

    /// <inheritdoc />
    public override void Initialize() => this.Styles.Add(new FluentTheme());
}
