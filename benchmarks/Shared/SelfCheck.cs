using System.Reflection;
using BenchmarkDotNet.Attributes;

namespace RentADeveloper.ResXLocalization.Benchmarks;

/// <summary>
/// Runs every benchmark method of a class once and throws when it does not produce the expected texts. Each
/// class calls it from its <c>[GlobalSetup]</c>, so a broken lookup stops that class with an error naming the
/// benchmark, instead of producing a number for the failure text.
/// </summary>
internal static class SelfCheck
{
    /// <summary>Verifies the benchmark methods of <paramref name="benchmarks" /> that return the text itself.</summary>
    /// <param name="benchmarks">The benchmark class instance.</param>
    /// <param name="expected">The text each benchmark method, by name, has to return.</param>
    public static void Verify(object benchmarks, Func<string, string> expected) =>
        Verify<string>(benchmarks, name => [expected(name)], static result => [result]);

    /// <summary>
    /// Verifies every benchmark method of <paramref name="benchmarks" />, after running the
    /// <c>[IterationSetup]</c> methods that apply to it.
    /// </summary>
    /// <typeparam name="TResult">The type every benchmark method of the class returns.</typeparam>
    /// <param name="benchmarks">The benchmark class instance.</param>
    /// <param name="expected">The texts each benchmark method, by name, has to produce, in order.</param>
    /// <param name="texts">Reads the texts out of what a benchmark method returned.</param>
    /// <exception cref="InvalidOperationException">A benchmark method produced other texts.</exception>
    public static void Verify<TResult>(
        object benchmarks,
        Func<string, IReadOnlyList<string>> expected,
        Func<TResult, IReadOnlyList<string>> texts
    )
    {
        var type = benchmarks.GetType();
        var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
        var iterationSetups = methods
            .Select(static method =>
                (Method: method, SetupAttribute: method.GetCustomAttribute<IterationSetupAttribute>())
            )
            .Where(static setup => setup.SetupAttribute is not null)
            .ToList();

        foreach (var method in methods.Where(static method => method.IsDefined(typeof(BenchmarkAttribute))))
        {
            foreach (var setup in iterationSetups)
            {
                if (setup.SetupAttribute?.Targets is not { Length: > 0 } targets || targets.Contains(method.Name))
                {
                    _ = setup.Method.Invoke(benchmarks, BindingFlags.DoNotWrapExceptions, null, null, null);
                }
            }

            var actual = texts((TResult)method.Invoke(benchmarks, BindingFlags.DoNotWrapExceptions, null, null, null)!);
            var wanted = expected(method.Name);

            if (actual.Count != wanted.Count)
            {
                throw new InvalidOperationException(
                    $"Self-check of {method.DeclaringType!.Name}.{method.Name} failed: expected {wanted.Count} texts, "
                        + $"but it produced {actual.Count}."
                );
            }

            for (var index = 0; index < wanted.Count; index++)
            {
                if (!string.Equals(actual[index], wanted[index], StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"Self-check of {method.DeclaringType!.Name}.{method.Name} failed: text {index + 1} of "
                            + $"{wanted.Count} is \"{actual[index]}\" instead of \"{wanted[index]}\"."
                    );
                }
            }
        }
    }
}
