[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]

namespace RentADeveloper.ResXLocalization.Avalonia.Sample.Tests;

/// <summary>
/// Configures the headless Avalonia application that hosts every test. It registers the same resource
/// managers the real app registers in <see cref="Program" />, so the search-all lookups behave identically
/// under test.
/// </summary>
public sealed class TestAppBuilder
{
    /// <summary>
    /// Avalonia.Headless only ever calls the static factory below through the assembly attribute, so the
    /// type is never instantiated.
    /// </summary>
    private TestAppBuilder() { }

    public static AppBuilder BuildAvaloniaApp()
    {
        Localizer.Current.RegisterResourceManager(ApplicationStrings.ResourceManager);
        Localizer.Current.RegisterResourceManager(SortingStrings.ResourceManager);
        Localizer.Current.RegisterResourceManager(BrandingStrings.ResourceManager);

        return AppBuilder
            .Configure<App>()
            .UseSkia()
            .UseHeadless(new() { ShouldRenderOnUIThread = true, UseHeadlessDrawing = false });
    }
}
