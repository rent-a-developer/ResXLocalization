using BenchmarkDotNet.Attributes;
using RentADeveloper.ResXLocalization.Benchmarks;
using RentADeveloper.ResXLocalization.MessageFormat;

namespace RentADeveloper.ResXLocalization.Core.Benchmarks;

/// <summary>
/// One ICU message with two named arguments, through the localizer with <c>UseMessageFormat()</c> installed,
/// against formatting the designer class's property with the MessageFormat library directly. Both sides use a
/// formatter with the library's defaults, so pattern caching is the same, and the ratio is the cost of
/// ResXLocalization.MessageFormat itself: building the argument dictionary, the culture substitution and the
/// call through the localizer.
/// </summary>
[Config(typeof(BenchmarkConfig))]
public class MessageFormatBenchmarks
{
    private static readonly ArgumentValues Values = ArgumentValues.Instance;

    private readonly Jeffijoe.MessageFormat.MessageFormatter messageFormatter = new();

    private readonly Dictionary<string, object?> values = new(StringComparer.Ordinal)
    {
        [nameof(Values.Count)] = Values.Count,
        [nameof(Values.Name)] = Values.Name,
    };

    /// <summary>Restores the default formatter for the classes that run after this one in the same process.</summary>
    [GlobalCleanup]
    public void Cleanup() => Localizer.Current.MessageFormatter = StringFormatMessageFormatter.Instance;

    [Benchmark]
    public string GetWithMessageFormat() =>
        Localizer.Current.Get(
            BenchmarkStringsKeys.FilesDeleted,
            MessageArguments.FromTuples((nameof(Values.Count), Values.Count), (nameof(Values.Name), Values.Name))
        );

    [Benchmark(Baseline = true)]
    public string MessageFormatterDirect() =>
        this.messageFormatter.FormatMessage(BenchmarkStrings.FilesDeleted, this.values, BenchmarkCulture.German);

    [GlobalSetup]
    public void Setup()
    {
        BenchmarkCulture.Use(BenchmarkCulture.German);
        Localizer.Current.UseMessageFormat();

        // The expected text is the satellite's pattern formatted by a separate formatter, so both rows have to
        // produce what the MessageFormat library makes of the German text.
        var expected = new Jeffijoe.MessageFormat.MessageFormatter().FormatMessage(
            ExpectedViewTexts.Text(BenchmarkCulture.German, "FilesDeleted"),
            this.values,
            BenchmarkCulture.German
        );
        SelfCheck.Verify(this, _ => expected);
    }
}
