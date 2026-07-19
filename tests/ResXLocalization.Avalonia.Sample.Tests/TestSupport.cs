namespace RentADeveloper.ResXLocalization.Avalonia.Sample.Tests;

/// <summary>
/// Shared helpers for the sample tests: access to the test-only resource files, a few binding utilities,
/// and the minimal service-provider stand-ins a markup extension needs outside of the XAML loader.
/// </summary>
internal static class TestSupport
{
    /// <summary>Collects the non-empty text of every <see cref="TextBlock" /> beneath a visual.</summary>
    /// <param name="root">The visual whose descendants to inspect.</param>
    /// <returns>The non-empty text values in visual-tree traversal order.</returns>
    public static String[] AllVisibleText(Visual root) =>
        root.GetVisualDescendants().OfType<TextBlock>()
            .Select(static textBlock => textBlock.Text ?? String.Empty)
            .Where(static text => text.Length > 0)
            .ToArray();

    /// <summary>
    /// Binds a <see cref="LocalizeEnumExtension" /> to a fresh text block whose <c>DataContext</c> is the
    /// supplied enumeration value, mirroring how the extension is used inside an item template.
    /// </summary>
    /// <param name="extension">The markup extension that creates the localization binding.</param>
    /// <param name="value">The enumeration value to expose as the text block's data context.</param>
    /// <returns>The bound text block after pending UI work has been processed.</returns>
    public static TextBlock BindLocalizedEnum(LocalizeEnumExtension extension, Enum value)
    {
        var textBlock = new TextBlock();
        var binding = (BindingBase)extension.ProvideValue(
            new SimpleServiceProvider(new SimpleProvideValueTarget(textBlock))
        );
        textBlock.Bind(TextBlock.TextProperty, binding);
        textBlock.DataContext = value;
        Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);
        return textBlock;
    }

    /// <summary>Binds a <see cref="LocalizeExtension" /> to a fresh text block's Text and returns it.</summary>
    /// <param name="extension">The markup extension that creates the localization binding.</param>
    /// <returns>The bound text block.</returns>
    public static TextBlock BindLocalizedText(LocalizeExtension extension)
    {
        var textBlock = new TextBlock();
        textBlock.Bind(TextBlock.TextProperty, (BindingBase)extension.ProvideValue(null!));
        return textBlock;
    }

    /// <summary>
    /// Binds a <see cref="LocalizeExtension" /> to a fresh text block's Text, supplying the text block
    /// as the extension's target - mirroring real XAML usage - so the produced binding can read the
    /// <see cref="LocalizeArgs" /> arguments set on it.
    /// </summary>
    /// <param name="extension">The markup extension that creates the localization binding.</param>
    /// <returns>The bound text block.</returns>
    public static TextBlock BindLocalizedTextWithTarget(LocalizeExtension extension)
    {
        var textBlock = new TextBlock();
        var binding = (BindingBase)extension.ProvideValue(
            new SimpleServiceProvider(new SimpleProvideValueTarget(textBlock))
        );
        textBlock.Bind(TextBlock.TextProperty, binding);
        return textBlock;
    }

    /// <summary>Pumps queued UI work so that bindings settle before assertions.</summary>
    public static void PumpUi() =>
        Dispatcher.UIThread.RunJobs(DispatcherPriority.Loaded);

    /// <summary>
    /// Resets the ambient localizer to English and registers the two test-only files. Registration dedups
    /// by reference, so calling this from every test is safe.
    /// </summary>
    public static void ResetToEnglishWithTestCatalogs()
    {
        Localizer.Current.CurrentCulture = English;
        Localizer.Current.RegisterResourceManager(CatalogResources);
        Localizer.Current.RegisterResourceManager(FallbackResources);
    }

    /// <summary>English culture, used to reset the ambient localizer at the start of each test.</summary>
    public static readonly CultureInfo English = new("en");

    /// <summary>German culture. Used to assert that values switch live.</summary>
    public static readonly CultureInfo German = new("de");

    /// <summary>The test-only Catalog file; registered first so it wins search-all ties for "Shared".</summary>
    private static readonly ResourceManager CatalogResources =
        new("RentADeveloper.ResXLocalization.Avalonia.Sample.Tests.Resources.Catalog", typeof(TestSupport).Assembly);

    /// <summary>The test-only Fallback file; registered after <see cref="CatalogResources" />.</summary>
    private static readonly ResourceManager FallbackResources =
        new("RentADeveloper.ResXLocalization.Avalonia.Sample.Tests.Resources.Fallback", typeof(TestSupport).Assembly);

    private sealed class SimpleProvideValueTarget(Object targetObject) : IProvideValueTarget
    {
        public Object TargetObject { get; } = targetObject;

        public Object TargetProperty => null!;
    }

    private sealed class SimpleServiceProvider(IProvideValueTarget target) : IServiceProvider
    {
        public Object? GetService(Type serviceType) =>
            serviceType == typeof(IProvideValueTarget) ? target : null;
    }
}
