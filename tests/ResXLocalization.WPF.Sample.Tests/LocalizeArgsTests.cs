using System.Windows.Markup;
using System.Windows.Media;

namespace RentADeveloper.ResXLocalization.WPF.Sample.Tests;

/// <summary>
/// Covers the dynamic composite-format arguments of <c>{l:Localize}</c>, matching the sample's card 6:
/// the <see cref="LocalizeArgs" /> attached properties supply the arguments, the rendered text
/// re-formats live on every argument or culture change, and the no-argument behavior stays unchanged.
/// </summary>
public class LocalizeArgsTests
{
    [Fact]
    public void OneArgument_FormatsTheResolvedString() => WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            var textBlock = TestSupport.BindLocalizedText(new(ApplicationStringsKeys.PeopleInvited));
            LocalizeArgs.SetArg0(textBlock, 3);
            TestSupport.Flush();

            textBlock.Text.Should().Be("3 people invited");
        }
    );

    [Fact]
    public void MultipleArguments_FormatAllPlaceholders() => WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            var textBlock = TestSupport.BindLocalizedText(new("FormatPair"));
            LocalizeArgs.SetArg0(textBlock, 2);
            LocalizeArgs.SetArg1(textBlock, 10);
            TestSupport.Flush();

            textBlock.Text.Should().Be("File 2 of 10");
        }
    );

    [Fact]
    public void ScopedLookup_FormatsWithArguments() => WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            var textBlock = TestSupport.BindLocalizedText(
                new() { Key = "PeopleInvited", ResourceManager = ApplicationStrings.ResourceManager }
            );
            LocalizeArgs.SetArg0(textBlock, 4);
            TestSupport.Flush();

            textBlock.Text.Should().Be("4 people invited");
        }
    );

    [Fact]
    public void ArgumentChange_UpdatesTheText_Live() => WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            var textBlock = TestSupport.BindLocalizedText(new(ApplicationStringsKeys.PeopleInvited));
            LocalizeArgs.SetArg0(textBlock, 1);
            TestSupport.Flush();
            textBlock.Text.Should().Be("1 people invited");

            LocalizeArgs.SetArg0(textBlock, 2);
            TestSupport.Flush();
            textBlock.Text.Should().Be("2 people invited");
        }
    );

    [Fact]
    public void CultureChange_ReFormats_Live() => WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            var textBlock = TestSupport.BindLocalizedText(new(ApplicationStringsKeys.PeopleInvited));
            LocalizeArgs.SetArg0(textBlock, 3);
            TestSupport.Flush();
            textBlock.Text.Should().Be("3 people invited");

            Localizer.Current.CurrentCulture = TestSupport.German;
            TestSupport.Flush();
            textBlock.Text.Should().Be("3 Personen eingeladen");
        }
    );

    [Fact]
    public void NoArguments_ResolvesWithoutFormatting_KeepingLiteralBraces() => WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            // No argument is set, so the value must come back verbatim - composite formatting would
            // throw on (or mangle) the un-escaped literal braces.
            var textBlock = TestSupport.BindLocalizedText(new("CurlyNoArgs"));

            textBlock.Text.Should().Be("Literal {braces} stay");
        }
    );

    [Fact]
    public void NullArgument_IsSet_AndFormatsAsEmpty() => WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            var textBlock = TestSupport.BindLocalizedText(new(ApplicationStringsKeys.PeopleInvited));
            LocalizeArgs.SetArg0(textBlock, null);
            TestSupport.Flush();

            // Null is a set argument (distinct from "never set"), and String.Format renders it as empty.
            textBlock.Text.Should().Be(" people invited");
        }
    );

    [Fact]
    public void HigherSlotAlone_FillsInteriorGapsWithNull() => WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            var textBlock = TestSupport.BindLocalizedText(new("FormatGap"));
            LocalizeArgs.SetArg2(textBlock, "X");
            TestSupport.Flush();

            // Only Arg2 is set: Arg0 and Arg1 become null, so {0} renders empty and {2} renders "X".
            textBlock.Text.Should().Be("[|X]");
        }
    );

    [Fact]
    public void MissingKey_ReturnsSentinelUnformatted() => WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            var textBlock = TestSupport.BindLocalizedText(new("NoSuchKeyEver"));
            LocalizeArgs.SetArg0(textBlock, 1);
            TestSupport.Flush();

            textBlock.Text.Should().Be("!NoSuchKeyEver!");
        }
    );

    [Fact]
    public void InsideDataTemplate_EachItemFormatsItsOwnArguments() => WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            // Parsed (not compiled) XAML still exercises the real WPF template deferral: ProvideValue
            // runs once while the template loads, and the RelativeSource.Self bindings must resolve
            // per applied item, with the argument (the item, via the DataContext) arriving later.
            const String xaml =
                """
                <ItemsControl xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                              xmlns:l="clr-namespace:RentADeveloper.ResXLocalization.WPF;assembly=ResXLocalization.WPF"
                              xmlns:res="clr-namespace:RentADeveloper.ResXLocalization.WPF.Sample.Resources;assembly=ResXLocalization.WPF.Sample"
                              xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                  <ItemsControl.ItemTemplate>
                    <DataTemplate>
                      <TextBlock l:LocalizeArgs.Arg0="{Binding}"
                                 Text="{l:Localize {x:Static res:ApplicationStringsKeys.PeopleInvited}}" />
                    </DataTemplate>
                  </ItemsControl.ItemTemplate>
                </ItemsControl>
                """;

            var itemsControl = (ItemsControl)XamlReader.Parse(xaml);
            itemsControl.ItemsSource = TemplateItems;

            // Realize the item containers and flush binding transfers without an interactive session.
            itemsControl.Measure(new(500, 500));
            itemsControl.Arrange(new(0, 0, 500, 500));
            itemsControl.UpdateLayout();
            TestSupport.Flush();

            var texts = VisualTexts(itemsControl);
            texts.Should().Contain("3 people invited");
            texts.Should().Contain("7 people invited");
        }
    );

    [Fact]
    public void BoundControlsWithArguments_AreCollected_WhenDiscarded() => WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            var references = CreateAndAbandonBoundControls(200);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            references.Should().NotContain(reference => reference.IsAlive);
        }
    );

    /// <summary>The items rendered by the data-template test; each becomes one Arg0 value.</summary>
    private static readonly Int32[] TemplateItems = [3, 7];

    private static List<WeakReference> CreateAndAbandonBoundControls(Int32 count)
    {
        var references = new List<WeakReference>(count);
        for (var index = 0; index < count; index++)
        {
            var textBlock = TestSupport.BindLocalizedText(new(ApplicationStringsKeys.PeopleInvited));
            LocalizeArgs.SetArg0(textBlock, index);
            references.Add(new(textBlock));
        }

        TestSupport.Flush();

        return references;
    }

    /// <summary>Collects the non-empty text of every <see cref="TextBlock" /> in the visual tree.</summary>
    /// <param name="root">The root of the visual tree to inspect.</param>
    /// <returns>The non-empty text values in visual-tree traversal order.</returns>
    private static String[] VisualTexts(DependencyObject root) =>
        VisualDescendants(root)
            .OfType<TextBlock>()
            .Select(textBlock => textBlock.Text ?? String.Empty)
            .Where(text => text.Length > 0)
            .ToArray();

    private static IEnumerable<DependencyObject> VisualDescendants(DependencyObject root)
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            yield return child;

            foreach (var descendant in VisualDescendants(child))
            {
                yield return descendant;
            }
        }
    }
}
