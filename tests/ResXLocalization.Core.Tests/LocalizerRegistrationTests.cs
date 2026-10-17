using System.ComponentModel;
using System.Reflection;

namespace RentADeveloper.ResXLocalization.Core.Tests;

/// <summary>
/// Exercises search-set maintenance: the registration probe, unregistering a single resource manager and
/// clearing the set, for example when a plugin that owns its strings is unloaded.
/// </summary>
public class LocalizerRegistrationTests
{
    private readonly TestResources resources = new();

    [Fact]
    public void Clear_EmptiesTheSearchSet_ButLeavesScopedLookupsWorking()
    {
        var localizer = this.resources.CreateLocalizer();

        localizer.ClearResourceManagers();

        localizer.Get("Greeting").Should().Be("!Greeting!");

        // Scoped and typed lookups never consult the registration set.
        localizer.Get("Greeting", resourceManager: this.resources.Catalog).Should().Be("Hello and welcome!");
        localizer.Get(new ResourceKey("Greeting", this.resources.Catalog)).Should().Be("Hello and welcome!");
    }

    [Fact]
    public void Register_AcceptsASubclass_WithoutProbingIt()
    {
        var localizer = new Localizer { CurrentCulture = TestResources.English };

        // The base class's probe would throw ArgumentNullException for this manager: it has no base name.
        localizer.RegisterResourceManager(new GreetingOnlyResourceManager());

        localizer.Get("Greeting").Should().Be("Hi");
    }

    [Fact]
    public void Register_RejectsAPlainManager_WhoseResourcesDoNotExist()
    {
        const string baseName = "RentADeveloper.ResXLocalization.Core.Tests.Resources.DoesNotExist";
        var localizer = new Localizer { CurrentCulture = TestResources.English };
        var revision = localizer.RefreshTriggeringRevision;

        var act = () =>
            localizer.RegisterResourceManager(new ResourceManager(baseName, typeof(TestResources).Assembly));

        act.Should()
            .Throw<ArgumentException>()
            .Where(exception => exception.ParamName == "resourceManager" && exception.Message.Contains(baseName))
            .WithInnerException<MissingManifestResourceException>();

        // Nothing was registered, so the search-all lookup misses instead of throwing.
        localizer.Get("Greeting").Should().Be("!Greeting!");
        localizer.RefreshTriggeringRevision.Should().Be(revision);
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
    public void RegistrationMembers_AreNotHiddenFromIntelliSense()
    {
        string[] names =
        [
            nameof(ILocalizer.ClearResourceManagers),
            nameof(ILocalizer.RegisterResourceManager),
            nameof(ILocalizer.UnregisterResourceManager),
        ];

        foreach (var type in new[] { typeof(ILocalizer), typeof(Localizer) })
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(method => names.Contains(method.Name))
                .ToArray();

            methods.Should().HaveCount(names.Length);
            methods.Should().NotContain(method => method.IsDefined(typeof(EditorBrowsableAttribute)));
        }
    }

    [Fact]
    public void Unregister_RejectsNull()
    {
        var localizer = this.resources.CreateLocalizer();

        var act = () => localizer.UnregisterResourceManager(null!);

        act.Should().Throw<ArgumentNullException>();
    }

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

    /// <summary>
    /// A resource manager that resolves strings without manifest resources: it overrides only
    /// <see cref="ResourceManager.GetString(string, CultureInfo?)" />.
    /// </summary>
    private sealed class GreetingOnlyResourceManager : ResourceManager
    {
        public override string? GetString(string name, CultureInfo? culture) => name == "Greeting" ? "Hi" : null;
    }
}
