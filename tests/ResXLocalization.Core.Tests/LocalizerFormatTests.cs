namespace RentADeveloper.ResXLocalization.Core.Tests;

/// <summary>
/// Exercises the format-argument overloads: the resolved resource value is treated as a composite
/// format string and formatted with the localizer's current culture, so numbers and dates inside a
/// translation follow the selected language.
/// </summary>
public class LocalizerFormatTests
{
    [Fact]
    public void SearchAll_FormatsTheResolvedValue_InTheCurrentCulture()
    {
        var localizer = this.resources.CreateLocalizer();

        // {0:N1} renders 1234.5 with an English decimal point...
        localizer.Get("ItemsFound", 1234.5, "Downloads").Should().Be("Found 1,234.5 items in Downloads.");

        // ...and with a German decimal comma after the switch, using the German template.
        localizer.CurrentCulture = TestResources.German;
        localizer.Get("ItemsFound", 1234.5, "Downloads").Should().Be("1.234,5 Elemente in Downloads gefunden.");
    }

    [Fact]
    public void Scoped_FormatsTheResolvedValue()
    {
        var localizer = this.resources.CreateLocalizer();

        localizer.Get("ItemsFound", this.resources.Catalog, 2.0, "Music")
            .Should().Be("Found 2.0 items in Music.");
    }

    [Fact]
    public void TypedKey_FormatsTheResolvedValue()
    {
        var localizer = this.resources.CreateLocalizer();
        var key = new ResourceKey("ItemsFound", this.resources.Catalog);

        localizer.Get(key, 2.0, "Music").Should().Be("Found 2.0 items in Music.");
    }

    [Fact]
    public void MissingKey_ReturnsTheSentinelUnformatted()
    {
        var localizer = this.resources.CreateLocalizer();

        localizer.Get("ThisKeyDoesNotExist", 1, 2).Should().Be("!ThisKeyDoesNotExist!");
        localizer.Get("ThisKeyDoesNotExist", this.resources.Catalog, 1, 2).Should().Be("!ThisKeyDoesNotExist!");
    }

    [Fact]
    public void EmptyKey_ReturnsEmptyString()
    {
        var localizer = this.resources.CreateLocalizer();

        localizer.Get(String.Empty, 1, 2).Should().BeEmpty();
    }

    [Fact]
    public void InvalidCompositeFormat_FailsLoudly()
    {
        var localizer = this.resources.CreateLocalizer();

        // The template needs two arguments; supplying none is a resource defect, not a missing
        // translation, so it must throw rather than degrade silently.
        var act = () => localizer.Get("ItemsFound", []);

        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void NullArgumentsArray_Throws()
    {
        var localizer = this.resources.CreateLocalizer();

        var act = () => localizer.Get("Greeting", (Object?[])null!);

        act.Should().Throw<ArgumentNullException>();
    }

    private readonly TestResources resources = new();
}
