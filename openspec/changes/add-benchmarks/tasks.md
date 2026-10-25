# Tasks

## 1. Project setup and feasibility check

- [ ] 1.1 Add `BenchmarkDotNet` to `Directory.Packages.props` (current stable version) and verify `dotnet restore ResXLocalization.slnx` succeeds
- [ ] 1.2 Create `benchmarks/Directory.Build.props` (imports the root props, adds `NoWarn` `CS1574;CS1591` with the same comment as the samples, and compiles `benchmarks/Shared/*.cs` into every benchmark project) and the three console projects `benchmarks/ResXLocalization.Core.Benchmarks` (`$(CurrentTargetFramework)`, Core and MessageFormat), `benchmarks/ResXLocalization.Avalonia.Benchmarks` (`$(CurrentTargetFramework)`, Avalonia) and `benchmarks/ResXLocalization.WPF.Benchmarks` (`$(CurrentWindowsTargetFramework)`, `UseWPF`), each with the library, Core and generator `ProjectReference`s the samples use and a `BenchmarkSwitcher` `Program`; verify all three build in Release
- [ ] 1.3 Add the three projects to `ResXLocalization.slnx` under `/04_benchmarks/`, and Core and Avalonia (not WPF) to `ResXLocalization.NonWindows.slnf`; verify `dotnet build ResXLocalization.NonWindows.slnf -c Release` and `dotnet test ResXLocalization.slnx -c Release` still pass and `dotnet test` runs no benchmark
- [ ] 1.4 Feasibility check: one Avalonia headless benchmark and one WPF `[STAThread]` benchmark that create a compiled XAML view with a `{l:Localize}` `TextBlock`, make it a window's content (headless window in Avalonia, a real `Window` outside the visible screen area in WPF) and check its text, and switch the culture once on a view whose `TextBlock` binds `[key]` on a minimal `TranslationSource` with `Source={x:Static ...}` (in Avalonia with compiled bindings), in their two projects; verify both run end to end with `dotnet run -c Release -- --filter *Feasibility* --inProcess --warmupCount 3 --iterationCount 3 --iterationTime 100` from each project and report time and allocations, record each project's total run time and confirm it leaves a suite of the planned size well under 10 minutes (otherwise cut the suite in the design before going on), then delete the feasibility check classes

## 2. Resource data and shared configuration

- [ ] 2.1 Write by hand, in each of the three projects, `Resources/BenchmarkStrings.resx`, `BenchmarkStrings.de.resx` (every key) and a `BenchmarkStrings.Designer.cs` in the shape `PublicResXFileCodeGenerator` emits, with the same project-file wiring as the samples: in Core about 10 keys (plain text, positional and named arguments, one ICU message with two named arguments, enum values) plus one `Filler.resx` with a single other key; in Avalonia and WPF 10 keys including an argument key and the enum keys; verify the generator emits `BenchmarkStringsKeys`, the designer properties compile and the satellites build
- [ ] 2.2 Add the shared `[Config]` to `benchmarks/Shared/` (MemoryDiagnoser; JSON full, GitHub markdown and HTML exporters; one job with `InProcessEmitToolchain`, 3 warmup and 3 measured iterations of 100 ms; `ArtifactsPath` set to `artifacts/benchmarks/<project>/` from the repository root) and `[STAThread]` on the WPF project's `Main`, plus the culture setup that sets `CultureInfo.CurrentUICulture` and `Localizer.Current.CurrentCulture` to the same culture; verify a run of a class in each project writes all three report files under `artifacts/benchmarks/<project>/results` and no `BenchmarkDotNet.Artifacts` folder, runs in-process (no generated project is built) and reports 3 warmup and 3 measured iterations
- [ ] 2.3 Add the self-check helper to `benchmarks/Shared/` that `[GlobalSetup]` calls to run each benchmark method once and throw on an unexpected text; verify by temporarily breaking one key that the class fails with an error naming the benchmark and produces no result row

## 3. Core and MessageFormat benchmarks

- [ ] 3.1 `CodeLookupBenchmarks`: designer property baseline (`BenchmarkStrings.<Key>`), typed key, scoped key, search-all key and enum `Get`, in `de-DE`; verify with `--filter *CodeLookup*` that every row has a Ratio and Allocated value
- [ ] 3.2 `SearchAllBenchmarks` with `[Params(1, 5, 20)]` registered managers: 0, 4 or 19 separate `ResourceManager` instances over `Filler.resx`, then `BenchmarkStrings`, with the key only in `BenchmarkStrings`; verify three rows with rising time
- [ ] 3.3 `ArgumentBenchmarks`: `string.Format(culture, BenchmarkStrings.<Key>, ...)` baseline, `FromValues`, `FromTuples`, `FromObject`; verify four rows and that the self-check passes for each
- [ ] 3.4 `MessageFormatBenchmarks`: one ICU message with two named arguments (a plural with `{Count}` and `{Name}`); a baseline that formats the designer property with one `new MessageFormatter()` and the same values and culture, against `Get` with `MessageArguments.FromTuples` and `UseMessageFormat()` installed; verify two rows and that the self-check confirms both produce the same text

## 4. Avalonia benchmarks

- [ ] 4.1 Headless session helper in the Avalonia project, started once in each class's `[GlobalSetup]` and running each body on its dispatcher; verify two classes in one run share the session without errors
- [ ] 4.2 Four 10-element `.axaml` views, each using one form for all elements and the 10 keys once (`{x:Static res:BenchmarkStrings.<Key>}`, `{l:Localize}` typed key, `{l:Localize}` with two `<l:Argument>` children bound to the view's `DataContext`, `{l:LocalizeEnum}`), and `AvaloniaFormsBenchmarks`, one row per view (the `{x:Static}` view is the baseline), each opening 10 instances (100 elements) in a `StackPanel` as the headless window's content; verify four rows and that every element's self-check sees its expected text
- [ ] 4.3 Avalonia `TranslationSource`: singleton with a string indexer over `BenchmarkStrings.ResourceManager` and a `Culture` setter that raises the indexer's change notification; verify a compiled-XAML `TextBlock` bound to `[key]` on it updates after a culture change
- [ ] 4.4 Write the 10-element `TranslationSource` `.axaml` view (the 10 keys once) and add `AvaloniaScalingBenchmarks`, reusing the `{l:Localize}` and `{x:Static}` views of 4.2, with `[Params(100, 10000)]`: open a view (N / 10 views in a `StackPanel` as the window's content; baseline the `{x:Static}` views) and culture switch (the views built in `[IterationSetup]`, `InvocationCount = 1`, done when the last element shows the new culture's text; baseline the `TranslationSource` views); verify each operation has a baseline and a library row per N, and the run time of the class

## 5. WPF benchmarks

- [ ] 5.1 WPF helpers: a `Window` shown with `ShowActivated = false` outside the visible screen area that hosts the measured view, and a dispatcher flush with `DispatcherFrame`; verify a compiled-XAML view with a `{l:Localize}` `TextBlock` shows its text after one flush
- [ ] 5.2 WPF `TranslationSource`, mirroring 4.3; verify the same way
- [ ] 5.3 The four 10-element `.xaml` views and `WpfFormsBenchmarks`, mirroring 4.2, all `[STAThread]`; verify the same rows as Avalonia and that the self-checks pass
- [ ] 5.4 The `TranslationSource` `.xaml` view and `WpfScalingBenchmarks`, mirroring 4.4; verify the same rows as Avalonia, and that the whole WPF project runs locally in under 6 minutes
- [ ] 5.5 Run the `ui_parity_reviewer` over the Avalonia and WPF benchmark classes and resolve any gap it reports

## 6. CI integration

- [ ] 6.1 Add `scripts/convert-benchmark-results.ps1` that reads the three projects' BenchmarkDotNet full JSON reports, writes `merged.json` (their `Benchmarks` arrays, the first report's host info) and `allocations.json` (`customSmallerIsBetter`, allocated bytes + 1 per benchmark); verify on local results that every benchmark of all three projects appears once in each file, with a number
- [ ] 6.2 Add the `benchmark-changes` job to `.github/workflows/ci.yml`: `git diff --name-only` against the pull request's base or the push's previous commit, with the output `run` true when anything changed under `src/`, `benchmarks/`, `build/`, the root `Directory.*` files, `global.json`, `.github/workflows/ci.yml` or `scripts/convert-benchmark-results.ps1`; verify on a documentation-only test pull request that `run` is false and the benchmark jobs are skipped, and on a code change that it is true
- [ ] 6.3 Add the `benchmarks` matrix job (Core, Avalonia, WPF) on `windows-latest`, running only when `benchmark-changes` says so: setup, restore, build and run its project with `--join`, upload its `artifacts/benchmarks/<project>/` folder, `timeout-minutes: 15`; verify with `pwsh -File scripts/tidy-code.ps1 -Scope all -Check` and `scripts/verify-line-endings.ps1`
- [ ] 6.4 Add the `benchmark-report` job that needs the matrix: download the three reports, merge with `scripts/convert-benchmark-results.ps1`, run `benchmark-action/github-action-benchmark` pinned by SHA twice (`benchmarkdotnet` "Time" at 150% without failing, `customSmallerIsBetter` "Allocations" at 125% failing, `summary-always`, `comment-on-alert` only for same-repository pull requests, `auto-push` only on `main`, branch `benchmark-data`, `max-items-in-chart: 200`), with job-level `contents: write` and `pull-requests: write`; verify with the lint job
- [ ] 6.5 Make the "Allocations" step's `fail-on-alert` false while the pull request has the label `accept-allocation-increase`, and add `labeled` and `unlabeled` to the `pull_request` trigger's `types` (with `opened`, `synchronize`, `reopened`); verify with the lint job, and on a test pull request that adding and removing the label re-runs the check and turns a failing allocation alert into a passing one and back
- [ ] 6.6 Create the `accept-allocation-increase` label in the repository (maintainer step, with a description saying it accepts a reviewed allocation increase for one pull request); verify it appears in the repository's labels
- [ ] 6.7 On `main`, have `benchmark-report` copy the HTML and markdown reports into `latest/` on the `benchmark-data` branch; verify on a pull-request run that the comparison is reported within 10 minutes of the benchmark jobs starting and its summary shows both series

## 7. Publishing and documentation

- [ ] 7.1 Change `package-and-docs` to upload the DocFX site as an ordinary artifact, and `deploy-docs` to need `package-and-docs` and `benchmark-report` with `if: always() && needs.package-and-docs.result == 'success'`, download the site, check out `benchmark-data` into `benchmarks/` inside it, then upload and deploy the Pages artifact; verify the workflow passes the lint job
- [ ] 7.2 Write a short `docs/guides/benchmarks.md` (a table of each benchmark and its baseline, one sentence on the Ratio and Allocated columns, absolute links to `benchmarks/latest/` and `benchmarks/history/`) and add it to `docs/toc.yml` under Guides; verify `dotnet tool run docfx build build/docfx/docfx.json --warningsAsErrors` passes
- [ ] 7.3 Add a "Running the benchmarks" section to `CONTRIBUTING.md` (local command, `--filter`, how CI gates allocations and alerts on time, and when a maintainer applies `accept-allocation-increase`) and the `benchmarks/` row to the project map in `AGENTS.md`; verify the documented command runs as written
- [ ] 7.4 Add an `[Unreleased]` entry in `CHANGELOG.md` for the published benchmark results page; verify the changelog has no dated section added

## 8. Integration check

- [ ] 8.1 Run `pwsh -File scripts/pre-commit-gate.ps1` and verify it passes with the benchmark projects included
- [ ] 8.2 After merge, verify the first push to `main` creates the `benchmark-data` branch and the deployed site serves `benchmarks/latest/` and `benchmarks/history/`, and that the next pull request's summary compares against that entry

## Workflow follow-up

- Archive the change after the integration check on `main` has passed.
