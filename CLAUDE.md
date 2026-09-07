# CLAUDE.md

Read [AGENTS.md](AGENTS.md) first — it is the canonical guidance, and all of it applies to Claude Code.

`.claude/` holds only discovery metadata and hook adapters; the procedures live in `.agents/` and `scripts/`.

| File | What it wires |
| --- | --- |
| `.claude/settings.json` | The two `PostToolUse` hooks that run after `Edit` and `Write` |
| `.claude/hooks/tidy-code.ps1` | Formats the file the edit touched, by calling `scripts/tidy-code.ps1` |
| `.claude/hooks/public-api-guard.ps1` | Prints the companion-edit checklist when a `PublicAPI.*.txt` file moves |
| `.claude/skills/commit/SKILL.md` | `disable-model-invocation: true`; points at `.agents/skills/commit/SKILL.md` |
| `.claude/agents/*.md` | The two reviewers, granted `Read`, `Grep` and `Glob` only |

[`.agents/README.md`](.agents/README.md) explains what the hooks will and will not do, and when to run
`scripts/tidy-code.ps1` yourself because no hook saw the edit.
