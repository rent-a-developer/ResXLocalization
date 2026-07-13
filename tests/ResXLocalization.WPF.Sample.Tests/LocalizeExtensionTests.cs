namespace RentADeveloper.ResXLocalization.WPF.Sample.Tests;

/// <summary>
/// Covers every way the <c>{l:Localize}</c> markup extension can be written, matching the sample's cards 1,
/// 2 and 3. Each form is constructed in code, bound to a text block, and its resolved value is asserted.
/// </summary>
public class LocalizeExtensionTests
{
    [Fact]
    public void BoundValue_SwitchesLive_OnCultureChange() => WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            var textBlock = TestSupport.BindLocalizedText(new(ApplicationStringsKeys.Greeting));
            textBlock.Text.Should().Be("Hello and welcome!");

            Localizer.Current.CurrentCulture = TestSupport.German;
            TestSupport.Flush();
            textBlock.Text.Should().Be("Hallo und willkommen!");
        }
    );

    [Fact]
    public void KeyProperty_ResolvesViaSearchAll() => WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            var textBlock = TestSupport.BindLocalizedText(new() { Key = "SortingHint" });

            textBlock.Text.Should().Be("Choose how documents are ordered.");
        }
    );

    [Fact]
    public void KeyProperty_WithResourceManager_ResolvesScoped() => WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            var textBlock = TestSupport.BindLocalizedText(
                new() { Key = "Greeting", ResourceManager = ApplicationStrings.ResourceManager }
            );

            textBlock.Text.Should().Be("Hello and welcome!");
        }
    );

    [Fact]
    public void ResourceKey_TakesPrecedenceOver_KeyAndResourceManager() => WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            // ResourceKey is set, so Key and ResourceManager are ignored and the typed key wins.
            var textBlock = TestSupport.BindLocalizedText(
                new()
                {
                    ResourceKey = ApplicationStringsKeys.Greeting,
                    Key = "SortingHint",
                    ResourceManager = SortingStrings.ResourceManager
                }
            );

            textBlock.Text.Should().Be("Hello and welcome!");
        }
    );

    [Fact]
    public void ResourceKeyConstructor_ResolvesViaTypedKey() => WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            var textBlock = TestSupport.BindLocalizedText(new(ApplicationStringsKeys.Greeting));

            textBlock.Text.Should().Be("Hello and welcome!");
        }
    );

    [Fact]
    public void ResourceKeyProperty_ResolvesViaTypedKey() => WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            var textBlock = TestSupport.BindLocalizedText(
                new() { ResourceKey = ApplicationStringsKeys.Greeting }
            );

            textBlock.Text.Should().Be("Hello and welcome!");
        }
    );

    [Fact]
    public void StringConstructor_ResolvesViaSearchAll() => WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            var textBlock = TestSupport.BindLocalizedText(new("Greeting"));

            textBlock.Text.Should().Be("Hello and welcome!");
        }
    );

    [Fact]
    public void StringConstructor_WithResourceManager_ResolvesScoped() => WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            var textBlock = TestSupport.BindLocalizedText(
                new("SortingHint") { ResourceManager = SortingStrings.ResourceManager }
            );

            textBlock.Text.Should().Be("Choose how documents are ordered.");
        }
    );
}
