using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace RentADeveloper.ResXLocalization.SourceGenerators.Tests;

/// <summary>
/// Unit tests for <see cref="ResxKeysGenerator" />. They drive the incremental generator in-memory
/// with <see cref="CSharpGeneratorDriver" /> against fabricated <c>.resx</c> / <c>.Designer.cs</c>
/// additional files and assert on the generated source - no file system, no MSBuild. The MSBuild
/// resource metadata a real build passes through the analyzer config file comes from
/// <see cref="TestMetadata" />, which derives it from each path the way a build would.
/// </summary>
public class ResxKeysGeneratorTests
{
    [Fact]
    public void CollidingSanitizedNames_GetNumericSuffixes()
    {
        var result = RunGenerator(
            ("project/Strings.resx", Resx(("A B", "a"), ("A-B", "b"))),
            ("project/Strings.Designer.cs", Designer("My.App"))
        );

        var code = SingleGeneratedSource(result).SourceText.ToString();

        code.Should().Contain("""A_B = new("A B", """);
        code.Should().Contain("""A_B2 = new("A-B", """);
    }

    [Fact]
    public void ControlCharactersInKeyNames_AreEscapedInTheGeneratedLiteral()
    {
        // A literal newline in an XML attribute would be normalized to a space, so the resx has to
        // use a character reference to smuggle one into the key name.
        const string resx = """
            <?xml version="1.0" encoding="utf-8"?>
            <root>
              <data name="Line&#10;Break" xml:space="preserve"><value>a</value></data>
            </root>
            """;

        var result = RunGenerator(("project/Strings.resx", resx), ("project/Strings.Designer.cs", Designer("My.App")));

        var code = SingleGeneratedSource(result).SourceText.ToString();

        code.Should().Contain("""Line_Break = new("Line\nBreak", """);
    }

    [Fact]
    public void DesignerInADifferentFolder_DoesNotPair()
    {
        var result = RunGenerator(
            ("project/Resources/Strings.resx", Resx(("Greeting", "Hello"))),
            ("project/Other/Strings.Designer.cs", Designer("My.App"))
        );

        var code = SingleGeneratedSource(result).SourceText.ToString();

        code.Should().Contain("namespace N.Resources;");
        code.Should().NotContain("My.App");
    }

    [Fact]
    public void DesignerNamespace_IsUsed_ButNotTheDesignersResourceManager()
    {
        var result = RunGenerator(
            ("project/Resources/Strings.resx", Resx(("Greeting", "Hello"))),
            ("project/Resources/Strings.Designer.cs", Designer("Custom.Ns"))
        );

        var code = SingleGeneratedSource(result).SourceText.ToString();

        code.Should().Contain("namespace Custom.Ns;");
        code.Should().Contain("""new("N.Resources.Strings", typeof(StringsKeys).Assembly);""");
        code.Should().NotContain("Custom.Ns.Strings");
        code.Should().NotContain("global::Custom");
    }

    [Fact]
    public void DesignerWithoutAResourceManagerMember_DoesNotPair()
    {
        // A WinForms Form1.Designer.cs sits next to Form1.resx and matches the *.Designer.cs name, but
        // it is not a resx accessor (no ResourceManager member), so it must not pair.
        const string formDesigner = """
            namespace My.App {
                partial class Strings {
                    private void InitializeComponent() { }
                }
            }
            """;

        var result = RunGenerator(
            ("project/Strings.resx", Resx(("Greeting", "Hello"))),
            ("project/Strings.Designer.cs", formDesigner)
        );

        var code = SingleGeneratedSource(result).SourceText.ToString();

        code.Should().Contain("namespace N;");
        code.Should().NotContain("My.App");
    }

    [Fact]
    public void DottedNeutralResxBaseName_EmitsASanitizedClassInTheResourceNameNamespace()
    {
        var result = RunGenerator(
            new TestMetadata().With("project/My.Strings.resx", ("WithCulture", "false")),
            ("project/My.Strings.resx", Resx(("Greeting", "Hello")))
        );

        var code = SingleGeneratedSource(result).SourceText.ToString();

        code.Should().Contain("namespace N.My;");
        code.Should().Contain("public static partial class My_StringsKeys");
        code.Should().Contain("""new("N.My.Strings", typeof(My_StringsKeys).Assembly);""");
    }

    [Fact]
    public void EditingAnUnrelatedResx_KeepsTheCollisionAndTheCollidingFilesCached()
    {
        var compilation = CSharpCompilation.Create("Tests");
        GeneratorDriver driver = CreateTrackingDriver(new TestMetadata());

        var otherResx = new InMemoryAdditionalText("project/Other.resx", Resx(("Alpha", "a")));

        driver = driver
            .AddAdditionalTexts([
                new InMemoryAdditionalText("project/A/Strings.resx", Resx(("Beta", "b"))),
                new InMemoryAdditionalText("project/A/Strings.Designer.cs", Designer("N")),
                new InMemoryAdditionalText("project/B/Strings.resx", Resx(("Gamma", "c"))),
                new InMemoryAdditionalText("project/B/Strings.Designer.cs", Designer("N")),
                otherResx,
            ])
            .RunGenerators(compilation)
            // Then edit only Other.resx's keys and run again.
            .ReplaceAdditionalText(
                otherResx,
                new InMemoryAdditionalText(otherResx.Path, Resx(("Alpha", "a"), ("Delta", "d")))
            )
            .RunGenerators(compilation);

        var result = driver.GetRunResult();
        result.Diagnostics.Select(diagnostic => diagnostic.Id).Should().Equal("RXLGEN004", "RXLGEN004");
        result
            .Results.Single()
            .GeneratedSources.Should()
            .ContainSingle()
            .Which.HintName.Should()
            .StartWith("N.OtherKeys.");

        var reasons = OutputReasons(driver).ToList();
        reasons.Should().HaveCount(3);
        reasons.Count(reason => reason == IncrementalStepRunReason.Cached).Should().Be(2);
        reasons.Should().ContainSingle(reason => reason == IncrementalStepRunReason.Modified);
    }

    [Fact]
    public void EscapedNamespaceSegments_ArePreserved()
    {
        const string designer =
            "namespace @class.Δ { internal class Strings { public static global::System.Resources.ResourceManager ResourceManager => null; } }";
        var result = RunGenerator(
            ("project/Strings.resx", Resx(("Greeting", "Hello"))),
            ("project/Strings.Designer.cs", designer)
        );

        SingleGeneratedSource(result).SourceText.ToString().Should().Contain("namespace @class.Δ;");
    }

    [Fact]
    public void GlobalNamespaceAndInvalidFilename_EmitValidSanitizedClass()
    {
        const string designer =
            "internal class OrderStatus { public static global::System.Resources.ResourceManager ResourceManager => null; }";
        var result = RunGenerator(
            ("project/Order-Status.resx", Resx(("Greeting", "Hello"))),
            ("project/Order-Status.Designer.cs", designer)
        );
        var code = SingleGeneratedSource(result).SourceText.ToString();

        code.Should().Contain("class Order_StatusKeys");
        code.Should().Contain("""new("N.Order-Status", typeof(Order_StatusKeys).Assembly);""");
        code.Should().NotContain("namespace");
    }

    [Fact]
    public void InvalidIdentifierCharacters_AreSanitized()
    {
        var result = RunGenerator(
            ("project/Strings.resx", Resx(("Some Key-1", "a"), ("2Fast", "b"))),
            ("project/Strings.Designer.cs", Designer("My.App"))
        );

        var code = SingleGeneratedSource(result).SourceText.ToString();

        code.Should().Contain("""Some_Key_1 = new("Some Key-1", """);
        code.Should().Contain("""_2Fast = new("2Fast", """);
    }

    [Fact]
    public void KeyEqualToTheGeneratedClassName_GetsANumericSuffix()
    {
        // A member must not be named like its enclosing type (CS0542), so the key "StringsKeys"
        // inside the generated StringsKeys class is renamed like any other collision.
        var result = RunGenerator(
            ("project/Strings.resx", Resx(("StringsKeys", "a"))),
            ("project/Strings.Designer.cs", Designer("My.App"))
        );

        var code = SingleGeneratedSource(result).SourceText.ToString();

        code.Should().Contain("""StringsKeys2 = new("StringsKeys", ResourceManager);""");
    }

    [Fact]
    public void KeyNamedResourceManager_GetsANumericSuffix()
    {
        var result = RunGenerator(("project/Strings.resx", Resx(("ResourceManager", "a"))));

        var code = SingleGeneratedSource(result).SourceText.ToString();

        code.Should().Contain("public static global::System.Resources.ResourceManager ResourceManager { get; }");
        code.Should().Contain("""ResourceManager2 = new("ResourceManager", ResourceManager);""");
    }

    [Fact]
    public void KeyThatIsACSharpKeyword_IsEscapedWithAnAtPrefix()
    {
        var result = RunGenerator(
            ("project/Strings.resx", Resx(("class", "a"), ("event", "b"))),
            ("project/Strings.Designer.cs", Designer("My.App"))
        );

        var code = SingleGeneratedSource(result).SourceText.ToString();

        code.Should().Contain("""@class = new("class", """);
        code.Should().Contain("""@event = new("event", """);
    }

    [Fact]
    public void LogicalName_ReplacesTheManifestResourceName()
    {
        var result = RunGenerator(
            new TestMetadata().With("project/Logical.resx", ("LogicalName", "Totally.Custom.Name.resources")),
            ("project/Logical.resx", Resx(("Greeting", "Hello")))
        );

        var code = SingleGeneratedSource(result).SourceText.ToString();

        code.Should().Contain("namespace Totally.Custom;");
        code.Should().Contain("""new("Totally.Custom.Name", typeof(LogicalKeys).Assembly);""");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MalformedEligibleResx_ReportsAnActionableDiagnostic(bool withDesigner)
    {
        var result = RunGenerator(
            withDesigner
                ?
                [
                    ("project/Strings.resx", "<root><data name='Broken'"),
                    ("project/Strings.Designer.cs", Designer("My.App")),
                ]
                : [("project/Strings.resx", "<root><data name='Broken'")]
        );

        var diagnostic = result.Diagnostics.Should().ContainSingle().Subject;
        diagnostic.Id.Should().Be("RXLGEN001");
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Warning);
        diagnostic.Location.GetLineSpan().Path.Should().Be("project/Strings.resx");
        diagnostic.GetMessage(CultureInfo.InvariantCulture).Should().Contain("Could not parse eligible resource file");
        result.Results.Single().GeneratedSources.Should().BeEmpty();
    }

    [Fact]
    public void MalformedResxThatCollides_ReportsTheCollisionInsteadOfTheParseError()
    {
        var result = RunGenerator(
            ("project/A/Strings.resx", "<root><data name='Broken'"),
            ("project/A/Strings.Designer.cs", Designer("N", "StringsA")),
            ("project/B/Strings.resx", Resx(("Beta", "b"))),
            ("project/B/Strings.Designer.cs", Designer("N", "StringsB"))
        );

        result.Diagnostics.Select(diagnostic => diagnostic.Id).Should().Equal("RXLGEN004", "RXLGEN004");
        result.Results.Single().GeneratedSources.Should().BeEmpty();
    }

    [Fact]
    public void Members_AreSortedByKeyName()
    {
        var result = RunGenerator(
            ("project/Strings.resx", Resx(("Zebra", "z"), ("Apple", "a"))),
            ("project/Strings.Designer.cs", Designer("My.App"))
        );

        var code = SingleGeneratedSource(result).SourceText.ToString();

        code.IndexOf("Apple", StringComparison.Ordinal)
            .Should()
            .BeLessThan(code.IndexOf("Zebra", StringComparison.Ordinal));
    }

    [Fact]
    public void NeutralResxWithSiblingDesigner_EmitsOneTypedKeyPerStringEntry()
    {
        var result = RunGenerator(
            ("project/Resources/Strings.resx", Resx(("WindowTitle", "My App"), ("Greeting", "Hello"))),
            ("project/Resources/Strings.Designer.cs", Designer("My.App.Resources"))
        );

        var source = SingleGeneratedSource(result);

        // The hint name is namespace-qualified and carries a per-directory digest (see the generator).
        source.HintName.Should().MatchRegex(@"^My\.App\.Resources\.StringsKeys\.[0-9a-f]{8}\.g\.cs$");

        var code = source.SourceText.ToString();
        code.Should().Contain("namespace My.App.Resources;");
        code.Should().Contain("public static partial class StringsKeys");
        code.Should()
            .Contain(
                """
                public static global::System.Resources.ResourceManager ResourceManager { get; } =
                        new("N.Resources.Strings", typeof(StringsKeys).Assembly);
                """
            );
        code.Should().Contain("""Greeting = new("Greeting", ResourceManager);""");
        code.Should().Contain("""WindowTitle = new("WindowTitle", ResourceManager);""");
    }

    [Fact]
    public void NonStringEntries_AreSkipped()
    {
        const string resx = """
            <?xml version="1.0" encoding="utf-8"?>
            <root>
              <data name="Greeting" xml:space="preserve"><value>Hello</value></data>
              <data name="Logo" type="System.Drawing.Bitmap, System.Drawing"><value>logo.png</value></data>
              <data name="Blob" mimetype="application/x-microsoft.net.object.binary.base64"><value>AAEC</value></data>
            </root>
            """;

        var result = RunGenerator(("project/Strings.resx", resx), ("project/Strings.Designer.cs", Designer("My.App")));

        var code = SingleGeneratedSource(result).SourceText.ToString();

        code.Should().Contain("Greeting");
        code.Should().NotContain("Logo");
        code.Should().NotContain("Blob");
    }

    [Fact]
    public void QuotesInKeyNames_AreEscapedInTheGeneratedLiteral()
    {
        var result = RunGenerator(
            ("project/Strings.resx", Resx(("""Say "Hi" now""", "a"))),
            ("project/Strings.Designer.cs", Designer("My.App"))
        );

        var code = SingleGeneratedSource(result).SourceText.ToString();

        code.Should().Contain("""Say__Hi__now = new("Say \"Hi\" now", """);
    }

    [Fact]
    public void ResourceManagerDeclaration_PrecedesTheFirstKey()
    {
        var result = RunGenerator(
            ("project/Resources/Strings.resx", Resx(("WindowTitle", "My App"), ("Greeting", "Hello")))
        );

        var code = SingleGeneratedSource(result).SourceText.ToString();

        code.IndexOf("ResourceManager ResourceManager", StringComparison.Ordinal)
            .Should()
            .BePositive()
            .And.BeLessThan(code.IndexOf("ResourceKey", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("global::System.Resources.ResourceManager", "")]
    [InlineData("System.Resources.ResourceManager", "")]
    [InlineData("ResourceManager", "using System.Resources;")]
    [InlineData("RM", "using RM = System.Resources.ResourceManager;")]
    public void ResourceManagerTypeSpellings_AreRecognized(string typeName, string usingDirective)
    {
        var designer = $$"""
            {{usingDirective}}
            namespace My.App;
            internal class ActualAccessor
            {
                public static {{typeName}} ResourceManager => null;
            }
            """;
        var result = RunGenerator(
            ("project/Strings.resx", Resx(("Greeting", "Hello"))),
            ("project/Strings.Designer.cs", designer)
        );

        // Pairing is what puts the class into the designer's namespace instead of the resource name's.
        SingleGeneratedSource(result).SourceText.ToString().Should().Contain("namespace My.App;");
    }

    [Fact]
    public void ResourceNameNamespaceSegments_AreSanitized()
    {
        var result = RunGenerator(
            new TestMetadata().With("project/Strings.resx", ("LogicalName", "My-App.event.Strings.resources")),
            ("project/Strings.resx", Resx(("Greeting", "Hello")))
        );

        var code = SingleGeneratedSource(result).SourceText.ToString();

        code.Should().Contain("namespace My_App.@event;");
        code.Should().Contain("""new("My-App.event.Strings", typeof(StringsKeys).Assembly);""");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ResxWithoutResourceName_ReportsRXLGEN003(bool wellFormed)
    {
        // A file without its resource name is not parsed at all, so malformed XML does not surface.
        var result = RunGenerator(
            new TestMetadata().Without("project/Strings.resx"),
            ("project/Strings.resx", wellFormed ? Resx(("Greeting", "Hello")) : "<root><data name='Broken'")
        );

        var diagnostic = result.Diagnostics.Should().ContainSingle().Subject;
        diagnostic.Id.Should().Be("RXLGEN003");
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Warning);
        diagnostic.Location.GetLineSpan().Path.Should().Be("project/Strings.resx");
        diagnostic.GetMessage(CultureInfo.InvariantCulture).Should().Contain("project/Strings.resx");
        result.Results.Single().GeneratedSources.Should().BeEmpty();
    }

    [Fact]
    public void ResxWithoutSiblingDesigner_EmitsKeysInTheResourceNameNamespace()
    {
        var result = RunGenerator(
            ("project/Resources/Strings.resx", Resx(("WindowTitle", "My App"), ("Greeting", "Hello")))
        );

        var code = SingleGeneratedSource(result).SourceText.ToString();

        code.Should().Contain("namespace N.Resources;");
        code.Should().Contain("public static partial class StringsKeys");
        code.Should()
            .Contain(
                """
                {
                    /// <summary>
                    /// The resource manager of Strings.resx - the instance every key of this class uses. Register this
                    /// instance for search-all lookups, and pass it for scoped lookups.
                    /// </summary>
                    public static global::System.Resources.ResourceManager ResourceManager { get; } =
                        new("N.Resources.Strings", typeof(StringsKeys).Assembly);
                """
            );
        code.Should().Contain("""Greeting = new("Greeting", ResourceManager);""");
        code.Should().Contain("""WindowTitle = new("WindowTitle", ResourceManager);""");
    }

    [Fact]
    public void SatelliteResx_ProducesNoOutput()
    {
        var result = RunGenerator(
            new TestMetadata().With("project/Strings.de.resx", ("WithCulture", "true")),
            ("project/Strings.de.resx", Resx(("Greeting", "Hallo"))),
            ("project/Strings.Designer.cs", Designer("My.App"))
        );

        result.Diagnostics.Should().BeEmpty();
        result.Results.Single().GeneratedSources.Should().BeEmpty();
    }

    [Fact]
    public void SecondRun_WithEquivalentInputs_CachesTheOutput()
    {
        var compilation = CSharpCompilation.Create("Tests");
        GeneratorDriver driver = CreateTrackingDriver(new TestMetadata());

        var resx = new InMemoryAdditionalText("project/Strings.resx", Resx(("Greeting", "Hello"), ("Bye", "Goodbye")));
        var designer = new InMemoryAdditionalText("project/Strings.Designer.cs", Designer("My.App"));

        driver = driver.AddAdditionalTexts([resx, designer]).RunGenerators(compilation);

        // Replace both inputs with fresh instances of byte-identical content: the parse transform
        // re-executes, but the resulting ResXFile must compare equal (thanks to EquatableArray-backed
        // keys), so the source output is served from the incremental cache rather than regenerated.
        driver = driver
            .ReplaceAdditionalText(resx, new InMemoryAdditionalText(resx.Path, resx.GetText().ToString()))
            .ReplaceAdditionalText(designer, new InMemoryAdditionalText(designer.Path, designer.GetText().ToString()))
            .RunGenerators(compilation);

        OutputReasons(driver)
            .Should()
            .NotBeEmpty()
            .And.OnlyContain(reason => reason == IncrementalStepRunReason.Cached);
    }

    [Fact]
    public void TouchingOneResx_ReRunsOnlyThatFilesOutput()
    {
        var compilation = CSharpCompilation.Create("Tests");
        GeneratorDriver driver = CreateTrackingDriver(new TestMetadata());

        var oneResx = new InMemoryAdditionalText("project/One/Strings.resx", Resx(("Alpha", "a")));

        driver = driver
            .AddAdditionalTexts([
                oneResx,
                new InMemoryAdditionalText("project/Two/Strings.resx", Resx(("Beta", "b"))),
                new InMemoryAdditionalText("project/One/Strings.Designer.cs", Designer("My.App.One")),
                new InMemoryAdditionalText("project/Two/Strings.Designer.cs", Designer("My.App.Two")),
            ])
            .RunGenerators(compilation);

        // Edit only the first file (add a key); its output must re-run while the untouched second
        // file's output stays cached. Replacing the text in place keeps every other input untouched.
        driver = driver
            .ReplaceAdditionalText(
                oneResx,
                new InMemoryAdditionalText(oneResx.Path, Resx(("Alpha", "a"), ("Gamma", "c")))
            )
            .RunGenerators(compilation);

        var reasons = OutputReasons(driver).ToList();

        reasons.Should().Contain(IncrementalStepRunReason.Cached);
        reasons
            .Should()
            .Contain(reason => reason == IncrementalStepRunReason.Modified || reason == IncrementalStepRunReason.New);
    }

    [Fact]
    public void TwoResxProducingTheSameKeysClass_InNamespacesThatDifferOnlyByAVerbatimAt_ReportRXLGEN004()
    {
        // Foo.@Bar (from the designer) and Foo.Bar (from the resource name) are the same C# namespace.
        var result = RunGenerator(
            new TestMetadata().With("project/B/Strings.resx", ("LogicalName", "Foo.Bar.Strings.resources")),
            ("project/A/Strings.resx", Resx(("Alpha", "a"))),
            ("project/A/Strings.Designer.cs", Designer("Foo.@Bar")),
            ("project/B/Strings.resx", Resx(("Beta", "b")))
        );

        result.Diagnostics.Select(diagnostic => diagnostic.Id).Should().Equal("RXLGEN004", "RXLGEN004");
        result.Results.Single().GeneratedSources.Should().BeEmpty();
    }

    [Fact]
    public void TwoResxProducingTheSameKeysClass_ReportRXLGEN004_AndGenerateNeither()
    {
        // Distinct accessor type names keep the input compilation free of a duplicate type; both
        // designers still declare namespace N, so both files would produce N.StringsKeys.
        var result = RunGenerator(
            ("project/A/Strings.resx", Resx(("Alpha", "a"))),
            ("project/A/Strings.Designer.cs", Designer("N", "StringsA")),
            ("project/B/Strings.resx", Resx(("Beta", "b"))),
            ("project/B/Strings.Designer.cs", Designer("N", "StringsB")),
            ("project/Other.resx", Resx(("Gamma", "c")))
        );

        result.Diagnostics.Should().HaveCount(2);
        result
            .Diagnostics.Select(diagnostic => diagnostic.Location.GetLineSpan().Path)
            .Should()
            .BeEquivalentTo("project/A/Strings.resx", "project/B/Strings.resx");
        result
            .Diagnostics.Should()
            .AllSatisfy(diagnostic =>
            {
                diagnostic.Id.Should().Be("RXLGEN004");
                diagnostic.Severity.Should().Be(DiagnosticSeverity.Warning);
                var message = diagnostic.GetMessage(CultureInfo.InvariantCulture);
                message.Should().Contain("project/A/Strings.resx");
                message.Should().Contain("project/B/Strings.resx");
                message.Should().Contain("'N.StringsKeys'");
            });

        var source = result.Results.Single().GeneratedSources.Should().ContainSingle().Subject;
        source.SourceText.ToString().Should().Contain("public static partial class OtherKeys");
    }

    [Fact]
    public void TwoResxWithTheSameBaseNameInDifferentFolders_GetDistinctHintNames()
    {
        var result = RunGenerator(
            ("project/One/Strings.resx", Resx(("Alpha", "a"))),
            ("project/One/Strings.Designer.cs", Designer("My.App.One")),
            ("project/Two/Strings.resx", Resx(("Beta", "b"))),
            ("project/Two/Strings.Designer.cs", Designer("My.App.Two"))
        );

        var sources = result.Results.Single().GeneratedSources;

        sources.Should().HaveCount(2);
        sources.Select(s => s.HintName).Should().OnlyHaveUniqueItems();
        sources.Select(s => s.HintName).Should().ContainSingle(h => h.StartsWith("My.App.One.StringsKeys."));
        sources.Select(s => s.HintName).Should().ContainSingle(h => h.StartsWith("My.App.Two.StringsKeys."));
    }

    [Fact]
    public void WinFormsDesignerMetadataEntries_AreSkipped()
    {
        // A WinForms form's resx stores component properties as "$this.Text" / ">>$this.Name"; only
        // the real string entry should surface as a key.
        const string resx = """
            <?xml version="1.0" encoding="utf-8"?>
            <root>
              <data name="$this.Text" xml:space="preserve"><value>Form1</value></data>
              <data name="&gt;&gt;$this.Name" xml:space="preserve"><value>$this</value></data>
              <data name="Greeting" xml:space="preserve"><value>Hello</value></data>
            </root>
            """;

        var result = RunGenerator(("project/Strings.resx", resx), ("project/Strings.Designer.cs", Designer("My.App")));

        var code = SingleGeneratedSource(result).SourceText.ToString();

        code.Should().Contain("""Greeting = new("Greeting", """);
        code.Should().NotContain("$this");
    }

    private static CSharpGeneratorDriver CreateTrackingDriver(TestMetadata metadata) =>
        CSharpGeneratorDriver.Create(
            [new ResxKeysGenerator().AsSourceGenerator()],
            parseOptions: null,
            optionsProvider: metadata,
            driverOptions: new(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true)
        );

    private static string Designer(string ns, string accessorType = "Strings") =>
        $$"""
            namespace {{ns}} {
                internal class {{accessorType}} {
                    public static global::System.Resources.ResourceManager ResourceManager {
                        get { return null; }
                    }
                }
            }
            """;

    private static IEnumerable<IncrementalStepRunReason> OutputReasons(GeneratorDriver driver) =>
        driver
            .GetRunResult()
            .Results.Single()
            .TrackedOutputSteps.SelectMany(step => step.Value)
            .SelectMany(run => run.Outputs)
            .Select(output => output.Reason);

    private static string Resx(params (string Name, string Value)[] entries)
    {
        var builder = new StringBuilder();

        builder.AppendLine("""<?xml version="1.0" encoding="utf-8"?>""").AppendLine("<root>");

        foreach (var (name, value) in entries)
        {
            builder.AppendLine(
                CultureInfo.InvariantCulture,
                $"""  <data name="{SecurityElement.Escape(name)}" xml:space="preserve"><value>{SecurityElement.Escape(value)}</value></data>"""
            );
        }

        builder.AppendLine("</root>");

        return builder.ToString();
    }

    private static GeneratorDriverRunResult RunGenerator(params (string Path, string Content)[] files) =>
        RunGenerator(new TestMetadata(), files);

    private static GeneratorDriverRunResult RunGenerator(
        TestMetadata metadata,
        params (string Path, string Content)[] files
    )
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(static path => MetadataReference.CreateFromFile(path));
        var sourceTrees = files
            .Where(static file => file.Path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .Select(static file => CSharpSyntaxTree.ParseText(file.Content, path: file.Path))
            .Append(
                CSharpSyntaxTree.ParseText(
                    "namespace RentADeveloper.ResXLocalization { public readonly struct ResourceKey { public ResourceKey(string key, global::System.Resources.ResourceManager manager) { } } }"
                )
            );
        var compilation = CSharpCompilation.Create(
            "Tests",
            sourceTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        var additionalTexts = files
            .Select(AdditionalText (file) => new InMemoryAdditionalText(file.Path, file.Content))
            .ToImmutableArray();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new ResxKeysGenerator().AsSourceGenerator()],
            additionalTexts,
            parseOptions: null,
            optionsProvider: metadata
        );
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);
        outputCompilation
            .GetDiagnostics()
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Should()
            .BeEmpty();
        return driver.GetRunResult();
    }

    private static GeneratedSourceResult SingleGeneratedSource(GeneratorDriverRunResult result)
    {
        result.Diagnostics.Should().BeEmpty();

        return result.Results.Single().GeneratedSources.Single();
    }

    private sealed class InMemoryAdditionalText(string path, string content) : AdditionalText
    {
        public override string Path => path;

        public override SourceText GetText(CancellationToken cancellationToken = default) =>
            SourceText.From(content, Encoding.UTF8);
    }

    /// <summary>
    /// The analyzer config options a real build writes for each <c>.resx</c>. Paths are relative to a
    /// project folder <c>project/</c> whose root namespace is <c>N</c>: <c>project/Resources/Strings.de.resx</c>
    /// gets <c>ManifestResourceName</c> <c>N.Resources.Strings</c> and <c>WithCulture</c> <c>true</c>, as
    /// MSBuild computes them. A test overrides single values with <see cref="With" /> or withholds them
    /// all with <see cref="Without" />.
    /// </summary>
    private sealed class TestMetadata : AnalyzerConfigOptionsProvider
    {
        private const string Prefix = "build_metadata.AdditionalFiles.";

        private static readonly HashSet<string> CultureNames = CultureInfo
            .GetCultures(CultureTypes.AllCultures)
            .Select(static culture => culture.Name)
            .Where(static name => name.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, ImmutableDictionary<string, string>> overrides = new(
            StringComparer.Ordinal
        );

        public override AnalyzerConfigOptions GlobalOptions =>
            new TestOptions(ImmutableDictionary<string, string>.Empty);

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) =>
            new TestOptions(ImmutableDictionary<string, string>.Empty);

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) =>
            new TestOptions(this.MetadataOf(textFile.Path));

        public TestMetadata With(string path, params (string Name, string Value)[] metadata)
        {
            this.overrides[path] = this.MetadataOf(path)
                .SetItems(
                    metadata.Select(static item => new KeyValuePair<string, string>(Prefix + item.Name, item.Value))
                );

            return this;
        }

        public TestMetadata Without(string path)
        {
            this.overrides[path] = ImmutableDictionary<string, string>.Empty;

            return this;
        }

        private static ImmutableDictionary<string, string> Derive(string path)
        {
            if (!path.EndsWith(".resx", StringComparison.OrdinalIgnoreCase))
            {
                return ImmutableDictionary<string, string>.Empty;
            }

            var segments = path.Substring(0, path.Length - ".resx".Length).Split('/').Skip(1).ToList();
            var fileName = segments[^1];
            var lastDot = fileName.LastIndexOf('.');
            var withCulture = lastDot >= 0 && CultureNames.Contains(fileName.Substring(lastDot + 1));

            if (withCulture)
            {
                segments[^1] = fileName.Substring(0, lastDot);
            }

            return ImmutableDictionary<string, string>
                .Empty.Add(Prefix + "ManifestResourceName", "N." + string.Join(".", segments))
                .Add(Prefix + "LogicalName", string.Empty)
                .Add(Prefix + "WithCulture", withCulture ? "true" : "false");
        }

        private ImmutableDictionary<string, string> MetadataOf(string path) =>
            this.overrides.TryGetValue(path, out var metadata) ? metadata : Derive(path);
    }

    private sealed class TestOptions(ImmutableDictionary<string, string> values) : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value) =>
            values.TryGetValue(key, out value);
    }
}
