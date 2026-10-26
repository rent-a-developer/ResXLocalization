using RentADeveloper.ResXLocalization.Benchmarks;

namespace RentADeveloper.ResXLocalization.Core.Benchmarks;

internal static class Program
{
    public static int Main(string[] args) => BenchmarkProgram.Run(typeof(Program).Assembly, args);
}
