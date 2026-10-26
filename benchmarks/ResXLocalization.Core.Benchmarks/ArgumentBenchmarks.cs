using BenchmarkDotNet.Attributes;
using RentADeveloper.ResXLocalization.Benchmarks;

namespace RentADeveloper.ResXLocalization.Core.Benchmarks;

/// <summary>
/// A typed-key lookup with the two format arguments of <see cref="ArgumentValues" /> in each
/// <see cref="MessageArguments" /> form, against
/// <see cref="string.Format(IFormatProvider, string, object, object)" /> over the designer class's property.
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class ArgumentBenchmarks
{
    private static readonly ArgumentValues Values = ArgumentValues.Instance;

    [Benchmark]
    public string GetWithFromObject() =>
        Localizer.Current.Get(
            BenchmarkStringsKeys.UserMessages,
            MessageArguments.FromObject(new { Values.Name, Values.Count })
        );

    [Benchmark]
    public string GetWithFromTuples() =>
        Localizer.Current.Get(
            BenchmarkStringsKeys.UserMessages,
            MessageArguments.FromTuples((nameof(Values.Name), Values.Name), (nameof(Values.Count), Values.Count))
        );

    [Benchmark]
    public string GetWithFromValues() =>
        Localizer.Current.Get(
            BenchmarkStringsKeys.UserMessages,
            MessageArguments.FromValues(Values.Name, Values.Count)
        );

    [GlobalSetup]
    public void Setup()
    {
        BenchmarkCulture.Use(BenchmarkCulture.German);

        var expected = string.Format(
            BenchmarkCulture.German,
            ExpectedViewTexts.Text(BenchmarkCulture.German, "UserMessages"),
            Values.Name,
            Values.Count
        );
        SelfCheck.Verify(this, _ => expected);
    }

    [Benchmark(Baseline = true)]
    public string StringFormatDesignerProperty() =>
        string.Format(BenchmarkCulture.German, BenchmarkStrings.UserMessages, Values.Name, Values.Count);
}
