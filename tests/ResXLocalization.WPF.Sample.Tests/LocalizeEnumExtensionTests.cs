using System.Reflection;
using System.Windows.Markup;

namespace RentADeveloper.ResXLocalization.WPF.Sample.Tests;

/// <summary>
/// Covers the <c>{l:LocalizeEnum}</c> markup extension, matching the sample's card 4: the enumeration value
/// comes from <see cref="LocalizeEnumExtension.Value" /> - a binding, the item-template form
/// <c>Value={Binding}</c>, or a fixed member - the four KeyPrefix × ResourceManager combinations, and format
/// arguments as <see cref="Argument" /> entries.
/// </summary>
public class LocalizeEnumExtensionTests
{
    [Fact]
    public void BoundArgument_FormatsTheEnumText_AndFollowsCultureChange() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            var textBlock = TestSupport.BindLocalizedEnum(
                new()
                {
                    Value = new Binding(nameof(TestViewModel.SortOrder)),
                    KeyPrefix = "Count_",
                    Arguments = { Arg(new Binding(nameof(TestViewModel.Count))) },
                },
                new TestViewModel { SortOrder = FileSortOrder.Ascending, Count = 3 }
            );
            textBlock.Text.Should().Be("3 files, ascending");

            Localizer.Current.CurrentCulture = TestSupport.German;
            TestSupport.Flush();
            textBlock.Text.Should().Be("3 Dateien, aufsteigend");
        });

    [Fact]
    public void BoundValue_FollowsValueAndCultureChanges() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();
            var viewModel = new TestViewModel { SortOrder = FileSortOrder.Ascending };

            var textBlock = TestSupport.BindLocalizedEnum(
                new() { Value = new Binding(nameof(TestViewModel.SortOrder)) },
                viewModel
            );
            textBlock.Text.Should().Be("Ascending");

            viewModel.SortOrder = FileSortOrder.Descending;
            TestSupport.Flush();
            textBlock.Text.Should().Be("Descending");

            Localizer.Current.CurrentCulture = TestSupport.German;
            TestSupport.Flush();
            textBlock.Text.Should().Be("Absteigend");
        });

    [Fact]
    public void BoundValue_SwitchesLive_OnCultureChange() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            var textBlock = TestSupport.BindLocalizedEnum(new() { Value = new Binding() }, FileSortOrder.Descending);
            textBlock.Text.Should().Be("Descending");

            Localizer.Current.CurrentCulture = TestSupport.German;
            TestSupport.Flush();
            textBlock.Text.Should().Be("Absteigend");
        });

    [Fact]
    public void ConstantValueAndArgument_Format() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            var textBlock = TestSupport.BindLocalizedEnum(
                new()
                {
                    Value = FileSortOrder.Ascending,
                    KeyPrefix = "Count_",
                    Arguments = { Arg(3) },
                },
                null
            );
            textBlock.Text.Should().Be("3 files, ascending");

            Localizer.Current.CurrentCulture = TestSupport.German;
            TestSupport.Flush();
            textBlock.Text.Should().Be("3 Dateien, aufsteigend");
        });

    [Fact]
    public void ConstructorArgument_SetsValue() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            var bound = TestSupport.BindLocalizedEnum(
                new(new Binding(nameof(TestViewModel.SortOrder))),
                new TestViewModel { SortOrder = FileSortOrder.Ascending }
            );
            var constant = TestSupport.BindLocalizedEnum(new(FileSortOrder.Ascending), null);
            var text = () => new LocalizeEnumExtension("Ascending").ProvideValue(EmptyServiceProvider.Instance);

            bound.Text.Should().Be("Ascending");
            constant.Text.Should().Be("Ascending");
            text.Should().Throw<InvalidOperationException>().WithMessage("*System.String*");
        });

    [Fact]
    public void CustomEnumKeyBuilder_AppliesToTheExtension() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();
            var original = Localizer.Current.EnumKeyBuilder;
            Localizer.Current.EnumKeyBuilder = static (value, keyPrefix) =>
                (keyPrefix ?? "Text_") + value.GetType().Name + "." + value;

            try
            {
                var textBlock = TestSupport.BindLocalizedEnum(new() { Value = new Binding() }, FileSortOrder.Ascending);

                textBlock.Text.Should().Be("Custom ascending");
            }
            finally
            {
                Localizer.Current.EnumKeyBuilder = original;
            }
        });

    [Fact]
    public void CustomPrefix_NoManager_ResolvesViaSearchAll() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            var textBlock = TestSupport.BindLocalizedEnum(
                new() { Value = new Binding(), KeyPrefix = "Display_" },
                FileSortOrder.Ascending
            );

            textBlock.Text.Should().Be("A to Z");
        });

    [Fact]
    public void CustomPrefix_WithManager_ResolvesScoped() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            var textBlock = TestSupport.BindLocalizedEnum(
                new()
                {
                    Value = new Binding(),
                    KeyPrefix = "Display_",
                    ResourceManager = SortingStringsKeys.ResourceManager,
                },
                FileSortOrder.Ascending
            );

            textBlock.Text.Should().Be("A to Z");
        });

    [Fact]
    public void DefaultPrefix_NoManager_ResolvesViaSearchAll() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            var textBlock = TestSupport.BindLocalizedEnum(new() { Value = new Binding() }, FileSortOrder.Ascending);

            textBlock.Text.Should().Be("Ascending");
        });

    [Fact]
    public void DefaultPrefix_WithManager_ResolvesScoped() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            var textBlock = TestSupport.BindLocalizedEnum(
                new() { Value = new Binding(), ResourceManager = ApplicationStringsKeys.ResourceManager },
                FileSortOrder.Ascending
            );

            textBlock.Text.Should().Be("Ascending");
        });

    [Fact]
    public void FormatterChange_ReResolvesTheExtensionAndTheConverter_WithoutALanguageSwitch() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();
            var previous = Localizer.Current.MessageFormatter;
            var viewModel = new TestViewModel { SortOrder = FileSortOrder.Ascending };

            try
            {
                var extension = TestSupport.BindLocalizedEnum(
                    new() { Value = new Binding(nameof(TestViewModel.SortOrder)) },
                    viewModel
                );

                var multiBinding = new MultiBinding { Converter = LocalizeEnumConverter.Default };
                multiBinding.Bindings.Add(
                    new Binding(nameof(ILocalizer.RefreshTriggeringRevision)) { Source = Localizer.Current }
                );
                multiBinding.Bindings.Add(new Binding(nameof(TestViewModel.SortOrder)));
                var converter = new TextBlock { DataContext = viewModel };
                converter.SetBinding(TextBlock.TextProperty, multiBinding);
                TestSupport.Flush();

                extension.Text.Should().Be("Ascending");
                converter.Text.Should().Be("Ascending");

                Localizer.Current.MessageFormatter = new ArgumentTests.UpperCaseFormatter();
                TestSupport.Flush();

                extension.Text.Should().Be("ASCENDING");
                converter.Text.Should().Be("ASCENDING");
            }
            finally
            {
                Localizer.Current.MessageFormatter = previous;
            }
        });

    [Fact]
    public void MissingOrNonEnumValue_Throws() =>
        WpfThread.Invoke(() =>
        {
            var missing = () => new LocalizeEnumExtension().ProvideValue(EmptyServiceProvider.Instance);
            var text = () =>
                new LocalizeEnumExtension { Value = "Ascending" }.ProvideValue(EmptyServiceProvider.Instance);

            missing.Should().Throw<InvalidOperationException>().WithMessage("*Value*");
            text.Should().Throw<InvalidOperationException>().WithMessage("*System.String*");
        });

    [Fact]
    public void MultiBindingOrPriorityBindingValue_Throws() =>
        WpfThread.Invoke(() =>
        {
            var multiBinding = () =>
                new LocalizeEnumExtension { Value = new MultiBinding() }.ProvideValue(EmptyServiceProvider.Instance);
            var priorityBinding = () =>
                new LocalizeEnumExtension { Value = new PriorityBinding() }.ProvideValue(EmptyServiceProvider.Instance);

            multiBinding.Should().Throw<InvalidOperationException>().WithMessage("*MultiBinding*");
            priorityBinding.Should().Throw<InvalidOperationException>().WithMessage("*PriorityBinding*");
        });

    [Fact]
    public void NullBoundValue_RendersEmpty() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            var textBlock = TestSupport.BindLocalizedEnum(
                new() { Value = new Binding(nameof(TestViewModel.SortOrder)) },
                new TestViewModel { SortOrder = null }
            );

            textBlock.Text.Should().BeEmpty();
        });

    [Fact]
    public void ParsedXaml_ShorthandBinding_SetsValue() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            const string xaml = """
                <TextBlock xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                           xmlns:l="clr-namespace:RentADeveloper.ResXLocalization.WPF;assembly=ResXLocalization.WPF"
                           Text="{l:LocalizeEnum {Binding SortOrder}}" />
                """;

            var textBlock = (TextBlock)XamlReader.Parse(xaml);
            textBlock.DataContext = new TestViewModel { SortOrder = FileSortOrder.Ascending };
            TestSupport.Flush();

            textBlock.Text.Should().Be("Ascending");
        });

    [Fact]
    public void PublicSurface_HasTheDeclaredShape() =>
        WpfThread.Invoke(() =>
        {
            var value = typeof(LocalizeEnumExtension).GetProperty(nameof(LocalizeEnumExtension.Value))!;
            var arguments = typeof(LocalizeEnumExtension).GetProperty(nameof(LocalizeEnumExtension.Arguments))!;

            value.PropertyType.Should().Be<object>();
            arguments.PropertyType.GetGenericArguments().Should().Equal(typeof(Argument));
            typeof(LocalizeEnumExtension).GetProperty("Argument").Should().BeNull();
            typeof(LocalizeEnumExtension)
                .GetCustomAttribute<ContentPropertyAttribute>()!
                .Name.Should()
                .Be(nameof(LocalizeEnumExtension.Arguments));
            typeof(LocalizeEnumExtension).GetCustomAttribute<XamlSetMarkupExtensionAttribute>().Should().NotBeNull();
        });

    /// <summary>Creates an unnamed argument.</summary>
    /// <param name="value">The constant or the binding.</param>
    /// <returns>The argument.</returns>
    private static Argument Arg(object? value) => new() { Value = value };

    /// <summary>A service provider without services, for a markup extension created in code.</summary>
    private sealed class EmptyServiceProvider : IServiceProvider
    {
        /// <summary>The shared instance.</summary>
        public static readonly EmptyServiceProvider Instance = new();

        /// <inheritdoc />
        public object? GetService(Type serviceType) => null;
    }
}
