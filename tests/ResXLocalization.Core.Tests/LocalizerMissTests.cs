namespace RentADeveloper.ResXLocalization.Core.Tests;

/// <summary>
/// Exercises the miss diagnostics: the <see cref="ILocalizer.TranslationNotFound" /> event and the
/// configurable <see cref="ILocalizer.MissingTranslationFormat" /> sentinel.
/// </summary>
public class LocalizerMissTests
{
    [Fact]
    public void SearchAllMiss_RaisesTranslationNotFound_WithoutAManager()
    {
        var localizer = this.resources.CreateLocalizer();
        var raised = new List<TranslationNotFoundEventArgs>();
        localizer.TranslationNotFound += (_, args) => raised.Add(args);

        localizer.Get("ThisKeyDoesNotExist");

        var miss = raised.Should().ContainSingle().Subject;
        miss.Key.Should().Be("ThisKeyDoesNotExist");
        miss.Culture.Should().Be(TestResources.English);
        miss.ResourceManager.Should().BeNull();
    }

    [Fact]
    public void ScopedMiss_RaisesTranslationNotFound_WithTheManager()
    {
        var localizer = this.resources.CreateLocalizer();
        var raised = new List<TranslationNotFoundEventArgs>();
        localizer.TranslationNotFound += (_, args) => raised.Add(args);

        localizer.Get("ThisKeyDoesNotExist", this.resources.Catalog);

        var miss = raised.Should().ContainSingle().Subject;
        miss.ResourceManager.Should().BeSameAs(this.resources.Catalog);
    }

    [Fact]
    public void EnumMiss_RaisesTranslationNotFound_WithTheConventionKey()
    {
        var localizer = this.resources.CreateLocalizer();
        var raised = new List<TranslationNotFoundEventArgs>();
        localizer.TranslationNotFound += (_, args) => raised.Add(args);

        localizer.Get(TestSortOrder.Descending);

        raised.Should().ContainSingle().Which.Key.Should().Be("Enum_TestSortOrder_Descending");
    }

    [Fact]
    public void SuccessfulAndEmptyLookups_DoNotRaiseTranslationNotFound()
    {
        var localizer = this.resources.CreateLocalizer();
        var raised = 0;
        localizer.TranslationNotFound += (_, _) => raised++;

        localizer.Get("Greeting");
        localizer.Get("Greeting", this.resources.Catalog);
        localizer.Get(String.Empty);
        _ = localizer["Greeting"];

        raised.Should().Be(0);
    }

    [Fact]
    public void MissingSatelliteEntry_FallsBackToNeutral_WithoutMissEvent()
    {
        var localizer = this.resources.CreateLocalizer();
        localizer.CurrentCulture = TestResources.German;
        var raised = 0;
        localizer.TranslationNotFound += (_, _) => raised++;

        localizer.Get("NeutralOnly", this.resources.Catalog).Should().Be("Neutral fallback value");
        raised.Should().Be(0);
    }

    [Fact]
    public void SpecificCulture_FallsBackToParentCulture_WithoutMissEvent()
    {
        var localizer = this.resources.CreateLocalizer();
        localizer.CurrentCulture = new("de-DE");
        var raised = 0;
        localizer.TranslationNotFound += (_, _) => raised++;

        localizer.Get("Greeting", this.resources.Catalog).Should().Be("Hallo und willkommen!");
        raised.Should().Be(0);
    }

    [Fact]
    public void MissingTranslationFormat_CustomizesTheSentinel()
    {
        var localizer = this.resources.CreateLocalizer();
        localizer.MissingTranslationFormat = "[missing: {0}]";

        localizer.Get("ThisKeyDoesNotExist").Should().Be("[missing: ThisKeyDoesNotExist]");
        localizer.Get("ThisKeyDoesNotExist", this.resources.Catalog)
            .Should().Be("[missing: ThisKeyDoesNotExist]");
    }

    [Fact]
    public void MissingTranslationFormat_DefaultsToBangKeyBang() =>
        new Localizer().MissingTranslationFormat.Should().Be("!{0}!");

    [Fact]
    public void MissingTranslationFormat_RejectsNull()
    {
        var localizer = this.resources.CreateLocalizer();

        var act = () => localizer.MissingTranslationFormat = null!;

        act.Should().Throw<ArgumentNullException>();
        localizer.MissingTranslationFormat.Should().Be("!{0}!");
    }

    [Fact]
    public void MissingTranslationFormat_RejectsAnInvalidFormat_Eagerly()
    {
        var localizer = this.resources.CreateLocalizer();

        // "{1}" references a second argument that the sentinel never supplies: reject it at assignment,
        // not later at the first miss.
        var act = () => localizer.MissingTranslationFormat = "{1}";

        act.Should().Throw<ArgumentException>().WithInnerException<FormatException>();
        localizer.MissingTranslationFormat.Should().Be("!{0}!");
    }

    [Fact]
    public void MissingTranslationFormat_AcceptsAFormatWithoutAPlaceholder()
    {
        var localizer = this.resources.CreateLocalizer();
        localizer.MissingTranslationFormat = "missing";

        localizer.Get("ThisKeyDoesNotExist").Should().Be("missing");
    }

    private readonly TestResources resources = new();
}
