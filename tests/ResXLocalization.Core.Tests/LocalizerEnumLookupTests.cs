namespace RentADeveloper.ResXLocalization.Core.Tests;

/// <summary>
/// Exercises the four enum lookup overloads and their argument validation. Enum values map to keys by
/// the <c>{KeyPrefix}{EnumTypeName}_{Value}</c> convention.
/// </summary>
public class LocalizerEnumLookupTests
{
    [Fact]
    public void SearchAll_DefaultPrefix_ResolvesAndSwitchesLive()
    {
        var localizer = this.resources.CreateLocalizer();

        localizer.Get(TestSortOrder.Ascending).Should().Be("Ascending");

        localizer.CurrentCulture = TestResources.German;
        localizer.Get(TestSortOrder.Ascending).Should().Be("Aufsteigend");
    }

    [Fact]
    public void SearchAll_CustomPrefix_Resolves()
    {
        var localizer = this.resources.CreateLocalizer();

        localizer.Get(TestSortOrder.Ascending, "Display_").Should().Be("A to Z");
    }

    [Fact]
    public void Scoped_DefaultPrefix_Resolves()
    {
        var localizer = this.resources.CreateLocalizer();

        localizer.Get(TestSortOrder.Ascending, this.resources.Catalog).Should().Be("Ascending");
    }

    [Fact]
    public void Scoped_CustomPrefix_Resolves()
    {
        var localizer = this.resources.CreateLocalizer();

        localizer.Get(TestSortOrder.Ascending, this.resources.Catalog, "Display_").Should().Be("A to Z");
    }

    [Fact]
    public void UnlocalizedEnumValue_ReturnsTheConventionKeyAsSentinel()
    {
        var localizer = this.resources.CreateLocalizer();

        localizer.Get(TestSortOrder.Descending).Should().Be("!Enum_TestSortOrder_Descending!");
    }

    [Fact]
    public void NullArguments_Throw()
    {
        var localizer = this.resources.CreateLocalizer();

        ((Action)(() => localizer.Get((Enum)null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => localizer.Get(TestSortOrder.Ascending, (String)null!)))
            .Should().Throw<ArgumentNullException>();
        ((Action)(() => localizer.Get(TestSortOrder.Ascending, (ResourceManager)null!)))
            .Should().Throw<ArgumentNullException>();
        ((Action)(() => localizer.Get(TestSortOrder.Ascending, this.resources.Catalog, null!)))
            .Should().Throw<ArgumentNullException>();
    }

    private readonly TestResources resources = new();
}
