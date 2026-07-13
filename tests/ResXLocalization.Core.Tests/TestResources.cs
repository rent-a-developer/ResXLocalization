namespace RentADeveloper.ResXLocalization.Core.Tests;

/// <summary>
/// Per-test fixtures for the Core engine tests. xUnit constructs the test class once per test method,
/// so a <c>new TestResources()</c> field hands every test its own <see cref="Catalog" /> and
/// <see cref="Fallback" /> resource managers. That isolation matters: a <see cref="ResourceManager" />
/// mutates its internal fallback cache on lookup, so sharing one instance lets a fallback lookup in one
/// test leak into another and, under parallel execution, race. Isolated instances let the whole suite
/// run in parallel. The cultures are immutable identifiers, so they stay static.
/// </summary>
internal sealed class TestResources
{
    /// <summary>English culture, the starting language of every test localizer.</summary>
    public static readonly CultureInfo English = new("en");

    /// <summary>German culture, used to assert that values switch live.</summary>
    public static readonly CultureInfo German = new("de");

    /// <summary>Gets the test-only Catalog file; registered first so it wins search-all ties for "Shared".</summary>
    public ResourceManager Catalog { get; } =
        new("RentADeveloper.ResXLocalization.Core.Tests.Resources.Catalog", typeof(TestResources).Assembly);

    /// <summary>Gets the test-only Fallback file; registered after <see cref="Catalog" />.</summary>
    public ResourceManager Fallback { get; } =
        new("RentADeveloper.ResXLocalization.Core.Tests.Resources.Fallback", typeof(TestResources).Assembly);

    /// <summary>Creates an isolated, English localizer with Catalog then Fallback registered.</summary>
    /// <returns>The configured localizer.</returns>
    public Localizer CreateLocalizer()
    {
        var localizer = new Localizer { CurrentCulture = English };
        localizer.RegisterResourceManager(this.Catalog);
        localizer.RegisterResourceManager(this.Fallback);
        return localizer;
    }
}
