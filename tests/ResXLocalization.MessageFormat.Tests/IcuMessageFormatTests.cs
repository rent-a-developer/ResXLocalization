using Jeffijoe.MessageFormat.Formatting;

namespace RentADeveloper.ResXLocalization.MessageFormat.Tests;

/// <summary>
/// Exercises <see cref="MessageFormatLocalizerExtensions.UseMessageFormat" /> and the ICU MessageFormat
/// formatter it installs, through the lookups of a <see cref="Localizer" />: ICU syntax for every resolved
/// value, with or without arguments, the mapping of positional and named arguments, the format culture, and
/// the formatting failures that return the failure text and raise <see cref="ILocalizer.LookupFailed" />.
/// </summary>
public class IcuMessageFormatTests
{
    private const string FailureText = "!Key!";
    private const string Key = "Key";

    [Theory]
    [InlineData("<b>{0}</b> saved", "<b>3</b> saved")]
    [InlineData("Press <Enter> to {0}", "Press <Enter> to 3")]
    public void AngleBrackets_AreText(string pattern, string expected) =>
        FormatShouldReturn(CreateLocalizer(), pattern, MessageArguments.FromValues(3), expected);

    [Theory]
    [InlineData("l'utilisateur {0}", "l'utilisateur 3", "en")]
    [InlineData("Supprimer l'élément « {0} » ?", "Supprimer l'élément « 3 » ?", "en")]
    [InlineData("Don't delete {0}", "Don't delete 3", "en")]
    [InlineData("It's {0}'s turn", "It's 3's turn", "en")]
    [InlineData("Rock 'n' roll {0}", "Rock 'n' roll 3", "en")]
    [InlineData("trailing quote {0}'", "trailing quote 3'", "en")]
    [InlineData("{0, plural, one {Don't delete # file} other {Don't delete # files}}", "Don't delete 3 files", "en")]
    [InlineData("{0, plural, one {l'élément} other {les # éléments}}", "les 3 éléments", "fr")]
    [InlineData("'{0}' was not found", "{0} was not found", "en")]
    [InlineData("Datei '{0}' wurde gelöscht", "Datei {0} wurde gelöscht", "en")]
    [InlineData("Can't find '{Name}'", "Can't find {Name}", "en")]
    [InlineData("'{0}'s file", "{0}s file", "en")]
    [InlineData("''{0}''", "'3'", "en")]
    [InlineData("It''s {0}", "It's 3", "en")]
    [InlineData("'{'literal'}' {0}", "{literal} 3", "en")]
    [InlineData("'#' in text {0}", "# in text 3", "en")]
    [InlineData("Press <Enter> to {0}", "Press <Enter> to 3", "en")]
    [InlineData("List<T> of {0}", "List<T> of 3", "en")]
    [InlineData("<b>{0}</b> saved", "<b>3</b> saved", "en")]
    [InlineData("a < b is {0}", "a < b is 3", "en")]
    public void ApostrophesAndAngleBrackets_RenderAsIcuDefinesThem(string pattern, string expected, string cultureName)
    {
        var localizer = CreateLocalizer();
        localizer.CurrentCulture = new CultureInfo(cultureName);

        FormatShouldReturn(localizer, pattern, MessageArguments.FromTuples((null, 3), ("Name", "Bob")), expected);
    }

    [Fact]
    public void ArgumentWithoutStyle_IsFormattedWithoutGrouping() =>
        FormatShouldReturn(CreateLocalizer(), "{0} people", MessageArguments.FromValues(1234567), "1234567 people");

    [Fact]
    public void CompositeFormatSyntax_ReturnsTheFailureText()
    {
        var localizer = CreateLocalizer();

        FormatShouldFail(localizer, "{0:N0} people", MessageArguments.FromValues(1234567))
            .Exception.Should()
            .BeAssignableTo<Jeffijoe.MessageFormat.MessageFormatterException>();
        FormatShouldFail(
            localizer,
            "{1:d}",
            MessageArguments.FromValues(1, new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Unspecified))
        );
        FormatShouldFail(localizer, "{{literal}}", MessageArguments.FromValues(1));
        FormatShouldFail(localizer, "a { b } c", MessageArguments.FromValues(1))
            .Exception.Should()
            .BeOfType<VariableNotFoundException>()
            .Which.MissingVariable.Should()
            .Be("b");
    }

    [Fact]
    public void Formatter_IsSafeForConcurrentUse()
    {
        const string pattern = "{Count, plural, one {# file} other {# files}} {Size, number}";
        var formatter = CreateLocalizer().MessageFormatter;
        CultureInfo[] cultures = [CultureInfo.GetCultureInfo("en"), CultureInfo.GetCultureInfo("de-DE")];

        // Input 0 to 3: the culture alternates with every input, the count changes every second input.
        string FormatInput(int input) =>
            formatter.Format(
                pattern,
                MessageArguments.FromTuples(("Count", input < 2 ? 1 : 5), ("Size", 1234.5)),
                cultures[input % 2]
            );

        var expected = Enumerable.Range(0, 4).Select(FormatInput).ToArray();
        expected.Should().Equal("1 file 1,234.5", "1 file 1.234,5", "5 files 1,234.5", "5 files 1.234,5");

        var mismatches = 0;
        var threads = Enumerable
            .Range(0, 8)
            .Select(thread => new Thread(() =>
            {
                for (var call = 0; call < 1000; call++)
                {
                    var input = (thread + call) % 4;

                    if (FormatInput(input) != expected[input])
                    {
                        Interlocked.Increment(ref mismatches);
                    }
                }
            }))
            .ToList();

        threads.ForEach(thread => thread.Start());
        threads.ForEach(thread => thread.Join());

        mismatches.Should().Be(0);
    }

    [Fact]
    public void MissingVariable_ReturnsTheFailureText_WithVariableNotFoundException() =>
        FormatShouldFail(CreateLocalizer(), "{Nope} x", MessageArguments.FromValues(1))
            .Exception.Should()
            .BeOfType<VariableNotFoundException>()
            .Which.Message.Should()
            .Contain("Nope");

    [Fact]
    public void MixedArguments_AreReachableUnderTheirPositionAndTheirName() =>
        FormatShouldReturn(
            CreateLocalizer(),
            "{0} ({Letter}:)",
            MessageArguments.FromTuples((null, "Data"), ("Letter", "D")),
            "Data (D:)"
        );

    [Fact]
    public void NameThatIsAPosition_OverwritesThatPosition() =>
        FormatShouldReturn(CreateLocalizer(), "{0}|{1}", MessageArguments.FromTuples((null, "a"), ("0", "b")), "b|b");

    [Fact]
    public void NamedArguments_WorkThroughEveryGetOverload()
    {
        const string pattern = "{Count, plural, one {# file} other {# files}}";
        var resourceManager = new DictionaryResourceManager(
            ("Files", pattern),
            ("Enum_TestKind_A", pattern),
            ("Prefix_TestKind_A", pattern)
        );
        var localizer = CreateLocalizer();
        localizer.RegisterResourceManager(resourceManager);
        ILocalizer interfaceLocalizer = localizer;
        var count = MessageArguments.FromTuples(("Count", 1));
        using var monitor = localizer.Monitor();

        string[] results =
        [
            localizer.Get("Files", MessageArguments.FromTuples(("Count", 1))),
            interfaceLocalizer.Get(
                "Files",
                MessageArguments.FromDictionary(new Dictionary<string, object?> { ["Count"] = 1 })
            ),
            localizer.Get("Files", MessageArguments.FromObject(new { Count = 1 }), resourceManager),
            localizer.Get(new ResourceKey("Files", resourceManager), count),
            interfaceLocalizer.Get(TestKind.A, count, resourceManager),
            localizer.Get(TestKind.A, count, resourceManager, "Prefix_"),
        ];

        results.Should().AllBe("1 file");
        monitor.Should().NotRaise(nameof(ILocalizer.LookupFailed));
    }

    [Theory]
    [InlineData("{0, plural, one {# file} other {# files}}", "0 files")]
    [InlineData("{0, number}", "")]
    [InlineData("{0, select, a {A} other {O}}", "O")]
    [InlineData("{0}", "")]
    public void NullArgument_RendersWithoutAFailure(string pattern, string expected) =>
        FormatShouldReturn(CreateLocalizer(), pattern, MessageArguments.FromValues((object?)null), expected);

    [Fact]
    public void NumberStyle_FollowsTheLookupCulture()
    {
        var localizer = CreateLocalizer();

        FormatShouldReturn(localizer, "{0, number}", MessageArguments.FromValues(1234.5), "1,234.5");

        localizer.CurrentCulture = new CultureInfo("de-DE");

        FormatShouldReturn(localizer, "{0, number}", MessageArguments.FromValues(1234.5), "1.234,5");
    }

    [Fact]
    public void NumberStyle_GroupsDigits_AndTheIntegerStyleDoesNot()
    {
        var localizer = CreateLocalizer();

        FormatShouldReturn(localizer, "{0, number}", MessageArguments.FromValues(1234567), "1,234,567");
        FormatShouldReturn(localizer, "{0, number, integer}", MessageArguments.FromValues(1234567), "1234567");
    }

    [Fact]
    public void Plural_AcceptsNumericStrings_AndFailsForOtherStrings()
    {
        const string pattern = "{0, plural, one {# file} other {# files}}";
        var localizer = CreateLocalizer();

        FormatShouldReturn(localizer, pattern, MessageArguments.FromValues("5"), "5 files");
        FormatShouldReturn(localizer, pattern, MessageArguments.FromValues("1"), "1 file");
        FormatShouldFail(localizer, pattern, MessageArguments.FromValues("abc"));
    }

    [Theory]
    [InlineData("", "1 item")]
    [InlineData("en", "1 item")]
    [InlineData("de-CH", "1 item")]
    [InlineData("iu-Latn-CA", "1 item")]
    [InlineData("zh-Hant-TW", "1 items")]
    public void Plural_FollowsThePluralRulesOfTheLookupCulture(string cultureName, string expected)
    {
        var localizer = CreateLocalizer();

        // An empty culture name is the invariant culture, which formats as en.
        localizer.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);

        FormatShouldReturn(
            localizer,
            "{0, plural, one {# item} other {# items}}",
            MessageArguments.FromValues(1),
            expected
        );
    }

    [Fact]
    public void Plural_SelectsTheBranchOfTheCount()
    {
        const string pattern = "{0, plural, one {# person invited} other {# people invited}}";
        var localizer = CreateLocalizer();

        FormatShouldReturn(localizer, pattern, MessageArguments.FromValues(1), "1 person invited");
        FormatShouldReturn(localizer, pattern, MessageArguments.FromValues(5), "5 people invited");
    }

    [Fact]
    public void ShortDateStyle_FormatsLikeTheEngineInTheLookupCulture()
    {
        var date = new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Unspecified);
        var expected = new JeffijoeMessageFormatter().FormatMessage(
            "{1, date, short}",
            new Dictionary<string, object?> { ["0"] = 0, ["1"] = date },
            CultureInfo.GetCultureInfo("en")
        );

        expected.Should().NotBe(FailureText);
        FormatShouldReturn(CreateLocalizer(), "{1, date, short}", MessageArguments.FromValues(0, date), expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("iu-Latn-CA")]
    public void SimpleArgument_FormatsInEveryCulture(string cultureName)
    {
        var localizer = CreateLocalizer();

        // An empty culture name is the invariant culture.
        localizer.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);

        FormatShouldReturn(localizer, "{0} items", MessageArguments.FromValues(5), "5 items");
    }

    [Fact]
    public void UseMessageFormat_NullLocalizer_ThrowsArgumentNullException()
    {
        var act = () => ((ILocalizer)null!).UseMessageFormat();

        act.Should().Throw<ArgumentNullException>().WithParameterName("localizer");
    }

    [Fact]
    public void UseMessageFormat_TheLookupCultureWinsOverTheCultureOfTheGivenFormatter()
    {
        var localizer = new Localizer { CurrentCulture = new CultureInfo("en") };
        localizer.UseMessageFormat(new JeffijoeMessageFormatter(culture: CultureInfo.GetCultureInfo("de-DE")));

        FormatShouldReturn(localizer, "{0, number}", MessageArguments.FromValues(1234.5), "1,234.5");
    }

    [Fact]
    public void UseMessageFormat_UsesTheGivenFormatterAsGiven()
    {
        const string pattern = "{0, date, medium}";
        var arguments = MessageArguments.FromValues(new DateTime(2026, 1, 31, 0, 0, 0, DateTimeKind.Unspecified));
        var localizer = CreateLocalizer();

        FormatShouldFail(localizer, pattern, arguments).Exception.Should().BeOfType<UnsupportedFormatStyleException>();

        localizer.UseMessageFormat(
            new JeffijoeMessageFormatter(
                customValueFormatter: new Jeffijoe.MessageFormat.CustomValueFormatters
                {
                    Date = (CultureInfo culture, object? value, string? style, out string? formatted) =>
                    {
                        if (style == "medium" && value is DateTime date)
                        {
                            formatted = date.ToString("d MMM yyyy", culture);
                            return true;
                        }

                        formatted = string.Empty;
                        return false;
                    },
                }
            )
        );

        FormatShouldReturn(localizer, pattern, arguments, "31 Jan 2026");
    }

    [Fact]
    public void UseMessageFormat_WithNullOrWithoutAFormatter_UsesANewFormatter()
    {
        var withNull = new Localizer { CurrentCulture = new CultureInfo("en") };
        withNull.UseMessageFormat(null);

        foreach (var localizer in new[] { withNull, CreateLocalizer() })
        {
            FormatShouldReturn(
                localizer,
                "{0, plural, one {# file} other {# files}}",
                MessageArguments.FromValues(1),
                "1 file"
            );
            FormatShouldFail(localizer, "{Nope} x", MessageArguments.FromValues(1));
        }
    }

    [Theory]
    [InlineData("Hello", "Hello")]
    [InlineData("It''s ready", "It's ready")]
    [InlineData("'{literal}'", "{literal}")]
    public void ValueWithoutArguments_IsParsedAsIcu(string pattern, string expected)
    {
        var localizer = CreateLocalizer();
        using var monitor = localizer.Monitor();

        LookUpWithoutArguments(localizer, pattern).Should().AllBe(expected);
        monitor.Should().NotRaise(nameof(ILocalizer.LookupFailed));
    }

    [Theory]
    [InlineData("{Count} a { b } c")]
    [InlineData("It''s <b>{0}</b>")]
    public void ValueWithoutArguments_ReferencingAVariable_ReturnsTheFailureText(string pattern)
    {
        var localizer = CreateLocalizer();
        using var monitor = localizer.Monitor();

        LookUpWithoutArguments(localizer, pattern).Should().AllBe(FailureText);
        monitor
            .LookupFailures()
            .Should()
            .HaveCount(5)
            .And.AllSatisfy(failure =>
            {
                failure.Reason.Should().Be(LookupFailureReason.MessageFormatFailed);
                failure.Exception.Should().BeOfType<VariableNotFoundException>();
            });
    }

    [Fact]
    public void WithoutUseMessageFormat_TheDefaultFormatterFormats() =>
        FormatShouldReturn(
            new Localizer { CurrentCulture = new CultureInfo("en") },
            "{0:N0} people",
            MessageArguments.FromValues(1234567),
            "1,234,567 people"
        );

    /// <summary>Creates a localizer in the culture <c>en</c> with the ICU formatter installed.</summary>
    /// <returns>The localizer.</returns>
    private static Localizer CreateLocalizer()
    {
        var localizer = new Localizer { CurrentCulture = new CultureInfo("en") };
        localizer.UseMessageFormat();
        return localizer;
    }

    /// <summary>Looks <paramref name="pattern" /> up under <see cref="Key" /> with a scoped lookup.</summary>
    /// <param name="localizer">The localizer.</param>
    /// <param name="pattern">The resource value.</param>
    /// <param name="arguments">The arguments of the lookup.</param>
    /// <returns>The result of the lookup.</returns>
    private static string Format(Localizer localizer, string pattern, MessageArguments arguments) =>
        localizer.Get(Key, arguments, new DictionaryResourceManager((Key, pattern)));

    /// <summary>
    /// Asserts that looking <paramref name="pattern" /> up returns the failure text and raises exactly one
    /// <see cref="ILocalizer.LookupFailed" /> with the reason <see cref="LookupFailureReason.MessageFormatFailed" />.
    /// </summary>
    /// <param name="localizer">The localizer.</param>
    /// <param name="pattern">The resource value.</param>
    /// <param name="arguments">The arguments of the lookup.</param>
    /// <returns>The failure the event reported.</returns>
    private static LookupFailure FormatShouldFail(Localizer localizer, string pattern, MessageArguments arguments)
    {
        using var monitor = localizer.Monitor();

        Format(localizer, pattern, arguments).Should().Be(FailureText);

        var failure = monitor.LookupFailures().Should().ContainSingle().Which;
        failure.Reason.Should().Be(LookupFailureReason.MessageFormatFailed);
        return failure;
    }

    /// <summary>
    /// Asserts that looking <paramref name="pattern" /> up returns <paramref name="expected" /> and raises no
    /// <see cref="ILocalizer.LookupFailed" />.
    /// </summary>
    /// <param name="localizer">The localizer.</param>
    /// <param name="pattern">The resource value.</param>
    /// <param name="arguments">The arguments of the lookup.</param>
    /// <param name="expected">The expected result.</param>
    private static void FormatShouldReturn(
        Localizer localizer,
        string pattern,
        MessageArguments arguments,
        string expected
    )
    {
        using var monitor = localizer.Monitor();

        Format(localizer, pattern, arguments).Should().Be(expected);

        monitor.Should().NotRaise(nameof(ILocalizer.LookupFailed));
    }

    /// <summary>
    /// Looks <paramref name="pattern" /> up with every form of a lookup without arguments: no arguments,
    /// <see langword="null" />, <see cref="MessageArguments.Empty" />, and empty positional and named arguments.
    /// </summary>
    /// <param name="localizer">The localizer.</param>
    /// <param name="pattern">The resource value.</param>
    /// <returns>The five results.</returns>
    private static string[] LookUpWithoutArguments(Localizer localizer, string pattern)
    {
        var resourceManager = new DictionaryResourceManager((Key, pattern));

        return
        [
            localizer.Get(Key, resourceManager: resourceManager),
            localizer.Get(Key, arguments: null, resourceManager: resourceManager),
            localizer.Get(Key, MessageArguments.Empty, resourceManager),
            localizer.Get(Key, MessageArguments.FromValues(), resourceManager),
            localizer.Get(Key, MessageArguments.FromDictionary(new Dictionary<string, object?>()), resourceManager),
        ];
    }
}
