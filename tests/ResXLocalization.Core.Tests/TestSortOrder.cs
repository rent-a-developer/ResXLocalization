namespace RentADeveloper.ResXLocalization.Core.Tests;

/// <summary>
/// Test-only enumeration for the enum lookup tests. Catalog.resx localizes
/// <see cref="Ascending" /> under both the default <c>Enum_</c> and the custom <c>Display_</c> prefix;
/// <see cref="Descending" /> is deliberately not localized anywhere.
/// </summary>
public enum TestSortOrder
{
    /// <summary>Localized in Catalog.resx (both prefixes, both cultures).</summary>
    Ascending = 0,

    /// <summary>Deliberately missing from every resource file.</summary>
    Descending = 1
}
