---
mode: agent
description: Full sprint lifecycle — plan, clarify, execute, review, close, repeat until phase complete.
---

# Sprint Lifecycle — Plan → Execute → Review → Close → Repeat

Run the full sprint lifecycle for the current project phase. Plans a sprint, gets approval, executes all tasks, reviews before closing, and repeats until the phase backlog is empty.

---

## Developer identity

Use always the instructions from `kk guide` subcommands to set your developer name and email for all `kk` operations. IMPORTANT: Never identify yourself as "GitHub Copilot" because it's not an engine at all!

---

## Phase 1 — Sprint Planning

### 1a — Check backlog

```powershell
npx kk task list -s N --format json
npx kk task list -s H --format json
npx kk task list -s D --format json
```

Review what's available. Draft (D) and Hold (H) tasks need refinement before entering a sprint.

### 1b — Refine and create tasks

For each task to include:
- Write clear **User Story**, **DoD**, **Implementation Notes**
- Use `kk task new --from-json <file> --consume` for programmatic creation
- Use `kk task update <id> --section ...` to fill in missing sections
- Move refined tasks to N status if they were D/H

Follow the reference document related by the user for sprint scope. 

WARNING: If the file was not provided, you must ask the user to provide it before proceeding using the **Interactive Clarification Block** explained below at 1d. 

### 1c — Plan the sprint

```powershell
npx kk sprint init --from-json <file> --consume
```

Or manually select tasks and initialize the sprint.

### 1d — Interactive Clarification Block (Planning Review)

Before opening the sprint, present an **Interactive Clarification Block**:

Q: Are the tasks understandable? Is the scope clear?
A:

Q: Are there any issues with the tasks as described?
A:

Q: Are there tasks missing or additional items to add?
A:

**WAIT for user input.** If the user requests changes, go back to 1b and repeat. Ask the same questions again via ICB. Only proceed to Phase 2 when there are no remaining actions.

---

## Phase 2 — Sprint Execution

Follow `sprint-execution.prompt.md` exactly:

### For EACH task (in priority order, then by ID):

#### 2a — Move to In-Progress + commit immediately

```powershell
npx kk task status <id> P --developer "Your engine <your-name-and-version@kwytco.com.br>"
git add -A
git commit -m "Change status of task [<id>] <title> to P" --author="Your engine <your-name-and-version@kwytco.com.br>"
```

#### 2b — Read the task file

Open and read the task markdown. Understand **User Story**, **DoD**, **Implementation Notes**, **Dependencies**.

#### 2c — Implement

- Read relevant files before modifying
- Satisfy ALL DoD items
- Follow project conventions (IBelzontBindable, call conventions, CSS rules, etc.)

#### 2d — Build & verify

```powershell
MSBuild.exe BelzontWE.sln /p:Configuration=Debug 2>&1 | Select-String -Pattern 'error CS|warning CS|Build successful|FAILED'
```

Zero errors required.

#### 2e — Mark Done + commit immediately

```powershell
npx kk task update <id> --check-all-dod
npx kk task status <id> T --developer "Your engine <your-name-and-version@kwytco.com.br>"
git add -A
git commit -m "Change status of task [<id>] <title> to T" --author="Your engine <your-name-and-version@kwytco.com.br>"
```

**CRITICAL**: Commit BEFORE starting the next task. ONE task at a time.

---

## Phase 3 — Pre-Close Review

After ALL tasks are status **T**, present an **Interactive Clarification Block**:

Q: Does the game run the mod well?
A:

Q: Does the sprint delivery need any fixes?
A:

Q: Are there any changes to the scope of the next sprint?
A:

**WAIT for user input.**

### If fixes are needed:

1. Create an extra task for EACH fix point using `kk task new --from-json`
2. Execute each fix task following Phase 2 rules (P → implement → T → commit)
3. Present ANOTHER Interactive Clarification Block (same questions)
4. Repeat until no new fixes are pending

---

## Phase 4 — Close Sprint

Only when there are NO pending fixes:

### 4a — Dry-run

```powershell
npx kk sprint close --dry-run --developer "Your engine <your-name-and-version@kwytco.com.br>"
```

### 4b — Close

```powershell
npx kk sprint close --developer "Your engine <your-name-and-version@kwytco.com.br>"
```

### 4c — Commit

```powershell
$author = "Your engine <your-name-and-version@kwytco.com.br>"
$message = "Close Sprint NNN - <Sprint Name>"
git add -A
git commit -m $message --author=$author
```

---

## Phase 5 — Next Sprint

If there are remaining tasks in original sprint group plan, go back to **Phase 1** and repeat the entire cycle.

If the sprint group is complete, report final status.

---

## Rules

- **ONE task at a time.** Never start a second task before the first is committed to T.
- **Commit kanban on EVERY status change** — both N→P and P→T.
- **Never batch kk operations** without committing between them.
- **Build must be clean** (0 errors) after each task.
- **No stopping between tasks** — complete all tasks in the sprint, then review.
- **Interactive Clarification Blocks** are the ONLY allowed pause points.
- **Follow `kk guide` commands** — if you forget a step, run `npx kk guide <topic> --profile agent`.
- **All binding classes** must implement `IBelzontBindable` via `DataBaseController` pattern.
- **Frontend call convention**: `engine.call("k45::<acronym>.<group>.<method>")` — never `engine.trigger()`.
- **CSS**: No shorthand properties (`background`, `margin`, `padding`, `border`). Always expand individually.
