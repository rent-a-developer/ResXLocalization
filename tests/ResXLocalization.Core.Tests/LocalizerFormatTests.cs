namespace RentADeveloper.ResXLocalization.Core.Tests;

/// <summary>
/// Exercises the format-argument overloads and the formatting pipeline: every resolved value passes
/// through <see cref="ILocalizer.MessageFormatter" />, which by default formats it as a composite format
/// string in the localizer's current culture when the lookup has arguments, and a formatting failure
/// returns the failure text and raises <see cref="ILocalizer.LookupFailed" />.
/// </summary>
public class LocalizerFormatTests
{
    private readonly TestResources resources = new();

    [Fact]
    public void EmptyArgumentsArray_ReturnsTheValueUnformatted()
    {
        var localizer = this.resources.CreateLocalizer();
        using var monitor = localizer.Monitor();

        localizer.Get("EscapedBraces", MessageArguments.FromValues()).Should().Be("Literal {{braces}} for {0}");
        monitor.Should().NotRaise(nameof(ILocalizer.LookupFailed));

        localizer.Get("EscapedBraces", MessageArguments.FromValues("x")).Should().Be("Literal {braces} for x");
    }

    [Fact]
    public void EmptyKey_ReturnsEmptyString()
    {
        var localizer = this.resources.CreateLocalizer();

        localizer.Get(string.Empty, MessageArguments.FromValues(1, 2)).Should().BeEmpty();
    }

    [Fact]
    public void FormatterException_OfAnyType_ReturnsTheFailureText_WithThatException()
    {
        var localizer = this.resources.CreateLocalizer();
        var exception = new InvalidOperationException("boom");
        localizer.MessageFormatter = new ThrowingFormatter(exception);
        using var monitor = localizer.Monitor();

        localizer.Get("Greeting", MessageArguments.FromValues(1)).Should().Be("!Greeting!");

        monitor.LookupFailures().Should().ContainSingle().Which.Exception.Should().BeSameAs(exception);
    }

    [Fact]
    public void FormatterException_WithoutArguments_ReturnsTheFailureText_AndRaisesLookupFailed()
    {
        var localizer = this.resources.CreateLocalizer();
        var exception = new InvalidOperationException("boom");
        localizer.MessageFormatter = new ThrowingFormatter(exception);
        using var monitor = localizer.Monitor();

        localizer.Get("Greeting").Should().Be("!Greeting!");
        localizer.Get("Greeting", resourceManager: this.resources.Catalog).Should().Be("!Greeting!");

        var raised = monitor.LookupFailures();
        raised.Should().HaveCount(2);
        raised
            .Should()
            .AllSatisfy(failure =>
            {
                failure.Reason.Should().Be(LookupFailureReason.MessageFormatFailed);
                failure.Arguments.Should().BeEmpty();
                failure.Exception.Should().BeSameAs(exception);
            });
        raised[0].ResourceManager.Should().BeNull();
        raised[1].ResourceManager.Should().BeSameAs(this.resources.Catalog);
    }

    [Fact]
    public void Formatter_ReceivesThePatternTheArgumentsAndTheCulture()
    {
        var localizer = this.resources.CreateLocalizer();
        var formatter = new CapturingFormatter();
        localizer.MessageFormatter = formatter;

        localizer.Get("ItemsFound", MessageArguments.FromValues(1, "x"));

        var call = formatter.Calls.Should().ContainSingle().Subject;
        call.Pattern.Should().Be("Found {0:N1} items in {1}.");
        call.Arguments.Should().Equal(new MessageArgument(0, null, 1), new MessageArgument(1, null, "x"));
        call.Culture.Should().Be(TestResources.English);
    }

    [Fact]
    public void FormattingFailure_InTheCurrentCulture_ReportsThatCultureAndPattern()
    {
        var localizer = this.resources.CreateLocalizer();
        localizer.CurrentCulture = TestResources.German;
        using var monitor = localizer.Monitor();

        localizer.Get("Malformed", MessageArguments.FromValues(1)).Should().Be("!Malformed!");

        var failure = monitor.LookupFailures().Should().ContainSingle().Subject;
        failure.Culture.Should().Be(TestResources.German);
        failure.Pattern.Should().Be("Kaputt {0");
    }

    [Fact]
    public void FormattingFailure_ReturnsTheFailureText_AndRaisesLookupFailed()
    {
        var localizer = this.resources.CreateLocalizer();
        using var monitor = localizer.Monitor();

        // The template needs two arguments; one is a resource defect. It is reported, not thrown.
        localizer.Get("ItemsFound", MessageArguments.FromValues("only-one")).Should().Be("!ItemsFound!");

        var failure = monitor.LookupFailures().Should().ContainSingle().Subject;
        failure.Reason.Should().Be(LookupFailureReason.MessageFormatFailed);
        failure.Key.Should().Be("ItemsFound");
        failure.Culture.Should().Be(TestResources.English);
        failure.ResourceManager.Should().BeNull();
        failure.Pattern.Should().Be("Found {0:N1} items in {1}.");
        failure.Arguments.Should().Equal(new MessageArgument(0, null, "only-one"));
        failure.Exception.Should().BeOfType<FormatException>();
    }

    [Fact]
    public void FormattingFailure_ScopedAndTyped_ReportsTheManager()
    {
        var localizer = this.resources.CreateLocalizer();
        using var monitor = localizer.Monitor();

        localizer.Get("Malformed", MessageArguments.FromValues(1), this.resources.Catalog).Should().Be("!Malformed!");
        localizer
            .Get(new ResourceKey("Malformed", this.resources.Catalog), MessageArguments.FromValues(1))
            .Should()
            .Be("!Malformed!");

        monitor
            .LookupFailures()
            .Should()
            .HaveCount(2)
            .And.AllSatisfy(failure => failure.ResourceManager.Should().BeSameAs(this.resources.Catalog));
    }

    [Fact]
    public void MessageFormatter_DefaultsToAFormatter_AndRejectsNull()
    {
        var localizer = new Localizer();
        var formatter = localizer.MessageFormatter;

        formatter.Should().NotBeNull();

        var act = () => localizer.MessageFormatter = null!;

        act.Should().Throw<ArgumentNullException>();
        localizer.MessageFormatter.Should().BeSameAs(formatter);
    }

    [Fact]
    public void MissingKey_ReportsTranslationNotFound_NotMessageFormatFailed()
    {
        var localizer = this.resources.CreateLocalizer();
        using var monitor = localizer.Monitor();

        localizer.Get("ThisKeyDoesNotExist", MessageArguments.FromValues(1, 2)).Should().Be("!ThisKeyDoesNotExist!");
        localizer
            .Get("ThisKeyDoesNotExist", MessageArguments.FromValues(1, 2), this.resources.Catalog)
            .Should()
            .Be("!ThisKeyDoesNotExist!");

        monitor
            .LookupFailures()
            .Should()
            .HaveCount(2)
            .And.AllSatisfy(failure => failure.Reason.Should().Be(LookupFailureReason.TranslationNotFound));
    }

    [Fact]
    public void MissingKey_ReturnsTheFailureTextUnformatted()
    {
        var localizer = this.resources.CreateLocalizer();

        localizer.Get("ThisKeyDoesNotExist", MessageArguments.FromValues(1, 2)).Should().Be("!ThisKeyDoesNotExist!");
        localizer
            .Get("ThisKeyDoesNotExist", MessageArguments.FromValues(1, 2), this.resources.Catalog)
            .Should()
            .Be("!ThisKeyDoesNotExist!");
    }

    [Fact]
    public void MissingKey_TheFailureText_NeverReachesTheFormatter()
    {
        var localizer = this.resources.CreateLocalizer();
        localizer.LookupFailureTextBuilder = failure => $"{{{failure.Key}}}";
        var formatter = new CapturingFormatter();
        localizer.MessageFormatter = formatter;
        using var monitor = localizer.Monitor();

        localizer.Get("ThisKeyDoesNotExist", MessageArguments.FromValues(1)).Should().Be("{ThisKeyDoesNotExist}");

        formatter.Calls.Should().BeEmpty();
        monitor
            .LookupFailures()
            .Should()
            .ContainSingle()
            .Which.Reason.Should()
            .Be(LookupFailureReason.TranslationNotFound);
    }

    [Fact]
    public void NoArgumentMembers_PassTheValueThroughTheFormatter_WithAnEmptyArray()
    {
        var localizer = this.resources.CreateLocalizer();
        var formatter = new CapturingFormatter();
        localizer.MessageFormatter = formatter;
        var catalog = this.resources.Catalog;

        string[] results =
        [
            localizer.Get("Greeting"),
            localizer["Greeting"],
            localizer.Get("Greeting", resourceManager: catalog),
            localizer.Get(new ResourceKey("Greeting", catalog)),
            localizer.Get(TestSortOrder.Ascending),
            localizer.Get(TestSortOrder.Ascending, resourceManager: catalog),
        ];
        localizer.Get("ThisKeyDoesNotExist");
        localizer.Get(string.Empty);

        formatter
            .Calls.Select(call => call.Pattern)
            .Should()
            .Equal(
                "Hello and welcome!",
                "Hello and welcome!",
                "Hello and welcome!",
                "Hello and welcome!",
                "Ascending",
                "Ascending"
            );
        formatter
            .Calls.Should()
            .AllSatisfy(call =>
            {
                call.Arguments.Should().BeEmpty();
                call.Culture.Should().Be(TestResources.English);
            });
        results.Should().Equal(formatter.Calls.Select(call => call.Pattern));
    }

    [Fact]
    public void NoArguments_InEveryForm_PassEmptyArgumentsToTheFormatter()
    {
        var localizer = this.resources.CreateLocalizer();
        var formatter = new CapturingFormatter();
        localizer.MessageFormatter = formatter;

        string[] results =
        [
            localizer.Get("Greeting"),
            localizer.Get("Greeting", null),
            localizer.Get("Greeting", MessageArguments.Empty),
            localizer.Get("Greeting", null, null),
            localizer.Get("Greeting", resourceManager: this.resources.Catalog),
            localizer["Greeting"],
        ];

        results.Should().AllBe("Hello and welcome!");
        formatter.Calls.Should().HaveCount(6).And.AllSatisfy(call => call.Arguments.Should().NotBeNull().And.BeEmpty());
    }

    [Fact]
    public void NullManager_MeansSearchAll_ButADefaultResourceKeyThrows()
    {
        var localizer = this.resources.CreateLocalizer();

        localizer.Get("Greeting", resourceManager: null).Should().Be("Hello and welcome!");
        localizer.Get(TestSortOrder.Ascending, resourceManager: null).Should().Be("Ascending");

        var typed = () => localizer.Get(default(ResourceKey));
        var typedWithArguments = () => localizer.Get(default(ResourceKey), MessageArguments.FromValues(1));

        typed.Should().Throw<ArgumentNullException>();
        typedWithArguments.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void PositionalAndNamedArguments_FormatByPosition_UnderTheDefaultFormatter()
    {
        var localizer = this.resources.CreateLocalizer();

        localizer.Get("FilePosition", MessageArguments.FromValues(3, 10)).Should().Be("File 3 of 10");
        localizer
            .Get("ItemsFound", MessageArguments.FromObject(new { Count = 1.5, Where = "x" }))
            .Should()
            .Be("Found 1.5 items in x.");
        localizer
            .Get(
                new ResourceKey("ItemsFound", this.resources.Catalog),
                MessageArguments.FromTuples(("Count", 1.5), ("Where", "x"))
            )
            .Should()
            .Be("Found 1.5 items in x.");
    }

    [Fact]
    public void Positions_NotTheEnumerationIndex_DecideWhichPlaceholderAValueFills()
    {
        var localizer = this.resources.CreateLocalizer();

        localizer
            .Get("FilePosition", MessageArguments.FromArguments(new(1, null, 10), new(0, null, 3)))
            .Should()
            .Be("File 3 of 10");
    }

    [Fact]
    public void Primitive_TreatsNullArgumentsAsNone_AndReturnsEmptyForAnEmptyKey()
    {
        var localizer = this.resources.CreateLocalizer();

        localizer.Get("Greeting", null, null).Should().Be("Hello and welcome!");
        localizer.Get(string.Empty, MessageArguments.FromValues(1), null).Should().BeEmpty();
        localizer.Get("Greeting", MessageArguments.Empty, null).Should().Be("Hello and welcome!");
    }

    [Fact]
    public void Scoped_FormatsTheResolvedValue()
    {
        var localizer = this.resources.CreateLocalizer();

        localizer
            .Get("ItemsFound", MessageArguments.FromValues(2.0, "Music"), this.resources.Catalog)
            .Should()
            .Be("Found 2.0 items in Music.");
    }

    [Fact]
    public void SearchAll_FormatsTheResolvedValue_InTheCurrentCulture()
    {
        var localizer = this.resources.CreateLocalizer();

        // {0:N1} renders 1234.5 with an English decimal point...
        localizer
            .Get("ItemsFound", MessageArguments.FromValues(1234.5, "Downloads"))
            .Should()
            .Be("Found 1,234.5 items in Downloads.");

        // ...and with a German decimal comma after the switch, using the German template.
        localizer.CurrentCulture = TestResources.German;
        localizer
            .Get("ItemsFound", MessageArguments.FromValues(1234.5, "Downloads"))
            .Should()
            .Be("1.234,5 Elemente in Downloads gefunden.");
    }

    [Fact]
    public void TypedKey_FormatsTheResolvedValue()
    {
        var localizer = this.resources.CreateLocalizer();
        var key = new ResourceKey("ItemsFound", this.resources.Catalog);

        localizer.Get(key, MessageArguments.FromValues(2.0, "Music")).Should().Be("Found 2.0 items in Music.");
    }

    /// <summary>A formatter that throws the given exception for every call.</summary>
    /// <param name="exception">The exception to throw.</param>
    private sealed class ThrowingFormatter(Exception exception) : IMessageFormatter
    {
        private readonly Exception exception = exception;

        public string Format(string pattern, MessageArguments arguments, CultureInfo culture) => throw this.exception;
    }
}
