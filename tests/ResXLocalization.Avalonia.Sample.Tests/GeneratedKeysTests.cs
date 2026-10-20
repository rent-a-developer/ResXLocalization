namespace RentADeveloper.ResXLocalization.Avalonia.Sample.Tests;

/// <summary>
/// Covers the generated <c>…Keys</c> classes of the sample: their keys use the class's own generated
/// resource manager, not the designer's.
/// </summary>
public class GeneratedKeysTests
{
    [AvaloniaFact]
    public void SearchAll_FindsTheSampleFiles_ThroughTheGeneratedManagers()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        Localizer.Current.Get("Greeting").Should().Be("Hello and welcome!");
        Localizer.Current.Get("PoweredBy").Should().Be("Powered by rent-a-developer");
        TestSupport.BindLocalizedText(new("Greeting")).Text.Should().Be("Hello and welcome!");

        try
        {
            // Proves that the generated instance is the registered one.
            Localizer.Current.UnregisterResourceManager(ApplicationStringsKeys.ResourceManager).Should().BeTrue();
        }
        finally
        {
            Localizer.Current.RegisterResourceManager(ApplicationStringsKeys.ResourceManager);
        }
    }

    [AvaloniaFact]
    public void TypedKeys_UseTheGeneratedResourceManager()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        ReferenceEquals(ApplicationStringsKeys.Greeting.Manager, ApplicationStringsKeys.ResourceManager)
            .Should()
            .BeTrue();
        Localizer.Current.Get(ApplicationStringsKeys.Greeting).Should().Be("Hello and welcome!");
    }
}
