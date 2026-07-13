namespace RentADeveloper.ResXLocalization.SourceGenerators.Tests;

/// <summary>
/// Unit tests for <see cref="ResxKeysGenerator" />. They drive the incremental generator in-memory
/// with <see cref="CSharpGeneratorDriver" /> against fabricated <c>.resx</c> / <c>.Designer.cs</c>
/// additional files and assert on the generated source - no file system, no MSBuild.
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
    public void DesignerInADifferentFolder_DoesNotPair()
    {
        var result = RunGenerator(
            ("project/Resources/Strings.resx", Resx(("Greeting", "Hello"))),
            ("project/Other/Strings.Designer.cs", Designer("My.App"))
        );

        result.Diagnostics.Should().ContainSingle().Which.Id.Should().Be("RXLGEN002");
        result.Results.Single().GeneratedSources.Should().BeEmpty();
    }

    [Fact]
    public void DottedResxBaseName_IsTreatedAsSatelliteAndSkipped()
    {
        var result = RunGenerator(
            ("project/My.Strings.resx", Resx(("Greeting", "Hello"))),
            ("project/My.Strings.Designer.cs", Designer("My.App"))
        );

        result.Results.Single().GeneratedSources.Should().BeEmpty();
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
    public void MalformedEligibleResx_ReportsAnActionableDiagnostic()
    {
        var result = RunGenerator(
            ("project/Strings.resx", "<root><data name='Broken'"),
            ("project/Strings.Designer.cs", Designer("My.App"))
        );

        var diagnostic = result.Diagnostics.Should().ContainSingle().Subject;
        diagnostic.Id.Should().Be("RXLGEN001");
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Warning);
        diagnostic.Location.GetLineSpan().Path.Should().Be("project/Strings.resx");
        diagnostic.GetMessage(CultureInfo.InvariantCulture).Should().Contain("Could not parse eligible resource file");
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

        code.IndexOf("Apple", StringComparison.Ordinal).Should().BeLessThan(
            code.IndexOf("Zebra", StringComparison.Ordinal)
        );
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
        code.Should().Contain(
            """Greeting = new("Greeting", global::My.App.Resources.Strings.ResourceManager);"""
        );
        code.Should().Contain(
            """WindowTitle = new("WindowTitle", global::My.App.Resources.Strings.ResourceManager);"""
        );
    }

    [Fact]
    public void NonStringEntries_AreSkipped()
    {
        const String resx =
            """
            <?xml version="1.0" encoding="utf-8"?>
            <root>
              <data name="Greeting" xml:space="preserve"><value>Hello</value></data>
              <data name="Logo" type="System.Drawing.Bitmap, System.Drawing"><value>logo.png</value></data>
              <data name="Blob" mimetype="application/x-microsoft.net.object.binary.base64"><value>AAEC</value></data>
            </root>
            """;

        var result = RunGenerator(
            ("project/Strings.resx", resx),
            ("project/Strings.Designer.cs", Designer("My.App"))
        );

        var code = SingleGeneratedSource(result).SourceText.ToString();

        code.Should().Contain("Greeting");
        code.Should().NotContain("Logo");
        code.Should().NotContain("Blob");
    }

    [Fact]
    public void WinFormsDesignerMetadataEntries_AreSkipped()
    {
        // A WinForms form's resx stores component properties as "$this.Text" / ">>$this.Name"; only
        // the real string entry should surface as a key.
        const String resx =
            """
            <?xml version="1.0" encoding="utf-8"?>
            <root>
              <data name="$this.Text" xml:space="preserve"><value>Form1</value></data>
              <data name="&gt;&gt;$this.Name" xml:space="preserve"><value>$this</value></data>
              <data name="Greeting" xml:space="preserve"><value>Hello</value></data>
            </root>
            """;

        var result = RunGenerator(
            ("project/Strings.resx", resx),
            ("project/Strings.Designer.cs", Designer("My.App"))
        );

        var code = SingleGeneratedSource(result).SourceText.ToString();

        code.Should().Contain("""Greeting = new("Greeting", """);
        code.Should().NotContain("this");
    }

    [Fact]
    public void DesignerWithoutAResourceManagerMember_DoesNotPair()
    {
        // A WinForms Form1.Designer.cs sits next to Form1.resx and matches the *.Designer.cs name, but
        // it is not a resx accessor (no ResourceManager member), so it must not pair.
        const String formDesigner =
            """
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

        result.Diagnostics.Should().ContainSingle().Which.Id.Should().Be("RXLGEN002");
        result.Results.Single().GeneratedSources.Should().BeEmpty();
    }

    [Theory]
    [InlineData("global::System.Resources.ResourceManager", "")]
    [InlineData("System.Resources.ResourceManager", "")]
    [InlineData("ResourceManager", "using System.Resources;")]
    [InlineData("RM", "using RM = System.Resources.ResourceManager;")]
    public void ResourceManagerTypeSpellings_AreRecognized(String typeName, String usingDirective)
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

        SingleGeneratedSource(result).SourceText.ToString()
            .Should().Contain("global::My.App.ActualAccessor.ResourceManager");
    }

    [Fact]
    public void GlobalNamespaceAndInvalidFilename_EmitValidSanitizedClass()
    {
        const String designer =
            "internal class OrderStatus { public static global::System.Resources.ResourceManager ResourceManager => null; }";
        var result = RunGenerator(
            ("project/Order-Status.resx", Resx(("Greeting", "Hello"))),
            ("project/Order-Status.Designer.cs", designer)
        );
        var code = SingleGeneratedSource(result).SourceText.ToString();

        code.Should().Contain("class Order_StatusKeys");
        code.Should().Contain("global::OrderStatus.ResourceManager");
        code.Should().NotContain("namespace ;");
    }

    [Fact]
    public void EscapedNamespaceSegments_ArePreserved()
    {
        const String designer =
            "namespace @class.Δ { internal class Strings { public static global::System.Resources.ResourceManager ResourceManager => null; } }";
        var result = RunGenerator(
            ("project/Strings.resx", Resx(("Greeting", "Hello"))),
            ("project/Strings.Designer.cs", designer)
        );

        SingleGeneratedSource(result).SourceText.ToString().Should().Contain("namespace @class.Δ;");
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
    public void ResxWithoutSiblingDesigner_ProducesNoOutput()
    {
        var result = RunGenerator(
            ("project/Strings.resx", Resx(("Greeting", "Hello")))
        );

        var diagnostic = result.Diagnostics.Should().ContainSingle().Subject;
        diagnostic.Id.Should().Be("RXLGEN002");
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Warning);
        diagnostic.Location.GetLineSpan().Path.Should().Be("project/Strings.resx");
        result.Results.Single().GeneratedSources.Should().BeEmpty();
    }

    [Fact]
    public void SatelliteResx_ProducesNoOutput()
    {
        var result = RunGenerator(
            ("project/Strings.de.resx", Resx(("Greeting", "Hallo"))),
            ("project/Strings.Designer.cs", Designer("My.App"))
        );

        // Only the neutral file (with no resx present here) could pair with the designer; the
        // satellite must not.
        result.Results.Single().GeneratedSources.Should().BeEmpty();
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
    public void TwoResxWithTheSameBaseNameAndTheSameNamespace_GetDistinctHintNames_AndMergeAsPartials()
    {
        // Customized accessors can legitimately have different type names while two same-named resx
        // files generate partial My.App.StringsKeys declarations. Both the input and output compile.
        var result = RunGenerator(
            ("project/One/Strings.resx", Resx(("Alpha", "a"))),
            ("project/One/Strings.Designer.cs", Designer("My.App").Replace("class Strings", "class StringsOne", StringComparison.Ordinal)),
            ("project/Two/Strings.resx", Resx(("Beta", "b"))),
            ("project/Two/Strings.Designer.cs", Designer("My.App").Replace("class Strings", "class StringsTwo", StringComparison.Ordinal))
        );

        result.Diagnostics.Should().BeEmpty();

        var sources = result.Results.Single().GeneratedSources;

        sources.Should().HaveCount(2);
        sources.Select(s => s.HintName).Should().OnlyHaveUniqueItems();
        sources.Select(s => s.SourceText.ToString())
            .Should()
            .AllSatisfy(code => code.Should().Contain("public static partial class StringsKeys"));
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
    public void KeyEqualToTheGeneratedClassName_GetsANumericSuffix()
    {
        // A member must not be named like its enclosing type (CS0542), so the key "StringsKeys"
        // inside the generated StringsKeys class is renamed like any other collision.
        var result = RunGenerator(
            ("project/Strings.resx", Resx(("StringsKeys", "a"))),
            ("project/Strings.Designer.cs", Designer("My.App"))
        );

        var code = SingleGeneratedSource(result).SourceText.ToString();

        code.Should().Contain("""StringsKeys2 = new("StringsKeys", """);
    }

    [Fact]
    public void ControlCharactersInKeyNames_AreEscapedInTheGeneratedLiteral()
    {
        // A literal newline in an XML attribute would be normalized to a space, so the resx has to
        // use a character reference to smuggle one into the key name.
        const String resx =
            """
            <?xml version="1.0" encoding="utf-8"?>
            <root>
              <data name="Line&#10;Break" xml:space="preserve"><value>a</value></data>
            </root>
            """;

        var result = RunGenerator(
            ("project/Strings.resx", resx),
            ("project/Strings.Designer.cs", Designer("My.App"))
        );

        var code = SingleGeneratedSource(result).SourceText.ToString();

        code.Should().Contain("""Line_Break = new("Line\nBreak", """);
    }

    [Fact]
    public void SecondRun_WithEquivalentInputs_CachesTheOutput()
    {
        var compilation = CSharpCompilation.Create("Tests");
        GeneratorDriver driver = CreateTrackingDriver();

        var resx = new InMemoryAdditionalText(
            "project/Strings.resx",
            Resx(("Greeting", "Hello"), ("Bye", "Goodbye"))
        );
        var designer = new InMemoryAdditionalText("project/Strings.Designer.cs", Designer("My.App"));

        driver = driver.AddAdditionalTexts([resx, designer]).RunGenerators(compilation);

        // Replace both inputs with fresh instances of byte-identical content: the parse transform
        // re-executes, but the resulting ResXFile must compare equal (thanks to EquatableArray-backed
        // keys), so the source output is served from the incremental cache rather than regenerated.
        driver = driver
            .ReplaceAdditionalText(resx, new InMemoryAdditionalText(resx.Path, resx.GetText().ToString()))
            .ReplaceAdditionalText(designer, new InMemoryAdditionalText(designer.Path, designer.GetText().ToString()))
            .RunGenerators(compilation);

        OutputReasons(driver).Should().NotBeEmpty().And.OnlyContain(
            reason => reason == IncrementalStepRunReason.Cached
        );
    }

    [Fact]
    public void TouchingOneResx_ReRunsOnlyThatFilesOutput()
    {
        var compilation = CSharpCompilation.Create("Tests");
        GeneratorDriver driver = CreateTrackingDriver();

        var oneResx = new InMemoryAdditionalText("project/One/Strings.resx", Resx(("Alpha", "a")));

        driver = driver
            .AddAdditionalTexts(
            [
                oneResx,
                new InMemoryAdditionalText("project/Two/Strings.resx", Resx(("Beta", "b"))),
                new InMemoryAdditionalText("project/One/Strings.Designer.cs", Designer("My.App.One")),
                new InMemoryAdditionalText("project/Two/Strings.Designer.cs", Designer("My.App.Two"))
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
        reasons.Should().Contain(
            reason => reason == IncrementalStepRunReason.Modified || reason == IncrementalStepRunReason.New
        );
    }

    private static CSharpGeneratorDriver CreateTrackingDriver() =>
        CSharpGeneratorDriver.Create(
            [new ResxKeysGenerator().AsSourceGenerator()],
            parseOptions: null,
            optionsProvider: null,
            driverOptions: new(
                IncrementalGeneratorOutputKind.None,
                trackIncrementalGeneratorSteps: true
            )
        );

    private static IEnumerable<IncrementalStepRunReason> OutputReasons(GeneratorDriver driver) =>
        driver.GetRunResult().Results.Single().TrackedOutputSteps
            .SelectMany(step => step.Value)
            .SelectMany(run => run.Outputs)
            .Select(output => output.Reason);

    private static String Designer(String ns) =>
        $$"""
          namespace {{ns}} {
              internal class Strings {
                  public static global::System.Resources.ResourceManager ResourceManager {
                      get { return null; }
                  }
              }
          }
          """;

    private static String Resx(params (String Name, String Value)[] entries)
    {
        var builder = new StringBuilder();

        builder
            .AppendLine("""<?xml version="1.0" encoding="utf-8"?>""")
            .AppendLine("<root>");

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

    private static GeneratorDriverRunResult RunGenerator(params (String Path, String Content)[] files)
    {
        var references = ((String)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(static path => MetadataReference.CreateFromFile(path));
        var sourceTrees = files
            .Where(static file => file.Path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .Select(static file => CSharpSyntaxTree.ParseText(file.Content, path: file.Path))
            .Append(CSharpSyntaxTree.ParseText(
                "namespace RentADeveloper.ResXLocalization { public readonly struct ResourceKey { public ResourceKey(string key, global::System.Resources.ResourceManager manager) { } } }"
            ));
        var compilation = CSharpCompilation.Create(
            "Tests",
            sourceTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        var additionalTexts = files
            .Select(AdditionalText (file) => new InMemoryAdditionalText(file.Path, file.Content))
            .ToImmutableArray();

        var driver = CSharpGeneratorDriver.Create(new ResxKeysGenerator())
            .AddAdditionalTexts(additionalTexts);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);
        outputCompilation.GetDiagnostics()
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

    private sealed class InMemoryAdditionalText(String path, String content) : AdditionalText
    {
        public override String Path => path;

        public override SourceText GetText(CancellationToken cancellationToken = default) =>
            SourceText.From(content, Encoding.UTF8);
    }
}
