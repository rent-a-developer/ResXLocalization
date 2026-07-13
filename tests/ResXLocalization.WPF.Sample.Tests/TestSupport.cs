namespace RentADeveloper.ResXLocalization.WPF.Sample.Tests;

/// <summary>
/// Shared helpers for the WPF sample tests: access to the test-only resource files, WPF binding utilities,
/// and a dispatcher flush so bindings settle before assertions. Every method is expected to run on the
/// shared WPF dispatcher thread (see <see cref="WpfThread" />).
/// </summary>
internal static class TestSupport
{
    /// <summary>Collects the non-empty text of every descendant <see cref="TextBlock" /> in the logical tree.</summary>
    /// <param name="root">The root of the logical tree to inspect.</param>
    /// <returns>The non-empty text values in logical-tree traversal order.</returns>
    public static String[] AllVisibleText(DependencyObject root) =>
        LogicalDescendants(root)
            .OfType<TextBlock>()
            .Select(textBlock => textBlock.Text ?? String.Empty)
            .Where(text => text.Length > 0)
            .ToArray();

    /// <summary>
    /// Binds a <see cref="LocalizeEnumExtension" /> to a fresh text block whose <c>DataContext</c> is the
    /// supplied enumeration value, mirroring how the extension is used inside an item template.
    /// </summary>
    /// <param name="extension">The markup extension that creates the localization binding.</param>
    /// <param name="value">The enumeration value to expose as the text block's data context.</param>
    /// <returns>The bound text block after pending binding transfers have been applied.</returns>
    public static TextBlock BindLocalizedEnum(LocalizeEnumExtension extension, Enum value)
    {
        var textBlock = new TextBlock { DataContext = value };
        textBlock.SetBinding(
            TextBlock.TextProperty,
            (BindingBase)extension.ProvideValue(EmptyServiceProvider.Instance)
        );
        Flush();
        return textBlock;
    }

    /// <summary>Binds a <see cref="LocalizeExtension" /> to a fresh text block's Text and returns it.</summary>
    /// <param name="extension">The markup extension that creates the localization binding.</param>
    /// <returns>The bound text block after pending binding transfers have been applied.</returns>
    public static TextBlock BindLocalizedText(LocalizeExtension extension)
    {
        var textBlock = new TextBlock();
        textBlock.SetBinding(
            TextBlock.TextProperty,
            (BindingBase)extension.ProvideValue(EmptyServiceProvider.Instance)
        );
        Flush();
        return textBlock;
    }

    /// <summary>Pumps the dispatcher down to Background priority so binding transfers apply.</summary>
    public static void Flush()
    {
        var frame = new DispatcherFrame();
        Dispatcher.CurrentDispatcher.BeginInvoke(
            DispatcherPriority.Background,
            new Action(() => frame.Continue = false)
        );
        Dispatcher.PushFrame(frame);
    }

    /// <summary>
    /// Resets the ambient localizer to English and registers the sample and test-only files in the same
    /// order the running app uses. Registration dedups by reference, so calling this from every test is safe.
    /// </summary>
    public static void ResetToEnglishWithTestCatalogs()
    {
        Localizer.Current.CurrentCulture = English;
        Localizer.Current.RegisterResourceManager(ApplicationStrings.ResourceManager);
        Localizer.Current.RegisterResourceManager(SortingStrings.ResourceManager);
        Localizer.Current.RegisterResourceManager(BrandingStrings.ResourceManager);
        Localizer.Current.RegisterResourceManager(CatalogResources);
        Localizer.Current.RegisterResourceManager(FallbackResources);
    }

    /// <summary>English culture, used to reset the ambient localizer at the start of each test.</summary>
    public static readonly CultureInfo English = new("en");

    /// <summary>German culture. Used to assert that values switch live.</summary>
    public static readonly CultureInfo German = new("de");

    private static IEnumerable<DependencyObject> LogicalDescendants(DependencyObject root)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
        {
            yield return child;

            foreach (var descendant in LogicalDescendants(child))
            {
                yield return descendant;
            }
        }
    }

    /// <summary>The test-only Catalog file; registered first so it wins search-all ties for "Shared".</summary>
    private static readonly ResourceManager CatalogResources =
        new("RentADeveloper.ResXLocalization.WPF.Sample.Tests.Resources.Catalog", typeof(TestSupport).Assembly);

    /// <summary>The test-only Fallback file; registered after <see cref="CatalogResources" />.</summary>
    private static readonly ResourceManager FallbackResources =
        new("RentADeveloper.ResXLocalization.WPF.Sample.Tests.Resources.Fallback", typeof(TestSupport).Assembly);

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public Object? GetService(Type serviceType) => null;

        public static readonly EmptyServiceProvider Instance = new();
    }
}
