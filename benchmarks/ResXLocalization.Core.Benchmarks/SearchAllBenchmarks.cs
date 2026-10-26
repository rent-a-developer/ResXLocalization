using System.Resources;
using BenchmarkDotNet.Attributes;
using RentADeveloper.ResXLocalization.Benchmarks;

namespace RentADeveloper.ResXLocalization.Core.Benchmarks;

/// <summary>
/// A search-all lookup whose key exists only in the last of <see cref="ManagerCount" /> registered resource
/// managers, against the loop an application writes without the library when it does not know which file holds
/// a key: <see cref="ResourceManager.GetString(string, System.Globalization.CultureInfo)" /> on each of the same
/// managers, in the same order, until one has it. Both rows do the same resource-manager work, so the ratio is
/// what the library adds to a search.
/// </summary>
/// <remarks>
/// Every manager before the last is a separate <see cref="ResourceManager" /> over <c>Filler.resx</c>: the
/// localizer tells managers apart by instance, and each instance has its own resource-set cache, so each miss
/// walks the whole culture fallback chain, <c>de-DE -&gt; de -&gt; neutral</c>, as a different file would.
/// </remarks>
[Config(typeof(BenchmarkConfig))]
public class SearchAllBenchmarks
{
    private ResourceManager[] resourceManagers = [];

    [Params(1, 5, 20)]
    public int ManagerCount { get; set; }

    [Benchmark]
    public string GetStringKeySearchAll() => Localizer.Current.Get("Greeting");

    [Benchmark(Baseline = true)]
    public string HandWrittenSearchAll()
    {
        foreach (var resourceManager in this.resourceManagers)
        {
            if (resourceManager.GetString("Greeting", BenchmarkCulture.German) is { } text)
            {
                return text;
            }
        }

        return "Greeting";
    }

    [GlobalSetup]
    public void Setup()
    {
        this.resourceManagers =
        [
            .. Enumerable
                .Range(1, this.ManagerCount - 1)
                .Select(static _ => new ResourceManager(
                    FillerKeys.ResourceManager.BaseName,
                    typeof(FillerKeys).Assembly
                )),
            BenchmarkStringsKeys.ResourceManager,
        ];

        Localizer.Current.ClearResourceManagers();

        foreach (var resourceManager in this.resourceManagers)
        {
            Localizer.Current.RegisterResourceManager(resourceManager);
        }

        BenchmarkCulture.Use(BenchmarkCulture.German);

        var expected = ExpectedViewTexts.Text(BenchmarkCulture.German, "Greeting");
        SelfCheck.Verify(this, _ => expected);
    }
}
