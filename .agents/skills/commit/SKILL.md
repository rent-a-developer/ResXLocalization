---
name: commit
description: Review, verify, deliberately stage, and commit the current ResXLocalization repository changes. Use only when the user explicitly asks you to create a Git commit; do not use for ordinary code changes, reviews, or status checks.
---

# Commit

Write a commit that matches how `main` is written, and check the repository's own release-hygiene rules first.

**This skill commits. It never pushes, never opens a pull request, and never tags.** Those are separate acts
and the user asks for them separately.

## 1. Look at what changed

```bash
git status
```

```bash
git diff HEAD
```

Read the actual diff. Do not write a message from file names alone.

## 2. Apply the CONTRIBUTING.md checklist

Before committing, check whether the change requires companion edits and raise anything missing with the user:

- **Public API changed?** The build already told you — an undeclared public member is `RS0016` and a vanished
  one `RS0017`. Record it with `pwsh -File scripts/update-public-api.ps1` and review the
  `PublicAPI.Unshipped.txt` diff; a `*REMOVED*` line is a break.
- **User-facing change?** `CHANGELOG.md` needs an entry under `## [Unreleased]`, in Keep-a-Changelog format
  (`### Added` / `### Changed` / `### Fixed`). Breaking changes are written `- **BREAKING:** …`. Internal
  formatting and tooling work needs no entry.
- **Interface or behaviour change?** The affected pages under `docs/` carry the examples, and the README
  carries the quick start. `PACKAGE_README.md` (the NuGet package page) only needs touching if the change
  makes its short overview wrong.
- **Touched one UI package?** Was it mirrored into the other? The Avalonia and WPF markup extensions and
  converters are deliberate mirrors. Delegate the check to the `ui_parity_reviewer` custom agent when the
  change meets that agent's scope.
- **Touched resource lookup, the generated keys, or the packaging that carries them?**
  `pwsh -File scripts/verify-package-aot.ps1 -Pack` — nothing is trimmed on the just-in-time compiler, so the
  test suites cannot see the damage. Delegate the review to the `aot_package_compat_reviewer` custom agent.

⚠️ **Do not bump a version, and do not date a changelog section.** The version in the repository-root
`Directory.Build.props`, the release date, promoting `PublicAPI.Unshipped.txt` to `Shipped`, and the tag are
the maintainer's, at release time. If the change looks like it needs a release, say so — do not perform one.

## 3. Verify it builds

`TreatWarningsAsErrors=true` means a style slip is a build break, and CONTRIBUTING.md requires that the build
succeeds with no warnings and the tests pass.

```bash
pwsh -File scripts/pre-commit-gate.ps1
```

That runs the public-API reminder, checks line endings, style, formatting and member ordering, builds Release
and runs the test suites. It does not edit your files; if it reports the tree as untidy, run
`pwsh -File scripts/pre-commit-gate.ps1 -Fix` and review what changed before committing it.

If it fails, report the failure and stop — do not commit over it.

## 4. Write the message

[Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/), with a **lowercase, imperative**
summary:

```text
feat: add dynamic format arguments to the localize extension
fix: stop the enum converter caching the previous culture
build: standardize repository tooling
```

- Types in use: `feat`, `fix`, `docs`, `test`, `build`, `ci`, `chore`.
- A breaking change is `feat!:` or `fix!:` plus a `BREAKING CHANGE:` **footer** saying what breaks and what to
  do about it. `BREAKING CHANGE` is a footer, never a type.
- Add a body when the *why* is not obvious from the subject.
- If the branch is `<type>/issue-<number>-<slug>`, reference the issue number in the body.

Stage deliberately — `git add` the relevant paths rather than `git add -A`, and confirm nothing unintended
(build output, local scratch files) is included. A `PublicAPI.*.txt` change belongs in the same commit as the
code that caused it, and a change to one UI package belongs in the same commit as its mirror in the other.

## 5. Commit

Commit only. Do not push and do not open a pull request unless the user asks.
