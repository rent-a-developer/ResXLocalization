using System.Diagnostics.CodeAnalysis;

namespace RentADeveloper.ResXLocalization.Core.Tests;

/// <summary>
/// Verifies the documented dependency-injection story: <c>new Localizer()</c> creates a fully
/// isolated instance whose registrations, culture, and notifications are independent of every other
/// instance (including the ambient <see cref="Localizer.Current" />).
/// </summary>
/// <remarks>
/// Shares the process-wide <see cref="Localizer.Current" /> with <see cref="LocalizerContractTests" />
/// (which reads it while this class replaces it), so the two run in one collection - serial with each
/// other, parallel with every other, isolated Core test class.
/// </remarks>
[Collection(AmbientLocalizerGroup.Name)]
public class IsolatedInstanceTests
{
    [Fact]
    public void Current_CanBeReplaced_AndRejectsNull()
    {
        var original = Localizer.Current;
        var replacement = this.resources.CreateLocalizer();

        try
        {
            Localizer.Current = replacement;

            Localizer.Current.Should().BeSameAs(replacement);
            var act = () => Localizer.Current = null!;
            act.Should().Throw<ArgumentNullException>();
            Localizer.Current.Should().BeSameAs(replacement);
        }
        finally
        {
            Localizer.Current = original;
        }
    }

    [Fact]
    public void Registrations_AreNotSharedBetweenInstances()
    {
        var first = new Localizer { CurrentCulture = TestResources.English };
        var second = new Localizer { CurrentCulture = TestResources.English };
        first.RegisterResourceManager(this.resources.Catalog);

        first.Get("CatalogOnly").Should().Be("CatalogOnlyValue");
        second.Get("CatalogOnly").Should().Be("!CatalogOnly!");
    }

    [Fact]
    public void CultureChanges_DoNotLeakIntoOtherInstances()
    {
        var first = this.resources.CreateLocalizer();
        var second = this.resources.CreateLocalizer();
        var secondNotified = false;
        second.CultureChanged += (_, _) => secondNotified = true;

        first.CurrentCulture = TestResources.German;

        first.Get("Greeting").Should().Be("Hallo und willkommen!");
        second.Get("Greeting").Should().Be("Hello and welcome!");
        second.CurrentCulture.Should().Be(TestResources.English);
        secondNotified.Should().BeFalse();
    }

    [Fact]
    public void AnIsolatedInstance_SatisfiesTheILocalizerContract() =>
        this.AssertLocalizerContract(this.resources.CreateLocalizer());

    private readonly TestResources resources = new();

    /// <summary>The typical DI shape: the consumer sees only the interface.</summary>
    /// <param name="localizer">The localizer under test, seen through the interface.</param>
    [SuppressMessage(
        "Performance",
        "CA1859:Use concrete types when possible for improved performance",
        Justification = "Exercising the ILocalizer interface surface is the point of this test."
    )]
    private void AssertLocalizerContract(ILocalizer localizer)
    {
        localizer.Get("Greeting").Should().Be("Hello and welcome!");
        localizer["Greeting"].Should().Be("Hello and welcome!");
        localizer.Get(new ResourceKey("Greeting", this.resources.Catalog)).Should().Be("Hello and welcome!");
    }
}
