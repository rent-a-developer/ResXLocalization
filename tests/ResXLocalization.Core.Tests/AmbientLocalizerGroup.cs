namespace RentADeveloper.ResXLocalization.Core.Tests;

/// <summary>
/// Groups the test classes that touch the process-wide <see cref="Localizer.Current" /> singleton into
/// a single xUnit collection. Classes in one collection do not run in parallel with each other, so the
/// writer (<see cref="IsolatedInstanceTests" />) and the reader (<see cref="LocalizerContractTests" />)
/// of <c>Localizer.Current</c> are serialized - while every other Core test class, being isolated,
/// keeps running in parallel. It carries no fixture; it exists only to name the collection.
/// </summary>
[CollectionDefinition(Name)]
public sealed class AmbientLocalizerGroup
{
    /// <summary>The collection name shared by the classes that use <see cref="Localizer.Current" />.</summary>
    public const String Name = "Ambient Localizer.Current";
}
