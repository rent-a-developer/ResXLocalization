using RentADeveloper.ResXLocalization.Core.Tests;

namespace RentADeveloper.ResXLocalization.Avalonia.Sample.Tests;

/// <summary>
/// Covers <see cref="LocalizeEnumConverter" /> (the hand-written <c>MultiBinding</c> path, card 5), both by
/// calling <c>Convert</c> directly and through a live <c>MultiBinding</c>: value 0 is the refresh trigger,
/// value 1 the enumeration value, and values 2 and up the format arguments, named by
/// <see cref="LocalizeEnumConverter.ArgumentNames" />.
/// </summary>
public class LocalizeEnumConverterTests
{
    [AvaloniaFact]
    public void ArgumentNames_DuplicateName_OrSharedInstance_Throws()
    {
        var duplicate = () => new LocalizeEnumConverter { ArgumentNames = "A,A" };
        var differentCase = () => new LocalizeEnumConverter { ArgumentNames = "A,a" };
        var shared = () => LocalizeEnumConverter.Default.ArgumentNames = "A";

        duplicate.Should().Throw<ArgumentException>().WithMessage("*A*");
        differentCase.Should().NotThrow();
        shared.Should().Throw<InvalidOperationException>();
    }

    [AvaloniaFact]
    public void ArgumentNames_NameTheArgumentsByPosition()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();
        var previous = Localizer.Current.MessageFormatter;
        var formatter = new CapturingFormatter();
        Localizer.Current.MessageFormatter = formatter;

        try
        {
            var converter = new LocalizeEnumConverter { KeyPrefix = "Count_", ArgumentNames = " Count , Total" };

            Convert(converter, 0, FileSortOrder.Ascending, 3, 10).Should().Be("3 files, ascending");
            converter.ArgumentNames.Should().Be(" Count , Total");
            formatter
                .Calls[^1]
                .Arguments.Should()
                .Equal(new MessageArgument(0, "Count", 3), new MessageArgument(1, "Total", 10));

            converter.ArgumentNames = ",Total";
            Convert(converter, 0, FileSortOrder.Ascending, 3, 10).Should().Be("3 files, ascending");
            converter.ArgumentNames.Should().Be(",Total");
            formatter
                .Calls[^1]
                .Arguments.Should()
                .Equal(new MessageArgument(0, null, 3), new MessageArgument(1, "Total", 10));

            converter.ArgumentNames = "Count,Total,Extra";
            Convert(converter, 0, FileSortOrder.Ascending, 3).Should().Be("3 files, ascending");
            converter.ArgumentNames.Should().Be("Count,Total,Extra");
            formatter.Calls[^1].Arguments.Should().Equal(new MessageArgument(0, "Count", 3));
        }
        finally
        {
            Localizer.Current.MessageFormatter = previous;
        }
    }

    [AvaloniaFact]
    public void CustomEnumKeyBuilder_AppliesToTheConverter()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();
        var original = Localizer.Current.EnumKeyBuilder;
        Localizer.Current.EnumKeyBuilder = static (value, keyPrefix) =>
            (keyPrefix ?? "Text_") + value.GetType().Name + "." + value;

        try
        {
            Convert(new LocalizeEnumConverter(), 0, FileSortOrder.Ascending).Should().Be("Custom ascending");
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

        var converter = new LocalizeEnumConverter { KeyPrefix = "Display_" };

        Convert(converter, 0, FileSortOrder.Ascending).Should().Be("A to Z");
    }

    [AvaloniaFact]
    public void CustomPrefix_WithManager_ResolvesScoped()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        var converter = new LocalizeEnumConverter
        {
            KeyPrefix = "Display_",
            ResourceManager = SortingStringsKeys.ResourceManager,
        };

        Convert(converter, 0, FileSortOrder.Ascending).Should().Be("A to Z");
    }

    [AvaloniaFact]
    public void DefaultPrefix_WithManager_ResolvesScoped()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        var converter = new LocalizeEnumConverter { ResourceManager = ApplicationStringsKeys.ResourceManager };

        Convert(converter, 0, FileSortOrder.Ascending).Should().Be("Ascending");
    }

    [AvaloniaFact]
    public void Default_NoPrefix_NoManager_ResolvesViaSearchAll()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        Convert(LocalizeEnumConverter.Default, 0, FileSortOrder.Ascending).Should().Be("Ascending");
    }

    [AvaloniaFact]
    public void KeyPrefix_DefaultsToNull_AndAcceptsNull_ButNotOnTheSharedInstance()
    {
        var converter = new LocalizeEnumConverter();
        converter.KeyPrefix.Should().BeNull();

        converter.KeyPrefix = "Display_";
        converter.KeyPrefix = null;

        converter.KeyPrefix.Should().BeNull();
        ((Action)(() => LocalizeEnumConverter.Default.KeyPrefix = null)).Should().Throw<InvalidOperationException>();
    }

    [AvaloniaFact]
    public void MultiBinding_SwitchesLive_OnCultureChange()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        var multiBinding = new MultiBinding { Converter = LocalizeEnumConverter.Default };
        multiBinding.Bindings.Add(
            new Binding(nameof(ILocalizer.RefreshTriggeringRevision)) { Source = Localizer.Current }
        );
        multiBinding.Bindings.Add(new Binding(nameof(Marker.Value)) { Source = new Marker(FileSortOrder.Ascending) });

        var textBlock = new TextBlock();
        textBlock.Bind(TextBlock.TextProperty, multiBinding);
        TestSupport.PumpUi();
        textBlock.Text.Should().Be("Ascending");

        Localizer.Current.CurrentCulture = TestSupport.German;
        TestSupport.PumpUi();
        textBlock.Text.Should().Be("Aufsteigend");
    }

    [AvaloniaFact]
    public void NonEnumOrEmptyInput_ReturnsEmptyString()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        Convert(LocalizeEnumConverter.Default, 0, "not an enum").Should().Be(string.Empty);
        Convert(LocalizeEnumConverter.Default, 0, null).Should().Be(string.Empty);
    }

    [AvaloniaFact]
    public void OldChildOrderOrTooFewValues_ReturnEmptyString()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        Convert(LocalizeEnumConverter.Default, FileSortOrder.Ascending, TestSupport.English).Should().BeEmpty();
        Convert(LocalizeEnumConverter.Default, 0).Should().BeEmpty();
        Convert(LocalizeEnumConverter.Default).Should().BeEmpty();
    }

    [AvaloniaFact]
    public void OneArgument_FormatsTheEnumText()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        Convert(new() { KeyPrefix = "Count_" }, 0, FileSortOrder.Ascending, 3).Should().Be("3 files, ascending");
    }

    [AvaloniaFact]
    public void SharedDefaultInstance_IsReadOnly()
    {
        // Mutating the process-wide Default would silently reconfigure every default conversion in
        // the app, so it must refuse; a private instance stays fully configurable.
        ((Action)(() => LocalizeEnumConverter.Default.KeyPrefix = "Display_"))
            .Should()
            .Throw<InvalidOperationException>();
        ((Action)(() => LocalizeEnumConverter.Default.ResourceManager = ApplicationStringsKeys.ResourceManager))
            .Should()
            .Throw<InvalidOperationException>();

        var own = new LocalizeEnumConverter { KeyPrefix = "Display_" };
        own.KeyPrefix.Should().Be("Display_");
    }

    [AvaloniaFact]
    public void TriggerSlot_IsNeverAnArgument()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        Convert(new() { KeyPrefix = "Count_" }, "not-an-argument", FileSortOrder.Ascending)
            .Should()
            .Be("{0} files, ascending");
    }

    [AvaloniaFact]
    public void TwoValues_DoNotFormat_KeepingLiteralBraces()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();
        using var monitor = Localizer.Current.Monitor();

        Convert(new() { KeyPrefix = "Braces_" }, 0, FileSortOrder.Ascending).Should().Be("{Ascending}");

        monitor.EventLog().Should().BeEmpty();
    }

    [AvaloniaFact]
    public void UnsetArgument_FormatsAsEmpty()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        Convert(new() { KeyPrefix = "Count_" }, 0, FileSortOrder.Ascending, AvaloniaProperty.UnsetValue)
            .Should()
            .Be(" files, ascending");
    }

    private static string Convert(LocalizeEnumConverter converter, params object?[] values) =>
        (string)converter.Convert(values, typeof(string), null, CultureInfo.InvariantCulture);

    private sealed class Marker(FileSortOrder value)
    {
        public FileSortOrder Value { get; } = value;
    }
}
