namespace RentADeveloper.ResXLocalization.Core.Tests;

/// <summary>
/// Exercises <see cref="ILocalizer.RefreshTriggeringRevision" />: it advances, with one
/// <c>PropertyChanged</c> for itself and one for <c>Item[]</c>, after every real change that can change a
/// lookup's result, and after nothing else.
/// </summary>
public class RefreshTriggeringRevisionTests
{
    private readonly TestResources resources = new();

    [Fact]
    public void EnumKeyBuilder_ANewInstanceAdvances_TheSameInstanceAgainDoesNot()
    {
        var localizer = this.resources.CreateLocalizer();
        Func<Enum, string?, string> builder = static (value, keyPrefix) => $"{keyPrefix}{value}";

        AssertAdvances(localizer, () => localizer.EnumKeyBuilder = builder);
        AssertDoesNotAdvance(localizer, () => localizer.EnumKeyBuilder = builder);
    }

    [Fact]
    public void EveryNoOp_DoesNotAdvance()
    {
        var localizer = this.resources.CreateLocalizer();
        var culture = localizer.CurrentCulture;
        var formatter = localizer.MessageFormatter;
        var builder = localizer.LookupFailureTextBuilder;

        AssertDoesNotAdvance(localizer, () => localizer.CurrentCulture = culture);
        AssertDoesNotAdvance(localizer, () => localizer.MessageFormatter = formatter);
        AssertDoesNotAdvance(localizer, () => localizer.LookupFailureTextBuilder = builder);
        AssertDoesNotAdvance(localizer, () => localizer.RegisterResourceManager(this.resources.Catalog));
        AssertDoesNotAdvance(localizer, () => localizer.UnregisterResourceManager(CreateUnregisteredManager()));

        localizer.ClearResourceManagers();
        AssertDoesNotAdvance(localizer, localizer.ClearResourceManagers);

        AssertDoesNotAdvance(
            localizer,
            () =>
                FluentActions.Invoking(() => localizer.MessageFormatter = null!).Should().Throw<ArgumentNullException>()
        );
        AssertDoesNotAdvance(
            localizer,
            () =>
                FluentActions
                    .Invoking(() => localizer.LookupFailureTextBuilder = null!)
                    .Should()
                    .Throw<ArgumentNullException>()
        );
    }

    [Fact]
    public void EveryRealChange_Advances()
    {
        var localizer = this.resources.CreateLocalizer();

        AssertAdvances(
            localizer,
            () => localizer.CurrentCulture = TestResources.German,
            "PropertyChanged(CurrentCulture)",
            "PropertyChanged(RefreshTriggeringRevision)",
            "PropertyChanged(Item[])",
            "CultureChanged"
        );
        AssertAdvances(localizer, () => localizer.MessageFormatter = new CapturingFormatter());
        AssertAdvances(localizer, () => localizer.LookupFailureTextBuilder = failure => $"[{failure.Key}]");
        AssertAdvances(localizer, () => localizer.RegisterResourceManager(CreateUnregisteredManager()));
        AssertAdvances(localizer, () => localizer.UnregisterResourceManager(this.resources.Catalog));
        AssertAdvances(localizer, localizer.ClearResourceManagers);
    }

    [Fact]
    public void StartsAtZero_AndWrapsAround()
    {
        var localizer = new Localizer();

        localizer.RefreshTriggeringRevision.Should().Be(0);

        // Start from English, so the switch to German below is a real change whatever the host culture.
        localizer.CurrentCulture = TestResources.English;
        typeof(Localizer).GetProperty(nameof(Localizer.RefreshTriggeringRevision))!.SetValue(localizer, int.MaxValue);
        localizer.CurrentCulture = TestResources.German;

        localizer.RefreshTriggeringRevision.Should().Be(int.MinValue);
    }

    /// <summary>
    /// Asserts that <paramref name="action" /> changes the revision and raises exactly
    /// <paramref name="expectedLog" /> (by default one revision and one indexer notification, in that order).
    /// </summary>
    /// <param name="localizer">The localizer under test.</param>
    /// <param name="action">The change to apply.</param>
    /// <param name="expectedLog">The expected event log; empty for the default.</param>
    private static void AssertAdvances(Localizer localizer, Action action, params string[] expectedLog)
    {
        var before = localizer.RefreshTriggeringRevision;
        using var monitor = localizer.Monitor();

        action();

        localizer.RefreshTriggeringRevision.Should().NotBe(before);
        monitor
            .EventLog()
            .Should()
            .Equal(
                expectedLog.Length == 0
                    ? ["PropertyChanged(RefreshTriggeringRevision)", "PropertyChanged(Item[])"]
                    : expectedLog
            );
    }

    /// <summary>Asserts that <paramref name="action" /> leaves the revision unchanged and raises nothing.</summary>
    /// <param name="localizer">The localizer under test.</param>
    /// <param name="action">The no-op to apply.</param>
    private static void AssertDoesNotAdvance(Localizer localizer, Action action)
    {
        var before = localizer.RefreshTriggeringRevision;
        using var monitor = localizer.Monitor();

        action();

        localizer.RefreshTriggeringRevision.Should().Be(before);
        monitor.EventLog().Should().BeEmpty();
    }

    /// <summary>Creates a manager over the Catalog resources that is a different, unregistered instance.</summary>
    /// <returns>The new manager.</returns>
    private static ResourceManager CreateUnregisteredManager() =>
        new("RentADeveloper.ResXLocalization.Core.Tests.Resources.Catalog", typeof(TestResources).Assembly);
}
