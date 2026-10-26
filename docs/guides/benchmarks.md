# Benchmarks

Every push to `main` measures what ResXLocalization costs an application, on the same GitHub-hosted Windows
runner, .NET 10 and the just-in-time compiler:

- [Latest results](https://rent-a-developer.github.io/ResXLocalization/benchmarks/latest/): the tables of the
  last commit on `main`, with the hardware and runtime they were measured on.
- [History](https://rent-a-developer.github.io/ResXLocalization/benchmarks/history/): the time and allocations
  of every benchmark over the last 200 commits on `main`.

Every benchmark is compared with the code an application writes without the library: the `.resx` designer
class and hand-written code over it.

| Benchmark | What it measures | Baseline |
| --- | --- | --- |
| `CodeLookupBenchmarks` | `Localizer.Current.Get` with a typed key, a scoped key, a search-all key and an enum value | The designer class's property, `BenchmarkStrings.Greeting` |
| `SearchAllBenchmarks` | A search-all lookup whose key is in the last of 1, 5 or 20 registered resource files | A hand-written loop that asks the same resource managers, in the same order, until one has the key |
| `ArgumentBenchmarks` | `Get` with `MessageArguments.FromValues`, `FromTuples` and `FromObject` | `string.Format` over the designer class's property |
| `MessageFormatBenchmarks` | `Get` with two named arguments and `UseMessageFormat()` installed | The MessageFormat library's `FormatMessage` over the designer class's property |
| `AvaloniaFormsBenchmarks`, `WpfFormsBenchmarks` | Opening 100 elements that all use `{l:Localize}`, `{l:Localize}` with two `<l:Argument>` children, or `{l:LocalizeEnum}` | The same views with `{x:Static}` of the designer class's properties |
| `AvaloniaScalingBenchmarks`, `WpfScalingBenchmarks`, Open | Opening a window with 100 or 10,000 `{l:Localize}` elements | The same views with `{x:Static}` |
| `AvaloniaScalingBenchmarks`, `WpfScalingBenchmarks`, Switch | Switching the culture while 100 or 10,000 `{l:Localize}` elements are live | The same views bound to a hand-written `TranslationSource` with a string indexer |

The **Ratio** column is a benchmark's time divided by its baseline's, so 1.20 means 20% slower than the code
without the library; **Allocated** is the memory one operation allocates, and **Alloc Ratio** compares it with
the baseline the same way.
