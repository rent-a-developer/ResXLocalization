namespace RentADeveloper.ResXLocalization.Benchmarks;

/// <summary>
/// The two format arguments of every lookup with arguments: the values the Core argument benchmarks pass, and
/// the view model the two <c>&lt;l:Argument&gt;</c> children of every element in ArgumentsView bind to.
/// </summary>
public sealed class ArgumentValues
{
    /// <summary>Gets the one instance the benchmarks use.</summary>
    public static ArgumentValues Instance { get; } = new();

    public int Count { get; } = 3;
    public string Name { get; } = "Ada";
}
