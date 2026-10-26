using RentADeveloper.ResXLocalization.Benchmarks;

namespace RentADeveloper.ResXLocalization.WPF.Benchmarks;

internal static class Program
{
    /// <summary>
    /// <c>[STAThread]</c>, because the in-process toolchain runs every benchmark on this thread, and WPF
    /// objects need an STA thread.
    /// </summary>
    /// <param name="args">The command-line arguments, passed to BenchmarkDotNet as given.</param>
    /// <returns>0 when every selected benchmark produced a result, otherwise 1.</returns>
    [STAThread]
    public static int Main(string[] args) => BenchmarkProgram.Run(typeof(Program).Assembly, args);
}
