using System.ComponentModel;
using System.Reflection;

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
    private readonly TestResources resources = new();

    [Fact]
    public void AssigningNullCulture_Throws()
    {
        var localizer = this.resources.CreateLocalizer();

        var act = () => localizer.CurrentCulture = null!;

        act.Should().Throw<ArgumentNullException>();
        localizer.CurrentCulture.Should().Be(TestResources.English);
    }

    [Fact]
    public void AssigningTheSameCulture_RaisesNoNotifications()
    {
        var localizer = this.resources.CreateLocalizer();
        using var monitor = localizer.Monitor();

        // Both the very same instance and a distinct-but-equal culture must be treated as "no change".
        var sameInstance = localizer.CurrentCulture;
        localizer.CurrentCulture = sameInstance;
        localizer.CurrentCulture = new("en");

        monitor.EventLog().Should().BeEmpty();
        localizer.CurrentCulture.Should().BeSameAs(sameInstance);
    }

    [Fact]
    public void CultureChange_RaisesCurrentCulture_ThenIndexer_ThenCultureChanged()
    {
        var localizer = this.resources.CreateLocalizer();
        using var monitor = localizer.Monitor();

        localizer.CurrentCulture = TestResources.German;

        monitor
            .EventLog()
            .Should()
            .Equal(
                "PropertyChanged(CurrentCulture)",
                "PropertyChanged(RefreshTriggeringRevision)",
                "PropertyChanged(Item[])",
                "CultureChanged"
            );
    }

    [Fact]
    public void CultureChanged_CarriesOldAndNewCulture()
    {
        var localizer = this.resources.CreateLocalizer();
        using var monitor = localizer.Monitor();

        localizer.CurrentCulture = TestResources.German;

        monitor
            .Should()
            .Raise(nameof(ILocalizer.CultureChanged))
            .WithArgs<CultureChangedEventArgs>(args =>
                args.OldCulture.Equals(TestResources.English) && args.NewCulture.Equals(TestResources.German)
            );
    }

    [Fact]
    public void CurrentCulture_DefaultsToCurrentUICulture() =>
        new Localizer().CurrentCulture.Should().Be(CultureInfo.CurrentUICulture);

    [Fact]
    public void Current_IsAProcessWideSingleton()
    {
        Localizer.Current.Should().BeSameAs(Localizer.Current);
        Localizer.Current.Should().BeAssignableTo<ILocalizer>();
    }

    [Fact]
    public void GetOverloads_AreExactlyOnePerKeyKind_WithoutParamsOrEditorBrowsable()
    {
        string[] expected =
        [
            "Get(Enum, MessageArguments, ResourceManager, String)",
            "Get(ResourceKey, MessageArguments)",
            "Get(String, MessageArguments, ResourceManager)",
        ];

        foreach (var type in new[] { typeof(ILocalizer), typeof(Localizer) })
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(method => method.Name == nameof(ILocalizer.Get))
                .ToArray();

            methods
                .Select(method =>
                    $"Get({string.Join(", ", method.GetParameters().Select(parameter => parameter.ParameterType.Name))})"
                )
                .Order(StringComparer.Ordinal)
                .Should()
                .Equal(expected);
            methods
                .SelectMany(method => method.GetParameters())
                .Should()
                .NotContain(parameter => parameter.IsDefined(typeof(ParamArrayAttribute)));
            methods.Should().NotContain(method => method.IsDefined(typeof(EditorBrowsableAttribute)));
        }
    }

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
}
