namespace RentADeveloper.ResXLocalization.WPF.Sample.Tests;

/// <summary>
/// Covers the four configurations of <see cref="LocalizeEnumConverter" /> (the "enum bound as a value"
/// path, card 5), both by calling <c>Convert</c> directly and through a live <c>MultiBinding</c>.
/// </summary>
public class LocalizeEnumConverterTests
{
    [Fact]
    public void CustomPrefix_NoManager_ResolvesViaSearchAll() => WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            var converter = new LocalizeEnumConverter { KeyPrefix = "Display_" };

            Convert(converter, FileSortOrder.Ascending).Should().Be("A to Z");
        }
    );

    [Fact]
    public void CustomPrefix_WithManager_ResolvesScoped() => WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            var converter = new LocalizeEnumConverter
            {
                KeyPrefix = "Display_",
                ResourceManager = SortingStrings.ResourceManager
            };

            Convert(converter, FileSortOrder.Ascending).Should().Be("A to Z");
        }
    );

    [Fact]
    public void Default_NoPrefix_NoManager_ResolvesViaSearchAll() => WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            Convert(LocalizeEnumConverter.Default, FileSortOrder.Ascending).Should().Be("Ascending");
        }
    );

    [Fact]
    public void DefaultPrefix_WithManager_ResolvesScoped() => WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            var converter = new LocalizeEnumConverter { ResourceManager = ApplicationStrings.ResourceManager };

            Convert(converter, FileSortOrder.Ascending).Should().Be("Ascending");
        }
    );

    [Fact]
    public void MultiBinding_SwitchesLive_OnCultureChange() => WpfThread.Invoke(() =>
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
        }
    );

    [Fact]
    public void NonEnumOrEmptyInput_ReturnsEmptyString() => WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            Convert(LocalizeEnumConverter.Default).Should().Be(String.Empty);
            LocalizeEnumConverter.Default.Convert(["not an enum"], typeof(String), null, CultureInfo.InvariantCulture)
                .Should()
                .Be(String.Empty);
        }
    );

    [Fact]
    public void SharedDefaultInstance_IsReadOnly() => WpfThread.Invoke(() =>
        {
            // Mutating the process-wide Default would silently reconfigure every default conversion
            // in the app, so it must refuse; a private instance stays fully configurable.
            ((Action)(() => LocalizeEnumConverter.Default.KeyPrefix = "Display_"))
                .Should().Throw<InvalidOperationException>();
            ((Action)(() => LocalizeEnumConverter.Default.ResourceManager = ApplicationStrings.ResourceManager))
                .Should().Throw<InvalidOperationException>();

            var own = new LocalizeEnumConverter { KeyPrefix = "Display_" };
            own.KeyPrefix.Should().Be("Display_");
        }
    );

    private static String Convert(LocalizeEnumConverter converter, params Object?[] values) =>
        (String)converter.Convert(values, typeof(String), null, CultureInfo.InvariantCulture);

    private sealed class Marker(FileSortOrder value)
    {
        public FileSortOrder Value { get; } = value;
    }
}
