# Proposal

## Why

ResXLocalization has no performance measurements. Nothing catches a change that makes lookups slower or
allocate more, and consumers cannot see what the library costs them: whether it makes their app slow, and how
it behaves with thousands of localized UI elements and a live culture switch. A small benchmark suite in CI
answers both questions without turning into a performance study.

## What Changes

- A BenchmarkDotNet suite under a new `benchmarks/` folder that measures the library's main features against
  the code a consumer would write without it - the ResX designer class and hand-written code over it:
  - Code lookups through `Localizer.Current.Get` (typed key, scoped key, search-all across several registered
    resource managers, enum), with and without `MessageArguments`, against the designer class's property and
    `string.Format` over it.
  - ICU MessageFormat through `ResXLocalization.MessageFormat` (one message with named arguments), against calling the
    MessageFormat library directly.
  - XAML lookups for Avalonia (headless) and WPF: the cost per element of each extension form
    (`LocalizeExtension`, with `Argument` children, `LocalizeEnumExtension`), measured on 100 elements,
    against `{x:Static}` of the designer class. Every UI benchmark is a XAML view compiled by the build and
    shown in a window, as in a consumer app.
  - Scaling with plain `LocalizeExtension`: opening a view with N localized elements, and switching the
    culture with N live bindings, for N = 100 and 10,000, against `{x:Static}` and a hand-written
    localization source with an indexer.
- CI jobs that run the suite, in under 10 minutes and in parallel with the existing build jobs, on every pull
  request and every push to `main` that changes code, fail a pull request when
  a benchmark allocates more than 125% of its bytes on `main`, and reports a time regression above a
  threshold without failing. A maintainer can accept a deliberate allocation increase for one pull request
  with the label `accept-allocation-increase`, which turns that failure into an alert.
- A published benchmark report on the existing GitHub Pages documentation site: the latest results with
  runner details, and the history of every benchmark over the commits on `main`.
- A documentation page that explains what the numbers mean for an application.
- No other localization library is benchmarked; a comparison with one can be added later.
- Benchmarks run on the JIT on .NET 10 only; Native AOT is not benchmarked.

No shipped package, public API or runtime behavior changes.

## Capabilities

### New Capabilities

- `performance-benchmarks`: the repository's benchmark suite, what it measures and against which baselines,
  how CI detects performance regressions with it, and how its results are published.

### Modified Capabilities

None. No spec exists yet, and no shipped behavior changes.

## Impact

- New projects, one per suite: `benchmarks/ResXLocalization.Core.Benchmarks` (Core and MessageFormat),
  `benchmarks/ResXLocalization.Avalonia.Benchmarks` and
  `benchmarks/ResXLocalization.WPF.Benchmarks` (Windows only), plus a `benchmarks/Directory.Build.props` and
  the code the three share.
- `ResXLocalization.slnx` and `ResXLocalization.NonWindows.slnf` gain the benchmark projects.
- `Directory.Packages.props` gains `BenchmarkDotNet`, referenced only by the benchmark projects, so no
  package's dependencies change.
- `.github/workflows/ci.yml` gains a benchmark job, and the documentation deployment includes its results.
  Results are stored in two places:
  - **CI artifacts** of every run, pull requests and `main`: the raw BenchmarkDotNet reports (JSON, HTML,
    markdown, logs), for inspecting that one run.
  - **The `benchmark-data` branch**, written only by pushes to `main`, one commit per push: `history/data.js`
    with the time and allocation results of the last 200 commits on `main` (the baseline pull requests
    compare against, and the data of the history chart), the chart page `history/index.html`, and the HTML
    and markdown reports of the latest run under `latest/`. The branch holds no code and is never merged;
    the documentation deployment copies it into the site. Writing it needs `contents: write` for the
    benchmark job.
- `docs/` gains a benchmarks page and a table-of-contents entry.
- The third-party GitHub Action `benchmark-action/github-action-benchmark` becomes a CI dependency, pinned by
  commit SHA like the existing actions.
