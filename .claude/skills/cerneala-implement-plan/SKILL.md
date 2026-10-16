---
name: cerneala-implement-plan
description: Implement a named Cerneala Markdown checklist plan end-to-end, one plan stage per batch, checking off each stage in the plan file immediately after it is verified. Use when the user says to implement a plan completely, continue an existing checklist plan, execute a named plan end-to-end, or check off stages as batches finish. Optimized for throughput — no redundant searches, edits, test runs, or status narration — while modifying code, tests, documentation, and the source plan until every applicable item and gate is complete.
---

# Cerneala Implement Plan

Execute the named checklist plan completely. The plan file is the only durable progress ledger: its checkmarks and status line say what is done. Keep working across stages and turns until the plan is finished or a real blocker needs the user.

Follow `CLAUDE.md`, especially "Orchestrator mode (mandatory)" and "Claude Code tooling and verification commands". The first says the parent only coordinates: it reads subagent results, the `SKILL.md` files it loads, the initial `FileTree.md`, `git diff` and filtered test reruns, and runs only git/gh commands, `New-FileTree.ps1`, and focused-test reruns. All search and reading goes to Explorers, all edits, builds, experiments and focused tests to Implementers, diff review to Reviewers, and design to Planners. The second covers background runs and the focused-test commands. Every "search", "read", "edit", "build" and "test" below is work that the parent delegates this way, unless the step says the parent does it itself.

## Start

0. **Worktree (MANDATORY, before anything else):** start in a temporary worktree, as `CLAUDE.md` describes under "Temporary worktree for skills", and work only there. If the plan file is missing from the worktree, or differs from the main checkout's copy, an Implementer copies the main checkout's copy into the worktree at the same path. After the final completion audit and its commit, finish the worktree the same way.
1. Resolve the requested plan to one exact Markdown file. Resolve a bare name against `docs/plans/` first; when the path is not known, an Explorer searches for it.
2. If several files match, ask the user which one (with `AskUserQuestion`). Do not pick one.
3. Tell the user in one short message, in their language:
   - which plan file
   - which stage you start with
   - how many stages remain

The user's invocation of this skill authorizes implementing, verifying, documenting, and checking off the whole plan. It also authorizes running the `git-session-commit-push` skill once, at the end of the final completion audit.

## Compaction Recovery (MANDATORY)

After every context compaction, before any search, edit, test, checklist update, or stage-completion claim:

1. The parent reads this complete file from disk: `.claude/skills/cerneala-implement-plan/SKILL.md`.
2. Recover the plan path from the compaction summary.
3. An Explorer re-reads the complete plan and reports its checkmarks, unchecked items and gates.
4. Rebuild the current stage from that report and the repository state (git status/diff by the parent, deeper questions by an Explorer), not from memory.
5. Continue from "Resolve and Audit the Plan".

A compaction summary is never a substitute for this skill or for the plan file.

## Resolve and Audit the Plan

- Once per session, before the first delegation and when you do not know the structure of the area the plan touches, the parent runs `.\Tools\scripts\New-FileTree.ps1`, then reads `FileTree.md` once for orientation. Later structure questions go to an Explorer.
- An Explorer reads the complete plan, and the parent works from its report. The report identifies:
  - already checked work;
  - unchecked actionable items;
  - unchecked gates;
  - dependencies on other plans;
  - optional or conditional items;
  - public API and documentation obligations;
  - verification and stop conditions.
- Do not trust old baseline descriptions; the repository may have moved on. Answer the "where does this live now / who calls it" questions for the active stage with Explorers (Roslyn MCP and Grep), in parallel when there are many independent questions. The decisive files are read by the Implementer that edits them, not by the parent.
- Confirm prerequisite plans are complete before starting a dependent plan. Run them first only when the requested plan explicitly makes them a dependency. Otherwise ask the user.

## Stage-Atomic Batches (MANDATORY)

One batch is exactly one numbered or named implementation stage from the source plan. This is a hard boundary, not a preference.

- Select the first incomplete stage whose dependencies are complete.
- Work only on that stage's unchecked tasks, tests, verification, and gate conditions.
- Do not split one stage across batches, combine adjacent stages, or interleave work from later stages.
- Do not start the next stage because one task is green. Finish the entire stage and its gate first.
- Mandatory collateral that keeps the current stage valid belongs to the current batch, and nothing else does. Example: canonical API docs for a public API this stage changes.
- If a stage is already implemented but unchecked, its batch is audit-and-verify: prove every item and gate, then check it off.
- At batch start, send one short message naming the stage and the exact unchecked items and gates to close.
- If the stage cannot be completed, stop on it. Leave its unfinished items unchecked and do not jump to easier work in another stage.

Exploration may look at future stages for dependency awareness. Implementation, tests, and edits stay inside the active stage.

## Throughput-First Execution (MANDATORY)

Optimize elapsed time and tool round-trips after correctness, repository policy, and stage boundaries are satisfied. Throughput means less redundant work, never weaker gates.

Default shape per stage: one consolidated audit, one implementation pass, one verification ladder, and one mandatory checklist checkpoint.

- Build one work map for the active stage, then execute it. Do not rediscover scope before every task.
- Put independent tool calls in one message: independent Explorer questions, git status checks, and other subagent assignments.
- Implementers read the complete content only of C# files they will edit (repository policy). For already-understood supporting files, they use targeted reads, Roslyn symbol queries, or Explorer answers.
- Implementers make related edits with as few `Edit` calls as stay readable. They do not rewrite whole files to change a few lines.
- An Implementer implements the whole stage before its verification ladder, except where the plan requires RED/GREEN test-first order. Only one Implementer edits a given file at a time.
- Choose the verification ladder once, run by the Implementer, with the parent rerunning the focused tests it reports (see "Verify"):
  - compile or a narrow test during development;
  - the required stage tests at the gate;
  - never the complete suite: where the stage or the final audit asks for it, run the focused tests for all code the plan changed instead (see "Verify").
- A successful verification stays valid until a later change touches code, project configuration, generated inputs, or another surface that can affect it. Documentation and checklist edits do not invalidate compiled test evidence.
- After a successful build of the current code state, use `--no-build` for compatible follow-up test commands. Never use it after unbuilt code or project changes.
- Do not rerun a green command for reassurance. Reuse current-state evidence and record it at the checkpoint.
- Run anything longer than about 2 minutes in the background (the Implementer, or the parent for a verification rerun) and keep working on non-dependent steps while it runs.
- Message the user only at stage start, stage completion, or when a real failure or blocker changes the plan. Do not narrate searches, edits, or passing micro-tests.
- Do not add speculative tests, experiments, abstractions, or documentation audits beyond what closes a real checklist item or gate.

## Execute a Batch

### 1. Revalidate scope

- Explorers do one consolidated scope pass for the active stage, with Grep and, for C# references, Roslyn MCP when connected. The parent does not search.
- The Implementer reads every C# file completely before editing it. Partial reads only after full context is known.
- Explorers and Implementers inspect only the definitions, references, tests, and public docs plausibly affected by the stage.
- Preserve user changes and unrelated dirty-worktree changes.
- Do not expand into non-goals or unrelated cleanup. Report unrelated smells separately, without fixing them.
- If you find a real Cerneala defect outside the plan, invoke `github-issue-raise` for it and continue the stage. Do not fix it inside the plan.

### 2. Implement

The parent starts one Implementer per bounded set of files, with a complete brief (stage items, exact paths, symbols, Explorer findings, rules below). The Implementer does the work below.

- Follow the plan's test-first order when specified. A RED test must fail for the intended reason before the production change.
- Prefer current Cerneala patterns, simple ownership, and explicit lifecycle handling.
- Keep public API changes exactly within the approved plan.
- If a public API changes, update `docs-site/documentation/classes/` in the same batch with the `writing-api-documentation` skill. This is mandatory current-stage collateral, not permission to start a later documentation stage.
- Update `docs-site/documentation/manifest.json` when API pages are added or renamed.
- Complete every implementation and test task of the active stage before its gate.

### 3. Verify

The Implementer runs the verification below. The parent then reads the Implementer's `git diff` (limited to the files in the brief) and reruns the focused tests it reported, reading only the filtered result lines, as `CLAUDE.md` describes under "Verification by the parent". The parent runs no other tests, builds, or harnesses.

- Run the narrowest tests that prove the stage behavior, then the related regression tests the plan names.
- Run formatting, build, source-generator, API, visual, or benchmark verification when the stage requires it, with the commands and prerequisites in `CLAUDE.md`.
- Tests are focused only: each test project that covers the changed code, with a `--filter`, as `CLAUDE.md` describes under "Focused tests". Never run the complete suite (`dotnet test Cerneala.slnx`, or a test project without `--filter`), even when the plan asks for it: the user chose focused tests only. A plan item that requires the full suite is satisfied by the focused tests for all code the plan changed.
- Run each required verification once in the latest relevant code state. Reuse the result at the gate and the final audit when nothing relevant changed afterwards.
- When a narrow test fails, fix it and rerun that failing subset first, before the stage's other focused tests.
- Investigate failures instead of checking the task and calling them "probably unrelated". An environmental cause needs evidence, for example the same failure on unchanged `master`.
- Never mark a task complete only because code was written. It must satisfy its stated verification.

### 4. Check off the stage immediately (MANDATORY)

After the entire active stage passes, the plan update is a blocking part of the batch. Do not run any implementation command for the next stage until every step below is done:

- An Implementer edits the source plan (the parent does not edit it), and the brief names the stage and the exact items and gates to check.
- Change `[ ]` to `[x]` for every active-stage item implemented and verified in this batch.
- Mark an active-stage gate `[x]` only when all its conditions are proven.
- A conditional task that was evaluated and found unnecessary gets `[x]` plus a short reason, such as `(Nu a fost necesar: ...)`.
- Leave blocked or unfinished work unchecked.
- Never check tasks from another stage. Preserve checkmarks that already existed, and do not rewrite historical checked items unless repository evidence proves them false.
- When the plan tracks status, set it to `in progres` after the first completed batch. Set `finalizat` only after the final audit.
- The parent runs `git diff --check` on the files touched by the batch and reads `git diff` of the plan file.
- An Explorer re-reads the entire updated stage section. Confirm from its report and the plan diff that completed items and gates are checked, unfinished ones are not, and no neighboring stage changed.
A stage is not a completed batch until the plan file has been edited and re-read.

### 5. Report and continue

- Send one short message in the user's language: the completed stage, the verification run with real counts, and the newly checked items and gates.
- Then select the next incomplete stage as a new batch and continue in the same turn when feasible. Do not stop because one stage is green; the target is end-to-end completion.

## Checklist Semantics

- `[ ]`: not yet proven complete.
- `[x]`: implemented or deliberately resolved, and verified.
- A checked objective does not check its implementation tasks. A checked task does not check its gate.
- Non-goal checkboxes may be checked only after final review confirms the forbidden scope was not introduced.
- A test task is complete only when the test exists and passes.
- A documentation task is complete only when the canonical page and the manifest are synchronized.
- A task that asks for the full suite is complete when the focused tests for all code the plan changed pass in the current implementation state. Check it with a short note naming the filters, such as `(Doar teste focusate: FullyQualifiedName~GridTests)`.

## Failure and Resume Rules

- If a test fails, keep the affected tasks unchecked and fix the failure inside the current stage before moving on.
- The Implementer retries a transient tooling failure once with the narrowest corrective action. If it repeats, an Implementer diagnoses that tool directly instead of restarting unrelated verification.
- If the plan conflicts with current architecture, stop that batch, record the contradiction, and ask the user before changing the approved design.
- If a user decision blocks the current stage, ask with `AskUserQuestion` (concrete example per option). Do not skip to a later stage while waiting.
- After an interruption, an Explorer reads the plan and reports the unchecked items, and the parent resumes from the first incomplete stage. After a compaction, follow the mandatory recovery sequence above.
- If a blocker stops the plan before the final audit, do not invoke `git-session-commit-push`. Report the completed stages and the blocker, and ask the user with `AskUserQuestion` whether to commit the verified work anyway.
- The plan file and repository state are authoritative, not memory of an older turn.

## Final Completion Audit

Before declaring the plan done:

- An Explorer re-reads the entire plan.
- Confirm from its report that every applicable task and gate is `[x]`, conditional and non-goal items have explicit resolutions, and required prerequisite plans are complete.
- An Implementer runs the plan's final targeted verification once after the last relevant implementation change, or the parent reuses an identical current-state run from the final stage.
- An Implementer runs the focused tests for all code the plan changed once in the final code state, each with a `--filter`, and the parent reruns them (filtered result lines only). Never run the complete suite, even when the plan defines a final full suite. Do not repeat them if they already passed after the last code or project change.
- An Implementer runs the final API documentation and manifest checks for public API changes, or the parent reuses results produced after the last relevant change.
- The parent runs `git diff --check` across all changed files.
- A Reviewer reviews `git diff` for accidental scope, leftover debug code, generated churn, and unchecked required work, and the parent reads `git diff` itself.
- An Implementer sets the plan status to `finalizat`.
- Invoke the `git-session-commit-push` skill (MANDATORY). Pass it the plan path, the completed stages, the final verification results with real counts, and any gate that still needs a human. If a required final gate failed or could not run, do not invoke it; report the blocker and ask the user with `AskUserQuestion` whether to commit anyway.
- Report in the user's language:
  - the completed stages
  - the verification results, with real counts
  - the plan path
  - the commit, the branch, and the pull-request URL, or the gate that blocked them
  - any gate that still needs a human, for example a visual check

Do not declare the plan done while required work remains. A long diff or a tiring plan is not a definition of done.

## Invocation Example

```text
/cerneala-implement-plan Implementeaza planul 2026-07-13-repeat-button end-to-end. Pastreaza un batch per etapa, bifeaza etapa dupa verificare si optimizeaza pentru throughput fara verificari redundante.
```
