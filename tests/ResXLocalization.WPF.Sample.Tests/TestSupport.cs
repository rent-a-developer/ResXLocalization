using System.Windows.Markup;
using System.Xaml;

namespace RentADeveloper.ResXLocalization.WPF.Sample.Tests;

/// <summary>
/// Shared helpers for the WPF sample tests: access to the test-only resource files, WPF binding utilities,
/// and a dispatcher flush so bindings settle before assertions. Every method is expected to run on the
/// shared WPF dispatcher thread (see <see cref="WpfThread" />).
/// </summary>
internal static class TestSupport
{
    /// <summary>The test-only Catalog file; registered first so it wins search-all ties for "Shared".</summary>
    public static readonly ResourceManager CatalogResources = new(
        "RentADeveloper.ResXLocalization.WPF.Sample.Tests.Resources.Catalog",
        typeof(TestSupport).Assembly
    );

    /// <summary>English culture, used to reset the ambient localizer at the start of each test.</summary>
    public static readonly CultureInfo English = new("en");

    /// <summary>German culture. Used to assert that values switch live.</summary>
    public static readonly CultureInfo German = new("de");

    /// <summary>The test-only Fallback file; registered after <see cref="CatalogResources" />.</summary>
    private static readonly ResourceManager FallbackResources = new(
        "RentADeveloper.ResXLocalization.WPF.Sample.Tests.Resources.Fallback",
        typeof(TestSupport).Assembly
    );

    /// <summary>Collects the non-empty text of every descendant <see cref="TextBlock" /> in the logical tree.</summary>
    /// <param name="root">The root of the logical tree to inspect.</param>
    /// <returns>The non-empty text values in logical-tree traversal order.</returns>
    public static string[] AllVisibleText(DependencyObject root) =>
        LogicalDescendants(root)
            .OfType<TextBlock>()
            .Select(textBlock => textBlock.Text ?? string.Empty)
            .Where(text => text.Length > 0)
            .ToArray();

    /// <summary>
    /// Binds a <see cref="LocalizeEnumExtension" /> to a fresh text block's Text, then sets the text block's
    /// <c>DataContext</c> and flushes pending binding transfers.
    /// </summary>
    /// <param name="extension">The markup extension that creates the localization binding.</param>
    /// <param name="dataContext">
    /// The data context: the enumeration value itself for <c>Value = new Binding()</c>, or a view model.
    /// </param>
    /// <returns>The bound text block after pending binding transfers have been applied.</returns>
    public static TextBlock BindLocalizedEnum(LocalizeEnumExtension extension, object? dataContext)
    {
        var textBlock = new TextBlock();
        textBlock.SetBinding(
            TextBlock.TextProperty,
            (BindingBase)extension.ProvideValue(EmptyServiceProvider.Instance)
        );
        textBlock.DataContext = dataContext;
        Flush();
        return textBlock;
    }

    /// <summary>
    /// Binds a <see cref="LocalizeExtension" /> to a fresh text block's Text, then sets the text block's
    /// <c>DataContext</c> and flushes pending binding transfers.
    /// </summary>
    /// <param name="extension">The markup extension that creates the localization binding.</param>
    /// <param name="dataContext">The data context for the argument bindings, if any.</param>
    /// <returns>The bound text block after pending binding transfers have been applied.</returns>
    public static TextBlock BindLocalizedText(LocalizeExtension extension, object? dataContext = null)
    {
        var textBlock = new TextBlock();
        textBlock.SetBinding(
            TextBlock.TextProperty,
            (BindingBase)extension.ProvideValue(EmptyServiceProvider.Instance)
        );
        textBlock.DataContext = dataContext;
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
        Localizer.Current.RegisterResourceManager(ApplicationStringsKeys.ResourceManager);
        Localizer.Current.RegisterResourceManager(SortingStringsKeys.ResourceManager);
        Localizer.Current.RegisterResourceManager(BrandingStringsKeys.ResourceManager);
        Localizer.Current.RegisterResourceManager(CatalogResources);
        Localizer.Current.RegisterResourceManager(FallbackResources);
    }

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

    /// <summary>
    /// Forces <c>DesignMode.IsInDesignMode</c> to a value and restores the previous value on dispose. The suite
    /// runs without parallelization, so the process-wide value cannot leak into a concurrent test.
    /// </summary>
    internal sealed class DesignModeScope : IDisposable
    {
        private readonly bool previousValue;

        /// <summary>Initializes a new instance of the <see cref="DesignModeScope" /> class.</summary>
        /// <param name="value">The value <c>DesignMode.IsInDesignMode</c> has until the scope is disposed.</param>
        public DesignModeScope(bool value)
        {
            this.previousValue = DesignMode.IsInDesignMode;
            DesignMode.IsInDesignMode = value;
        }

        public void Dispose() => DesignMode.IsInDesignMode = this.previousValue;
    }

    /// <summary>An <see cref="IProvideValueTarget" /> with a fixed target object and target property.</summary>
    /// <param name="targetObject">The object whose property the markup extension provides a value for.</param>
    /// <param name="targetProperty">
    /// The target property: a <see cref="DependencyProperty" />, or a CLR <c>PropertyInfo</c> as Rider's WPF
    /// previewer supplies it.
    /// </param>
    internal sealed class SimpleProvideValueTarget(object targetObject, object targetProperty) : IProvideValueTarget
    {
        public object TargetObject { get; } = targetObject;

        public object TargetProperty { get; } = targetProperty;
    }

    /// <summary>
    /// A service provider that supplies an <see cref="IProvideValueTarget" /> and, as the XAML loader does, an
    /// <see cref="IXamlSchemaContextProvider" />. WPF needs the schema context to let a <see cref="Setter" />
    /// receive a binding for its <c>Value</c> property; without it, WPF throws for a <see cref="Setter" />.
    /// </summary>
    /// <param name="target">The target to supply.</param>
    internal sealed class SimpleServiceProvider(IProvideValueTarget target)
        : IServiceProvider,
            IXamlSchemaContextProvider
    {
        private static readonly XamlSchemaContext SharedSchemaContext = new();

        public XamlSchemaContext SchemaContext => SharedSchemaContext;

        public object? GetService(Type serviceType) =>
            serviceType switch
            {
                _ when serviceType == typeof(IProvideValueTarget) => target,
                _ when serviceType == typeof(IXamlSchemaContextProvider) => this,
                _ => null,
            };
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public static readonly EmptyServiceProvider Instance = new();

        public object? GetService(Type serviceType) => null;
    }
}
