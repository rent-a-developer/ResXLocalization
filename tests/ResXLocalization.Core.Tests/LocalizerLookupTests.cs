namespace RentADeveloper.ResXLocalization.Core.Tests;

/// <summary>
/// Exercises the string lookup surface on an isolated localizer: search-all, scoped, typed, the
/// indexer, the empty-key and miss behavior, and the first-match-wins rule across multiple files.
/// </summary>
public class LocalizerLookupTests
{
    [Fact]
    public void SearchAll_ReturnsTheFirstMatch_InRegistrationOrder()
    {
        var localizer = this.resources.CreateLocalizer();

        // Catalog is registered before Fallback, so it wins the collision on "Shared".
        localizer.Get("Shared").Should().Be("CatalogShared");
    }

    [Fact]
    public void SearchAll_ReachesKeysUniqueToEachRegisteredFile()
    {
        var localizer = this.resources.CreateLocalizer();

        localizer.Get("CatalogOnly").Should().Be("CatalogOnlyValue");
        localizer.Get("FallbackOnly").Should().Be("FallbackOnlyValue");
    }

    [Fact]
    public void SearchAll_SwitchesLive_OnCultureChange()
    {
        var localizer = this.resources.CreateLocalizer();
        localizer.Get("Greeting").Should().Be("Hello and welcome!");

        localizer.CurrentCulture = TestResources.German;

        localizer.Get("Greeting").Should().Be("Hallo und willkommen!");
    }

    [Fact]
    public void Indexer_IsShorthandForSearchAllGet()
    {
        var localizer = this.resources.CreateLocalizer();

        localizer["Greeting"].Should().Be(localizer.Get("Greeting"));
        localizer["Greeting"].Should().Be("Hello and welcome!");
    }

    [Fact]
    public void ScopedLookup_ReadsExactlyTheNamedFile_EvenForACollidingKey()
    {
        var localizer = this.resources.CreateLocalizer();

        localizer.Get("Shared", this.resources.Catalog).Should().Be("CatalogShared");
        localizer.Get("Shared", this.resources.Fallback).Should().Be("FallbackShared");
    }

    [Fact]
    public void ScopedLookup_SwitchesLive_OnCultureChange()
    {
        var localizer = this.resources.CreateLocalizer();
        localizer.Get("Greeting", this.resources.Catalog).Should().Be("Hello and welcome!");

        localizer.CurrentCulture = TestResources.German;

        localizer.Get("Greeting", this.resources.Catalog).Should().Be("Hallo und willkommen!");
    }

    [Fact]
    public void TypedResourceKey_ResolvesThroughItsOwnManager()
    {
        var localizer = this.resources.CreateLocalizer();
        var key = new ResourceKey("Shared", this.resources.Fallback);

        // The key carries its manager, so it reads Fallback even though Catalog is registered first.
        localizer.Get(key).Should().Be("FallbackShared");
    }

    [Fact]
    public void NullOrEmptyKey_ReturnsEmptyString()
    {
        var localizer = this.resources.CreateLocalizer();

        localizer.Get(String.Empty).Should().BeEmpty();
        localizer.Get((String)null!).Should().BeEmpty();
        localizer.Get(String.Empty, this.resources.Catalog).Should().BeEmpty();
    }

    [Fact]
    public void MissingKey_ReturnsTheBangSentinel()
    {
        var localizer = this.resources.CreateLocalizer();

        localizer.Get("ThisKeyDoesNotExist").Should().Be("!ThisKeyDoesNotExist!");
        localizer.Get("ThisKeyDoesNotExist", this.resources.Catalog).Should().Be("!ThisKeyDoesNotExist!");
    }

    [Fact]
    public void ScopedLookup_RejectsNullResourceManager()
    {
        var localizer = this.resources.CreateLocalizer();

        var act = () => localizer.Get("Greeting", (ResourceManager)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void TypedResourceKey_Default_Throws()
    {
        var localizer = this.resources.CreateLocalizer();

        // default(ResourceKey) is constructible (it is a struct) but carries a null manager, so the
        // scoped overload's null check rejects it.
        var act = () => localizer.Get(default(ResourceKey));

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void SearchAll_WithNothingRegistered_ReturnsTheSentinel()
    {
        var localizer = new Localizer { CurrentCulture = TestResources.English };

        localizer.Get("Greeting").Should().Be("!Greeting!");
    }

    private readonly TestResources resources = new();
}
