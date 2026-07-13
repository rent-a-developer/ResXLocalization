namespace RentADeveloper.ResXLocalization.Avalonia.Sample.Tests;

/// <summary>
/// Covers the four KeyPrefix × ResourceManager combinations of the <c>{l:LocalizeEnum}</c> markup
/// extension, matching the sample's card 4. The extension reads the control's <c>DataContext</c>, so each
/// test binds it to a text block whose data context is the enumeration value.
/// </summary>
public class LocalizeEnumExtensionTests
{
    [AvaloniaFact]
    public void BoundValue_SwitchesLive_OnCultureChange()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        var textBlock = TestSupport.BindLocalizedEnum(new(), FileSortOrder.Descending);
        textBlock.Text.Should().Be("Descending");

        Localizer.Current.CurrentCulture = TestSupport.German;
        TestSupport.PumpUi();
        textBlock.Text.Should().Be("Absteigend");
    }

    [AvaloniaFact]
    public void CustomPrefix_NoManager_ResolvesViaSearchAll()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        var textBlock = TestSupport.BindLocalizedEnum(
            new() { KeyPrefix = "Display_" },
            FileSortOrder.Ascending
        );

        textBlock.Text.Should().Be("A to Z");
    }

    [AvaloniaFact]
    public void CustomPrefix_WithManager_ResolvesScoped()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        var textBlock = TestSupport.BindLocalizedEnum(
            new() { KeyPrefix = "Display_", ResourceManager = SortingStrings.ResourceManager },
            FileSortOrder.Ascending
        );

        textBlock.Text.Should().Be("A to Z");
    }

    [AvaloniaFact]
    public void DefaultPrefix_NoManager_ResolvesViaSearchAll()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        var textBlock = TestSupport.BindLocalizedEnum(new(), FileSortOrder.Ascending);

        textBlock.Text.Should().Be("Ascending");
    }

    [AvaloniaFact]
    public void DefaultPrefix_WithManager_ResolvesScoped()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        var textBlock = TestSupport.BindLocalizedEnum(
            new() { ResourceManager = ApplicationStrings.ResourceManager },
            FileSortOrder.Ascending
        );

        textBlock.Text.Should().Be("Ascending");
    }
}
