using RentADeveloper.ResXLocalization.Core.Tests;

namespace RentADeveloper.ResXLocalization.Avalonia.Sample.Tests;

/// <summary>
/// Covers a bound key of <c>{l:Localize}</c>: the key comes from the view model, as a
/// <see cref="ResourceKey" /> or a <see cref="string" />, is re-read on every conversion, works with arguments
/// and a scoping resource manager, and renders an empty string instead of throwing when the bound value gives
/// no key.
/// </summary>
public class BoundKeyTests
{
    [AvaloniaFact]
    public void BoundKeyProperty_Resolves()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        var textBlock = TestSupport.BindLocalizedText(
            new() { Key = new Binding("Key") },
            new TestViewModel { Key = "Greeting" }
        );

        textBlock.Text.Should().Be("Hello and welcome!");
    }

    [AvaloniaFact]
    public void BoundKey_ReResolvesOnCultureChange()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();
        var viewModel = new TestViewModel { Key = ApplicationStringsKeys.Greeting };

        var textBlock = TestSupport.BindLocalizedText(new(new Binding("Key")), viewModel);
        textBlock.Text.Should().Be("Hello and welcome!");

        Localizer.Current.CurrentCulture = TestSupport.German;
        TestSupport.PumpUi();
        textBlock.Text.Should().Be("Hallo und willkommen!");
    }

    [AvaloniaFact]
    public void BoundKey_ResolvesResourceKeyStringAndEmptyKey()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();
        var viewModel = new TestViewModel { Key = ApplicationStringsKeys.Greeting };

        var textBlock = TestSupport.BindLocalizedText(new(new Binding("Key")), viewModel);
        textBlock.Text.Should().Be("Hello and welcome!");

        viewModel.Key = "CatalogOnly";
        TestSupport.PumpUi();
        textBlock.Text.Should().Be("CatalogOnlyValue");

        viewModel.Key = string.Empty;
        TestSupport.PumpUi();
        textBlock.Text.Should().BeEmpty();
    }

    [AvaloniaFact]
    public void BoundKey_WithArguments_Formats()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        var textBlock = TestSupport.BindLocalizedText(
            new(new Binding("Key")) { Arguments = { Arg(new Binding("A")), Arg(new Binding("B")) } },
            new TestViewModel
            {
                Key = "FormatPair",
                A = 3,
                B = 10,
            }
        );

        textBlock.Text.Should().Be("File 3 of 10");
    }

    [AvaloniaFact]
    public void BoundKey_WithoutDataContext_RendersEmpty()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        var bind = () => TestSupport.BindLocalizedText(new(new Binding("Key")));

        bind.Should().NotThrow().Which.Text.Should().BeEmpty();
    }

    [AvaloniaFact]
    public void BoundKey_WithoutUsableValue_RendersEmpty_WithoutEvent()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();
        using var monitor = Localizer.Current.Monitor();
        var viewModel = new TestViewModel { Key = null };

        var textBlock = TestSupport.BindLocalizedText(new(new Binding("Key")), viewModel);
        textBlock.Text.Should().BeEmpty();

        viewModel.Key = 42;
        TestSupport.PumpUi();
        textBlock.Text.Should().BeEmpty();

        viewModel.Key = default(ResourceKey);
        TestSupport.PumpUi();
        textBlock.Text.Should().BeEmpty();
        monitor.LookupFailures().Should().BeEmpty();

        viewModel.Key = ApplicationStringsKeys.Greeting;
        TestSupport.PumpUi();
        textBlock.Text.Should().Be("Hello and welcome!");
    }

    [AvaloniaFact]
    public void BoundStringKey_WithResourceManager_IsScoped()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        var textBlock = TestSupport.BindLocalizedText(
            new(new Binding("Key")) { ResourceManager = ApplicationStringsKeys.ResourceManager },
            new TestViewModel { Key = "Greeting" }
        );

        textBlock.Text.Should().Be("Hello and welcome!");
    }

    /// <summary>Creates a positional argument.</summary>
    /// <param name="value">The value: a binding or a constant.</param>
    /// <returns>The argument.</returns>
    private static Argument Arg(object? value) => new() { Value = value };
}
