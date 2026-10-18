using System.Reflection;

namespace RentADeveloper.ResXLocalization.Avalonia.Sample.Tests;

/// <summary>
/// Covers the <c>{l:LocalizeEnum}</c> markup extension, matching the sample's card 4: the enumeration value
/// comes from <see cref="LocalizeEnumExtension.Value" /> - a binding, the item-template form
/// <c>Value={Binding}</c>, or a fixed member - the four KeyPrefix × ResourceManager combinations, and format
/// arguments as <see cref="Argument" /> entries.
/// </summary>
public class LocalizeEnumExtensionTests
{
    [AvaloniaFact]
    public void BoundArgument_FormatsTheEnumText_AndFollowsCultureChange()
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
        TestSupport.PumpUi();
        textBlock.Text.Should().Be("3 Dateien, aufsteigend");
    }

    [AvaloniaFact]
    public void BoundValue_FollowsValueAndCultureChanges()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();
        var viewModel = new TestViewModel { SortOrder = FileSortOrder.Ascending };

        var textBlock = TestSupport.BindLocalizedEnum(
            new() { Value = new Binding(nameof(TestViewModel.SortOrder)) },
            viewModel
        );
        textBlock.Text.Should().Be("Ascending");

        viewModel.SortOrder = FileSortOrder.Descending;
        TestSupport.PumpUi();
        textBlock.Text.Should().Be("Descending");

        Localizer.Current.CurrentCulture = TestSupport.German;
        TestSupport.PumpUi();
        textBlock.Text.Should().Be("Absteigend");
    }

    [AvaloniaFact]
    public void BoundValue_SwitchesLive_OnCultureChange()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        var textBlock = TestSupport.BindLocalizedEnum(new() { Value = new Binding() }, FileSortOrder.Descending);
        textBlock.Text.Should().Be("Descending");

        Localizer.Current.CurrentCulture = TestSupport.German;
        TestSupport.PumpUi();
        textBlock.Text.Should().Be("Absteigend");
    }

    [AvaloniaFact]
    public void ConstantValueAndArgument_Format()
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
        TestSupport.PumpUi();
        textBlock.Text.Should().Be("3 Dateien, aufsteigend");
    }

    [AvaloniaFact]
    public void ConstructorArgument_SetsValue()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        var bound = TestSupport.BindLocalizedEnum(
            new(new Binding(nameof(TestViewModel.SortOrder))),
            new TestViewModel { SortOrder = FileSortOrder.Ascending }
        );
        var constant = TestSupport.BindLocalizedEnum(new(FileSortOrder.Ascending), null);
        var text = () => new LocalizeEnumExtension("Ascending").ProvideValue(null!);

        bound.Text.Should().Be("Ascending");
        constant.Text.Should().Be("Ascending");
        text.Should().Throw<InvalidOperationException>().WithMessage("*System.String*");
    }

    [AvaloniaFact]
    public void CustomEnumKeyBuilder_AppliesToTheExtension()
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
    }

    [AvaloniaFact]
    public void CustomPrefix_NoManager_ResolvesViaSearchAll()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        var textBlock = TestSupport.BindLocalizedEnum(
            new() { Value = new Binding(), KeyPrefix = "Display_" },
            FileSortOrder.Ascending
        );

        textBlock.Text.Should().Be("A to Z");
    }

    [AvaloniaFact]
    public void CustomPrefix_WithManager_ResolvesScoped()
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
    }

    [AvaloniaFact]
    public void DefaultPrefix_NoManager_ResolvesViaSearchAll()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        var textBlock = TestSupport.BindLocalizedEnum(new() { Value = new Binding() }, FileSortOrder.Ascending);

        textBlock.Text.Should().Be("Ascending");
    }

    [AvaloniaFact]
    public void DefaultPrefix_WithManager_ResolvesScoped()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        var textBlock = TestSupport.BindLocalizedEnum(
            new() { Value = new Binding(), ResourceManager = ApplicationStringsKeys.ResourceManager },
            FileSortOrder.Ascending
        );

        textBlock.Text.Should().Be("Ascending");
    }

    [AvaloniaFact]
    public void FormatterChange_ReResolvesTheExtensionAndTheConverter_WithoutALanguageSwitch()
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
            converter.Bind(TextBlock.TextProperty, multiBinding);
            TestSupport.PumpUi();

            extension.Text.Should().Be("Ascending");
            converter.Text.Should().Be("Ascending");

            Localizer.Current.MessageFormatter = new ArgumentTests.UpperCaseFormatter();
            TestSupport.PumpUi();

            extension.Text.Should().Be("ASCENDING");
            converter.Text.Should().Be("ASCENDING");
        }
        finally
        {
            Localizer.Current.MessageFormatter = previous;
        }
    }

    [AvaloniaFact]
    public void MissingOrNonEnumValue_Throws()
    {
        var missing = () => new LocalizeEnumExtension().ProvideValue(null!);
        var text = () => new LocalizeEnumExtension { Value = "Ascending" }.ProvideValue(null!);

        missing.Should().Throw<InvalidOperationException>().WithMessage("*Value*");
        text.Should().Throw<InvalidOperationException>().WithMessage("*System.String*");
    }

    [AvaloniaFact]
    public void NullBoundValue_RendersEmpty()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        var textBlock = TestSupport.BindLocalizedEnum(
            new() { Value = new Binding(nameof(TestViewModel.SortOrder)) },
            new TestViewModel { SortOrder = null }
        );

        textBlock.Text.Should().BeEmpty();
    }

    [AvaloniaFact]
    public void PublicSurface_HasTheDeclaredShape()
    {
        var value = typeof(LocalizeEnumExtension).GetProperty(nameof(LocalizeEnumExtension.Value))!;
        var arguments = typeof(LocalizeEnumExtension).GetProperty(nameof(LocalizeEnumExtension.Arguments))!;

        value.PropertyType.Should().Be<object>();
        arguments.PropertyType.GetGenericArguments().Should().Equal(typeof(Argument));
        typeof(LocalizeEnumExtension).GetProperty("Argument").Should().BeNull();
        value.GetCustomAttribute<AssignBindingAttribute>().Should().NotBeNull();
        arguments.GetCustomAttribute<global::Avalonia.Metadata.ContentAttribute>().Should().NotBeNull();
    }

    /// <summary>Creates an unnamed argument.</summary>
    /// <param name="value">The constant or the binding.</param>
    /// <returns>The argument.</returns>
    private static Argument Arg(object? value) => new() { Value = value };
}
