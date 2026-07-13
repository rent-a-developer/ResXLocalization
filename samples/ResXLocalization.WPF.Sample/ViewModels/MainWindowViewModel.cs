using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using RentADeveloper.ResXLocalization.WPF.Sample.Models;
using RentADeveloper.ResXLocalization.WPF.Sample.Resources;

namespace RentADeveloper.ResXLocalization.WPF.Sample.ViewModels;

/// <summary>
/// The view model for <see cref="Views.MainWindow" />. It drives the language switcher and the sort-order
/// selection, and it exposes the results of every non-formatting <c>Get</c> overload plus the indexer
/// as properties so the view can bind to them. Those properties are recomputed whenever the language
/// changes, which is what makes the bottom section of the window switch live.
/// </summary>
public sealed partial class MainWindowViewModel : ObservableObject, IDisposable
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel" /> class.
    /// </summary>
    /// <param name="localizer">
    /// The localization service. It is injected (rather than reaching for <see cref="Localizer.Current" />
    /// directly) so the view model can be unit-tested against the same ambient instance the app uses.
    /// </param>
    public MainWindowViewModel(ILocalizer localizer)
    {
        this.localizer = localizer;
        this.SelectedLanguage =
            Array.Find(this.SupportedLanguages, language => language.Name == localizer.CurrentCulture.Name)
            ?? this.SupportedLanguages[0];

        // The code-behind read-outs are computed on demand from the localizer. A culture switch does not
        // change any backing field, so raise the notifications ourselves to make those bindings refresh.
        localizer.CultureChanged += this.OnLocalizerCultureChanged;
    }

    /// <summary>Releases the culture-change subscription when the owning window closes.</summary>
    public void Dispose()
    {
        this.localizer.CultureChanged -= this.OnLocalizerCultureChanged;
        GC.SuppressFinalize(this);
    }

    /// <summary>Gets all values of <see cref="FileSortOrder" />, shown in the sort-order combo boxes.</summary>
    public FileSortOrder[] FileSortOrders { get; } = Enum.GetValues<FileSortOrder>();

    /// <summary>Gets the greeting via the indexer, which is shorthand for the search-all <c>Get(string)</c>.</summary>
    public String GreetingViaIndexer => this.localizer["Greeting"];

    /// <summary>Gets the greeting via the scoped overload that names one file: <c>Get(string, ResourceManager)</c>.</summary>
    public String GreetingViaScopedManager =>
        this.localizer.Get("Greeting", ApplicationStrings.ResourceManager);

    // ---- Code-behind string lookups (each maps to one ILocalizer member) ------------------------------

    /// <summary>Gets the greeting via the key-only, search-all overload: <c>Get(string)</c>.</summary>
    public String GreetingViaSearchAll => this.localizer.Get("Greeting");

    /// <summary>Gets the greeting via the typed-key overload: <c>Get(ResourceKey)</c>.</summary>
    public String GreetingViaTypedKey => this.localizer.Get(ApplicationStringsKeys.Greeting);

    /// <summary>
    /// Gets a deliberately missing key, to show the engine's miss marker: a lookup that finds nothing returns
    /// <c>!key!</c> rather than throwing or returning null.
    /// </summary>
    public String MissingKeyExample => this.localizer.Get("ThisKeyDoesNotExist");

    /// <summary>
    /// Gets the branding text scoped to the internal <c>BrandingStrings</c> file from code-behind. XAML cannot
    /// name an internal <c>ResourceManager</c>, but ordinary C# in the same assembly can.
    /// </summary>
    public String PoweredByViaScopedInternalManager =>
        this.localizer.Get("PoweredBy", BrandingStrings.ResourceManager);

    /// <summary>
    /// Gets the branding text via a typed key. This resolves even though <c>BrandingStrings</c> has an internal
    /// accessor, because the generated <c>BrandingStringsKeys</c> class is always public.
    /// </summary>
    public String PoweredByViaTypedKey => this.localizer.Get(BrandingStringsKeys.PoweredBy);

    /// <summary>The selected sort order. Changing it refreshes the four enum read-outs below.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SortOrderViaSearchAllDefaultPrefix))]
    [NotifyPropertyChangedFor(nameof(SortOrderViaSearchAllCustomPrefix))]
    [NotifyPropertyChangedFor(nameof(SortOrderViaScopedDefaultPrefix))]
    [NotifyPropertyChangedFor(nameof(SortOrderViaScopedCustomPrefix))]
    public partial FileSortOrder SelectedFileSortOrder { get; set; } = FileSortOrder.Ascending;

    /// <summary>
    /// The selected language. Assigning a different value switches the whole UI by setting
    /// <see cref="ILocalizer.CurrentCulture" />, which the engine broadcasts to every localized binding.
    /// </summary>
    [ObservableProperty]
    public partial CultureInfo SelectedLanguage { get; set; }

    /// <summary>Gets the selection via <c>Get(Enum, ResourceManager, keyPrefix)</c>: scoped, custom prefix.</summary>
    public String SortOrderViaScopedCustomPrefix =>
        this.localizer.Get(this.SelectedFileSortOrder, SortingStrings.ResourceManager, "Display_");

    /// <summary>Gets the selection via <c>Get(Enum, ResourceManager)</c>: scoped, default prefix.</summary>
    public String SortOrderViaScopedDefaultPrefix =>
        this.localizer.Get(this.SelectedFileSortOrder, ApplicationStrings.ResourceManager);

    /// <summary>Gets the selection via <c>Get(Enum, keyPrefix)</c>: search-all with a custom prefix.</summary>
    public String SortOrderViaSearchAllCustomPrefix =>
        this.localizer.Get(this.SelectedFileSortOrder, "Display_");

    // ---- Code-behind enum lookups (the selected FileSortOrder via each enum Get overload) --------------

    /// <summary>Gets the selection via <c>Get(Enum)</c>: search-all with the default <c>Enum_</c> prefix.</summary>
    public String SortOrderViaSearchAllDefaultPrefix => this.localizer.Get(this.SelectedFileSortOrder);

    /// <summary>Gets the languages offered in the language switcher.</summary>
    public CultureInfo[] SupportedLanguages { get; } =
    [
        new("en"),
        new("de")
    ];

    private void OnLocalizerCultureChanged(Object? sender, CultureChangedEventArgs e)
    {
        foreach (var propertyName in CultureSensitiveProperties)
        {
            this.OnPropertyChanged(propertyName);
        }
    }

    partial void OnSelectedLanguageChanged(CultureInfo value) =>
        this.localizer.CurrentCulture = value;

    private readonly ILocalizer localizer;

    /// <summary>
    /// The read-out properties have no backing field, so a culture switch must notify them explicitly.
    /// </summary>
    private static readonly String[] CultureSensitiveProperties =
    [
        nameof(GreetingViaSearchAll),
        nameof(GreetingViaIndexer),
        nameof(GreetingViaTypedKey),
        nameof(GreetingViaScopedManager),
        nameof(PoweredByViaTypedKey),
        nameof(PoweredByViaScopedInternalManager),
        nameof(MissingKeyExample),
        nameof(SortOrderViaSearchAllDefaultPrefix),
        nameof(SortOrderViaSearchAllCustomPrefix),
        nameof(SortOrderViaScopedDefaultPrefix),
        nameof(SortOrderViaScopedCustomPrefix)
    ];
}
