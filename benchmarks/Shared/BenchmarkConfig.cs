using System.Reflection;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Perfolizer.Horology;

namespace RentADeveloper.ResXLocalization.Benchmarks;

/// <summary>
/// The one configuration every benchmark class uses, through <c>[Config(typeof(BenchmarkConfig))]</c>, locally
/// and in CI.
/// </summary>
/// <remarks>
/// <para>
/// One short job: 3 warmup and 15 measured iterations of 100 ms, instead of BenchmarkDotNet's 500 ms and its
/// automatic choice of the iteration count. 15 iterations keep the mean time stable enough for the alert at 150%
/// in classes that run one operation per iteration, which is every UI class; the scaling classes, whose
/// 10,000-element operations are slow, lower it. Allocations, which fail the check, do not depend on the number
/// of iterations. The warmups stay, so that every measurement runs on fully optimized code, which allocates some
/// objects on the stack that unoptimized code allocates on the heap.
/// </para>
/// <para>
/// The in-process toolchain runs every benchmark in this process instead of a separately built and started
/// one, which removes a build and a process start per benchmark.
/// </para>
/// </remarks>
public sealed class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        this.AddJob(
            Job.Default.WithToolchain(InProcessEmitToolchain.Instance)
                .WithWarmupCount(3)
                .WithIterationCount(15)
                .WithIterationTime(TimeInterval.FromMilliseconds(100))
        );
        this.AddDiagnoser(MemoryDiagnoser.Default);

        // The full JSON report is what CI reads. The GitHub markdown and HTML reports, which CI publishes, come
        // from BenchmarkDotNet's default configuration, which every [Config] is merged with; adding them here
        // too would make BenchmarkDotNet warn about duplicate exporters.
        this.AddExporter(JsonExporter.Full);

        // artifacts/ is where the repository puts everything it generates: .gitignore excludes it and
        // scripts/clean-build-artifacts.ps1 deletes it. BenchmarkDotNet adds results/ and the logs below it.
        this.ArtifactsPath = Path.Combine(
            FindRepositoryRoot(),
            "artifacts",
            "benchmarks",
            Assembly.GetEntryAssembly()!.GetName().Name!
        );
    }

    /// <summary>Finds the repository root: the nearest directory above the binaries with the solution file.</summary>
    /// <returns>The full path of the repository root.</returns>
    /// <exception cref="InvalidOperationException">The binaries are not below the repository root.</exception>
    private static string FindRepositoryRoot()
    {
        for (
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            directory is not null;
            directory = directory.Parent
        )
        {
            if (File.Exists(Path.Combine(directory.FullName, "ResXLocalization.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new InvalidOperationException(
            $"No directory above {AppContext.BaseDirectory} contains ResXLocalization.slnx. Run the benchmarks from their build output in the repository."
        );
    }
}
