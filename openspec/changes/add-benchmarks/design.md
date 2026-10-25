# Design

## Context

See proposal.md for the motivation and specs/performance-benchmarks/spec.md for the requirements.

The code paths being measured:

```
 Code:  Localizer.Get(key, args, rm?)
          +-- rm given? --> rm.GetString(key, culture)
          |   no        --> loop over registered managers, first hit wins
          +-- MessageFormatter.Format(value, args, culture)   (runs even without arguments)
                default: string.Format | ICU: Dictionary + Jeffijoe FormatMessage

 XAML:  ProvideValue --> MultiBinding per element
          [0] RefreshTriggeringRevision of Localizer.Current   [1] key   [2..] Argument values
          LocalizedMessageConverter --> MessageArguments.FromTuples --> Localizer.Get
        Culture switch --> PropertyChanged --> every live binding calls Get again
```

Constraints from the repository:

- Projects under `tests/` inherit xUnit, coverlet and the test SDK from `tests/Directory.Build.props`, which a
  BenchmarkDotNet console app must not carry. Every project still has to pass the repository's analyzers and
  style gate (`scripts/tidy-code.ps1`), which the root `Directory.Build.props` imports.
- WPF builds only on Windows; `ResXLocalization.NonWindows.slnf` is what Linux builds use.
- Samples reference the source generator as an analyzer `ProjectReference`; the resx wiring comes from the
  root `Directory.Build.targets`.
- GitHub Pages already deploys the DocFX site through `actions/deploy-pages` on pushes to `main`, built with
  `--warningsAsErrors`, so a classic `gh-pages` branch would not be served.
- Every third-party action is pinned by commit SHA; Dependabot keeps them current.

## Goals / Non-Goals

**Goals:**
- Numbers that answer "will this make my app slow" and "how does it scale" with as little benchmark code as
  possible.
- A regression check that gives the same result every time it runs on the same code, even though the speed
  of GitHub's shared CI machines varies from run to run.
- Only existing tools: BenchmarkDotNet for measuring, `benchmark-action/github-action-benchmark` for history,
  comparison, alerts and charts.

**Non-Goals:**
- Native AOT benchmarks. The code these benchmarks measure does the same work under AOT; the existing AOT gate covers correctness.
  `MessageArguments.FromObject` uses reflection and would be the one to add if AOT numbers are ever wanted.
- .NET 8 benchmarks, multiple operating systems, rendering or layout cost, startup and first-lookup cost
  (satellite assembly loading), the source generator's build time.
- Comparisons with other localization libraries; they can be added later.
- Optimizing anything the benchmarks reveal; that is follow-up work.
- Running `main` and the pull request one after the other on the same machine to compare them.

## Decisions

### Three projects under a new `benchmarks/` folder, one per suite

```
 benchmarks/
   Directory.Build.props                 imports root props, NoWarn, compiles Shared/*.cs
   Shared/                               [Config], self-check helper (compiled into all three)
   ResXLocalization.Core.Benchmarks      Core, MessageFormat                     $(CurrentTargetFramework)
   ResXLocalization.Avalonia.Benchmarks  Avalonia headless                       $(CurrentTargetFramework)
   ResXLocalization.WPF.Benchmarks       WPF                                     $(CurrentWindowsTargetFramework)
```

The projects mirror the packages and the test suites. The Core suite answers "did the engine get slower" and
"what does one lookup cost"; the UI suites answer "what does a localized element cost and how does it scale".
Core needs its own suite because a lookup is a small share of a UI benchmark's time and allocations: a
lookup that doubles in cost moves a UI row by a few percent, below both CI thresholds.

- MessageFormat stays in the Core suite: it is one class, and it measures Core lookups with the ICU
  formatter installed.
- `benchmarks/Directory.Build.props` imports the root props, like `tests/` does, adds the `NoWarn` for
  `CS1574;CS1591` that samples and tests carry for generated keys, and compiles `benchmarks/Shared/*.cs` into
  every benchmark project, so the configuration and the self-check exist once.
- All three join `ResXLocalization.slnx` under `/04_benchmarks/`; Core and Avalonia also join the
  non-Windows filter, WPF does not, as in `src/`. They are built by the existing build jobs, so a broken
  benchmark fails the build, but they are not test projects and `dotnet test` ignores them.
- The UI suites have one row per extension form (typed key, `Argument`, enum), not every key type and
  argument form: those are the Core suite's.

Alternatives: Core with Avalonia and WPF separate (splits by "builds on Linux", which says nothing about what
a project measures); no Core suite (Core regressions disappear in the UI numbers, and the code-only features -
`MessageArguments`, search-all - are not reached through XAML); a fourth
project for MessageFormat (a project for one class); placing them under `tests/` (inherits the test stack).

### Resource data

- **Core**: `Resources/BenchmarkStrings.resx` with about 10 keys (a plain text, texts with positional and
  named arguments, one ICU message with two named arguments, the enum values), a `.de` satellite that translates
  them, and no `de-DE` satellite. Every benchmark looks up in `de-DE`, which is how most apps ship: a
  specific culture with a satellite for its language. The fallback walk `de-DE -> de` is `ResourceManager`'s
  and costs the baseline and the library the same; it matters for search-all, where every manager without
  the key is walked through the whole chain (`de-DE -> de -> neutral`) before the next one is tried, and
  that walk is the library's cost. Search-all benchmarks register 0, 4 or 19
  managers without the key before `BenchmarkStrings`, for 1, 5 or 20 managers in all. They come from one
  `Filler.resx` with a single other key: the benchmark creates 19 `ResourceManager` instances over its base
  name. The library tells managers apart by instance, and each instance has its own resource-set cache, so
  every miss is a real lookup with the full fallback walk, the same work as 19 different files.
- **Avalonia and WPF**: each its own `Resources/BenchmarkStrings.resx` with 10 keys, including an argument
  key and the enum keys, with a `.de` satellite. Each 10-element view uses all 10 keys once, so a view has
  different texts, as a real one does.

The key count does not change the numbers: `ResourceManager` keeps a loaded resource set in a dictionary, so
one lookup costs the same with 10 keys or 1,000. Loading the set is a one-time startup cost and is not
measured.

Every `BenchmarkStrings.resx` has a `BenchmarkStrings.Designer.cs` in the shape `PublicResXFileCodeGenerator`
emits: a public static property per key that calls `ResourceManager.GetString(name, resourceCulture)`. It is
the baseline (see "Baselines"). Typed keys come from the source generator, which handles a resx with a
designer, as in the samples.

All resx, designer and view files are small enough to write by hand and are committed, like the samples'
files. The samples hand-write their designer files because `PublicResXFileCodeGenerator` runs only inside
Visual Studio. The data is fixed, so nothing needs to regenerate it.

Alternatives: a script that generates large data sets (1,000 keys, 100-element views), which would run once
and then go out of date, and whose size does not change the results; a build-time generator, which would be
new tooling for a fixed data set.

### BenchmarkDotNet configuration

- One `[Config]` shared by all classes: `MemoryDiagnoser`, the JSON (full) and GitHub-markdown and HTML
  exporters, and one short job used locally and in CI:
  - **3 warmup and 3 measured iterations of 100 ms each**, instead of BenchmarkDotNet's 500 ms and its
    automatic choice of more iterations. Fewer, shorter iterations make the mean time vary more between
    runs, which is acceptable because time only raises an alert at 150%. Allocations, which fail the check,
    do not change with the number of iterations.
  - **The warmups stay**: .NET 10 allocates some objects on the stack, but only in fully optimized code. A
    benchmark measured before the JIT has optimized it would count allocations a running app does not have
    (spec: "Measurements run on optimized code").
  - **In-process toolchain** (`InProcessEmitToolchain`): every benchmark runs in the BenchmarkDotNet host
    process instead of a separately built and started process. That removes a build and a process start per
    benchmark; the cost is that benchmarks share one process and its GC state, which does not change these
    numbers noticeably. The host's `Main` is `[STAThread]` in the WPF project, so the WPF benchmarks run on
    an STA thread.
- **Results go to `artifacts/benchmarks/<project>/`**: the shared config sets BenchmarkDotNet's
  `ArtifactsPath` there, resolved from the repository root, instead of its default `BenchmarkDotNet.Artifacts`
  folder in the working directory. `artifacts/` is where the repository puts everything it generates on
  purpose: `.gitignore` already excludes it and `scripts/clean-build-artifacts.ps1` already deletes it, so
  neither needs a change.
- Each class marks its library-free variant `[Baseline = true]` (see "Baselines"), so the Ratio column is the
  overhead.
- `[Params]` for manager count (1, 5, 20) and element count (100, 10,000). Two element counts are enough to
  show whether the cost per element stays the same as N grows.
- Each class's `[GlobalSetup]` runs every benchmark method of the class once and throws when the text is not
  the expected one, which makes BenchmarkDotNet stop the class with an error (spec: "Benchmarks verify their
  own results").
- `Program.Main` is `BenchmarkSwitcher.FromAssembly(...).Run(args)`, so local runs can filter with
  `--filter`.

### Baselines

Every baseline is the code an application writes without the library; no other localization library is
benchmarked (a comparison can be added later as extra rows, outside the allocation gate).

| Benchmark | `[Baseline = true]` |
| --- | --- |
| Code lookup (typed, scoped, search-all, enum) | `BenchmarkStrings.Greeting` (designer property) |
| Code lookup with arguments | `string.Format(culture, BenchmarkStrings.PeopleInvited, count)` |
| ICU MessageFormat | `messageFormatter.FormatMessage(BenchmarkStrings.FilesDeleted, values, culture)` |
| Extension forms (100 elements) | the same 10-element views with `{x:Static}` |
| Opening a view with N elements | the same views with `{x:Static}` |
| Culture switch with N elements | the same views with `{Binding [Key], Source={x:Static local:TranslationSource.Instance}}` |

- **Culture**: the designer class reads `CultureInfo.CurrentUICulture` while its `Culture` property is unset,
  so each `[GlobalSetup]` sets `CurrentUICulture` and `Localizer.Current.CurrentCulture` to the same culture.
- **ICU**: the baseline uses one `new MessageFormatter()` with the library's defaults, which is what
  `UseMessageFormat()` creates, so pattern caching is the same on both sides. The ratio is the cost of
  `ResXLocalization.MessageFormat` itself: building the argument dictionary, the culture substitution and the
  call through the localizer. What ICU costs compared with `string.Format` is the MessageFormat library's
  property and is not measured. One message is enough: plural, select and the other ICU features run inside
  the MessageFormat library and cost the baseline the same. The adapter's overhead depends only on the
  number of arguments and whether they are named, so the message has two named arguments, which run both
  of the adapter's loops (positions and names).
- **`TranslationSource`**: the usual hand-written way to switch languages live, about 20 lines per UI
  framework, in each UI project. A singleton implementing `INotifyPropertyChanged` with
  `this[string key] => BenchmarkStrings.ResourceManager.GetString(key, this.Culture)` and a `Culture` setter
  that raises `PropertyChanged` for the indexer (`"Item[]"`). Elements bind `[key]` on it with the
  framework's normal binding. Its culture-switch row sets `TranslationSource.Instance.Culture`; the library's
  row sets `Localizer.Current.CurrentCulture`.

Alternatives: raw `ResourceManager.GetString` (the same cost as the designer property, but code consumers do
not recognize); Microsoft.Extensions.Localization in the Core suite (rarely used in XAML applications, so it
answers no question of this suite); WPFLocalizeExtension in the WPF suite (left for later: new releases of
a third-party project to keep up with, and no equivalent for Avalonia).

### UI benchmarks use compiled XAML views

Every UI row, baselines included, is a view written in XAML and compiled by the build, as in a consumer
app:

```
 Consumer app                          Benchmark
 +-----------------------------+       +---------------------------------------+
 | MainView.axaml / .xaml      |       | LocalizedView.axaml / .xaml           |
 |   <TextBlock Text=          |       |   <TextBlock Text=                    |
 |    "{l:Localize ...}"/>     |  ==   |    "{l:Localize ...}"/>               |
 | compiled by the build       |       | compiled by the build (same tooling)  |
 | new MainView() -> window    |       | new LocalizedView() -> window         |
 +-----------------------------+       +---------------------------------------+
```

- **Avalonia** compiles `.axaml` into IL, so `new LocalizedView()` runs the generated code that creates the
  extension, calls `ProvideValue` with the real XAML context and applies the binding.
- **WPF** compiles `.xaml` to BAML, and `InitializeComponent` reads it at run time with WPF's XAML reader and
  its service provider. Reading BAML is a cost consumers pay too; the baseline views pay it as well, so the
  ratio still shows what `{l:Localize}` adds.
- **Views per UI project**: five 10-element views, written by hand, each using one form for all its
  elements:

  ```
   {x:Static}                      baseline of both benchmarks
   {l:Localize}                    forms and scaling
   {l:Localize} + 2 <l:Argument>   forms only (arguments bound to the view's DataContext)
   {l:LocalizeEnum}                forms only
   TranslationSource               scaling only (baseline of the culture switch)
  ```

- **Forms**: one row per view, each opening 10 instances (100 elements) of it in a window. 100 elements
  spread the fixed cost of a view and a window, so a row's ratio to `{x:Static}` is what that form costs per
  element in a realistic view. A benchmark with a single element is not used: no app opens one, and its
  number would mostly be that fixed cost.
- **Scaling**: plain `{l:Localize}` only. The benchmark puts N / 10 instances of a 10-element view into a
  `StackPanel` that is a window's content: 10 or 1,000 views for N = 100 or 10,000, like a
  screen built from user controls. One form is enough because what can grow faster than the element count
  is shared by all forms: every element subscribes to `Localizer.Current` through its refresh-trigger
  binding, and a culture switch updates every subscribed binding. What `<l:Argument>` and
  `{l:LocalizeEnum}` add - more bindings, building the enum key - is per element and independent of the
  other elements, so it is N times the constant the forms benchmark measures. A view with mixed forms costs
  about the sum of its elements' per-form costs.
- **What is measured**: creating the view, making it the window's content, and ending when its texts are
  set. Rendering is not measured: it is the same for every row and is the UI framework's cost, not the
  library's.
- **Culture switch**: `[IterationSetup]` creates the N elements in a window; the benchmark switches the
  culture (`Localizer.Current.CurrentCulture`, or `TranslationSource.Instance.Culture` for its baseline),
  processes the dispatcher's pending work and checks the last element's text.
- **Avalonia** runs on the headless platform: `[GlobalSetup]` starts a `HeadlessUnitTestSession` once, every
  benchmark body runs through its dispatcher, and the window is a headless window. Headless replaces only
  the operating system window, input, compositor and message loop; compiled XAML, controls, the property
  system, bindings and the library are the same code as in a desktop app, and binding updates after
  `PropertyChanged` run on the UI thread without the dispatcher. The numbers therefore show the library's
  cost, but not a full frame of a real app.
- **WPF** has no headless mode and uses a real `Window`, shown with `ShowActivated = false` outside the
  visible screen area. The benchmarks are marked `[STAThread]` so BenchmarkDotNet runs them on an STA
  thread, and pending dispatcher work is processed with a `DispatcherFrame`.

Alternatives: a scaling run per form (mostly parallel lines, three times the slowest part of the suite,
and the arguments form would need a library-free baseline for live switching with arguments); building the
elements in code and calling `ProvideValue` directly (not what a consumer app
runs, and WPF would need a hand-written service provider); parsing XAML at run time
(`AvaloniaRuntimeXamlLoader`, `XamlReader.Parse`), which no consumer app does; a real Avalonia desktop
window (its timing includes the operating system and rendering, which vary between runs and are not the
library's cost).

### Regression detection with github-action-benchmark, two series

The action reads one BenchmarkDotNet JSON file (`tool: benchmarkdotnet`) and compares only the mean time.
Each project writes its own report (`--join` merges the classes within a project), so one script merges the
three and derives the allocation series; the job then feeds the action twice:

```
 Core / Avalonia / WPF  *-report-full-compressed.json  (one per project)
   |
   v
 scripts/convert-benchmark-results.ps1
   +--> merged.json      (the three Benchmarks arrays, the first report's host info)
   |      --> github-action-benchmark  tool: benchmarkdotnet          "Time"
   |            alert-threshold 150%, fail-on-alert false, summary-always true
   |
   +--> allocations.json (name + AllocatedBytes per benchmark, customSmallerIsBetter format)
          --> github-action-benchmark  tool: customSmallerIsBetter    "Allocations"
                alert-threshold 125%, summary-always true
                fail-on-alert: true, false while the PR has label accept-allocation-increase
```

Benchmark names include their class's namespace, which differs per project, so names from the three reports
cannot collide.

- **Pull request**: `auto-push: false`; the action compares against the latest entry on the data branch
  and writes the result to the job summary. `comment-on-alert` is on only when the pull request comes from
  this repository, because a fork's token cannot comment.
- **Push to `main`**: `auto-push: true` appends the entry to the `benchmark-data` branch.
- An allocation baseline of 0 bytes: the action computes the ratio against 0; the conversion script writes
  allocated bytes plus 1 so a 0 -> N change becomes an alert instead of a division by zero (spec: "a
  benchmark that allocated nothing on `main` SHALL fail on any allocation").

Alternatives: a hand-written comparer (new tool to maintain); failing on slower times (the
speed of shared CI machines varies, so the check would fail at random); BenchmarkDotNet's own
`--statisticalTest` against a baseline run (needs both versions run on the same machine, which is left out).

### Accepting a deliberate allocation increase with a label

The "Allocations" step sets
`fail-on-alert: ${{ !contains(github.event.pull_request.labels.*.name, 'accept-allocation-increase') }}`, so
a labeled pull request still gets the alert in the job summary (and the comment) but passes. The workflow's
`pull_request` trigger lists `types: [opened, synchronize, reopened, labeled, unlabeled]`, so adding or
removing the label re-runs the check. Nothing else is needed: the merged push to `main` records the higher
values, which become the baseline.

- Only people with triage access or more can label a pull request, so a contributor cannot accept their own
  increase; a maintainer does it after reading the alert.
- `labeled` and `unlabeled` re-run the whole CI workflow, not only the benchmark job; the existing
  `concurrency` group cancels the run in progress. Accepted: labeling is rare.

Alternatives: a separate workflow on `labeled` only (a second place that runs the benchmarks); a commit
message marker (contributors could set it themselves); a hand-edited baseline file (has to be maintained).

### Three parallel benchmark jobs and one report job, on windows-latest

```
 benchmark-changes --> benchmarks (matrix: Core | Avalonia | WPF) --> benchmark-report
   run? yes/no           each: restore, build, run, upload JSON        merge, two action steps,
                                                                       latest/ on main
```

- **`benchmark-changes`**: a small job that compares the changed files with the pull request's base (or,
  on a push, with the previous commit of `main`) using `git diff --name-only`, and outputs whether
  anything changed under `src/`, `benchmarks/`, `build/`, the root `Directory.*` files, `global.json`,
  `.github/workflows/ci.yml` or `scripts/convert-benchmark-results.ps1`. The other two jobs run only when it
  did (spec: "Change without code is not benchmarked"). A skipped job counts as passed for a required check,
  so documentation pull requests are not blocked.
- **`benchmarks`**: a three-entry matrix, one suite per entry, all on `windows-latest`. Each builds and runs
  its own project and uploads its BenchmarkDotNet artifacts. The wall-clock time is the slowest suite's,
  expected to be WPF. One runner type keeps one comparable history. `timeout-minutes: 15`, so a hanging
  benchmark cannot hold up CI for long.
- **`benchmark-report`**: needs the matrix, downloads the three JSON reports, runs
  `scripts/convert-benchmark-results.ps1` and the two `github-action-benchmark` steps, and on `main` writes
  `latest/` to the data branch. Only this job has `contents: write` (push to the data branch) and
  `pull-requests: write` (comments); the workflow default stays `contents: read`.

All of this runs in parallel to the build jobs, so it lengthens CI only if it takes longer than the slowest
of them.

Alternatives: one job running the three suites one after the other (the wall-clock time is their sum); a
third-party action to detect changed paths (`git diff` does the same without a new dependency).

### Publishing through the data branch

The `benchmark-data` branch holds the action's history (`history/data.js` and its chart `index.html`) and,
written by the job on `main` in the same push, the latest BenchmarkDotNet HTML and markdown reports under
`latest/`. `deploy-docs` builds the Pages content from two sources:

```
 package-and-docs  --(artifact: docs-site)--+
                                            +--> deploy-docs: site + benchmarks/ --> Pages
 benchmark-data branch (latest/, history/) -+
```

- `package-and-docs` uploads the DocFX site as an ordinary artifact instead of the Pages artifact.
- `deploy-docs` (main only) downloads it, checks out `benchmark-data` into `benchmarks/` inside the site,
  then runs `upload-pages-artifact` and `deploy-pages`. It needs `package-and-docs` and `benchmark-report`
  with `if: always() && needs.package-and-docs.result == 'success'`, so a failed or skipped benchmark run
  leaves the last successful results on the site (spec: "Benchmark run fails on main").
- A short page, `docs/guides/benchmarks.md`: a table of each benchmark and its baseline, one sentence on
  the Ratio and Allocated columns, and links to
  `https://rent-a-developer.github.io/ResXLocalization/benchmarks/latest/` and `.../benchmarks/history/` by
  absolute URL, because those files are not DocFX content and relative links would fail
  `--warningsAsErrors`. It joins `docs/toc.yml` under Guides. No worked examples: the results answer whether
  the library is fast enough by themselves.

Alternative: commit result markdown into `docs/` from CI (a bot commit on `main` per push).

## Risks / Trade-offs

- [The times measured on shared CI machines vary by 10-30% between runs] -> a slower time only raises an
  alert, at 150%; only more allocations fail the check.
- [Allocation numbers of UI benchmarks include small allocations of the UI framework, such as dispatcher
  work] -> the 25% tolerance, which also leaves room for a feature that adds a small allocation; if a UI
  benchmark still fails and passes at random, move it to a third series that alerts without failing.
- [WPF under BenchmarkDotNet's in-process toolchain: STA thread and dispatcher] -> the first task is a
  feasibility check that runs one WPF benchmark end to end before the rest is written.
- [Avalonia headless session under the in-process toolchain] -> the same feasibility check for Avalonia.
- [The two frameworks may expect different change notifications for an indexer binding (`"Item[]"` or
  `"Item"`), and Avalonia's compiled bindings must accept an indexer on a `Source={x:Static ...}` source] ->
  the feasibility check also switches the culture once on a `TranslationSource` binding in each framework,
  and the self-check catches a baseline that does not update.
- [The first run on `main` has no history] -> the action does not alert without a previous entry; the first
  pull request after merge compares against it.
- [The benchmarks could still make CI slower than the 10-minute target] -> the estimate is about 2 minutes
  of restore and build plus 1-4 minutes of measuring per suite. The feasibility check measures the real time
  of one suite; if a suite takes over 10 minutes, the suite is cut further before the rest is written. The
  first full CI run confirms it.
- [The 10,000-element scaling benchmarks are slow, mostly in WPF] -> those classes run one invocation per
  iteration (`InvocationCount = 1`, needed anyway for `[IterationSetup]`).
- [Short iterations make the times vary more between runs] -> accepted: times only alert, at 150%, and
  allocations are not affected.
- [A pull request that changes a benchmark's name loses its history] -> accepted; the renamed benchmark is
  treated as new and does not fail.
- [The data branch grows with each push to `main`] -> the action keeps at most `max-items-in-chart` entries
  (set to 200).

## Migration Plan

1. Merge the projects, the job and the docs page.
2. The first push to `main` creates the `benchmark-data` branch and the first entry.
3. Pull requests compare against it from then on.

Rollback: remove the job and restore the Pages artifact upload in `package-and-docs`; the branch can stay.
