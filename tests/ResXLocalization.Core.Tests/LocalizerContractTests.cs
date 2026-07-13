namespace RentADeveloper.ResXLocalization.Core.Tests;

/// <summary>
/// Verifies the behavioral contracts the engine documents: the exact change-notification order, the
/// same-culture no-op, the default culture, argument validation, and registration de-duplication.
/// </summary>
/// <remarks>
/// Reads the process-wide <see cref="Localizer.Current" /> (Current_IsAProcessWideSingleton), so it
/// shares a collection with <see cref="IsolatedInstanceTests" /> (which replaces it) to stop the two
/// from racing.
/// </remarks>
[Collection(AmbientLocalizerGroup.Name)]
public class LocalizerContractTests
{
    [Fact]
    public void CultureChange_RaisesCurrentCulture_ThenIndexer_ThenCultureChanged()
    {
        var localizer = this.resources.CreateLocalizer();
        var events = new List<String>();
        localizer.PropertyChanged += (_, args) => events.Add($"PropertyChanged({args.PropertyName})");
        localizer.CultureChanged += (_, _) => events.Add("CultureChanged");

        localizer.CurrentCulture = TestResources.German;

        events.Should().Equal(
            "PropertyChanged(CurrentCulture)",
            "PropertyChanged(Item[])",
            "CultureChanged"
        );
    }

    [Fact]
    public void CultureChanged_CarriesOldAndNewCulture()
    {
        var localizer = this.resources.CreateLocalizer();
        CultureChangedEventArgs? captured = null;
        localizer.CultureChanged += (_, args) => captured = args;

        localizer.CurrentCulture = TestResources.German;

        captured.Should().NotBeNull();
        captured!.OldCulture.Should().Be(TestResources.English);
        captured.NewCulture.Should().Be(TestResources.German);
    }

    [Fact]
    public void AssigningTheSameCulture_RaisesNoNotifications()
    {
        var localizer = this.resources.CreateLocalizer();
        var eventCount = 0;
        localizer.PropertyChanged += (_, _) => eventCount++;
        localizer.CultureChanged += (_, _) => eventCount++;

        // Both the very same instance and a distinct-but-equal culture must be treated as "no change".
        var sameInstance = localizer.CurrentCulture;
        localizer.CurrentCulture = sameInstance;
        localizer.CurrentCulture = new("en");

        eventCount.Should().Be(0);
        localizer.CurrentCulture.Should().BeSameAs(sameInstance);
    }

    [Fact]
    public void AssigningNullCulture_Throws()
    {
        var localizer = this.resources.CreateLocalizer();

        var act = () => localizer.CurrentCulture = null!;

        act.Should().Throw<ArgumentNullException>();
        localizer.CurrentCulture.Should().Be(TestResources.English);
    }

    [Fact]
    public void CurrentCulture_DefaultsToCurrentUICulture() =>
        new Localizer().CurrentCulture.Should().Be(CultureInfo.CurrentUICulture);

    [Fact]
    public void RegisterResourceManager_RejectsNull()
    {
        var act = () => new Localizer().RegisterResourceManager(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RegisteringTheSameManagerTwice_IsANoOp()
    {
        var localizer = new Localizer { CurrentCulture = TestResources.English };
        localizer.RegisterResourceManager(this.resources.Catalog);
        localizer.RegisterResourceManager(this.resources.Catalog);
        localizer.RegisterResourceManager(this.resources.Fallback);

        // Catalog still wins the "Shared" tie exactly once; the duplicate registration must not
        // displace Fallback or disturb the search order.
        localizer.Get("Shared").Should().Be("CatalogShared");
        localizer.Get("FallbackOnly").Should().Be("FallbackOnlyValue");
    }

    [Fact]
    public void Current_IsAProcessWideSingleton()
    {
        Localizer.Current.Should().BeSameAs(Localizer.Current);
        Localizer.Current.Should().BeAssignableTo<ILocalizer>();
    }

    private readonly TestResources resources = new();
}
