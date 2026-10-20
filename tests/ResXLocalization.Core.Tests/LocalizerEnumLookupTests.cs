namespace RentADeveloper.ResXLocalization.Core.Tests;

/// <summary>
/// Exercises the enum lookup and <see cref="ILocalizer.EnumKeyBuilder" />. By default enum values map to
/// keys by the <c>{KeyPrefix}{EnumTypeName}_{Value}</c> convention.
/// </summary>
public class LocalizerEnumLookupTests
{
    private readonly TestResources resources = new();

    [Fact]
    public void Builder_DefaultIsSharedAndBuildsTheConventionKey_AndRejectsNull()
    {
        var first = new Localizer();
        var second = new Localizer();
        var builder = first.EnumKeyBuilder;

        second.EnumKeyBuilder.Should().BeSameAs(builder);
        builder(TestSortOrder.Ascending, null).Should().Be("Enum_TestSortOrder_Ascending");
        builder(TestSortOrder.Ascending, "X_").Should().Be("X_TestSortOrder_Ascending");

        var act = () => first.EnumKeyBuilder = null!;

        act.Should().Throw<ArgumentNullException>();
        first.EnumKeyBuilder.Should().BeSameAs(builder);
    }

    [Fact]
    public void Builder_NullOrEmptyResult_Throws_AndItsExceptionPropagates()
    {
        var localizer = this.resources.CreateLocalizer();
        var boom = new InvalidOperationException("boom");

        localizer.EnumKeyBuilder = (_, _) => null!;
        var nullResult = () => localizer.Get(TestSortOrder.Ascending);
        nullResult
            .Should()
            .Throw<InvalidOperationException>()
            .Which.Message.Should()
            .Contain("EnumKeyBuilder")
            .And.Contain("TestSortOrder");

        localizer.EnumKeyBuilder = (_, _) => "";
        var emptyResult = () => localizer.Get(TestSortOrder.Ascending);
        emptyResult
            .Should()
            .Throw<InvalidOperationException>()
            .Which.Message.Should()
            .Contain("EnumKeyBuilder")
            .And.Contain("TestSortOrder");

        localizer.EnumKeyBuilder = (_, _) => throw boom;
        var throwing = () => localizer.Get(TestSortOrder.Ascending);
        throwing.Should().Throw<InvalidOperationException>().Which.Should().BeSameAs(boom);
    }

    [Fact]
    public void Builder_ReceivesTheValueAndTheKeyPrefix()
    {
        var localizer = this.resources.CreateLocalizer();
        var calls = new List<(Enum Value, string? KeyPrefix)>();
        localizer.EnumKeyBuilder = (value, keyPrefix) =>
        {
            calls.Add((value, keyPrefix));
            return "Enum_TestSortOrder_Ascending";
        };

        localizer.Get(TestSortOrder.Ascending);
        localizer.Get(TestSortOrder.Ascending, keyPrefix: "Display_");

        calls.Should().Equal((TestSortOrder.Ascending, null), (TestSortOrder.Ascending, "Display_"));
    }

    [Fact]
    public void CustomBuilder_AppliesToEveryEnumLookup_AndKeyPrefixOverridesOnlyItsPrefix()
    {
        var localizer = this.resources.CreateLocalizer();
        localizer.EnumKeyBuilder = static (value, keyPrefix) =>
            (keyPrefix ?? "Text_") + value.GetType().Name + "." + value;

        localizer.Get(TestSortOrder.Ascending).Should().Be("Custom ascending");
        localizer.Get(TestSortOrder.Ascending, resourceManager: this.resources.Catalog).Should().Be("Custom ascending");
        localizer.Get(TestSortOrder.Ascending, MessageArguments.FromValues(3)).Should().Be("Custom ascending");
        localizer
            .Get(TestSortOrder.Ascending, MessageArguments.FromValues(3), keyPrefix: "Count_")
            .Should()
            .Be("!Count_TestSortOrder.Ascending!");
    }

    [Fact]
    public void NullKeyPrefix_MeansTheBuildersDefault()
    {
        var localizer = this.resources.CreateLocalizer();

        localizer.Get(TestSortOrder.Ascending, keyPrefix: null).Should().Be("Ascending");
        localizer.Get(TestSortOrder.Ascending, null, this.resources.Catalog, null).Should().Be("Ascending");
        localizer
            .Get(TestSortOrder.Ascending, MessageArguments.FromValues(3), keyPrefix: null)
            .Should()
            .Be("Ascending");
    }

    [Fact]
    public void NullValueOrDefaultResourceKey_Throws()
    {
        var localizer = this.resources.CreateLocalizer();

        ((Action)(() => localizer.Get((Enum)null!))).Should().Throw<ArgumentNullException>();
        ((Action)(() => localizer.Get((Enum)null!, MessageArguments.FromValues(1))))
            .Should()
            .Throw<ArgumentNullException>();
        ((Action)(() => localizer.Get(default(ResourceKey)))).Should().Throw<ArgumentNullException>();
        ((Action)(() => localizer.Get(default(ResourceKey), MessageArguments.FromValues(1))))
            .Should()
            .Throw<ArgumentNullException>();
    }

    [Fact]
    public void Scoped_CustomPrefix_Resolves()
    {
        var localizer = this.resources.CreateLocalizer();

        localizer
            .Get(TestSortOrder.Ascending, resourceManager: this.resources.Catalog, keyPrefix: "Display_")
            .Should()
            .Be("A to Z");
    }

    [Fact]
    public void Scoped_DefaultPrefix_Resolves()
    {
        var localizer = this.resources.CreateLocalizer();

        localizer.Get(TestSortOrder.Ascending, resourceManager: this.resources.Catalog).Should().Be("Ascending");
    }

    [Fact]
    public void SearchAll_CustomPrefix_Resolves()
    {
        var localizer = this.resources.CreateLocalizer();

        localizer.Get(TestSortOrder.Ascending, keyPrefix: "Display_").Should().Be("A to Z");
    }

    [Fact]
    public void SearchAll_DefaultPrefix_ResolvesAndSwitchesLive()
    {
        var localizer = this.resources.CreateLocalizer();

        localizer.Get(TestSortOrder.Ascending).Should().Be("Ascending");

        localizer.CurrentCulture = TestResources.German;
        localizer.Get(TestSortOrder.Ascending).Should().Be("Aufsteigend");
    }

    [Fact]
    public void UnlocalizedEnumValue_ReturnsTheConventionKeyAsFailureText()
    {
        var localizer = this.resources.CreateLocalizer();

        localizer.Get(TestSortOrder.Descending).Should().Be("!Enum_TestSortOrder_Descending!");
    }

    [Fact]
    public void UnlocalizedEnumValue_WithArguments_ReturnsFailureText_AndReportsOneMiss()
    {
        var localizer = this.resources.CreateLocalizer();
        using var monitor = localizer.Monitor();

        localizer
            .Get(TestSortOrder.Descending, MessageArguments.FromValues(1))
            .Should()
            .Be("!Enum_TestSortOrder_Descending!");

        monitor
            .LookupFailures()
            .Should()
            .ContainSingle()
            .Which.Reason.Should()
            .Be(LookupFailureReason.TranslationNotFound);
    }

    [Fact]
    public void WithArguments_AndCustomPrefix_Formats_ScopedAndLive()
    {
        var localizer = this.resources.CreateLocalizer();

        localizer
            .Get(TestSortOrder.Ascending, MessageArguments.FromValues(3), keyPrefix: "Count_")
            .Should()
            .Be("3 items, ascending");
        localizer
            .Get(TestSortOrder.Ascending, MessageArguments.FromValues(3), this.resources.Catalog, "Count_")
            .Should()
            .Be("3 items, ascending");

        localizer.CurrentCulture = TestResources.German;
        localizer
            .Get(TestSortOrder.Ascending, MessageArguments.FromValues(3), keyPrefix: "Count_")
            .Should()
            .Be("3 Elemente, aufsteigend");
    }

    [Fact]
    public void WithArguments_AndDefaultPrefix_ReturnsTheValueWithoutPlaceholder()
    {
        var localizer = this.resources.CreateLocalizer();

        localizer.Get(TestSortOrder.Ascending, MessageArguments.FromValues(3)).Should().Be("Ascending");
    }
}
