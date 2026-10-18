using System.Reflection;
using System.Windows.Markup;
using System.Windows.Media;
using RentADeveloper.ResXLocalization.Core.Tests;

namespace RentADeveloper.ResXLocalization.WPF.Sample.Tests;

/// <summary>
/// Covers the format arguments of <c>{l:Localize}</c>, matching the sample's card 6: <see cref="Argument" />
/// entries supply constant or bound values, optionally named; the rendered text re-formats live on every
/// argument change and on every refresh of the localizer; and a localized text without arguments is not
/// formatted.
/// </summary>
public class ArgumentTests
{
    /// <summary>The XML namespace declarations of the parsed XAML snippets.</summary>
    private const string Namespaces = """
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:l="clr-namespace:RentADeveloper.ResXLocalization.WPF;assembly=ResXLocalization.WPF"
        xmlns:res="clr-namespace:RentADeveloper.ResXLocalization.WPF.Sample.Resources;assembly=ResXLocalization.WPF.Sample"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        """;

    /// <summary>The items rendered by the data-template test; each becomes one argument value.</summary>
    private static readonly int[] TemplateItems = [3, 7];

    [Fact]
    public void ArgumentChange_UpdatesTheText_Live() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();
            var viewModel = new TestViewModel { Count = 1 };

            var textBlock = TestSupport.BindLocalizedText(
                new(ApplicationStringsKeys.PeopleInvited) { Arguments = { Arg(new Binding("Count")) } },
                viewModel
            );
            textBlock.Text.Should().Be("1 people invited");

            viewModel.Count = 2;
            TestSupport.Flush();
            textBlock.Text.Should().Be("2 people invited");
        });

    [Fact]
    public void BoundArgument_UpdatesLive_OnValueAndCultureChange() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();
            var viewModel = new TestViewModel { Count = 5 };

            var textBlock = TestSupport.BindLocalizedText(
                new(ApplicationStringsKeys.PeopleInvited) { Arguments = { Arg(new Binding("Count")) } },
                viewModel
            );
            textBlock.Text.Should().Be("5 people invited");

            viewModel.Count = 6;
            TestSupport.Flush();
            textBlock.Text.Should().Be("6 people invited");

            Localizer.Current.CurrentCulture = TestSupport.German;
            TestSupport.Flush();
            textBlock.Text.Should().Be("6 Personen eingeladen");
        });

    [Fact]
    public void ConstantArguments_Format() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            var textBlock = TestSupport.BindLocalizedText(new("FormatPair") { Arguments = { Arg(3), Arg("10") } });

            textBlock.Text.Should().Be("File 3 of 10");
        });

    [Fact]
    public void CultureChange_ReFormats_Live() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            var textBlock = TestSupport.BindLocalizedText(
                new(ApplicationStringsKeys.PeopleInvited) { Arguments = { Arg(new Binding("Count")) } },
                new TestViewModel { Count = 3 }
            );
            textBlock.Text.Should().Be("3 people invited");

            Localizer.Current.CurrentCulture = TestSupport.German;
            TestSupport.Flush();
            textBlock.Text.Should().Be("3 Personen eingeladen");
        });

    [Fact]
    public void DuplicateOrEmptyNames_Throw() =>
        WpfThread.Invoke(() =>
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
        });

    [Fact]
    public void FormattingFailure_RendersTheFailureText_AndRaisesLookupFailed() =>
        WpfThread.Invoke(() =>
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
        });

    [Fact]
    public void InsideItemTemplate_EachItemFormatsItsOwnArguments() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            // Parsed (not compiled) XAML still exercises the real WPF template deferral: ProvideValue runs
            // per applied item, and the argument value (the item, via the DataContext) arrives later.
            const string xaml = $$"""
                <ItemsControl {{Namespaces}}>
                  <ItemsControl.ItemTemplate>
                    <DataTemplate>
                      <TextBlock>
                        <TextBlock.Text>
                          <l:Localize Key="{x:Static res:ApplicationStringsKeys.PeopleInvited}">
                            <l:Argument Value="{Binding}" />
                          </l:Localize>
                        </TextBlock.Text>
                      </TextBlock>
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
        });

    [Fact]
    public void InvalidKeyOrMissingValue_Throws() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            var noKey = () => new LocalizeExtension().ProvideValue(null!);
            var numberKey = () => new LocalizeExtension(42).ProvideValue(null!);
            var defaultResourceKey = () => new LocalizeExtension(default(ResourceKey)).ProvideValue(null!);
            var noValue = () =>
                new LocalizeExtension("FormatPair") { Arguments = { new Argument() } }.ProvideValue(null!);

            noKey.Should().Throw<InvalidOperationException>().WithMessage("*Key*");
            numberKey.Should().Throw<InvalidOperationException>().WithMessage("*System.Int32*");
            defaultResourceKey.Should().Throw<ArgumentNullException>();
            noValue.Should().Throw<InvalidOperationException>().WithMessage("*Value*");
        });

    [Fact]
    public void MissingKey_RendersTheFailureText() =>
        WpfThread.Invoke(() =>
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
        });

    [Fact]
    public void MixedNamedAndUnnamedArguments_ReachTheFormatterWithPositionAndName() =>
        WpfThread.Invoke(() =>
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
        });

    [Fact]
    public void MultiBindingOrPriorityBindingValue_Throws() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            var multiBinding = () =>
                new LocalizeExtension("FormatPair") { Arguments = { Arg(new MultiBinding()) } }.ProvideValue(null!);
            var priorityBinding = () =>
                new LocalizeExtension("FormatPair") { Arguments = { Arg(new PriorityBinding()) } }.ProvideValue(null!);

            multiBinding.Should().Throw<InvalidOperationException>().WithMessage("*MultiBinding*");
            priorityBinding.Should().Throw<InvalidOperationException>().WithMessage("*PriorityBinding*");
        });

    [Fact]
    public void MultipleArguments_FormatAllPlaceholders() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            var textBlock = TestSupport.BindLocalizedText(
                new("FormatPair") { Arguments = { Arg(new Binding("A")), Arg(new Binding("B")) } },
                new TestViewModel { A = 2, B = 10 }
            );

            textBlock.Text.Should().Be("File 2 of 10");
        });

    [Fact]
    public void NamedArguments_FormatByPosition() =>
        WpfThread.Invoke(() =>
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
        });

    [Fact]
    public void NoArguments_ResolvesWithoutFormatting_KeepingLiteralBraces() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            // Without arguments the value must come back verbatim - composite formatting would throw on (or
            // mangle) the un-escaped literal braces.
            var textBlock = TestSupport.BindLocalizedText(new("CurlyNoArgs"));

            textBlock.Text.Should().Be("Literal {braces} stay");
        });

    [Fact]
    public void NullArgument_IsSet_AndFormatsAsEmpty() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            // Null is a set argument (distinct from "never set"), and string.Format renders it as empty.
            var textBlock = TestSupport.BindLocalizedText(
                new(ApplicationStringsKeys.PeopleInvited) { Arguments = { Arg(null) } }
            );

            textBlock.Text.Should().Be(" people invited");
        });

    [Fact]
    public void OneArgument_FormatsTheResolvedString() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            var textBlock = TestSupport.BindLocalizedText(
                new(ApplicationStringsKeys.PeopleInvited) { Arguments = { Arg(3) } }
            );

            textBlock.Text.Should().Be("3 people invited");
        });

    [Fact]
    public void ParsedXaml_AssignsTheArgumentBinding_AndPassesXNullAsNull() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            const string xaml = $$"""
                <TextBlock {{Namespaces}}>
                  <TextBlock.Text>
                    <l:Localize Key="FormatPair">
                      <l:Argument Value="{Binding A}" />
                      <l:Argument Value="{x:Null}" />
                    </l:Localize>
                  </TextBlock.Text>
                </TextBlock>
                """;

            var textBlock = (TextBlock)XamlReader.Parse(xaml);
            textBlock.DataContext = new TestViewModel { A = 3 };
            TestSupport.Flush();

            textBlock.Text.Should().Be("File 3 of ");
        });

    [Fact]
    public void PublicSurface_HasTheDeclaredShape() =>
        WpfThread.Invoke(() =>
        {
            var value = typeof(Argument).GetProperty(nameof(Argument.Value))!;
            var key = typeof(LocalizeExtension).GetProperty(nameof(LocalizeExtension.Key))!;
            var arguments = typeof(LocalizeExtension).GetProperty(nameof(LocalizeExtension.Arguments))!;

            value.PropertyType.Should().Be<object>();
            key.PropertyType.Should().Be<object>();
            arguments.PropertyType.GetGenericArguments().Should().Equal(typeof(Argument));
            typeof(LocalizeExtension).GetProperty("ResourceKey").Should().BeNull();
            typeof(LocalizeExtension).GetProperty("Argument").Should().BeNull();
            typeof(Argument).GetCustomAttribute<XamlSetMarkupExtensionAttribute>().Should().NotBeNull();
            typeof(LocalizeExtension)
                .GetCustomAttribute<ContentPropertyAttribute>()!
                .Name.Should()
                .Be(nameof(LocalizeExtension.Arguments));
            typeof(Argument).IsAssignableTo(typeof(DependencyObject)).Should().BeFalse();
        });

    [Fact]
    public void RefreshTrigger_ReResolves_OnFormatterTextBuilderAndRegistrationChange() =>
        WpfThread.Invoke(() =>
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
                TestSupport.Flush();
                invited.Text.Should().Be("{0} PEOPLE INVITED");
                missing.Text.Should().Be("!ThisKeyDoesNotExist!");
                catalogOnly.Text.Should().Be("!CatalogOnly!");

                // The failure text bypasses the formatter, so it is not upper-cased.
                Localizer.Current.LookupFailureTextBuilder = failure => $"[{failure.Key}]";
                TestSupport.Flush();
                missing.Text.Should().Be("[ThisKeyDoesNotExist]");
                catalogOnly.Text.Should().Be("[CatalogOnly]");

                Localizer.Current.RegisterResourceManager(TestSupport.CatalogResources);
                TestSupport.Flush();
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
        });

    [Fact]
    public void ScopedLookup_FormatsWithArguments() =>
        WpfThread.Invoke(() =>
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
        });

    [Fact]
    public void StyleSetter_FormatsWithArguments_WithoutATargetElement() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();

            // Parsed XAML, because WPF calls ProvideValue with the Setter as its target only from the XAML
            // reader; the extension then returns the multi-binding itself, which the style applies per element.
            const string xaml = $$"""
                <StackPanel {{Namespaces}}>
                  <StackPanel.Resources>
                    <Style TargetType="TextBlock">
                      <Setter Property="Text">
                        <Setter.Value>
                          <l:Localize Key="FormatPair">
                            <l:Argument Value="{Binding A}" />
                            <l:Argument Value="7" />
                          </l:Localize>
                        </Setter.Value>
                      </Setter>
                    </Style>
                  </StackPanel.Resources>
                  <TextBlock />
                  <TextBlock />
                </StackPanel>
                """;

            var host = (StackPanel)XamlReader.Parse(xaml);
            host.DataContext = new TestViewModel { A = 3 };
            host.Measure(new(500, 500));
            host.Arrange(new(0, 0, 500, 500));
            host.UpdateLayout();
            TestSupport.Flush();

            host.Children.OfType<TextBlock>()
                .Select(textBlock => textBlock.Text)
                .Should()
                .Equal("File 3 of 7", "File 3 of 7");

            Localizer.Current.CurrentCulture = TestSupport.German;
            TestSupport.Flush();
            host.Children.OfType<TextBlock>()
                .Select(textBlock => textBlock.Text)
                .Should()
                .Equal("Datei 3 von 7", "Datei 3 von 7");
        });

    [Fact]
    public void TextAndToolTip_OnOneElement_HaveTheirOwnArguments() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();
            var textBlock = new TextBlock();

            textBlock.SetBinding(
                TextBlock.TextProperty,
                (BindingBase)
                    new LocalizeExtension("FormatPair")
                    {
                        Arguments = { Arg(new Binding("A")), Arg(new Binding("B")) },
                    }.ProvideValue(null!)
            );
            textBlock.SetBinding(
                FrameworkElement.ToolTipProperty,
                (BindingBase)
                    new LocalizeExtension("FormatPair")
                    {
                        Arguments = { Arg(new Binding("B")), Arg(new Binding("A")) },
                    }.ProvideValue(null!)
            );
            textBlock.DataContext = new TestViewModel { A = 1, B = 2 };
            TestSupport.Flush();

            textBlock.Text.Should().Be("File 1 of 2");
            textBlock.ToolTip.Should().Be("File 2 of 1");
        });

    [Fact]
    public void TwelveArguments_HaveNoUpperLimit() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();
            var extension = new LocalizeExtension("Twelve");

            foreach (var letter in "abcdefghijkl")
            {
                extension.Arguments.Add(Arg(letter.ToString()));
            }

            var textBlock = TestSupport.BindLocalizedText(extension);

            textBlock.Text.Should().Be("abcdefghijkl");
        });

    [Fact]
    public void UnresolvedAndNullValues_RenderEmpty_AndReachTheFormatterAsNull() =>
        WpfThread.Invoke(() =>
        {
            TestSupport.ResetToEnglishWithTestCatalogs();
            var previous = Localizer.Current.MessageFormatter;
            var formatter = new CapturingFormatter();
            Localizer.Current.MessageFormatter = formatter;

            // Installing the formatter re-resolves every localized binding still alive; count only this test's.
            TestSupport.Flush();
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
        });

    /// <summary>Creates an unnamed argument.</summary>
    /// <param name="value">The constant or the binding.</param>
    /// <returns>The argument.</returns>
    private static Argument Arg(object? value) => new() { Value = value };

    /// <summary>Creates a named argument.</summary>
    /// <param name="name">The name.</param>
    /// <param name="value">The constant or the binding.</param>
    /// <returns>The argument.</returns>
    private static Argument Arg(string name, object? value) => new() { Name = name, Value = value };

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

    /// <summary>Collects the non-empty text of every <see cref="TextBlock" /> in the visual tree.</summary>
    /// <param name="root">The root of the visual tree to inspect.</param>
    /// <returns>The non-empty text values in visual-tree traversal order.</returns>
    private static string[] VisualTexts(DependencyObject root) =>
        VisualDescendants(root)
            .OfType<TextBlock>()
            .Select(textBlock => textBlock.Text ?? string.Empty)
            .Where(text => text.Length > 0)
            .ToArray();

    /// <summary>A formatter that ignores the arguments and upper-cases the resolved value.</summary>
    internal sealed class UpperCaseFormatter : IMessageFormatter
    {
        /// <inheritdoc />
        public string Format(string pattern, MessageArguments arguments, CultureInfo culture) =>
            pattern.ToUpperInvariant();
    }
}
