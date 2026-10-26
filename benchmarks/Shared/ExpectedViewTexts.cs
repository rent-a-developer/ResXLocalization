using System.Globalization;

namespace RentADeveloper.ResXLocalization.Benchmarks;

/// <summary>
/// The texts the self-checks expect, read from the project's <c>BenchmarkStrings</c> resx files: the neutral file
/// for English and the <c>.de</c> satellite for German. A German text is read from the satellite alone, without
/// falling back to the neutral file, so a missing satellite fails the self-check instead of letting every side
/// of a benchmark show English.
/// </summary>
internal static class ExpectedViewTexts
{
    /// <summary>The keys of every 10-element view except the enum view, element by element.</summary>
    private static readonly string[] ViewKeys =
    [
        "Title",
        "Save",
        "Cancel",
        "Open",
        "Close",
        "Delete",
        "UserMessages",
        "Enum_Priority_Priority1",
        "Enum_Priority_Priority2",
        "Enum_Priority_Priority3",
    ];

    /// <summary>
    /// Gets the English texts of a view without arguments, after a culture switch. They are the neutral file's,
    /// which <see cref="System.Resources.ResourceManager" /> holds under the invariant culture: there is no
    /// <c>.en</c> satellite, so <see cref="BenchmarkCulture.English" /> falls back to the neutral file at run
    /// time, and reading <c>en</c> without fallback would find nothing.
    /// </summary>
    public static IReadOnlyList<string> English => Texts(CultureInfo.InvariantCulture, ViewKeys);

    /// <summary>Gets the German texts of a view without arguments: <c>{x:Static}</c>, <c>{l:Localize}</c>, TranslationSource.</summary>
    public static IReadOnlyList<string> German => Texts(BenchmarkCulture.German, ViewKeys);

    /// <summary>Gets the German texts of the enum view, one per <see cref="Priority" /> member.</summary>
    public static IReadOnlyList<string> GermanEnum =>
        Texts(
            BenchmarkCulture.German,
            [.. Enum.GetValues<Priority>().Select(static value => $"Enum_Priority_{value}")]
        );

    /// <summary>Gets the German texts of the view whose elements all have the arguments of <see cref="ArgumentValues" />.</summary>
    public static IReadOnlyList<string> GermanWithArguments =>
        [
            .. German.Select(static text =>
                string.Format(
                    BenchmarkCulture.German,
                    text,
                    ArgumentValues.Instance.Name,
                    ArgumentValues.Instance.Count
                )
            ),
        ];

    /// <summary>The texts of <paramref name="viewCount" /> instances of a view, one after the other.</summary>
    /// <param name="view">The texts of one view.</param>
    /// <param name="viewCount">The number of views.</param>
    /// <returns>The texts of all views, in order.</returns>
    public static IReadOnlyList<string> Repeat(IReadOnlyList<string> view, int viewCount) =>
        [.. Enumerable.Repeat(view, viewCount).SelectMany(static texts => texts)];

    /// <summary>
    /// Reads the text of <paramref name="key" /> from the resx file of <paramref name="culture" />'s language, or
    /// from the neutral file for <see cref="CultureInfo.InvariantCulture" />, without falling back to another file.
    /// </summary>
    /// <param name="culture">A culture, such as <c>de-DE</c>, or <see cref="CultureInfo.InvariantCulture" />.</param>
    /// <param name="key">The resource key.</param>
    /// <returns>The text.</returns>
    /// <exception cref="InvalidOperationException">The file or the key does not exist.</exception>
    public static string Text(CultureInfo culture, string key)
    {
        var fileCulture =
            culture.IsNeutralCulture || culture.Equals(CultureInfo.InvariantCulture) ? culture : culture.Parent;
        var resourceSet =
            BenchmarkStrings.ResourceManager.GetResourceSet(fileCulture, createIfNotExists: true, tryParents: false)
            ?? throw new InvalidOperationException($"BenchmarkStrings has no resource file for '{fileCulture.Name}'.");

        return resourceSet.GetString(key)
            ?? throw new InvalidOperationException($"BenchmarkStrings ({fileCulture.Name}) has no key '{key}'.");
    }

    private static string[] Texts(CultureInfo culture, string[] keys) => [.. keys.Select(key => Text(culture, key))];
}
