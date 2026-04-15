---
mode: agent
description: Execute a full sprint start-to-finish — work all tasks in the ActiveSprint one by one and close when all are done.
---

# Sprint Execution — Start to Finish

Execute every task currently in the `ActiveSprint/` folder of `.klyte-kanban/`, in order, and close the sprint when all are complete. Do **not** stop between tasks.

---

## Developer identity

Use always the instructions from `kk guide` subcommands to set your developer name and email for all `kk` operations.

---

## Step 0 — Verify the active sprint

```powershell
npx kk task list -s N --format json
```

Confirm the tasks you will work on. Note their IDs.

---

## Step 1 — Work tasks one at a time

For **each** task (in priority order, then by ID):

### 1a — Move task to In-Progress and commit kanban IMMEDIATELY

```powershell
npx kk task status <id> P --developer "Your engine <your-name-and-version@kwytco.com.br>"
git add -A
git commit -m "Change status of task [<id>] <title> to P" --author="Your engine <your-name-and-version@kwytco.com.br>"
```

**CRITICAL:** Commit before doing any code work.

### 1b — Read the task file

Open `.klyte-kanban/ActiveSprint/P_*_<id>_*.md` and read:
- **User Story** — what the user needs
- **DoD** — the acceptance criteria you must satisfy
- **Implementation Notes** — hints from the planner
- **Depends on** — prerequisite tasks (should already be T status)

### 1c — Implement

Write or modify code in the project repo to satisfy all DoD items.

Key rules:
- Always read the relevant files before modifying them
- Run the build after changes: `dotnet build "BelzontWE.sln"`
- Write tests (even if all `[Ignore]` — document what the test would verify and why it can't run without Unity)
- Tests that don't require Unity game runtime MUST pass

### 1d — Verify

```powershell
dotnet test "BelzontWE.sln" 2>&1 | Select-Object -Last 8
```

Confirm: `Falha: 0`. Pre-existing failures (like `STRING_RENDERING_BATCH_Is256`) are acceptable if they were there before your changes.

### 1e — Mark all DoD items done, move task to Done, commit kanban IMMEDIATELY

```powershell
npx kk task update <id> --check-all-dod
npx kk task status <id> T --developer "Your engine <your-name-and-version@kwytco.com.br>"
git add -A
git commit -m "Change status of task [<id>] <title> to T" --author="Your engine <your-name-and-version@kwytco.com.br>"
```

---

## Step 2 — Close the sprint

After all tasks are status **T**:

### 2a — Dry-run preview

```powershell
npx kk sprint close --dry-run --developer "Your engine <your-name-and-version@kwytco.com.br>"
```

Verify all tasks are listed as "archived". If any task is not T, go back and complete it.

### 2b — Close

```powershell
npx kk sprint close --developer "Your engine <your-name-and-version@kwytco.com.br>"
```

### 2c — Commit

```powershell
$author = "Your engine <your-name-and-version@kwytco.com.br>"
$message = "Close Sprint XXX - <Sprint Name>"
git add -A
git commit -m $message --author=$author
```

---

## Rules

- **ONE task at a time.** Never start a second task before the first is committed to T.
- **Commit kanban on EVERY status change** — both N→P and P→T.
- **Never batch kk operations** without committing between them.
- **Tests that require Unity** → mark `[Ignore]` with a comment explaining what they would test.
- **Build must be clean** (0 errors) after each task.
- **No stopping** — complete all tasks in the sprint, then close it.
- **Follow guides from kk tool** - if you forget any step or do not have the guides on your memo, run `npx kk guide` and read the relevant sections.
- **The user may move back tasks if found a DoD item not satisfied** — if that happens, repeat the process for that task focusing on DoD items not set, and move it back to T at the end.
- **The user may do code reviews and change some code if they find something that can be improved** — he may not ask you to do the changes and do it by themselves, but if they ask you to do the changes, do them and commit with the same author as the original commit for that task.
