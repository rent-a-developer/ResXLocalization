namespace RentADeveloper.ResXLocalization.Avalonia.Sample.Tests;

/// <summary>
/// Covers the generated <c>…Keys</c> classes of the sample: their keys use the class's own generated
/// resource manager, not the designer's.
/// </summary>
public class GeneratedKeysTests
{
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
