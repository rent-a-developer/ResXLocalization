<!-- Thank you for contributing! Please read CONTRIBUTING.md first. -->

## What does this change?

<!-- A short description of the change and, for behavior changes, the motivation. Link the related issue: Fixes #123 -->

## Checklist

Everything under **Always** applies to every pull request. The rest applies only when its trigger does —
tick it, or leave it and say why in the description. A box that does not apply is not a box to tick.

### Always

- [ ] `pwsh -File scripts/pre-commit-gate.ps1` passes. It runs the public-API reminder, checks line endings,
      style, formatting and member ordering, builds Release and runs the test suites. Use `-Fix` to have it
      apply the tidying rather than only report it.
- [ ] The Release build produces **zero warnings**. `TreatWarningsAsErrors` is on, so this also covers style,
      member ordering, trim and public-API diagnostics.
- [ ] Branch name follows `<type>/issue-<number>-<slug>`, or `<type>/<slug>` when there is no issue — see
      [CONTRIBUTING.md](../CONTRIBUTING.md#branches).

### If the change adds behaviour or fixes a bug

- [ ] It is covered by tests.
- [ ] `CHANGELOG.md` has an entry under `## [Unreleased]`. Do **not** bump a version — the version, the
      release date and the tag are the maintainer's, at release time.

### If the change touches one UI package

- [ ] It is mirrored into the other, or it genuinely does not apply there — see
      [CONTRIBUTING.md](../CONTRIBUTING.md#avalonia-and-wpf-symmetry). The Avalonia and WPF markup
      extensions and converters are deliberate mirrors of each other.

### If the change touches resource lookup, the generated keys, or the packaging that carries them

Culture fallback, satellite discovery, the source generator's output, the `buildTransitive` wiring, or a
dependency version. Nothing is trimmed on the just-in-time compiler, so no other check in this repository
can see the damage a mistake here does to a trimmed application.

- [ ] `pwsh -File scripts/verify-package-aot.ps1 -Pack` passes for **both** frameworks
      (`-Framework net8.0` and the `net10.0` default).
- [ ] No new `IL2xxx` / `IL3xxx` diagnostics, and none suppressed.

### If the change touches the public API

- [ ] The affected `PublicAPI.Unshipped.txt` is updated with `pwsh -File scripts/update-public-api.ps1`, and
      the diff was reviewed line by line. A `*REMOVED*` entry is a breaking change.
- [ ] The XML documentation and the affected pages under `docs/` are updated.
