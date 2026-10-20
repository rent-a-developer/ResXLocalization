using RentADeveloper.ResXLocalization.Core.Tests;

namespace RentADeveloper.ResXLocalization.Avalonia.Sample.Tests;

/// <summary>
/// Covers every way the <c>{l:Localize}</c> markup extension can be written, matching the sample's cards 1,
/// 2 and 3. Each form is constructed in code, bound to a text block, and its resolved value is asserted.
/// </summary>
public class LocalizeExtensionTests
{
    [AvaloniaFact]
    public void BoundValue_SwitchesLive_OnCultureChange()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        var textBlock = TestSupport.BindLocalizedText(new(ApplicationStringsKeys.Greeting));
        textBlock.Text.Should().Be("Hello and welcome!");

        Localizer.Current.CurrentCulture = TestSupport.German;
        textBlock.Text.Should().Be("Hallo und willkommen!");
    }

    [AvaloniaFact]
    public void KeyProperty_ResolvesViaSearchAll()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        var textBlock = TestSupport.BindLocalizedText(new() { Key = "SortingHint" });

        textBlock.Text.Should().Be("Choose how documents are ordered.");
    }

    [AvaloniaFact]
    public void KeyProperty_ResourceManagerScopesAStringKey_AndIsIgnoredForAResourceKey()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        var scoped = TestSupport.BindLocalizedText(
            new() { Key = "Greeting", ResourceManager = ApplicationStringsKeys.ResourceManager }
        );

        // The typed key resolves through its own manager; the Catalog manager does not contain Greeting.
        var typed = TestSupport.BindLocalizedText(
            new() { Key = ApplicationStringsKeys.Greeting, ResourceManager = TestSupport.CatalogResources }
        );

        scoped.Text.Should().Be("Hello and welcome!");
        typed.Text.Should().Be("Hello and welcome!");
    }

    [AvaloniaFact]
    public void KeyProperty_WithResourceKey_ResolvesViaTypedKey()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        var textBlock = TestSupport.BindLocalizedText(new() { Key = ApplicationStringsKeys.Greeting });

        textBlock.Text.Should().Be("Hello and welcome!");
    }

    [AvaloniaFact]
    public void KeyProperty_WithResourceManager_ResolvesScoped()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        var textBlock = TestSupport.BindLocalizedText(
            new() { Key = "Greeting", ResourceManager = ApplicationStringsKeys.ResourceManager }
        );

        textBlock.Text.Should().Be("Hello and welcome!");
    }

    [AvaloniaFact]
    public void NoArguments_PassesTheValueThroughTheFormatter()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();
        var previous = Localizer.Current.MessageFormatter;
        var formatter = new CapturingFormatter();
        Localizer.Current.MessageFormatter = formatter;

        // Installing the formatter re-resolves every localized binding still alive; count only this test's.
        TestSupport.PumpUi();
        var firstCall = formatter.Calls.Count;

        try
        {
            var textBlock = TestSupport.BindLocalizedText(new("CurlyNoArgs"));
            TestSupport.PumpUi();

            textBlock.Text.Should().Be("Literal {braces} stay");
            formatter.Calls.Skip(firstCall).Should().NotBeEmpty();
            formatter
                .Calls.Skip(firstCall)
                .Should()
                .AllSatisfy(call =>
                {
                    call.Pattern.Should().Be("Literal {braces} stay");
                    call.Arguments.Should().BeEmpty();
                });
        }
        finally
        {
            Localizer.Current.MessageFormatter = previous;
        }
    }

    [AvaloniaFact]
    public void ResourceKeyConstructor_ResolvesViaTypedKey()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        var textBlock = TestSupport.BindLocalizedText(new(ApplicationStringsKeys.Greeting));

        textBlock.Text.Should().Be("Hello and welcome!");
    }

    [AvaloniaFact]
    public void StringConstructor_ResolvesViaSearchAll()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        var textBlock = TestSupport.BindLocalizedText(new("Greeting"));

        textBlock.Text.Should().Be("Hello and welcome!");
    }

    [AvaloniaFact]
    public void StringConstructor_WithResourceManager_ResolvesScoped()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        var textBlock = TestSupport.BindLocalizedText(
            new("SortingHint") { ResourceManager = SortingStringsKeys.ResourceManager }
        );

        textBlock.Text.Should().Be("Choose how documents are ordered.");
    }
}
