namespace RentADeveloper.ResXLocalization.Core.Tests;

/// <summary>
/// Exercises culture discovery: the test assembly embeds neutral resources (reported as the
/// invariant culture) and ships a German satellite for both test files.
/// </summary>
public class AvailableCulturesTests
{
    [Fact]
    public void PerManager_ReportsNeutralAsInvariant_AndEachSatelliteCulture()
    {
        var localizer = this.resources.CreateLocalizer();

        var cultures = localizer.GetAvailableCultures(this.resources.Catalog);

        cultures.Should().Contain(CultureInfo.InvariantCulture);
        cultures.Should().Contain(CultureInfo.GetCultureInfo("de"));
        cultures.Should().NotContain(CultureInfo.GetCultureInfo("fr"));
    }

    [Fact]
    public void PerManager_SortsTheInvariantCultureFirst()
    {
        var localizer = this.resources.CreateLocalizer();

        var cultures = localizer.GetAvailableCultures(this.resources.Catalog);

        cultures[0].Should().Be(CultureInfo.InvariantCulture);
    }

    [Fact]
    public void AcrossAllManagers_ReturnsTheUnion()
    {
        var localizer = this.resources.CreateLocalizer();

        var cultures = localizer.GetAvailableCultures();

        cultures.Should().Contain(CultureInfo.InvariantCulture);
        cultures.Should().Contain(CultureInfo.GetCultureInfo("de"));
        cultures.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void WithNothingRegistered_ReturnsAnEmptyList()
    {
        var localizer = new Localizer();

        localizer.GetAvailableCultures().Should().BeEmpty();
    }

    [Fact]
    public void FallbackLookups_DoNotPolluteTheResult()
    {
        var localizer = this.resources.CreateLocalizer();

        // A lookup in a culture without its own satellite falls back to the neutral resources and
        // gets CACHED by the ResourceManager; discovery must not mistake that cache entry for a
        // shipped culture.
        localizer.CurrentCulture = CultureInfo.GetCultureInfo("fr");
        localizer.Get("Greeting").Should().Be("Hello and welcome!");

        var cultures = localizer.GetAvailableCultures(this.resources.Catalog);

        cultures.Should().NotContain(CultureInfo.GetCultureInfo("fr"));
        cultures.Should().Contain(CultureInfo.GetCultureInfo("de"));
        cultures.Should().Contain(CultureInfo.InvariantCulture);
    }

    [Fact]
    public void PerManager_RejectsNull()
    {
        var localizer = this.resources.CreateLocalizer();

        var act = () => localizer.GetAvailableCultures(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    private readonly TestResources resources = new();
}
