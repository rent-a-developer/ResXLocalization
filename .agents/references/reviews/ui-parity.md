# Avalonia and WPF parity review

Use this whenever `src/ResXLocalization.Avalonia` or `src/ResXLocalization.WPF` changes, or when
their samples or test suites do.

This is a **review**: report findings, cite file and line, do not edit files.

## The contract

The two UI packages are deliberate mirrors. A developer who knows one is expected to be able to read
the other, and an application that uses both gets the same markup, the same property names and the
same behaviour from each. The shared engine lives in `ResXLocalization.Core` and is referenced by
both, so anything that is not framework-specific belongs there rather than in one of the mirrors.

| Concern | Avalonia | WPF |
| --- | --- | --- |
| Localize a key | `LocalizeExtension` | `LocalizeExtension` |
| Localize an enum in a template | `LocalizeEnumExtension` | `LocalizeEnumExtension` |
| Localize an enum as a bound value | `LocalizeEnumConverter` | `LocalizeEnumConverter` |
| Composite-format arguments | `LocalizeArgs.Arg0`…`Arg8` | `LocalizeArgs.Arg0`…`Arg8` |

Where the frameworks genuinely differ, the difference is expected to be **local and explained**:
Avalonia binds through `AvaloniaProperty` and an observable, WPF through `DependencyProperty` and a
`MultiBinding`; Avalonia subscribes with weak events, WPF relies on its own weak binding-target
references; WPF has no Native AOT. None of those is a licence for a different public API.

## What to check

1. **The public surface.** For every public type, member, property name and default value added,
   removed or renamed in one package, is the mirror present in the other with the same name and the
   same default? Compare `src/ResXLocalization.Avalonia/PublicAPI.Shipped.txt` and
   `src/ResXLocalization.WPF/PublicAPI.Shipped.txt` together with their `Unshipped` files — the two
   lists are the fastest way to see an asymmetry, and a change that updates only one of them is a
   finding on its own.

2. **The markup.** Does the same XAML work in both? A new markup-extension property that only one
   package accepts means the same view cannot be shared, which is the thing this parity exists to
   protect.

3. **The behaviour, not just the signature.** A property that exists in both but resolves differently
   — a different key prefix default, a different fallback, a different moment at which it re-reads
   the culture — is a worse finding than a missing member, because it looks correct.

4. **Where the code belongs.** Logic that is not framework-specific and exists in both packages belongs
   in `ResXLocalization.Core`. `EnumKeyConvention` is the precedent: it is internal to Core and shared
   with both UI packages through `InternalsVisibleTo`, so both build identical keys by construction
   rather than by review.

5. **The tests.** `tests/ResXLocalization.Avalonia.Sample.Tests` and
   `tests/ResXLocalization.WPF.Sample.Tests` mirror each other too. A new test in one and not the
   other means the mirrored behaviour is asserted once.

6. **The samples.** Both sample applications demonstrate every feature. A feature added to one sample
   and not the other leaves the two showcases claiming different things.

7. **The documentation.** The guides under `docs/` describe both frameworks in one voice, and say
   "the same XAML in Avalonia and WPF" in several places. A change that makes that untrue needs the
   sentence changed, not left.

## When the answer is "it does not apply"

That is a legitimate outcome, and Native AOT is the standing example: WPF does not support it, so an
AOT-related property or annotation on the Avalonia side has no WPF counterpart. Say which of the
differences above explains it. An unexplained asymmetry is the finding; an explained one is a note.

## What to report

For each finding: the file and line in the package that changed, the file and line where the mirror
would go, and whether it is a missing member, a differing default, or a behavioural difference. If
you need the diff of the change or the result of a build, ask the caller for it.
