using System.Windows.Markup;
using RentADeveloper.ResXLocalization.Core.Tests;

namespace RentADeveloper.ResXLocalization.WPF.Sample.Tests;

/// <summary>
/// Covers a bound key of <c>{l:Localize}</c>: the key comes from the view model, as a
/// <see cref="ResourceKey" /> or a <see cref="string" />, is re-read on every conversion, works with arguments
/// and a scoping resource manager, and renders an empty string instead of throwing when the bound value gives
/// no key.
/// </summary>
public class BoundKeyTests
{
    /// <summary>The XML namespace declarations of the parsed XAML snippets.</summary>
    private const string Namespaces = """
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:l="clr-namespace:RentADeveloper.ResXLocalization.WPF;assembly=ResXLocalization.WPF"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        """;

    [Fact]
    public void BoundKeyProperty_Resolves() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            var textBlock = TestSupport.BindLocalizedText(
                new() { Key = new Binding("Key") },
                new TestViewModel { Key = "Greeting" }
            );

            textBlock.Text.Should().Be("Hello and welcome!");
        });

    [Fact]
    public void BoundKey_ReResolvesOnCultureChange() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();
            var viewModel = new TestViewModel { Key = ApplicationStringsKeys.Greeting };

            var textBlock = TestSupport.BindLocalizedText(new(new Binding("Key")), viewModel);
            textBlock.Text.Should().Be("Hello and welcome!");

            Localizer.Current.CurrentCulture = TestSupport.German;
            TestSupport.Flush();
            textBlock.Text.Should().Be("Hallo und willkommen!");
        });

    [Fact]
    public void BoundKey_ResolvesResourceKeyStringAndEmptyKey() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();
            var viewModel = new TestViewModel { Key = ApplicationStringsKeys.Greeting };

            var textBlock = TestSupport.BindLocalizedText(new(new Binding("Key")), viewModel);
            textBlock.Text.Should().Be("Hello and welcome!");

            viewModel.Key = "CatalogOnly";
            TestSupport.Flush();
            textBlock.Text.Should().Be("CatalogOnlyValue");

            viewModel.Key = string.Empty;
            TestSupport.Flush();
            textBlock.Text.Should().BeEmpty();
        });

    [Fact]
    public void BoundKey_WithArguments_Formats() =>
        WpfThread.Invoke(() =>
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
        });

    [Fact]
    public void BoundKey_WithoutDataContext_RendersEmpty() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            var bind = () => TestSupport.BindLocalizedText(new(new Binding("Key")));

            bind.Should().NotThrow().Which.Text.Should().BeEmpty();
        });

    [Fact]
    public void BoundKey_WithoutUsableValue_RendersEmpty_WithoutEvent() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();
            using var monitor = Localizer.Current.Monitor();
            var viewModel = new TestViewModel { Key = null };

            var textBlock = TestSupport.BindLocalizedText(new(new Binding("Key")), viewModel);
            textBlock.Text.Should().BeEmpty();

            viewModel.Key = 42;
            TestSupport.Flush();
            textBlock.Text.Should().BeEmpty();

            viewModel.Key = default(ResourceKey);
            TestSupport.Flush();
            textBlock.Text.Should().BeEmpty();
            monitor.LookupFailures().Should().BeEmpty();

            viewModel.Key = ApplicationStringsKeys.Greeting;
            TestSupport.Flush();
            textBlock.Text.Should().Be("Hello and welcome!");
        });

    [Fact]
    public void BoundStringKey_WithResourceManager_IsScoped() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            var textBlock = TestSupport.BindLocalizedText(
                new(new Binding("Key")) { ResourceManager = ApplicationStringsKeys.ResourceManager },
                new TestViewModel { Key = "Greeting" }
            );

            textBlock.Text.Should().Be("Hello and welcome!");
        });

    [Fact]
    public void MultiBindingKey_Throws() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            var provide = () => new LocalizeExtension { Key = new MultiBinding() }.ProvideValue(null!);

            provide.Should().Throw<InvalidOperationException>().WithMessage("*MultiBinding*");
        });

    [Fact]
    public void ParsedBoundKey_IsAssignedByTheHandler() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();
            const string attributeXaml = $$$"""<TextBlock {{{Namespaces}}} Text="{l:Localize Key={Binding Key}}" />""";
            const string elementXaml = $$"""
                <TextBlock {{Namespaces}}>
                  <TextBlock.Text>
                    <l:Localize Key="{Binding Key}">
                      <l:Argument Value="x" />
                    </l:Localize>
                  </TextBlock.Text>
                </TextBlock>
                """;

            var attribute = (TextBlock)XamlReader.Parse(attributeXaml);
            var element = (TextBlock)XamlReader.Parse(elementXaml);
            attribute.DataContext = new TestViewModel { Key = "Greeting" };
            element.DataContext = new TestViewModel { Key = "Greeting" };
            TestSupport.Flush();

            attribute.Text.Should().Be("Hello and welcome!");
            element.Text.Should().Be("Hello and welcome!");
        });

    /// <summary>Creates a positional argument.</summary>
    /// <param name="value">The value: a binding or a constant.</param>
    /// <returns>The argument.</returns>
    private static Argument Arg(object? value) => new() { Value = value };
}
