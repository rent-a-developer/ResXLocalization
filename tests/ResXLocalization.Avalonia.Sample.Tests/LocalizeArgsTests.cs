using Avalonia.Controls.Templates;

namespace RentADeveloper.ResXLocalization.Avalonia.Sample.Tests;

/// <summary>
/// Covers the dynamic composite-format arguments of <c>{l:Localize}</c>, matching the sample's card 6:
/// the <see cref="LocalizeArgs" /> attached properties supply the arguments, the rendered text
/// re-formats live on every argument or culture change, and the no-argument behavior stays unchanged.
/// </summary>
public class LocalizeArgsTests
{
    [AvaloniaFact]
    public void OneArgument_FormatsTheResolvedString()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        var textBlock = TestSupport.BindLocalizedTextWithTarget(new(ApplicationStringsKeys.PeopleInvited));
        LocalizeArgs.SetArg0(textBlock, 3);
        TestSupport.PumpUi();

        textBlock.Text.Should().Be("3 people invited");
    }

    [AvaloniaFact]
    public void MultipleArguments_FormatAllPlaceholders()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        var textBlock = TestSupport.BindLocalizedTextWithTarget(new("FormatPair"));
        LocalizeArgs.SetArg0(textBlock, 2);
        LocalizeArgs.SetArg1(textBlock, 10);
        TestSupport.PumpUi();

        textBlock.Text.Should().Be("File 2 of 10");
    }

    [AvaloniaFact]
    public void ScopedLookup_FormatsWithArguments()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        var textBlock = TestSupport.BindLocalizedTextWithTarget(
            new() { Key = "PeopleInvited", ResourceManager = ApplicationStrings.ResourceManager }
        );
        LocalizeArgs.SetArg0(textBlock, 4);
        TestSupport.PumpUi();

        textBlock.Text.Should().Be("4 people invited");
    }

    [AvaloniaFact]
    public void ArgumentChange_UpdatesTheText_Live()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        var textBlock = TestSupport.BindLocalizedTextWithTarget(new(ApplicationStringsKeys.PeopleInvited));
        LocalizeArgs.SetArg0(textBlock, 1);
        TestSupport.PumpUi();
        textBlock.Text.Should().Be("1 people invited");

        LocalizeArgs.SetArg0(textBlock, 2);
        TestSupport.PumpUi();
        textBlock.Text.Should().Be("2 people invited");
    }

    [AvaloniaFact]
    public void CultureChange_ReFormats_Live()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        var textBlock = TestSupport.BindLocalizedTextWithTarget(new(ApplicationStringsKeys.PeopleInvited));
        LocalizeArgs.SetArg0(textBlock, 3);
        TestSupport.PumpUi();
        textBlock.Text.Should().Be("3 people invited");

        Localizer.Current.CurrentCulture = TestSupport.German;
        TestSupport.PumpUi();
        textBlock.Text.Should().Be("3 Personen eingeladen");
    }

    [AvaloniaFact]
    public void NoArguments_ResolvesWithoutFormatting_KeepingLiteralBraces()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        // No argument is set, so the value must come back verbatim - composite formatting would
        // throw on (or mangle) the un-escaped literal braces.
        var textBlock = TestSupport.BindLocalizedTextWithTarget(new("CurlyNoArgs"));

        textBlock.Text.Should().Be("Literal {braces} stay");
    }

    [AvaloniaFact]
    public void NullArgument_IsSet_AndFormatsAsEmpty()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        var textBlock = TestSupport.BindLocalizedTextWithTarget(new(ApplicationStringsKeys.PeopleInvited));
        LocalizeArgs.SetArg0(textBlock, null);
        TestSupport.PumpUi();

        // Null is a set argument (distinct from "never set"), and String.Format renders it as empty.
        textBlock.Text.Should().Be(" people invited");
    }

    [AvaloniaFact]
    public void HigherSlotAlone_FillsInteriorGapsWithNull()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        var textBlock = TestSupport.BindLocalizedTextWithTarget(new("FormatGap"));
        LocalizeArgs.SetArg2(textBlock, "X");
        TestSupport.PumpUi();

        // Only Arg2 is set: Arg0 and Arg1 become null, so {0} renders empty and {2} renders "X".
        textBlock.Text.Should().Be("[|X]");
    }

    [AvaloniaFact]
    public void MissingKey_ReturnsSentinelUnformatted()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        var textBlock = TestSupport.BindLocalizedTextWithTarget(new("NoSuchKeyEver"));
        LocalizeArgs.SetArg0(textBlock, 1);
        TestSupport.PumpUi();

        textBlock.Text.Should().Be("!NoSuchKeyEver!");
    }

    [AvaloniaFact]
    public void InsideItemTemplate_EachItemFormatsItsOwnArguments()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        // Mirrors real item-template usage: the extension's target is created per item, and the
        // argument value (the item, via the DataContext) arrives only after ProvideValue ran.
        var itemsControl = new ItemsControl
        {
            ItemsSource = new[] { 3, 7 },
            ItemTemplate = new FuncDataTemplate<Int32>((_, _) =>
                {
                    var textBlock = TestSupport.BindLocalizedTextWithTarget(
                        new(ApplicationStringsKeys.PeopleInvited)
                    );
                    textBlock.Bind(LocalizeArgs.Arg0Property, new Binding());
                    return textBlock;
                }
            )
        };

        var window = new Window { Content = itemsControl };
        window.Show();
        TestSupport.PumpUi();

        var texts = TestSupport.AllVisibleText(window);
        texts.Should().Contain("3 people invited");
        texts.Should().Contain("7 people invited");

        window.Close();
    }

    [AvaloniaFact]
    public void BoundControlsWithArguments_AreCollected_WhenDiscarded()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        var references = CreateAndAbandonBoundControls(200);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        references.Should().NotContain(reference => reference.IsAlive);
    }

    private static List<WeakReference> CreateAndAbandonBoundControls(Int32 count)
    {
        var references = new List<WeakReference>(count);
        for (var index = 0; index < count; index++)
        {
            var textBlock = TestSupport.BindLocalizedTextWithTarget(
                new(ApplicationStringsKeys.PeopleInvited)
            );
            LocalizeArgs.SetArg0(textBlock, index);
            references.Add(new(textBlock));
        }

        TestSupport.PumpUi();

        return references;
    }
}
