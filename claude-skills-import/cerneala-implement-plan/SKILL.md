---
name: cerneala-implement-plan
description: Implement a named Cerneala Markdown checklist plan end-to-end under a persistent task-list execution contract, optimizing for throughput while using exactly one plan stage per batch and mandatorily checking off that stage immediately after verification. Use when the user says to implement a plan completely, continue an existing checklist plan, execute a named plan end-to-end, or check off stages as batches finish. This skill minimizes redundant searches, edits, test runs, and status narration while modifying code, tests, documentation, and the source plan until every applicable item and gate is complete.
---

# Cerneala Implement Plan

Execute the named checklist plan completely. Use the plan file as the durable progress ledger and the Claude task list (TaskCreate/TaskUpdate/TaskList) as the cross-turn execution contract.

## Start the Execution Contract

1. Resolve the requested plan to one exact Markdown file.
2. Call `TaskList` before creating tasks.
3. If no unfinished task list exists for this plan, create one with `TaskCreate`: one task per plan stage, in plan order. The first task's description must contain:
   - the resolved plan path;
   - the requirement to implement, verify, document, and check off the plan end-to-end;
   - the explicit requirement that, after every context compaction, Claude must
     re-read this complete `cerneala-implement-plan` skill from disk before
     resuming any work.
4. Do not impose a token or time budget unless the user explicitly requested one.
5. If an unfinished task list already targets the same plan, continue it instead of creating a duplicate.
6. If an unfinished task list for a different plan is active, do not falsely complete or replace it. Report the conflict and ask the user whether to pause, cancel, or redirect the existing work.
7. Keep the task list active across batches and turns. Mark the final task `completed` only after the plan is genuinely finished.

The user's invocation of this skill is explicit authorization to create the task list.

## Compaction Recovery (MANDATORY)

After every context compaction, re-read this complete
`cerneala-implement-plan/SKILL.md` file from disk. This is mandatory even when the
compacted summary appears to contain all skill instructions or a reliable workflow
recap.

Before any repository search, edit, test, checklist update, or stage-completion
claim after a compaction:

1. Read the entire skill file, not only this recovery section.
2. Call `TaskList` and recover the exact plan path from the first task's description.
3. Follow the freshly reloaded skill from `Resolve and Audit the Plan` onward,
   including re-reading the plan and reconstructing its current stage.

Do not treat a compaction summary as a substitute for the skill. Enforce this
recovery sequence for an older matching task list even if its description predates the
mandatory compaction wording.

## Resolve and Audit the Plan

- Follow the repository instructions (`CLAUDE.md`, and `AGENTS.md` when present) before inspecting or editing anything.
- Run `Tools/scripts/New-FileTree.ps1`, then read `FileTree.md`.
- Use direct file reads and `rg` for text.
- Resolve a bare plan name against `docs/plans/` first, then other documented plan locations only if needed.
- If multiple files match, ask for the exact plan instead of choosing the least ugly filename.
- Read the complete plan and identify:
  - already checked work;
  - unchecked actionable items;
  - unchecked gates;
  - dependencies on other plans;
  - optional or conditional items;
  - public API and documentation obligations;
  - verification and stop conditions.
- Inspect the current code and tests before trusting old baseline descriptions. The repository may have moved on while the plan sat there collecting dust.
- Confirm prerequisite plans are complete before starting a dependent plan. If they are not complete, execute them first only when the requested plan explicitly makes them a dependency; otherwise ask the user.

## Stage-Atomic Batches (MANDATORY)

One batch is exactly one numbered/named implementation stage from the source plan. This is a hard execution boundary, not a preference.

- Select the first incomplete stage whose dependencies are complete.
- Work only on that stage's unchecked tasks, tests, verification, and gate conditions.
- Do not split one stage across multiple batches.
- Do not combine adjacent stages, even when they are small.
- Do not interleave work from later stages while the current stage is open.
- Do not start the next stage merely because one task in the current stage is green. Finish the entire stage and its gate first.
- If repository policy requires collateral work to keep the current stage valid, such as synchronizing public API documentation with a public API change, include only that mandatory collateral in the current batch. Do not otherwise advance or check a later stage early.
- If a stage is already implemented in repository state but unchecked, its batch is an audit-and-verification batch: prove every item and gate, then check it off.
- At batch start, name the stage and enumerate the exact unchecked items and gate conditions to close in one concise update.
- In the task list (TaskCreate/TaskUpdate), keep only the current stage `in_progress`; later stages remain `pending`.
- If the current stage cannot be completed, stop on that stage. Leave its unfinished items unchecked and do not jump ahead to easier work from another stage.

The source plan remains the durable ledger. Repository exploration may inspect future stages for dependency awareness, but implementation, tests, and edits must stay inside the active stage boundary.

## Throughput-First Execution (MANDATORY)

Optimize end-to-end elapsed time and tool round-trips after correctness, repository policy, and stage boundaries are satisfied. Throughput means less redundant work, never weaker gates.

Default shape for each stage: one consolidated audit pass, one implementation pass, one verification ladder, and one mandatory checklist checkpoint.

- Build one work map for the active stage, then execute it continuously. Do not rediscover scope before every task.
- Batch independent reads, status checks, and tool calls when the tool surface supports safe parallel execution.
- Read the full contents only of C# files that are likely to be edited, as required by repository policy. Use targeted reads, symbols, and references for already-understood supporting files instead of rereading them repeatedly.
- Consolidate related manual edits into the fewest coherent Edit/Write tool calls.
- Implement the complete stage before entering its verification ladder, except where the plan explicitly requires RED/GREEN test-first sequencing.
- Choose the verification ladder once: compile or narrow test during development, required stage tests at the gate, and the full suite only when the stage or final audit requires it.
- Treat a successful verification as valid until a later change touches code, project configuration, generated inputs, or another surface that can affect it. Documentation and checklist-only edits do not invalidate compiled test evidence.
- After a successful build of the current code state, use `--no-build` and `--no-restore` for compatible follow-up test commands. Never use them after code or project changes that have not been built.
- Do not rerun a green command merely for reassurance. Reuse current-state evidence and record it at the stage checkpoint.
- For an already-implemented stage, audit all items and gates in one pass, run the minimum sufficient proof once, and checkpoint the stage.
- Send commentary only at stage start, stage completion, or when a real failure/blocker changes the execution path. Do not narrate individual searches, patches, or passing micro-tests.
- Do not add speculative tests, experiments, abstractions, or documentation audits beyond what closes a real checklist item or gate.

## Execute a Batch

### 1. Revalidate scope

- Perform one consolidated scope pass for the whole active stage.
- Read every C# file completely before editing it.
- Use targeted partial reads only after full context is known.
- Inspect only the definitions, references, tests, and public docs plausibly affected by the batch.
- Preserve user changes and unrelated dirty worktree changes.
- Do not expand into non-goals or unrelated cleanup.
- Surface unrelated smells separately without fixing them.

### 2. Implement

- Follow the plan's test-first order when specified.
- Use the Edit/Write tools for manual edits.
- Prefer current Cerneala patterns, simple ownership, and explicit lifecycle handling.
- Keep public API changes exactly within the approved plan.

- If a public API changes, update `docs-site/documentation/classes/` in the same batch using `writing-api-documentation`; treat this as mandatory current-stage collateral, not permission to implement the later documentation stage.
- Update `docs-site/documentation/manifest.json` when API pages are added or renamed.
- Complete every implementation and test task belonging to the active stage before moving to its gate.

### 3. Verify

- Run the narrowest tests that prove the batch behavior.
- Run related regression tests named by the plan.
- Run formatting, build, source-generator, API-diff, visual, benchmark, or full-suite verification when required by the batch.
- Run each required verification once in the latest relevant code state. If it passes and no relevant implementation input changes afterward, reuse that result at the gate and final audit.
- When a narrow test fails, fix and rerun that failing subset first. Do not repeatedly pay for the full suite while diagnosing a local failure.
- Investigate failures instead of checking the task and writing "probably unrelated" like an optimistic arsonist.
- Never mark a task complete only because code was written. It must satisfy its stated verification.

### 4. Commit the stage checkpoint immediately (MANDATORY)

After the entire active stage passes, checklist synchronization is a blocking part of the batch. Do not run any implementation command for the next stage until all steps below are complete:

- Edit the source plan with the Edit tool.
- Change `[ ]` to `[x]` for every active-stage item implemented and verified by this batch.
- Mark every active-stage gate `[x]` only when all of its conditions are proven.
- For a conditional task that was evaluated and correctly found unnecessary, mark it `[x]` and append a short reason such as `(Nu a fost necesar: ...)`.
- Leave blocked or unfinished work unchecked.
- Never check tasks from another stage as part of the current stage checkpoint, except to preserve checkmarks that already existed.
- Do not rewrite historical checked items unless repository evidence proves they are false.
- Change plan status to `in progres` after the first completed batch when the plan tracks status.
- Change status to `finalizat` only after all applicable tasks, gates, documentation, and final verification are complete.
- Run `git diff --check` for the files touched by the batch.
- Re-read the entire updated stage section and confirm all completed items and gates are checked, unfinished items remain unchecked, and no neighboring stage changed accidentally.
- Send the stage-completion commentary update before beginning the next batch.

Checking off the finished stage is mandatory. A stage is not a completed batch until the source plan has been patched and re-read.

### 5. Report and continue

- Send a short commentary update naming the completed stage, verification run, and newly checked items/gates.
- Only after that checkpoint, select the next incomplete stage as a new batch and continue in the same turn when feasible.
- Do not stop merely because one stage is green; the goal is end-to-end completion.

## Checklist Semantics

Interpret checklist states strictly:

- `[ ]`: not yet proven complete.
- `[x]`: implemented or deliberately resolved, and verified.
- A checked objective does not automatically check its implementation tasks.
- A checked implementation task does not automatically check its gate.
- Non-goal checkboxes may be checked only after final review confirms the forbidden scope was not introduced.
- A test task is complete only when the test exists and passes.
- A documentation task is complete only when the correct source-of-truth page and manifest state are synchronized.
- A full-suite task is complete only after the full suite passes in the current implementation state.

## Failure and Resume Rules

- If a test fails, keep affected tasks unchecked and fix the failure inside the current stage before moving on.
- Retry a transient tooling failure once with the narrowest corrective action. If it repeats, diagnose that tool directly instead of restarting unrelated verification.
- If the plan conflicts with current architecture, stop only that batch, record the contradiction, and ask the user before changing the approved design.
- If an external or user decision blocks progress, keep the task list active and continue while meaningful work remains elsewhere.
- Report the work as blocked only when the same blocker repeats and no meaningful in-scope work remains, never merely because the work is difficult or large.
- After interruption without compaction, call `TaskList`, read the plan, inspect unchecked items, and resume from the first incomplete stage. After any context compaction, follow the stricter mandatory compaction recovery sequence above.
- Treat the plan file and repository state as authoritative, not memory of an older turn.

## Final Completion Audit

Before completing the task list:

- Re-read the entire plan.
- Confirm every applicable task and gate is `[x]`.
- Confirm conditional and non-goal items have explicit resolutions.
- Confirm all prerequisite plans required by this plan are complete.
- Run the plan's final targeted verification once after the last relevant implementation change, or reuse an identical current-state run already completed by the final stage.
- Run `dotnet test Cerneala.slnx` once in the final code state unless the plan explicitly defines a different final suite. Do not repeat it if it already passed after the last code or project-file change.
- Run final API documentation and manifest checks for public API changes, or reuse results produced after the last relevant API/doc change.
- Run `git diff --check` across all files changed for the plan.
- Review `git diff` for accidental scope, stale debug code, generated churn, and unchecked required work.
- Update plan status to `finalizat`.
- Mark the remaining task(s) `completed` with `TaskUpdate`.
- Report the completed batches, verification results, and plan path.

Do not complete the task list while required work remains. Near-zero budget, a long diff, or being tired of the plan are not definitions of done.

## Invocation Example

```text
/cerneala-implement-plan Implementeaza planul 2026-07-13-repeat-button end-to-end. Pastreaza un batch per etapa, bifeaza etapa dupa verificare si optimizeaza pentru throughput fara verificari redundante.
```
