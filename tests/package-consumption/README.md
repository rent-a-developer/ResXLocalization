# Package consumption tests

These package-only apps consume the **packed NuGet packages** (not project references) and are the
only place the most fragile parts of the packaging are exercised end to end:

- the `buildTransitive/<PackageId>.targets` auto-import that feeds `.resx` + `.Designer.cs` files
  to the source generator, together with the MSBuild resource name of each `.resx` that every keys
  class is created from - for a file without a designer (`Resources/Designerless.resx`), a name
  replaced by `LogicalName` (`Logical.resx`) and one the `DependentUpon` convention takes from a class
  (`Conventional.resx` next to `Conventional.cs`), in a multi-targeted project (`AvaloniaConsumer`)
  and through WPF's temporary markup-compile pass (`WpfConsumer`),
- the source generator shipped as `analyzers/dotnet/cs`,
- the transitive `ResXLocalization.Core` package,
- the `net8.0` target of the multi-targeted packages,
- the satellite-assembly flow of a real consumer.

The Avalonia and WPF apps compile real framework XAML against the generated `StringsKeys` and
`DesignerlessKeys` classes, instantiate the view, and assert rendered live language switching.
`CombinedConsumer` references both UI packages and proves that one Core assembly is resolved. Every
app exits non-zero on failure.

The folder has its own `Directory.Build.props`/`.targets`/`Directory.Packages.props` boundary so
none of the repository's in-repo wiring leaks in, and its own isolated package cache (`.packages`).

## Running locally

```shell
# From the repository root: pack the packages, then run the consumers.
dotnet pack src/ResXLocalization.Core/ResXLocalization.Core.csproj -c Release -o artifacts/packages
dotnet pack src/ResXLocalization.Avalonia/ResXLocalization.Avalonia.csproj -c Release -o artifacts/packages
dotnet pack src/ResXLocalization.WPF/ResXLocalization.WPF.csproj -c Release -o artifacts/packages   # Windows only

# A reused MSBuild node keeps evaluating the previously packed targets of the same version; stop it.
dotnet build-server shutdown

dotnet run --project tests/package-consumption/AvaloniaConsumer/AvaloniaConsumer.csproj -c Release
dotnet run --project tests/package-consumption/WpfConsumer/WpfConsumer.csproj -c Release            # Windows only
dotnet run --project tests/package-consumption/CombinedConsumer/CombinedConsumer.csproj -c Release # Windows only
```

After re-packing, delete `tests/package-consumption/.packages` so NuGet picks up the new build of
the same version number.
