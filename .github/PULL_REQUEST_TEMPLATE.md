<!-- Thank you for contributing! Please read CONTRIBUTING.md first. -->

## What does this change?

<!-- A short description of the change and, for behavior changes, the motivation. Link the related issue: Fixes #123 -->

## Checklist

- [ ] `dotnet build ResXLocalization.slnx -c Release` succeeds with **zero warnings**.
- [ ] All four test suites pass (Core, source generator, Avalonia, WPF); new behavior / fixed bugs are covered by tests.
- [ ] Avalonia/WPF symmetry preserved (mirrored change in the other UI package, if applicable).
- [ ] No new `IL2026`/`IL3050` warnings (Avalonia/Core stay Native-AOT clean).
- [ ] Public API changes are declared in the affected `PublicAPI.Unshipped.txt`.
- [ ] XML docs and `README.md` updated for public API changes.
- [ ] `CHANGELOG.md` updated under *Unreleased*.
- [ ] Code formatted (`scripts/formatCode.cmd` / `scripts/formatCode.sh`).
