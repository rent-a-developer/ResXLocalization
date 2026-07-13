namespace RentADeveloper.ResXLocalization.WPF.Sample.Models;

/// <summary>
/// The order in which a list of files can be displayed. The sample localizes the members of this
/// enumeration two different ways to show how <c>KeyPrefix</c> selects between competing label sets:
/// <list type="bullet">
///   <item>
///     The default <c>Enum_</c> prefix maps to keys such as <c>Enum_FileSortOrder_Ascending</c> in
///     <c>ApplicationStrings.resx</c>, which read as "Ascending" / "Aufsteigend".
///   </item>
///   <item>
///     The custom <c>Display_</c> prefix maps to keys such as <c>Display_FileSortOrder_Ascending</c> in
///     <c>SortingStrings.resx</c>, which read as the friendlier "A to Z" / "A bis Z".
///   </item>
/// </list>
/// </summary>
public enum FileSortOrder
{
    /// <summary>Files are shown in their natural, unsorted order.</summary>
    Unsorted = 0,

    /// <summary>Files are shown in ascending order.</summary>
    Ascending = 1,

    /// <summary>Files are shown in descending order.</summary>
    Descending = 2
}
