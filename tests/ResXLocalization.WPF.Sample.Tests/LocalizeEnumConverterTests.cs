namespace RentADeveloper.ResXLocalization.WPF.Sample.Tests;

/// <summary>
/// Covers the four configurations of <see cref="LocalizeEnumConverter" /> (the "enum bound as a value"
/// path, card 5), both by calling <c>Convert</c> directly and through a live <c>MultiBinding</c>.
/// </summary>
public class LocalizeEnumConverterTests
{
    [Fact]
    public void CustomEnumKeyBuilder_AppliesToTheConverter() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();
            var original = Localizer.Current.EnumKeyBuilder;
            Localizer.Current.EnumKeyBuilder = static (value, keyPrefix) =>
                (keyPrefix ?? "Text_") + value.GetType().Name + "." + value;

            try
            {
                Convert(new LocalizeEnumConverter(), FileSortOrder.Ascending, TestSupport.English)
                    .Should()
                    .Be("Custom ascending");
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

            var converter = new LocalizeEnumConverter { KeyPrefix = "Display_" };

            Convert(converter, FileSortOrder.Ascending).Should().Be("A to Z");
        });

    [Fact]
    public void CustomPrefix_WithManager_ResolvesScoped() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            var converter = new LocalizeEnumConverter
            {
                KeyPrefix = "Display_",
                ResourceManager = SortingStringsKeys.ResourceManager,
            };

            Convert(converter, FileSortOrder.Ascending).Should().Be("A to Z");
        });

    [Fact]
    public void DefaultPrefix_WithManager_ResolvesScoped() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            var converter = new LocalizeEnumConverter { ResourceManager = ApplicationStringsKeys.ResourceManager };

            Convert(converter, FileSortOrder.Ascending).Should().Be("Ascending");
        });

    [Fact]
    public void Default_NoPrefix_NoManager_ResolvesViaSearchAll() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            Convert(LocalizeEnumConverter.Default, FileSortOrder.Ascending).Should().Be("Ascending");
        });

    [Fact]
    public void KeyPrefix_DefaultsToNull_AndAcceptsNull_ButNotOnTheSharedInstance() =>
        WpfThread.Invoke(() =>
        {
            var converter = new LocalizeEnumConverter();
            converter.KeyPrefix.Should().BeNull();

            converter.KeyPrefix = "Display_";
            converter.KeyPrefix = null;

            converter.KeyPrefix.Should().BeNull();
            ((Action)(() => LocalizeEnumConverter.Default.KeyPrefix = null))
                .Should()
                .Throw<InvalidOperationException>();
        });

    [Fact]
    public void MultiBinding_SwitchesLive_OnCultureChange() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            var multiBinding = new MultiBinding { Converter = LocalizeEnumConverter.Default };
            multiBinding.Bindings.Add(
                new Binding(nameof(Marker.Value)) { Source = new Marker(FileSortOrder.Ascending) }
            );
            multiBinding.Bindings.Add(new Binding(nameof(ILocalizer.CurrentCulture)) { Source = Localizer.Current });

            var textBlock = new TextBlock();
            textBlock.SetBinding(TextBlock.TextProperty, multiBinding);
            TestSupport.Flush();
            textBlock.Text.Should().Be("Ascending");

            Localizer.Current.CurrentCulture = TestSupport.German;
            TestSupport.Flush();
            textBlock.Text.Should().Be("Aufsteigend");
        });

    [Fact]
    public void NonEnumOrEmptyInput_ReturnsEmptyString() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            Convert(LocalizeEnumConverter.Default).Should().Be(string.Empty);
            LocalizeEnumConverter
                .Default.Convert(["not an enum"], typeof(string), null, CultureInfo.InvariantCulture)
                .Should()
                .Be(string.Empty);
        });

    [Fact]
    public void SharedDefaultInstance_IsReadOnly() =>
        WpfThread.Invoke(() =>
        {
            // Mutating the process-wide Default would silently reconfigure every default conversion
            // in the app, so it must refuse; a private instance stays fully configurable.
            ((Action)(() => LocalizeEnumConverter.Default.KeyPrefix = "Display_"))
                .Should()
                .Throw<InvalidOperationException>();
            ((Action)(() => LocalizeEnumConverter.Default.ResourceManager = ApplicationStringsKeys.ResourceManager))
                .Should()
                .Throw<InvalidOperationException>();

            var own = new LocalizeEnumConverter { KeyPrefix = "Display_" };
            own.KeyPrefix.Should().Be("Display_");
        });

    private static string Convert(LocalizeEnumConverter converter, params object?[] values) =>
        (string)converter.Convert(values, typeof(string), null, CultureInfo.InvariantCulture);

    private sealed class Marker(FileSortOrder value)
    {
        public FileSortOrder Value { get; } = value;
    }
}
