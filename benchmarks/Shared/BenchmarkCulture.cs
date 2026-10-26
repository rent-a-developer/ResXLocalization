using System.Globalization;

namespace RentADeveloper.ResXLocalization.Benchmarks;

/// <summary>The cultures the benchmarks look up in, and the setup that makes every side use the same one.</summary>
internal static class BenchmarkCulture
{
    /// <summary>The culture the culture-switch benchmarks switch to and from.</summary>
    public static readonly CultureInfo English = CultureInfo.GetCultureInfo("en");

    /// <summary>
    /// The culture every benchmark looks up in, as most applications ship: a specific culture with a satellite
    /// for its language only, so every lookup walks <c>de-DE -&gt; de</c>.
    /// </summary>
    public static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    /// <summary>
    /// Sets the culture of every side a benchmark compares to <paramref name="culture" />: the designer class,
    /// which reads <see cref="CultureInfo.CurrentUICulture" /> while its <c>Culture</c> property is unset,
    /// <see cref="Localizer.Current" /> and <see cref="TranslationSource.Instance" />. Setups call it on the thread
    /// the benchmark runs on; a culture-switch benchmark instead sets only the side it measures.
    /// </summary>
    /// <param name="culture">The culture every side looks up in.</param>
    public static void Use(CultureInfo culture)
    {
        CultureInfo.CurrentUICulture = culture;
        Localizer.Current.CurrentCulture = culture;
        TranslationSource.Instance.Culture = culture;
    }
}
