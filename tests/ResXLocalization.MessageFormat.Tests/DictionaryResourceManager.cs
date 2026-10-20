namespace RentADeveloper.ResXLocalization.MessageFormat.Tests;

/// <summary>
/// A <see cref="ResourceManager" /> over in-memory values, so that ICU patterns need no <c>.resx</c> file.
/// Every value is a neutral value: it is returned for every culture.
/// </summary>
/// <param name="values">The resource keys and their values.</param>
internal sealed class DictionaryResourceManager(params (string Key, string Value)[] values) : ResourceManager
{
    /// <summary>The values, keyed by resource key.</summary>
    private readonly Dictionary<string, string> values = values.ToDictionary(
        entry => entry.Key,
        entry => entry.Value,
        StringComparer.Ordinal
    );

    /// <inheritdoc />
    public override string? GetString(string name, CultureInfo? culture) => this.values.GetValueOrDefault(name);
}
