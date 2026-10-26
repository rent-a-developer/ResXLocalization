using BenchmarkDotNet.Attributes;
using RentADeveloper.ResXLocalization.Benchmarks;

namespace RentADeveloper.ResXLocalization.Core.Benchmarks;

/// <summary>
/// One <see cref="Localizer.Get(ResourceKey, MessageArguments?)" /> per lookup mode, in <c>de-DE</c>, against the
/// designer class's property.
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class CodeLookupBenchmarks
{
    [Benchmark]
    public string GetEnum() => Localizer.Current.Get(Priority.Priority1);

    [Benchmark]
    public string GetStringKeyScoped() =>
        Localizer.Current.Get("Greeting", resourceManager: BenchmarkStringsKeys.ResourceManager);

    [Benchmark]
    public string GetStringKeySearchAll() => Localizer.Current.Get("Greeting");

    [Benchmark]
    public string GetTypedKey() => Localizer.Current.Get(BenchmarkStringsKeys.Greeting);

    [Benchmark(Baseline = true)]
    public string ResXDesignerProperty() => BenchmarkStrings.Greeting;

    [GlobalSetup]
    public void Setup()
    {
        Localizer.Current.ClearResourceManagers();
        Localizer.Current.RegisterResourceManager(BenchmarkStringsKeys.ResourceManager);
        BenchmarkCulture.Use(BenchmarkCulture.German);

        SelfCheck.Verify(
            this,
            static name =>
                ExpectedViewTexts.Text(
                    BenchmarkCulture.German,
                    name == nameof(GetEnum) ? "Enum_Priority_Priority1" : "Greeting"
                )
        );
    }
}
