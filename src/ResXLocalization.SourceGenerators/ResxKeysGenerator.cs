using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace RentADeveloper.ResXLocalization.SourceGenerators;

/// <summary>
/// Incremental Roslyn source generator that emits a typed, compile-validated key class for each
/// neutral <c>.resx</c> file that has a matching resource designer in the compilation. For a file
/// <c>Strings.resx</c> it generates a
/// <c>StringsKeys</c> class exposing one static
/// <c>RentADeveloper.ResXLocalization.ResourceKey</c> per string entry, each bound to the
/// file's <see cref="System.Resources.ResourceManager" />. The generated keys give editor
/// auto-completion and turn missing or renamed resources into compile errors.
/// </summary>
/// <remarks>
/// <para>
/// The generator pairs each <c>.resx</c> with its <c>.Designer.cs</c> file to recover the namespace
/// and resource manager reference; a <c>.resx</c> without a matching designer (such as a satellite
/// translation file) produces no output. A dot-free neutral file without a recognized classic
/// accessor reports <c>RXLGEN002</c>; malformed eligible XML reports <c>RXLGEN001</c>.
/// </para>
/// <para>
/// Member names are sanitized into valid C# identifiers: invalid characters become underscores,
/// names starting with a digit are prefixed with an underscore, reserved keywords are emitted in
/// their <c>@</c>-escaped form, and names that would collide (with each other or with the class
/// name) get a numeric suffix. The generated classes are <c>partial</c>, so two <c>.resx</c> files
/// with the same base name whose designers declare the same namespace merge into one class.
/// </para>
/// </remarks>
[Generator]
public sealed class ResxKeysGenerator : IIncrementalGenerator
{
    /// <summary>
    /// Configures the generation pipeline. Called once by the Roslyn host to register the
    /// <c>.resx</c>-driven source outputs.
    /// </summary>
    /// <param name="context">The initialization context used to build the incremental pipeline.</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var resXFiles = context.AdditionalTextsProvider
            .Where(static f => f.Path.EndsWith(".resx", StringComparison.OrdinalIgnoreCase))
            .Select(static (f, ct) => ParseResxFile(f, ct))
            .Where(static m => m is not null)
            .Select(static (m, _) => m!.Value);

        var resXDesignerFiles = context.AdditionalTextsProvider
            .Where(static f => f.Path.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            .Select(static (f, ct) => ParseResXDesignerFile(f, ct))
            .Where(static m => m is not null)
            .Select(static (m, _) => m!.Value)
            .Collect();

        // One typed-key class per resx (incremental per file).
        var keysPipeline = resXFiles.Combine(resXDesignerFiles);

        context.RegisterSourceOutput(keysPipeline, static (spc, pair) => EmitKeysClass(spc, pair.Left, pair.Right));
    }

    /// <summary>
    /// Emits the typed-key class for one parsed <c>.resx</c> file - or, instead of source, reports
    /// <c>RXLGEN002</c> when the file has no matching designer accessor or <c>RXLGEN001</c> when
    /// its XML failed to parse.
    /// </summary>
    /// <param name="context">The production context receiving the generated source and diagnostics.</param>
    /// <param name="resXFile">The parsed <c>.resx</c> file to generate keys for.</param>
    /// <param name="resXDesignerFiles">Every designer accessor found in the compilation, used for pairing.</param>
    private static void EmitKeysClass(
        SourceProductionContext context,
        ResXFile resXFile,
        ImmutableArray<ResXDesignerFile> resXDesignerFiles
    )
    {
        var resXDesignerFile = FindResXDesignerFile(resXFile, resXDesignerFiles);

        if (resXDesignerFile is null)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    MissingDesignerDescriptor,
                    Location.Create(resXFile.Path, default, default),
                    resXFile.Path
                )
            );
            return;
        }

        if (resXFile.ParseError is not null)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    InvalidResxDescriptor,
                    Location.Create(resXFile.Path, default, default),
                    resXFile.Path,
                    resXFile.ParseError
                )
            );
            return;
        }

        var ns = resXDesignerFile.Value.Namespace;
        var className = SanitizeIdentifier(resXFile.FileBaseName + "Keys");
        var resourceManagerReference = "global::"
            + (ns.Length == 0 ? String.Empty : ns + ".")
            + resXDesignerFile.Value.AccessorType
            + ".ResourceManager";

        var membersBuilder = new StringBuilder();

        // Seed with the class name: a C# member must not be named like its enclosing type (CS0542),
        // so a key equal to the class name falls into the regular numeric-suffix collision handling.
        var usedMemberNames = new HashSet<String>(StringComparer.Ordinal) { className };

        foreach (var key in resXFile.Keys)
        {
            var memberName = SanitizeMemberName(key, usedMemberNames);

            membersBuilder.AppendLine(
                $"    public static readonly global::RentADeveloper.ResXLocalization.ResourceKey {EscapeIdentifier(memberName)} = new({SymbolDisplay.FormatLiteral(key, quote: true)}, {resourceManagerReference});"
            );
        }

        var classCode =
            $$"""
              // <auto-generated/>
              // Generated from a .resx by RentADeveloper.ResXLocalization.SourceGenerators - regenerates on build.
              #nullable enable

              {{(ns.Length == 0 ? String.Empty : "namespace " + ns + ";")}}

              /// <summary>Typed, compile-validated keys generated from {{resXFile.FileBaseName}}.resx.</summary>
              public static partial class {{className}}
              {
              {{membersBuilder}}
              }
              """;

        // The namespace-qualified name keeps the hint unique across resx with the same base name in
        // different namespaces; the directory hash disambiguates the remaining case of two resx with
        // the same base name whose designers declare the SAME namespace (their partial classes merge,
        // but each AddSource call still needs its own hint name).
        context.AddSource(
            (ns.Length == 0 ? String.Empty : ns.Replace("@", String.Empty) + ".")
                + className
                + "."
                + StableHash(resXFile.DirectoryPath)
                + ".g.cs",
            SourceText.From(classCode, Encoding.UTF8)
        );
    }

    /// <summary>Escapes a sanitized member name that happens to be a reserved C# keyword.</summary>
    /// <param name="name">The sanitized member name.</param>
    /// <returns>The name, <c>@</c>-prefixed when it is a reserved keyword.</returns>
    private static String EscapeIdentifier(String name) =>
        SyntaxFacts.GetKeywordKind(name) == SyntaxKind.None ? name : "@" + name;

    /// <summary>
    /// Pairs a <c>.resx</c> file with its designer accessor: same base name in the same directory,
    /// both compared case-insensitively.
    /// </summary>
    /// <param name="resxFile">The parsed <c>.resx</c> file to find the designer for.</param>
    /// <param name="resXDesignerFiles">Every designer accessor found in the compilation.</param>
    /// <returns>The matching designer, or <see langword="null" /> when none matches.</returns>
    private static ResXDesignerFile? FindResXDesignerFile(
        ResXFile resxFile,
        ImmutableArray<ResXDesignerFile> resXDesignerFiles
    )
    {
        // No LINQ FirstOrDefault here: ResXDesignerFile is a struct, so FirstOrDefault would yield a
        // default instance (never null) and a .resx without a designer would emit a broken class.
        foreach (var resXDesignerFile in resXDesignerFiles)
        {
            if (String.Equals(
                    resXDesignerFile.FileBaseName,
                    resxFile.FileBaseName,
                    StringComparison.OrdinalIgnoreCase
                ) &&
                String.Equals(
                    resXDesignerFile.DirectoryPath,
                    resxFile.DirectoryPath,
                    StringComparison.OrdinalIgnoreCase
                ))
            {
                return resXDesignerFile;
            }
        }

        return null;
    }

    /// <summary>
    /// Parses a <c>.Designer.cs</c> file and extracts the classic resource accessor: the type
    /// declaring a static <c>ResourceManager</c> property of type
    /// <see cref="System.Resources.ResourceManager" /> (recognized fully qualified, via a
    /// <c>using System.Resources;</c> import, or through a using alias), plus its namespace.
    /// </summary>
    /// <param name="text">The designer file, supplied as an additional text.</param>
    /// <param name="ct">The token used to cancel parsing.</param>
    /// <returns>
    /// The designer model, or <see langword="null" /> when the file is unreadable or declares no
    /// recognizable resource accessor.
    /// </returns>
    private static ResXDesignerFile? ParseResXDesignerFile(AdditionalText text, CancellationToken ct)
    {
        var fileName = Path.GetFileName(text.Path);
        var fileBaseName = StripSuffix(fileName, ".Designer.cs");

        if (fileBaseName.Length == 0)
        {
            return null;
        }

        var source = text.GetText(ct);

        if (source is null)
        {
            return null;
        }

        var root = CSharpSyntaxTree.ParseText(source, cancellationToken: ct).GetRoot(ct);
        var resourceManagerAliases = root.DescendantNodes()
            .OfType<UsingDirectiveSyntax>()
            .Where(static directive => directive.Alias is not null
                && IsResourceManagerType(
                    directive.Name?.ToString(),
                    ImmutableHashSet<String>.Empty
                ))
            .Select(static directive => directive.Alias!.Name.Identifier.ValueText)
            .ToImmutableHashSet(StringComparer.Ordinal);
        var importsSystemResources = root.DescendantNodes()
            .OfType<UsingDirectiveSyntax>()
            .Any(static directive => directive.Alias is null && directive.Name?.ToString() == "System.Resources");

        foreach (var property in root.DescendantNodes().OfType<PropertyDeclarationSyntax>())
        {
            if (property.Identifier.ValueText != "ResourceManager"
                || !property.Modifiers.Any(SyntaxKind.StaticKeyword)
                || !IsResourceManagerType(property.Type.ToString(), resourceManagerAliases, importsSystemResources))
            {
                continue;
            }

            var accessor = property.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
            if (accessor is null)
            {
                continue;
            }

            var namespaceNode = accessor.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
            return new ResXDesignerFile(
                Path.GetDirectoryName(text.Path)!,
                fileBaseName,
                namespaceNode?.Name.ToString() ?? String.Empty,
                EscapeIdentifier(accessor.Identifier.ValueText)
            );
        }

        return null;
    }

    /// <summary>
    /// Parses a neutral <c>.resx</c> file into its ordinally sorted string-resource keys. Satellite
    /// files (dotted base names such as <c>Strings.de.resx</c>) are skipped without parsing.
    /// </summary>
    /// <param name="text">The <c>.resx</c> file, supplied as an additional text.</param>
    /// <param name="ct">The token used to cancel parsing.</param>
    /// <returns>
    /// The parsed model, carrying either the keys or - for malformed XML - the parse error to report
    /// at emit time; or <see langword="null" /> when the file is a satellite or unreadable.
    /// </returns>
    private static ResXFile? ParseResxFile(AdditionalText text, CancellationToken ct)
    {
        var fileBaseName = Path.GetFileNameWithoutExtension(text.Path);

        // Satellites (Strings1.de.resx) keep a dotted base name; they have no designer, so they get
        // skipped at pairing time anyway. Skipping here too avoids parsing them.
        if (fileBaseName.Length == 0 || fileBaseName.IndexOf('.') >= 0)
        {
            return null;
        }

        var source = text.GetText(ct);

        if (source is null)
        {
            return null;
        }

        try
        {
            var keys = ReadResXKeys(source.ToString())
                .OrderBy(static k => k, StringComparer.Ordinal)
                .ToImmutableArray();
            return new ResXFile(text.Path, Path.GetDirectoryName(text.Path)!, fileBaseName, new(keys), null);
        }
        catch (System.Xml.XmlException exception)
        {
            return new ResXFile(text.Path, Path.GetDirectoryName(text.Path)!, fileBaseName, [], exception.Message);
        }
    }

    /// <summary>
    /// Reads the string-resource entry names from a <c>.resx</c> document, applying the same
    /// filtering as ResGen: WinForms designer metadata (names starting with <c>$</c> or
    /// <c>&gt;&gt;</c>) and non-string resources (entries with a <c>type</c> or <c>mimetype</c>
    /// attribute) are skipped.
    /// </summary>
    /// <param name="xml">The raw <c>.resx</c> XML.</param>
    /// <returns>The string-resource key names, in document order.</returns>
    /// <exception cref="System.Xml.XmlException"><paramref name="xml" /> is not well-formed.</exception>
    private static IEnumerable<String> ReadResXKeys(String xml)
    {
        var doc = XDocument.Parse(xml);

        foreach (var data in doc.Root?.Elements("data") ?? [])
        {
            var name = data.Attribute("name")?.Value;

            if (String.IsNullOrEmpty(name))
            {
                continue;
            }

            // Skip WinForms designer metadata entries. A form's resx stores component properties as
            // "$this.Text" and ">>$this.Name"; ResGen skips any name starting with "$" or ">>", and so
            // do we - they are not string resources and have no accessor member.
            if (name![0] == '$' || name.StartsWith(">>", StringComparison.Ordinal))
            {
                continue;
            }

            // Skip non-String resources (colors, images, …) - those carry a type/mimetype attribute.
            if (data.Attribute("type") is not null || data.Attribute("mimetype") is not null)
            {
                continue;
            }

            yield return name;
        }
    }

    /// <summary>
    /// Converts a resource key into a unique, valid member name: non-alphanumeric characters become
    /// underscores, a leading digit gets an underscore prefix, and a name already taken - by another
    /// key or by the class itself - gets a numeric suffix (<c>2</c>, <c>3</c>, …).
    /// </summary>
    /// <param name="key">The resource key to convert.</param>
    /// <param name="usedMemberNames">The names taken so far; the chosen name is added to the set.</param>
    /// <returns>The unique member name, not yet <c>@</c>-escaped (see <see cref="EscapeIdentifier" />).</returns>
    private static String SanitizeMemberName(String key, HashSet<String> usedMemberNames)
    {
        var builder = new StringBuilder(key.Length);

        foreach (var c in key)
        {
            builder.Append(Char.IsLetterOrDigit(c) ? c : '_');
        }

        if (builder.Length == 0 || Char.IsDigit(builder[0]))
        {
            builder.Insert(0, '_');
        }

        var baseName = builder.ToString();

        var name = baseName;
        var suffix = 1;

        while (!usedMemberNames.Add(name))
        {
            name = baseName + ++suffix;
        }

        return name;
    }

    /// <summary>
    /// Converts arbitrary text into a valid C# identifier: invalid characters become underscores,
    /// an invalid first character gets an underscore prefix, and reserved keywords are
    /// <c>@</c>-escaped.
    /// </summary>
    /// <param name="value">The text to convert.</param>
    /// <returns>The valid, escaped identifier.</returns>
    private static String SanitizeIdentifier(String value)
    {
        var builder = new StringBuilder(value.Length + 1);
        foreach (var character in value)
        {
            builder.Append(SyntaxFacts.IsIdentifierPartCharacter(character) ? character : '_');
        }

        if (builder.Length == 0 || !SyntaxFacts.IsIdentifierStartCharacter(builder[0]))
        {
            builder.Insert(0, '_');
        }

        return EscapeIdentifier(builder.ToString());
    }

    /// <summary>
    /// Determines whether a designer property type refers to
    /// <see cref="System.Resources.ResourceManager" /> - written fully qualified (with or without
    /// <c>global::</c>), as the bare name under a <c>using System.Resources;</c> import, or through
    /// a using alias.
    /// </summary>
    /// <param name="typeName">The property type as written in the designer source.</param>
    /// <param name="aliases">The file's using aliases that resolve to the resource manager type.</param>
    /// <param name="importsSystemResources">Whether the file imports <c>System.Resources</c>.</param>
    /// <returns><see langword="true" /> when the type refers to the resource manager type.</returns>
    private static Boolean IsResourceManagerType(
        String? typeName,
        ImmutableHashSet<String> aliases,
        Boolean importsSystemResources = false
    )
    {
        var normalized = typeName?.Replace("global::", String.Empty);
        return normalized == "System.Resources.ResourceManager"
            || (normalized == "ResourceManager" && importsSystemResources)
            || (normalized is not null && aliases.Contains(normalized));
    }

    /// <summary>
    /// Produces a short, deterministic (FNV-1a) hex digest of a path, used to keep hint names unique
    /// per source directory. Case-normalized to match the OrdinalIgnoreCase path pairing above.
    /// </summary>
    /// <param name="value">The path to digest.</param>
    /// <returns>An eight-character lowercase hex digest.</returns>
    private static String StableHash(String value)
    {
        var hash = 2166136261u;

        foreach (var c in value.ToUpperInvariant())
        {
            hash = unchecked((hash ^ c) * 16777619u);
        }

        return hash.ToString("x8", CultureInfo.InvariantCulture);
    }

    /// <summary>Removes a suffix from a file name, comparing case-insensitively.</summary>
    /// <param name="name">The file name to trim.</param>
    /// <param name="suffix">The suffix to remove.</param>
    /// <returns>
    /// <paramref name="name" /> without <paramref name="suffix" />, or unchanged when the suffix
    /// is absent.
    /// </returns>
    private static String StripSuffix(String name, String suffix) =>
        name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
            ? name.Substring(0, name.Length - suffix.Length)
            : name;

    /// <summary>
    /// <c>RXLGEN001</c> (warning): an eligible <c>.resx</c> file - neutral, with a matching designer -
    /// contains malformed XML, so no keys were generated for it.
    /// </summary>
    private static readonly DiagnosticDescriptor InvalidResxDescriptor = new(
        "RXLGEN001",
        "Invalid .resx file",
        "Could not parse eligible resource file '{0}': {1}",
        "ResXLocalization.SourceGenerator",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    /// <summary>
    /// <c>RXLGEN002</c> (warning): a neutral <c>.resx</c> file has no same-folder classic
    /// <c>.Designer.cs</c> accessor with a static <c>ResourceManager</c> property, so no keys were
    /// generated for it.
    /// </summary>
    private static readonly DiagnosticDescriptor MissingDesignerDescriptor = new(
        "RXLGEN002",
        "Eligible .resx has no recognized classic designer",
        "Resource file '{0}' needs a same-folder classic .Designer.cs accessor with a static System.Resources.ResourceManager property",
        "ResXLocalization.SourceGenerator",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    /// <summary>The incremental-pipeline model of a parsed neutral <c>.resx</c> file.</summary>
    /// <param name="Path">The full path of the <c>.resx</c> file, used in diagnostic locations.</param>
    /// <param name="DirectoryPath">The containing directory, used to pair the file with its designer.</param>
    /// <param name="FileBaseName">The file name without extension, for example <c>Strings</c>.</param>
    /// <param name="Keys">The string-resource keys, ordinally sorted; empty when parsing failed.</param>
    /// <param name="ParseError">The XML parse error message, or <see langword="null" /> when parsing succeeded.</param>
    private readonly record struct ResXFile(
        String Path,
        String DirectoryPath,
        String FileBaseName,
        EquatableArray<String> Keys,
        String? ParseError
    );

    /// <summary>The incremental-pipeline model of a classic <c>.Designer.cs</c> resource accessor.</summary>
    /// <param name="DirectoryPath">The containing directory, used to pair the designer with its <c>.resx</c>.</param>
    /// <param name="FileBaseName">The file name without the <c>.Designer.cs</c> suffix.</param>
    /// <param name="Namespace">The namespace declared in the designer; empty for the global namespace.</param>
    /// <param name="AccessorType">
    /// The (already <c>@</c>-escaped) name of the type exposing the static <c>ResourceManager</c>
    /// property.
    /// </param>
    private readonly record struct ResXDesignerFile(
        String DirectoryPath,
        String FileBaseName,
        String Namespace,
        String AccessorType
    );
}
