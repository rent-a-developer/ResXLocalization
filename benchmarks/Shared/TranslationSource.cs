using System.ComponentModel;
using System.Globalization;

namespace RentADeveloper.ResXLocalization.Benchmarks;

/// <summary>
/// The usual hand-written way to switch languages live without ResXLocalization, and the baseline of the
/// culture switch: elements bind <c>{Binding [Key], Source={x:Static shared:TranslationSource.Instance}}</c>,
/// and setting <see cref="Culture" /> makes every such binding read its text again.
/// </summary>
public sealed class TranslationSource : INotifyPropertyChanged
{
    private CultureInfo culture = CultureInfo.CurrentUICulture;

    private TranslationSource() { }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Gets the one instance the views bind to.</summary>
    public static TranslationSource Instance { get; } = new();

    /// <summary>Gets or sets the culture the indexer looks up in, and notifies every indexer binding.</summary>
    public CultureInfo Culture
    {
        get => this.culture;
        set
        {
            this.culture = value;

            // The name that tells every indexer binding to read again: WPF's Binding.IndexerName, "Item[]", in the
            // WPF project, the only one with a -windows target framework; Avalonia's "Item" otherwise, because its
            // compiled indexer bindings ignore "Item[]". One framework-specific name, not both, and not an empty
            // name ("every property changed"), which both frameworks accept but handle at a higher cost: the
            // baseline does no more work than an application's own source.
#if WINDOWS
            this.PropertyChanged?.Invoke(this, new("Item[]"));
#else
            this.PropertyChanged?.Invoke(this, new("Item"));
#endif
        }
    }

    /// <summary>Gets the text of <paramref name="key" /> in <see cref="Culture" />.</summary>
    /// <param name="key">The resource key.</param>
    /// <returns>The text, or <paramref name="key" /> when there is none.</returns>
    public string this[string key] => BenchmarkStrings.ResourceManager.GetString(key, this.culture) ?? key;
}
