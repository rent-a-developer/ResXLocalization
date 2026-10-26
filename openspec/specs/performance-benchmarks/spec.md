# performance-benchmarks Specification

## Purpose

Measures what ResXLocalization costs an application and detects performance regressions in the library, so
maintainers see a slower change before it ships and consumers can judge the overhead before they adopt it.

## Requirements

### Requirement: Baselines are the code an application writes without the library
Every benchmark SHALL be reported as a ratio to a baseline that does the same work without ResXLocalization:
the ResX designer class's property, hand-written code over it, or a direct call of the MessageFormat library.
No other localization library SHALL be benchmarked.

#### Scenario: Every result has a baseline
- **WHEN** the suite runs
- **THEN** every result table contains one baseline row that uses no ResXLocalization API, and every other
  row shows its ratio to that row

#### Scenario: Baseline and library produce the same text
- **WHEN** a benchmark is checked before it is measured
- **THEN** its baseline and its ResXLocalization rows produce the same text for the same key, culture and
  arguments

### Requirement: Code lookups are benchmarked against the ResX designer class
The suite SHALL measure `Localizer.Current.Get` for a typed key, a scoped key, a search-all key and an enum
value, each with mean time and allocated bytes, against the designer class's property for the same key, read
in the same culture.

#### Scenario: Typed key lookup reports its overhead
- **WHEN** the code lookup benchmarks run
- **THEN** the results contain a designer-property baseline row and a typed-key `Get` row with the same key
  and culture, and the `Get` row shows its ratio to the baseline and its allocated bytes

### Requirement: Search-all lookups are benchmarked by the number of registered resource managers
The suite SHALL measure a search-all lookup whose key exists only in the last registered resource manager,
for 1, 5 and 20 registered resource managers, against hand-written code that searches the same resource
managers, in the same order and culture, until one contains the key.

#### Scenario: Lookup cost grows with the registered managers
- **WHEN** the search-all benchmark runs
- **THEN** the results contain one row per manager count (1, 5 and 20), each finding the key in the last
  registered manager

#### Scenario: The baseline searches the same managers
- **WHEN** the search-all benchmark runs with a manager count
- **THEN** its baseline row searches the same resource manager instances in the same order and culture, so
  the ratio shows only what ResXLocalization adds to the search

#### Scenario: Managers without the key are searched in the lookup culture
- **WHEN** the search-all benchmark looks up a key in a specific culture, such as `de-DE`
- **THEN** each registered manager that does not contain the key is searched in that culture, including its
  culture fallback chain, before the next manager is tried, as in an application

### Requirement: Format arguments in code are benchmarked
The suite SHALL measure `Get` with positional arguments (`MessageArguments.FromValues`), named arguments
(`MessageArguments.FromTuples`) and object arguments (`MessageArguments.FromObject`), against
`string.Format` over the designer class's property with the same culture and arguments.

#### Scenario: Each argument form reports its overhead
- **WHEN** the argument benchmarks run
- **THEN** the results contain one row per argument form and a `string.Format` baseline row with the same
  pattern, culture and argument values

### Requirement: ICU MessageFormat is benchmarked against the MessageFormat library
The suite SHALL measure one lookup formatted by the ICU MessageFormat formatter, for a message with two
named arguments, against formatting the designer class's property directly with a formatter of the
MessageFormat library that has the library's default settings, with the same culture and argument values.
The overhead does not depend on which ICU features a message uses, so no other message is measured.

#### Scenario: One message with named arguments is measured
- **WHEN** the MessageFormat benchmarks run
- **THEN** the results contain a direct MessageFormat library baseline row and one ResXLocalization row for
  the same message with two named arguments, and both produce the same text

#### Scenario: The overhead of the adapter is visible
- **WHEN** the MessageFormat benchmarks run
- **THEN** each ResXLocalization row shows its ratio to the direct MessageFormat library call for the same
  message

### Requirement: UI benchmarks use XAML compiled by the build, as a consumer app does
Every UI benchmark, its baselines included, SHALL write the markup extensions, bindings and `{x:Static}` in
XAML views that the project's build compiles with the UI framework's own tooling, SHALL create those views
as an application does, and SHALL measure until the view is the content of a window and its texts are set.
Rendering SHALL NOT be part of the measurement.

#### Scenario: A benchmark row is a compiled view
- **WHEN** a UI benchmark runs
- **THEN** the measured operation creates a view defined in a `.axaml` or `.xaml` file of the benchmark
  project, makes it the content of a window, and ends when the view's texts are set

#### Scenario: XAML is not parsed at runtime
- **WHEN** a UI benchmark creates a view
- **THEN** the view's markup was compiled by the build, and no XAML is parsed from a string or file at run
  time

### Requirement: Avalonia benchmarks run on the headless platform
The Avalonia suite SHALL run on Avalonia's headless platform, which keeps Avalonia's controls, XAML,
property system and bindings and replaces only the operating system window, input and rendering.

#### Scenario: Avalonia suite runs without a desktop
- **WHEN** the Avalonia suite runs on a CI machine
- **THEN** it opens no operating system window and needs no display

### Requirement: Each extension form is benchmarked for Avalonia and WPF
For each UI framework, the suite SHALL measure opening 100 text elements that all use one form -
`LocalizeExtension` with a typed key, `LocalizeExtension` with two `Argument` children, or
`LocalizeEnumExtension` - from 10-element views used 10 times in one window, against the same layout with
`{x:Static}` of the designer class's property.

#### Scenario: Each form reports its cost per element
- **WHEN** the forms benchmark runs for a UI framework
- **THEN** the results contain a `{x:Static}` baseline row and one row per form, each for 100 elements, and
  every element shows its expected localized text

### Requirement: Scaling with many localized elements is benchmarked
For each UI framework, the suite SHALL measure opening a view with N localized text elements, and switching
the culture while N localized text elements are live, for N = 100 and 10,000, reporting time and
allocated bytes. The N elements SHALL use `LocalizeExtension` with a typed key and SHALL come from a
compiled view with 10 text elements, used N / 10 times in one window.

#### Scenario: Culture switch updates every element
- **WHEN** the culture-switch benchmark runs with N live elements
- **THEN** the measurement ends only after all N elements show the text of the new culture

#### Scenario: Results are reported per element count
- **WHEN** the scaling benchmarks run
- **THEN** the results contain one row per N for opening a view and one row per N for a culture switch

### Requirement: Scaling benchmarks have library-free baselines
Opening a view SHALL be measured against the same views with `{x:Static}`, and a culture switch against the
same views with every element bound in XAML to a hand-written localization source: a singleton with a string indexer over the designer class's
resource manager that raises a change notification for its indexer when its culture changes.

#### Scenario: Opening a view is compared with static texts
- **WHEN** the open-view benchmark runs with N elements
- **THEN** the results contain a `{x:Static}` baseline row and a `LocalizeExtension` row for the same N

#### Scenario: Culture switch is compared with a hand-written source
- **WHEN** the culture-switch benchmark runs with N live elements
- **THEN** the results contain a hand-written-source baseline row and a `LocalizeExtension` row for the same
  N, and both end with all N elements showing the text of the new culture

### Requirement: Benchmarks verify their own results
Every benchmark SHALL check, once before it is measured, that its operation produces the expected text, and
SHALL stop the run with an error when it does not.

#### Scenario: A broken lookup does not produce a number
- **WHEN** a benchmarked lookup returns the lookup failure text instead of the expected text
- **THEN** the benchmark run fails with an error that names the benchmark, and no result is reported for it

### Requirement: CI runs the suite on pull requests and on main
CI SHALL run the full suite on every pull request to `main` and on every push to `main` that changes code,
build configuration or the benchmarks, on one fixed runner type and .NET 10 on the JIT. From the start of
the benchmark jobs to the published comparison, the run SHALL take under 10 minutes of wall-clock time.

#### Scenario: Pull request runs the suite
- **WHEN** a pull request to `main` that changes code is opened or updated
- **THEN** CI runs every benchmark, attaches the raw results to the run, and the benchmark comparison is
  reported within 10 minutes of the benchmark jobs starting

#### Scenario: Change without code is not benchmarked
- **WHEN** a pull request or a push to `main` changes only files outside the library sources, the benchmarks,
  the build configuration and the CI workflow, such as documentation
- **THEN** the benchmarks do not run, and the benchmark check does not block the pull request

### Requirement: Measurements run on optimized code
Every benchmark SHALL be measured only after warmup iterations that let the JIT compile its code fully
optimized, so allocations that optimized code avoids, such as objects the JIT allocates on the stack, are
not counted.

#### Scenario: Allocations match an application's
- **WHEN** a benchmark's allocations are measured
- **THEN** they are measured after its warmup iterations, on the same optimized code a long-running
  application runs

### Requirement: CI fails a pull request whose benchmarks allocate more
CI SHALL fail a pull request when any benchmark allocates more than 125% of the bytes per operation of the
same benchmark in the latest result from `main`. A benchmark that allocated nothing on `main` SHALL fail on
any allocation. A benchmark with no result on `main` SHALL NOT fail the check.

#### Scenario: Extra allocation fails the check
- **WHEN** a pull request makes a benchmark allocate more than 125% of its bytes per operation on `main`
- **THEN** the benchmark check fails and names the benchmark with both allocation values

#### Scenario: Small growth does not fail the check
- **WHEN** a benchmark's bytes per operation grow by at most 25% over `main`, such as a new feature adding a small allocation
- **THEN** the check does not fail because of that benchmark

#### Scenario: New benchmark does not fail the check
- **WHEN** a pull request adds a benchmark that has no result on `main`
- **THEN** the check does not fail because of that benchmark

### Requirement: A label accepts an allocation increase for one pull request
While a pull request carries the label `accept-allocation-increase`, CI SHALL report allocation increases
above the threshold as alerts, as it reports time regressions, instead of failing. Adding or removing the
label SHALL re-run the benchmark check. The label SHALL NOT change the check of any other pull request.

#### Scenario: Labeled pull request passes with an alert
- **WHEN** a pull request with the label `accept-allocation-increase` makes a benchmark allocate more than
  125% of its bytes per operation on `main`
- **THEN** the benchmark check passes, and the job summary lists the benchmark with both allocation values

#### Scenario: Label is removed
- **WHEN** the label is removed from a pull request whose benchmarks allocate more than the threshold
- **THEN** the benchmark check runs again and fails

#### Scenario: Accepted increase becomes the new baseline
- **WHEN** a labeled pull request is merged and its push to `main` records new results
- **THEN** later pull requests compare against the increased allocation values

### Requirement: CI reports time regressions without failing
CI SHALL report every benchmark whose mean time exceeds 150% of the latest result from `main`, in the run's
job summary and, where the run may comment, on the pull request. A time regression alone SHALL NOT fail the
check.

#### Scenario: Slower benchmark is reported
- **WHEN** a pull request makes a benchmark take more than 150% of its time on `main`
- **THEN** the job summary lists the benchmark with both times, and the check still passes if no allocation
  increased

#### Scenario: Pull request from a fork
- **WHEN** the pull request comes from a fork and the run cannot comment
- **THEN** the comparison appears in the job summary and the run does not fail because it cannot comment

### Requirement: Benchmark results are published on the documentation site
Each push to `main` SHALL publish, on the GitHub Pages documentation site, that commit's results as tables
with the runner's hardware and runtime details, and a history chart per benchmark over the commits on
`main`. A failed benchmark run SHALL NOT block the documentation deployment.

#### Scenario: Consumer reads the latest results
- **WHEN** a push to `main` finishes and the documentation site is deployed
- **THEN** the site has that commit's result tables, with the hardware and runtime they were measured on, and
  the history chart, both reachable from the documentation's table of contents

#### Scenario: Benchmark run fails on main
- **WHEN** the benchmark job fails on a push to `main`
- **THEN** the documentation site is still deployed, with the benchmark results of the last successful run

### Requirement: The documentation links the results
The documentation SHALL have one short page that links the latest results and the history chart and says,
for each benchmark, what it is compared with, and that the Ratio column is the time relative to that
baseline.

#### Scenario: Reader finds the results
- **WHEN** a developer opens the benchmarks page from the documentation's table of contents
- **THEN** it links the latest results and the history chart and names each benchmark's baseline
