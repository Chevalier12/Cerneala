---
name: cerneala-implement-plan
description: Implement a named Cerneala Markdown checklist plan end-to-end under a persistent `/goal`, executing the remaining work in coherent batches and marking `[x]` only after each batch is implemented and verified. Use when the user says to implement a plan completely, continue an existing checklist plan, execute a named plan end-to-end, or check off tasks as batches finish. This skill modifies code, tests, documentation, and the source plan until every applicable item and gate is complete.
---

# Cerneala Implement Plan

Execute the named checklist plan completely. Use the plan file as the durable progress ledger and a Codex goal as the cross-turn execution contract.

## Start the Goal

1. Resolve the requested plan to one exact Markdown file.
2. Call `get_goal` before creating a goal.
3. If no unfinished goal exists, call `create_goal` with a concrete objective containing the resolved plan path and the requirement to implement, verify, document, and check off the plan end-to-end.
4. Do not set `token_budget` unless the user explicitly requested a budget.
5. If the active goal already targets the same plan, continue it instead of creating a duplicate.
6. If a different unfinished goal is active, do not falsely complete or replace it. Report the conflict and request that the user pause, cancel, or redirect the existing goal.
7. Keep the goal active across batches and turns. Call `update_goal(status: "complete")` only after the plan is genuinely finished.

The user's invocation of this skill is explicit authorization to create the goal.

## Resolve and Audit the Plan

- Follow the repository `AGENTS.md` before inspecting or editing anything.
- Run `Tools/scripts/New-FileTree.ps1`, then read `FileTree.md`.
- Use RoslynRepoIndexer as the primary repository search and navigation tool.
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

## Build Batches

Treat a batch as the smallest coherent group that can be implemented and verified without leaving the repository in a deliberately broken state.

- Default to one plan stage per batch.
- Split a large stage when it contains independently verifiable foundations and integrations.
- Combine tiny adjacent stages only when their intermediate state would not compile or cannot be tested meaningfully.
- Preserve dependency order from the plan.
- At the beginning of a batch, identify the exact unchecked items and gate conditions it intends to close.
- Use `update_plan` for the current execution outline when several steps are involved, but keep the Markdown checklist as the source of truth.
- Continue to the next batch automatically after verification unless the user pauses or redirects the work.

## Execute a Batch

### 1. Revalidate scope

- Read every C# file before editing it with `ri read <filePath>`.
- Use `ri pread` only after full context is known.
- Inspect definitions, references, tests, and public docs affected by the batch.
- Preserve user changes and unrelated dirty worktree changes.
- Do not expand into non-goals or unrelated cleanup.
- Surface unrelated smells separately without fixing them.

### 2. Implement

- Follow the plan's test-first order when specified.
- Use `apply_patch` for manual edits.
- Prefer current Cerneala patterns, simple ownership, and explicit lifecycle handling.
- Keep public API changes exactly within the approved plan.
- After every code or project-file modification, run:

```powershell
dotnet run --no-build --project .\Tools\RoslynRepoIndexer\src\RoslynRepoIndexer.Cli\RoslynRepoIndexer.Cli.csproj -- index .\Cerneala.slnx --json
```

- If a public API changes, update `docs-site/documentation/classes/` in the same batch using `writing-api-documentation`.
- Update `docs-site/documentation/manifest.json` when API pages are added or renamed.
- Do not postpone required tests or docs to a fictional cleanup batch unless the source plan explicitly orders them later.

### 3. Verify

- Run the narrowest tests that prove the batch behavior.
- Run related regression tests named by the plan.
- Run formatting, build, source-generator, API-diff, visual, benchmark, or full-suite verification when required by the batch.
- Investigate failures instead of checking the task and writing "probably unrelated" like an optimistic arsonist.
- Never mark a task complete only because code was written. It must satisfy its stated verification.

### 4. Update the checklist immediately

After the batch passes:

- Edit the source plan with `apply_patch`.
- Change `[ ]` to `[x]` only for items implemented and verified by this batch.
- Mark a gate `[x]` only when every condition under that gate is proven.
- For a conditional task that was evaluated and correctly found unnecessary, mark it `[x]` and append a short reason such as `(Nu a fost necesar: ...)`.
- Leave blocked or unfinished work unchecked.
- Do not rewrite historical checked items unless repository evidence proves they are false.
- Change plan status to `in progres` after the first completed batch when the plan tracks status.
- Change status to `finalizat` only after all applicable tasks, gates, documentation, and final verification are complete.
- Run `git diff --check` for the files touched by the batch.
- Re-read the updated checklist section to confirm no item was checked accidentally.

### 5. Report and continue

- Send a short commentary update naming the completed batch, verification run, and newly checked items.
- Continue with the next unchecked batch in the same turn when feasible.
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

- If a test fails, keep affected tasks unchecked and fix the failure before moving on.
- If the plan conflicts with current architecture, stop only that batch, record the contradiction, and ask the user before changing the approved design.
- If an external or user decision blocks progress, keep the goal active while meaningful work remains elsewhere.
- Mark the goal `blocked` only under the goal tool's repeated-blocker rules, never merely because the work is difficult or large.
- After interruption or context compaction, call `get_goal`, read the plan, inspect unchecked items, and resume from the first incomplete dependency.
- Treat the plan file and repository state as authoritative, not memory of an older turn.

## Final Completion Audit

Before completing the goal:

- Re-read the entire plan.
- Confirm every applicable task and gate is `[x]`.
- Confirm conditional and non-goal items have explicit resolutions.
- Confirm all prerequisite plans required by this plan are complete.
- Run the plan's final targeted verification.
- Run `dotnet test Cerneala.slnx` unless the plan explicitly defines a different final suite.
- Run final API documentation and manifest checks for public API changes.
- Run `git diff --check` across all files changed for the goal.
- Review `git diff` for accidental scope, stale debug code, generated churn, and unchecked required work.
- Update plan status to `finalizat`.
- Reindex the final repository state when code or project files changed.
- Call `update_goal(status: "complete")`.
- Report the completed batches, verification results, plan path, and final token usage returned by the goal tool.

Do not complete the goal while required work remains. Near-zero budget, a long diff, or being tired of the plan are not definitions of done.

## Invocation Example

```text
$cerneala-implement-plan Implementeaza planul 2026-07-13-repeat-button end-to-end. Bifeaza tot ce termini dupa fiecare batch verificat.
```
