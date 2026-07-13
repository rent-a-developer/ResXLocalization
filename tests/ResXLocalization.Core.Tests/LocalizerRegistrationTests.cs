namespace RentADeveloper.ResXLocalization.Core.Tests;

/// <summary>
/// Exercises search-set maintenance: unregistering a single resource manager and clearing the set,
/// for example when a plugin that owns its strings is unloaded.
/// </summary>
public class LocalizerRegistrationTests
{
    [Fact]
    public void Unregister_RemovesTheManagerFromTheSearchOrder()
    {
        var localizer = this.resources.CreateLocalizer();
        localizer.Get("Shared").Should().Be("CatalogShared");

        localizer.UnregisterResourceManager(this.resources.Catalog).Should().BeTrue();

        // With Catalog gone, the "Shared" collision now falls to Fallback.
        localizer.Get("Shared").Should().Be("FallbackShared");
        localizer.Get("CatalogOnly").Should().Be("!CatalogOnly!");
    }

    [Fact]
    public void Unregister_ReturnsFalse_ForAManagerThatWasNeverRegistered()
    {
        var localizer = new Localizer { CurrentCulture = TestResources.English };

        localizer.UnregisterResourceManager(this.resources.Catalog).Should().BeFalse();
    }

    [Fact]
    public void Unregister_RejectsNull()
    {
        var localizer = this.resources.CreateLocalizer();

        var act = () => localizer.UnregisterResourceManager(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RegisteringTwice_KeepsASingleEntry_SoOneUnregisterRemovesIt()
    {
        var localizer = new Localizer { CurrentCulture = TestResources.English };
        localizer.RegisterResourceManager(this.resources.Catalog);
        localizer.RegisterResourceManager(this.resources.Catalog);

        localizer.UnregisterResourceManager(this.resources.Catalog).Should().BeTrue();

        // A second unregister finds nothing left: the duplicate registration was de-duplicated.
        localizer.UnregisterResourceManager(this.resources.Catalog).Should().BeFalse();
        localizer.Get("CatalogOnly").Should().Be("!CatalogOnly!");
    }

    [Fact]
    public void Clear_EmptiesTheSearchSet_ButLeavesScopedLookupsWorking()
    {
        var localizer = this.resources.CreateLocalizer();

        localizer.ClearResourceManagers();

        localizer.Get("Greeting").Should().Be("!Greeting!");

        // Scoped and typed lookups never consult the registration set.
        localizer.Get("Greeting", this.resources.Catalog).Should().Be("Hello and welcome!");
        localizer.Get(new ResourceKey("Greeting", this.resources.Catalog)).Should().Be("Hello and welcome!");
    }

    private readonly TestResources resources = new();
}
