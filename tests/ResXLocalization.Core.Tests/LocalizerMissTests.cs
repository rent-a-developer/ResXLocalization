namespace RentADeveloper.ResXLocalization.Core.Tests;

/// <summary>
/// Exercises the lookup-failure diagnostics: the <see cref="ILocalizer.LookupFailed" /> event and
/// the failure text produced by the configurable <see cref="ILocalizer.LookupFailureTextBuilder" />, for a
/// missing translation and for a formatting failure.
/// </summary>
public class LocalizerMissTests
{
    private readonly TestResources resources = new();

    [Fact]
    public void EnumMiss_RaisesLookupFailed_WithTheConventionKey()
    {
        var localizer = this.resources.CreateLocalizer();
        using var monitor = localizer.Monitor();

        localizer.Get(TestSortOrder.Descending);

        monitor.LookupFailures().Should().ContainSingle().Which.Key.Should().Be("Enum_TestSortOrder_Descending");
    }

    [Fact]
    public void LookupFailureTextBuilder_AcceptsAFunctionWithoutTheKey()
    {
        var localizer = this.resources.CreateLocalizer();
        localizer.LookupFailureTextBuilder = _ => "missing";

        localizer.Get("ThisKeyDoesNotExist").Should().Be("missing");
    }

    [Fact]
    public void LookupFailureTextBuilder_CustomizesTheFailureText()
    {
        var localizer = this.resources.CreateLocalizer();
        localizer.LookupFailureTextBuilder = failure => $"[missing: {failure.Key}]";

        localizer.Get("ThisKeyDoesNotExist").Should().Be("[missing: ThisKeyDoesNotExist]");
        localizer.Get("ThisKeyDoesNotExist", this.resources.Catalog).Should().Be("[missing: ThisKeyDoesNotExist]");
        localizer.Get(TestSortOrder.Descending).Should().Be("[missing: Enum_TestSortOrder_Descending]");
        localizer.Get(string.Empty).Should().BeEmpty();
    }

    [Fact]
    public void LookupFailureTextBuilder_DefaultsToBangKeyBang()
    {
        var first = new Localizer().LookupFailureTextBuilder;

        first.Should().BeSameAs(new Localizer().LookupFailureTextBuilder);
        first(new(LookupFailureReason.TranslationNotFound, "Key", CultureInfo.InvariantCulture, null, [], null, null))
            .Should()
            .Be("!Key!");
    }

    [Fact]
    public void LookupFailureTextBuilder_FormattingFailure_ReceivesTheFailure_AfterLookupFailed()
    {
        var localizer = this.resources.CreateLocalizer();
        localizer.CurrentCulture = TestResources.German;
        using var monitor = localizer.Monitor();
        var calls = new List<LookupFailure>();
        var eventRaisedBeforeCall = false;
        localizer.LookupFailureTextBuilder = failure =>
        {
            calls.Add(failure);
            eventRaisedBeforeCall = monitor.LookupFailures().Length == 1;
            return "[broken]";
        };

        localizer.Get("Malformed", this.resources.Catalog, 1).Should().Be("[broken]");

        eventRaisedBeforeCall.Should().BeTrue();
        var call = calls.Should().ContainSingle().Subject;
        call.Should().Be(monitor.LookupFailures().Single());
        call.Reason.Should().Be(LookupFailureReason.MessageFormatFailed);
        call.Key.Should().Be("Malformed");
        call.Culture.Should().Be(TestResources.German);
        call.ResourceManager.Should().BeSameAs(this.resources.Catalog);
        call.Arguments.Should().Equal(new MessageArgument(0, null, 1));
        call.Pattern.Should().Be("Kaputt {0");
        call.Exception.Should().BeOfType<FormatException>();
    }

    [Fact]
    public void LookupFailureTextBuilder_FormattingFailure_UsesTheDefault()
    {
        var localizer = this.resources.CreateLocalizer();

        localizer.Get("Malformed", 1).Should().Be("!Malformed!");
    }

    [Fact]
    public void LookupFailureTextBuilder_IsNotCalled_ForAResolvedValue()
    {
        var localizer = this.resources.CreateLocalizer();
        var calls = 0;
        localizer.LookupFailureTextBuilder = failure =>
        {
            calls++;
            return failure.Key;
        };

        localizer.Get("Greeting").Should().Be("Hello and welcome!");
        calls.Should().Be(0);
    }

    [Fact]
    public void LookupFailureTextBuilder_NullResult_Throws()
    {
        var localizer = this.resources.CreateLocalizer();
        localizer.LookupFailureTextBuilder = _ => null!;

        var miss = () => localizer.Get("ThisKeyDoesNotExist");
        var formatFailure = () => localizer.Get("Malformed", 1);

        miss.Should()
            .Throw<InvalidOperationException>()
            .Which.Message.Should()
            .ContainAll("LookupFailureTextBuilder", "TranslationNotFound", "ThisKeyDoesNotExist");
        formatFailure
            .Should()
            .Throw<InvalidOperationException>()
            .Which.Message.Should()
            .ContainAll("LookupFailureTextBuilder", "MessageFormatFailed", "Malformed");
    }

    [Fact]
    public void LookupFailureTextBuilder_ReceivesTheDetailsOfTheMiss()
    {
        var localizer = this.resources.CreateLocalizer();
        localizer.CurrentCulture = TestResources.German;
        using var monitor = localizer.Monitor();
        var calls = new List<LookupFailure>();
        localizer.LookupFailureTextBuilder = failure =>
        {
            calls.Add(failure);
            return string.Empty;
        };
        var catalog = this.resources.Catalog;

        localizer.Get("ThisKeyDoesNotExist");
        localizer.Get("ThisKeyDoesNotExist", catalog);
        localizer.Get(new ResourceKey("ThisKeyDoesNotExist", catalog));
        localizer.Get(TestSortOrder.Descending, catalog);

        calls
            .Select(call => (call.Key, call.Culture, call.ResourceManager))
            .Should()
            .Equal(
                ("ThisKeyDoesNotExist", TestResources.German, (ResourceManager?)null),
                ("ThisKeyDoesNotExist", TestResources.German, catalog),
                ("ThisKeyDoesNotExist", TestResources.German, catalog),
                ("Enum_TestSortOrder_Descending", TestResources.German, catalog)
            );
        calls
            .Should()
            .AllSatisfy(call =>
            {
                call.Reason.Should().Be(LookupFailureReason.TranslationNotFound);
                call.Arguments.Should().BeEmpty();
                call.Pattern.Should().BeNull();
                call.Exception.Should().BeNull();
            });
        calls.Should().Equal(monitor.LookupFailures());
    }

    [Fact]
    public void LookupFailureTextBuilder_RejectsNull()
    {
        var localizer = this.resources.CreateLocalizer();
        var builder = localizer.LookupFailureTextBuilder;

        var act = () => localizer.LookupFailureTextBuilder = null!;

        act.Should().Throw<ArgumentNullException>();
        localizer.LookupFailureTextBuilder.Should().BeSameAs(builder);
    }

    [Fact]
    public void LookupFailureTextBuilder_ThrowingFunction_Propagates_AfterTheEvent()
    {
        var localizer = this.resources.CreateLocalizer();
        var exception = new InvalidOperationException("boom");
        localizer.LookupFailureTextBuilder = _ => throw exception;
        using var monitor = localizer.Monitor();

        var miss = () => localizer.Get("ThisKeyDoesNotExist");
        var formatFailure = () => localizer.Get("Malformed", 1);

        miss.Should().Throw<InvalidOperationException>().Which.Should().BeSameAs(exception);
        monitor.LookupFailures().Should().ContainSingle();
        formatFailure.Should().Throw<InvalidOperationException>().Which.Should().BeSameAs(exception);
        monitor.LookupFailures().Should().HaveCount(2);
    }

    [Fact]
    public void MissingSatelliteEntry_FallsBackToNeutral_WithoutMissEvent()
    {
        var localizer = this.resources.CreateLocalizer();
        localizer.CurrentCulture = TestResources.German;
        using var monitor = localizer.Monitor();

        localizer.Get("NeutralOnly", this.resources.Catalog).Should().Be("Neutral fallback value");
        monitor.Should().NotRaise(nameof(ILocalizer.LookupFailed));
    }

    [Fact]
    public void ScopedMiss_RaisesLookupFailed_WithTheManager()
    {
        var localizer = this.resources.CreateLocalizer();
        using var monitor = localizer.Monitor();

        localizer.Get("ThisKeyDoesNotExist", this.resources.Catalog);

        var miss = monitor.LookupFailures().Should().ContainSingle().Subject;
        miss.ResourceManager.Should().BeSameAs(this.resources.Catalog);
    }

    [Fact]
    public void SearchAllMiss_RaisesLookupFailed_WithoutAManager()
    {
        var localizer = this.resources.CreateLocalizer();
        using var monitor = localizer.Monitor();

        localizer.Get("ThisKeyDoesNotExist");

        var miss = monitor.LookupFailures().Should().ContainSingle().Subject;
        miss.Key.Should().Be("ThisKeyDoesNotExist");
        miss.Culture.Should().Be(TestResources.English);
        miss.ResourceManager.Should().BeNull();
    }

    [Fact]
    public void SpecificCulture_FallsBackToParentCulture_WithoutMissEvent()
    {
        var localizer = this.resources.CreateLocalizer();
        localizer.CurrentCulture = new("de-DE");
        using var monitor = localizer.Monitor();

        localizer.Get("Greeting", this.resources.Catalog).Should().Be("Hallo und willkommen!");
        monitor.Should().NotRaise(nameof(ILocalizer.LookupFailed));
    }

    [Fact]
    public void SuccessfulAndEmptyLookups_DoNotRaiseLookupFailed()
    {
        var localizer = this.resources.CreateLocalizer();
        using var monitor = localizer.Monitor();

        localizer.Get("Greeting");
        localizer.Get("Greeting", this.resources.Catalog);
        localizer.Get(string.Empty);
        _ = localizer["Greeting"];

        monitor.Should().NotRaise(nameof(ILocalizer.LookupFailed));
    }
}
