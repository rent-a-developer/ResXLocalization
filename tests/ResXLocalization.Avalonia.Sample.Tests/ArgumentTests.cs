using System.Reflection;
using Avalonia.Controls.Templates;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Styling;
using RentADeveloper.ResXLocalization.Core.Tests;

namespace RentADeveloper.ResXLocalization.Avalonia.Sample.Tests;

/// <summary>
/// Covers the format arguments of <c>{l:Localize}</c>, matching the sample's card 6: <see cref="Argument" />
/// entries supply constant or bound values, optionally named; the rendered text re-formats live on every
/// argument change and on every refresh of the localizer; and a localized text without arguments is not
/// formatted.
/// </summary>
public class ArgumentTests
{
    [AvaloniaFact]
    public void ArgumentChange_UpdatesTheText_Live()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();
        var viewModel = new TestViewModel { Count = 1 };

        var textBlock = TestSupport.BindLocalizedText(
            new(ApplicationStringsKeys.PeopleInvited) { Arguments = { Arg(new Binding("Count")) } },
            viewModel
        );
        textBlock.Text.Should().Be("1 people invited");

        viewModel.Count = 2;
        TestSupport.PumpUi();
        textBlock.Text.Should().Be("2 people invited");
    }

    [AvaloniaFact]
    public void BoundArgument_UpdatesLive_OnValueAndCultureChange()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();
        var viewModel = new TestViewModel { Count = 5 };

        var textBlock = TestSupport.BindLocalizedText(
            new(ApplicationStringsKeys.PeopleInvited) { Arguments = { Arg(new Binding("Count")) } },
            viewModel
        );
        textBlock.Text.Should().Be("5 people invited");

        viewModel.Count = 6;
        TestSupport.PumpUi();
        textBlock.Text.Should().Be("6 people invited");

        Localizer.Current.CurrentCulture = TestSupport.German;
        TestSupport.PumpUi();
        textBlock.Text.Should().Be("6 Personen eingeladen");
    }

    [AvaloniaFact]
    public void ConstantArguments_Format()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        var textBlock = TestSupport.BindLocalizedText(new("FormatPair") { Arguments = { Arg(3), Arg("10") } });

        textBlock.Text.Should().Be("File 3 of 10");
    }

    [AvaloniaFact]
    public void CultureChange_ReFormats_Live()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        var textBlock = TestSupport.BindLocalizedText(
            new(ApplicationStringsKeys.PeopleInvited) { Arguments = { Arg(new Binding("Count")) } },
            new TestViewModel { Count = 3 }
        );
        textBlock.Text.Should().Be("3 people invited");

        Localizer.Current.CurrentCulture = TestSupport.German;
        TestSupport.PumpUi();
        textBlock.Text.Should().Be("3 Personen eingeladen");
    }

    [AvaloniaFact]
    public void DuplicateOrEmptyNames_Throw()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        var duplicate = () =>
            new LocalizeExtension("FormatPair") { Arguments = { Arg("Count", 1), Arg("Count", 2) } }.ProvideValue(
                null!
            );
        var differentCase = () =>
            new LocalizeExtension("FormatPair") { Arguments = { Arg("A", 1), Arg("a", 2) } }.ProvideValue(null!);
        var empty = () =>
            new LocalizeExtension("FormatPair") { Arguments = { Arg(string.Empty, 1) } }.ProvideValue(null!);

        duplicate.Should().Throw<InvalidOperationException>().WithMessage("*Count*");
        differentCase.Should().NotThrow();
        empty.Should().Throw<InvalidOperationException>().WithMessage("*Name*");
    }

    [AvaloniaFact]
    public void FormattingFailure_RendersTheFailureText_AndRaisesLookupFailed()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();
        using var monitor = Localizer.Current.Monitor();

        var textBlock = TestSupport.BindLocalizedText(
            new("FormatBroken") { Arguments = { Arg(new Binding("Count")) } },
            new TestViewModel { Count = 1 }
        );

        textBlock.Text.Should().Be("!FormatBroken!");
        monitor
            .LookupFailures()
            .Should()
            .NotBeEmpty()
            .And.OnlyContain(failure =>
                failure.Reason == LookupFailureReason.MessageFormatFailed
                && failure.Key == "FormatBroken"
                && failure.Exception is FormatException
            );
    }

    [AvaloniaFact]
    public void InsideItemTemplate_EachItemFormatsItsOwnArguments()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        // Mirrors real item-template usage: the argument value (the item, via the DataContext) arrives only
        // after ProvideValue ran.
        var itemsControl = new ItemsControl
        {
            ItemsSource = new[] { 3, 7 },
            ItemTemplate = new FuncDataTemplate<int>(
                (_, _) =>
                {
                    var textBlock = new TextBlock();
                    textBlock.Bind(
                        TextBlock.TextProperty,
                        (BindingBase)
                            new LocalizeExtension(ApplicationStringsKeys.PeopleInvited)
                            {
                                Arguments = { Arg(new Binding()) },
                            }.ProvideValue(null!)
                    );
                    return textBlock;
                }
            ),
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
    public void InvalidKeyOrMissingValue_Throws()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        var noKey = () => new LocalizeExtension().ProvideValue(null!);
        var numberKey = () => new LocalizeExtension(42).ProvideValue(null!);
        var defaultResourceKey = () => new LocalizeExtension(default(ResourceKey)).ProvideValue(null!);
        var noValue = () => new LocalizeExtension("FormatPair") { Arguments = { new Argument() } }.ProvideValue(null!);

        noKey.Should().Throw<InvalidOperationException>().WithMessage("*Key*");
        numberKey.Should().Throw<InvalidOperationException>().WithMessage("*System.Int32*");
        defaultResourceKey.Should().Throw<ArgumentNullException>();
        noValue.Should().Throw<InvalidOperationException>().WithMessage("*Value*");
    }

    [AvaloniaFact]
    public void MissingKey_RendersTheFailureText()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();
        using var monitor = Localizer.Current.Monitor();

        var textBlock = TestSupport.BindLocalizedText(new("ThisKeyDoesNotExist") { Arguments = { Arg(1) } });

        textBlock.Text.Should().Be("!ThisKeyDoesNotExist!");
        monitor
            .LookupFailures()
            .Should()
            .NotBeEmpty()
            .And.OnlyContain(failure => failure.Reason == LookupFailureReason.TranslationNotFound);
    }

    [AvaloniaFact]
    public void MixedNamedAndUnnamedArguments_ReachTheFormatterWithPositionAndName()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();
        var previous = Localizer.Current.MessageFormatter;
        var formatter = new CapturingFormatter();
        Localizer.Current.MessageFormatter = formatter;

        try
        {
            var textBlock = TestSupport.BindLocalizedText(
                new("FormatPair") { Arguments = { Arg(new Binding("A")), Arg("Total", new Binding("B")) } },
                new TestViewModel { A = 3, B = 10 }
            );

            textBlock.Text.Should().Be("File 3 of 10");
            formatter
                .Calls[^1]
                .Arguments.Should()
                .Equal(new MessageArgument(0, null, 3), new MessageArgument(1, "Total", 10));
        }
        finally
        {
            Localizer.Current.MessageFormatter = previous;
        }
    }

    [AvaloniaFact]
    public void MultiBindingAndDynamicResourceValues_AreAccepted()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();
        var textBlock = new TextBlock();
        textBlock.Resources["Total"] = "10";
        var multiBinding = new MultiBinding
        {
            Bindings = { new Binding("A"), new Binding("B") },
            StringFormat = "{0}+{1}",
        };
        var dynamicResource = new DynamicResourceExtension("Total").ProvideValue(new EmptyServiceProvider());

        textBlock.Bind(
            TextBlock.TextProperty,
            (BindingBase)
                new LocalizeExtension("FormatPair")
                {
                    Arguments = { Arg(multiBinding), Arg(dynamicResource) },
                }.ProvideValue(null!)
        );
        textBlock.DataContext = new TestViewModel { A = 1, B = 2 };
        TestSupport.PumpUi();
        textBlock.Text.Should().Be("File 1+2 of 10");

        textBlock.Resources["Total"] = "11";
        TestSupport.PumpUi();
        textBlock.Text.Should().Be("File 1+2 of 11");
    }

    [AvaloniaFact]
    public void MultipleArguments_FormatAllPlaceholders()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        var textBlock = TestSupport.BindLocalizedText(
            new("FormatPair") { Arguments = { Arg(new Binding("A")), Arg(new Binding("B")) } },
            new TestViewModel { A = 2, B = 10 }
        );

        textBlock.Text.Should().Be("File 2 of 10");
    }

    [AvaloniaFact]
    public void NamedArguments_FormatByPosition()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        var textBlock = TestSupport.BindLocalizedText(
            new("PartitionDisplay")
            {
                Arguments =
                {
                    Arg("Label", new Binding("Label")),
                    Arg("Letter", new Binding("Letter")),
                    Arg("Drive", new Binding("Drive")),
                    Arg("Partition", new Binding("Partition")),
                },
            },
            new TestViewModel
            {
                Label = "My USB",
                Letter = "F",
                Drive = 2,
                Partition = 1,
            }
        );

        textBlock.Text.Should().Be("My USB (F:) - drive 2, partition 1");
    }

    [AvaloniaFact]
    public void NoArguments_ResolvesWithoutFormatting_KeepingLiteralBraces()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        // Without arguments the value must come back verbatim - composite formatting would throw on (or
        // mangle) the un-escaped literal braces.
        var textBlock = TestSupport.BindLocalizedText(new("CurlyNoArgs"));

        textBlock.Text.Should().Be("Literal {braces} stay");
    }

    [AvaloniaFact]
    public void NullArgument_IsSet_AndFormatsAsEmpty()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        // Null is a set argument (distinct from "never set"), and string.Format renders it as empty.
        var textBlock = TestSupport.BindLocalizedText(
            new(ApplicationStringsKeys.PeopleInvited) { Arguments = { Arg(null) } }
        );

        textBlock.Text.Should().Be(" people invited");
    }

    [AvaloniaFact]
    public void OneArgument_FormatsTheResolvedString()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        var textBlock = TestSupport.BindLocalizedText(
            new(ApplicationStringsKeys.PeopleInvited) { Arguments = { Arg(3) } }
        );

        textBlock.Text.Should().Be("3 people invited");
    }

    [AvaloniaFact]
    public void PublicSurface_HasTheDeclaredShape()
    {
        var value = typeof(Argument).GetProperty(nameof(Argument.Value))!;
        var key = typeof(LocalizeExtension).GetProperty(nameof(LocalizeExtension.Key))!;
        var arguments = typeof(LocalizeExtension).GetProperty(nameof(LocalizeExtension.Arguments))!;

        value.PropertyType.Should().Be<object>();
        key.PropertyType.Should().Be<object>();
        arguments.PropertyType.GetGenericArguments().Should().Equal(typeof(Argument));
        typeof(LocalizeExtension).GetProperty("ResourceKey").Should().BeNull();
        typeof(LocalizeExtension).GetProperty("Argument").Should().BeNull();
        value.GetCustomAttribute<AssignBindingAttribute>().Should().NotBeNull();
        key.GetCustomAttribute<AssignBindingAttribute>().Should().NotBeNull();
        arguments.GetCustomAttribute<global::Avalonia.Metadata.ContentAttribute>().Should().NotBeNull();
        typeof(Argument).IsAssignableTo(typeof(AvaloniaObject)).Should().BeFalse();
    }

    [AvaloniaFact]
    public void RefreshTrigger_ReResolves_OnFormatterTextBuilderAndRegistrationChange()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();
        var previousFormatter = Localizer.Current.MessageFormatter;
        var previousTextBuilder = Localizer.Current.LookupFailureTextBuilder;

        try
        {
            var invited = TestSupport.BindLocalizedText(
                new(ApplicationStringsKeys.PeopleInvited) { Arguments = { Arg(new Binding("Count")) } },
                new TestViewModel { Count = 5 }
            );
            var missing = TestSupport.BindLocalizedText(new("ThisKeyDoesNotExist"));
            Localizer.Current.UnregisterResourceManager(TestSupport.CatalogResources);
            var catalogOnly = TestSupport.BindLocalizedText(new("CatalogOnly"));

            invited.Text.Should().Be("5 people invited");
            missing.Text.Should().Be("!ThisKeyDoesNotExist!");
            catalogOnly.Text.Should().Be("!CatalogOnly!");

            Localizer.Current.MessageFormatter = new UpperCaseFormatter();
            TestSupport.PumpUi();
            invited.Text.Should().Be("{0} PEOPLE INVITED");
            missing.Text.Should().Be("!ThisKeyDoesNotExist!");
            catalogOnly.Text.Should().Be("!CatalogOnly!");

            // The failure text bypasses the formatter, so it is not upper-cased.
            Localizer.Current.LookupFailureTextBuilder = failure => $"[{failure.Key}]";
            TestSupport.PumpUi();
            missing.Text.Should().Be("[ThisKeyDoesNotExist]");
            catalogOnly.Text.Should().Be("[CatalogOnly]");

            Localizer.Current.RegisterResourceManager(TestSupport.CatalogResources);
            TestSupport.PumpUi();
            catalogOnly.Text.Should().Be("CATALOGONLYVALUE");
        }
        finally
        {
            Localizer.Current.MessageFormatter = previousFormatter;
            Localizer.Current.LookupFailureTextBuilder = previousTextBuilder;

            // Registering again appended Catalog after Fallback; restore the registration order.
            Localizer.Current.ClearResourceManagers();
            TestSupport.ResetToEnglishWithTestCatalogs();
        }
    }

    [AvaloniaFact]
    public void ScopedLookup_FormatsWithArguments()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        var textBlock = TestSupport.BindLocalizedText(
            new()
            {
                Key = "PeopleInvited",
                ResourceManager = ApplicationStringsKeys.ResourceManager,
                Arguments = { Arg(4) },
            }
        );

        textBlock.Text.Should().Be("4 people invited");
    }

    [AvaloniaFact]
    public void StyleSetter_FormatsWithArguments_WithoutATargetElement()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();

        var style = new Style(selector => selector.OfType<TextBlock>())
        {
            Setters =
            {
                new Setter(
                    TextBlock.TextProperty,
                    (BindingBase)
                        new LocalizeExtension("FormatPair")
                        {
                            Arguments = { Arg(new Binding("A")), Arg("7") },
                        }.ProvideValue(null!)
                ),
            },
        };
        var host = new StackPanel
        {
            DataContext = new TestViewModel { A = 3 },
            Children = { new TextBlock(), new TextBlock() },
        };
        host.Styles.Add(style);
        var window = new Window { Content = host };
        window.Show();
        TestSupport.PumpUi();

        host.Children.OfType<TextBlock>()
            .Select(textBlock => textBlock.Text)
            .Should()
            .Equal("File 3 of 7", "File 3 of 7");

        Localizer.Current.CurrentCulture = TestSupport.German;
        TestSupport.PumpUi();
        host.Children.OfType<TextBlock>()
            .Select(textBlock => textBlock.Text)
            .Should()
            .Equal("Datei 3 von 7", "Datei 3 von 7");

        window.Close();
    }

    [AvaloniaFact]
    public void TextAndToolTip_OnOneElement_HaveTheirOwnArguments()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();
        var textBlock = new TextBlock();

        textBlock.Bind(
            TextBlock.TextProperty,
            (BindingBase)
                new LocalizeExtension("FormatPair")
                {
                    Arguments = { Arg(new Binding("A")), Arg(new Binding("B")) },
                }.ProvideValue(null!)
        );
        textBlock.Bind(
            ToolTip.TipProperty,
            (BindingBase)
                new LocalizeExtension("FormatPair")
                {
                    Arguments = { Arg(new Binding("B")), Arg(new Binding("A")) },
                }.ProvideValue(null!)
        );
        textBlock.DataContext = new TestViewModel { A = 1, B = 2 };
        TestSupport.PumpUi();

        textBlock.Text.Should().Be("File 1 of 2");
        ToolTip.GetTip(textBlock).Should().Be("File 2 of 1");
    }

    [AvaloniaFact]
    public void TwelveArguments_HaveNoUpperLimit()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();
        var extension = new LocalizeExtension("Twelve");

        foreach (var letter in "abcdefghijkl")
        {
            extension.Arguments.Add(Arg(letter.ToString()));
        }

        var textBlock = TestSupport.BindLocalizedText(extension);

        textBlock.Text.Should().Be("abcdefghijkl");
    }

    [AvaloniaFact]
    public void UnresolvedAndNullValues_RenderEmpty_AndReachTheFormatterAsNull()
    {
        TestSupport.ResetToEnglishWithTestCatalogs();
        var previous = Localizer.Current.MessageFormatter;
        var formatter = new CapturingFormatter();
        Localizer.Current.MessageFormatter = formatter;

        // Installing the formatter re-resolves every localized binding still alive; count only this test's.
        TestSupport.PumpUi();
        var firstCall = formatter.Calls.Count;

        try
        {
            var unresolved = TestSupport.BindLocalizedText(
                new("FormatPair") { Arguments = { Arg(new Binding("A")), Arg(new Binding("B")) } }
            );
            var nulls = TestSupport.BindLocalizedText(
                new("FormatPair") { Arguments = { Arg(null), Arg(new Binding("Nothing")) } },
                new TestViewModel()
            );

            unresolved.Text.Should().Be("File  of ");
            nulls.Text.Should().Be("File  of ");
            formatter
                .Calls.Skip(firstCall)
                .SelectMany(call => call.Arguments)
                .Should()
                .NotBeEmpty()
                .And.OnlyContain(argument => argument.Value == null);
        }
        finally
        {
            Localizer.Current.MessageFormatter = previous;
        }
    }

    /// <summary>Creates an unnamed argument.</summary>
    /// <param name="value">The constant or the binding.</param>
    /// <returns>The argument.</returns>
    private static Argument Arg(object? value) => new() { Value = value };

    /// <summary>Creates a named argument.</summary>
    /// <param name="name">The name.</param>
    /// <param name="value">The constant or the binding.</param>
    /// <returns>The argument.</returns>
    private static Argument Arg(string name, object? value) => new() { Name = name, Value = value };

    /// <summary>A formatter that ignores the arguments and upper-cases the resolved value.</summary>
    internal sealed class UpperCaseFormatter : IMessageFormatter
    {
        /// <inheritdoc />
        public string Format(string pattern, MessageArguments arguments, CultureInfo culture) =>
            pattern.ToUpperInvariant();
    }

    /// <summary>A service provider without services, for a markup extension created in code.</summary>
    private sealed class EmptyServiceProvider : IServiceProvider
    {
        /// <inheritdoc />
        public object? GetService(Type serviceType) => null;
    }
}
