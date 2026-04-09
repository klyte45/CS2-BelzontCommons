---
mode: agent
description: Update changelog.md with all user-facing changes since the last release, compiled from git history.
---

# Update Changelog from Git History

Update the `changelog.md` file in the mod project with all user-facing changes introduced since the last release commit.

## Step 1 — Locate the last release commit

Run `git log --oneline --no-merges` from the workspace root. Release commits start with a version tag like `v0.2.3r4`. Identify the most recent release commit hash to use as the boundary.

## Step 2 — Collect changes

For each commit between the release boundary and HEAD (exclusive):

1. Read the commit subject and body with `git show <hash> --stat` to understand what was changed.
2. For task-based commits, read the corresponding task file from `.klyte-kanban/Archive/Sprint_XXX/`. Focus on: **User Story**, **Background**, and **DoD** (Definition of Done).
3. Classify each change as:
   - **Behavior change** — modifies or removes existing behavior in a way users will notice
   - **New feature** — adds something users can use or see
   - **Bug fix** — corrects incorrect behavior users experienced *that was present in the last released version*
   - **Skip** — internal refactor, test infrastructure, CI, developer tooling, kanban bookkeeping, or git submodule-only changes with no user impact

4. **Net-zero filtering:** If a feature or behavior was introduced *and* reverted/removed *within the same development cycle* (i.e., never shipped to users in a release), exclude it entirely — both the feature entry and any associated bug fix entries. Similarly, if a bug was introduced by a new feature in this same cycle and was fixed before release, do not list the bug fix separately; only describe the final shipped state of the feature.

## Step 3 — Determine the new version number

Read the `<Version>` field from the project `.csproj` file (ignore the debug timestamp override). This is the new version. Format it as `vX.Y.ZrW` where the last `.W` in the four-part version becomes `rW`. Example: `0.2.4.0` → `v0.2.4r0`.

Get today's date formatted as `DD-MMM-YY` in English with uppercase month (e.g., `06-APR-26`).

## Step 4 — Write the new changelog entry

Format:

```
# vX.Y.ZrW (DD-MMM-YY)

- [behavior change items first]
- [new feature items]
- [bug fix items]
```

Writing rules:

- **Order:** behavior changes first, then new features, then bug fixes.
- **Language:** target end users, not developers. Describe what changed for the player, not how it was implemented.
- **Include:** anything a player would notice — new UI elements, changed workflows, fixed visible bugs, added languages, performance improvements visible to the user.
- **Exclude:** code refactors, test framework changes, CI fixes, developer tooling, linting, internal naming cleanup with no user-visible effect.
- **Detail level:** new features and behavior changes may have up to 4 lines of additional context. Bug fixes get one line only.
- **Bold highlights:** use `**bold section markers**` for important items. No emojis.
- **No repetition:** merge closely related items rather than listing the same feature twice.

If any newly introduced bugs are known and unfixed, append them after the regular entries:

```
**Known issues:**
- [brief description of unfixed bug introduced in this version]
```

## Step 5 — Handle the previous version section

Check how many days have passed since the last release date.

**If 30 days or fewer:** replace the previous version's top-level `#` header line with `## FROM`, keeping all its content. Example: `# v0.2.3r4 (04-DEC-25)` becomes `## FROM v0.2.3r4 (04-DEC-25)`. Prepend the new entry above that.

**If more than 30 days:** remove all existing content from `changelog.md` first (it is considered outdated), then write only the new version entry.

## Step 6 — Update the file

Write the resulting content to `changelog.md` following the rules above.

## Changelog format reference

```markdown
# vX.Y.ZrW (DD-MMM-YY)
- Behavior change 1
- New feature 1
- Bug fix 1

## FROM vX.Y.ZrW-1 (DD-MMM-YY)
- Previous change 1
```

- The top section (before the first `## FROM`) contains changes introduced in the current version.
- `## FROM` marks the immediately preceding version's content and is only kept when that version was released within the last 30 days.
- `MMM` is always 3-letter English abbreviation, capitalized (JAN, FEB, MAR, APR, MAY, JUN, JUL, AUG, SEP, OCT, NOV, DEC).
