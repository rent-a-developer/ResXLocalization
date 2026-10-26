using System.Reflection;
using BenchmarkDotNet.Running;

namespace RentADeveloper.ResXLocalization.Benchmarks;

/// <summary>The entry point every benchmark project's <c>Main</c> delegates to.</summary>
internal static class BenchmarkProgram
{
    /// <summary>
    /// Runs the benchmarks of <paramref name="assembly" /> that the command line selects, for example with
    /// <c>--filter *CodeLookup*</c>, or all of them without arguments, instead of BenchmarkDotNet's interactive
    /// prompt.
    /// </summary>
    /// <param name="assembly">The benchmark project's assembly.</param>
    /// <param name="args">The command-line arguments, passed to BenchmarkDotNet as given.</param>
    /// <returns>
    /// 0 when every selected benchmark produced a result; 1 when one failed, such as a self-check, so that
    /// CI fails instead of reporting a table with a missing row, and 1 when nothing ran, such as arguments
    /// without <c>--filter</c>, which make BenchmarkDotNet wait for a selection that never comes.
    /// </returns>
    public static int Run(Assembly assembly, string[] args)
    {
        var summaries = BenchmarkSwitcher
            .FromAssembly(assembly)
            .Run(args.Length == 0 ? ["--filter", "*"] : args)
            .ToList();

        return
            summaries.Count == 0
            || summaries.Exists(static summary =>
                summary.HasCriticalValidationErrors || summary.Reports.Any(static report => !report.Success)
            )
            ? 1
            : 0;
    }
}
