---
name: cerneala-implement-plan
description: Implement a named Cerneala Markdown checklist plan end-to-end, one plan stage per batch, checking off each stage in the plan file immediately after it is verified. Use when the user says to implement a plan completely, continue an existing checklist plan, execute a named plan end-to-end, or check off stages as batches finish. Optimized for throughput — no redundant searches, edits, test runs, or status narration — while modifying code, tests, documentation, and the source plan until every applicable item and gate is complete.
---

# Cerneala Implement Plan

Execute the named checklist plan completely. The plan file is the only durable progress ledger: its checkmarks and status line say what is done. Keep working across stages and turns until the plan is finished or a real blocker needs the user.

Follow `CLAUDE.md`, especially "Claude Code tooling and verification commands". It covers Haiku explorers for discovery, Roslyn MCP, `Edit`, background runs, and the full-suite commands.

## Start

1. Resolve the requested plan to one exact Markdown file. Resolve a bare name against `docs/plans/` first; delegate the lookup to a Haiku explorer when the path is not known.
2. If several files match, ask the user which one (with `AskUserQuestion`). Do not pick one.
3. Tell the user in one short message, in their language:
   - which plan file
   - which stage you start with
   - how many stages remain

The user's invocation of this skill authorizes implementing, verifying, documenting, and checking off the whole plan. It does not authorize commits, pushes, or PRs.

## Compaction Recovery (MANDATORY)

After every context compaction, before any search, edit, test, checklist update, or stage-completion claim:

1. Read this complete file from disk: `.claude/skills/cerneala-implement-plan/SKILL.md`.
2. Recover the plan path from the compaction summary.
3. Re-read the complete plan.
4. Rebuild the current stage from the plan's checkmarks and the repository state, not from memory.
5. Continue from "Resolve and Audit the Plan".

A compaction summary is never a substitute for this skill or for the plan file.

## Resolve and Audit the Plan

- Run `.\Tools\scripts\New-FileTree.ps1`, then read `FileTree.md` once for orientation. This is the fixed orientation exception in `CLAUDE.md`.
- Read the complete plan and identify:
  - already checked work;
  - unchecked actionable items;
  - unchecked gates;
  - dependencies on other plans;
  - optional or conditional items;
  - public API and documentation obligations;
  - verification and stop conditions.
- Do not trust old baseline descriptions; the repository may have moved on. Delegate the "where does this live now / who calls it" questions for the active stage to Haiku explorers, in parallel when independent. Then read the decisive files yourself.
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
- Put independent tool calls in one message: parallel explorer assignments, independent reads, independent status checks.
- Read the complete content only of C# files you will edit (repository policy). For already-understood supporting files, use targeted reads, Roslyn symbol queries, or explorer answers.
- Make related edits with as few `Edit` calls as stay readable. Do not rewrite whole files to change a few lines.
- Implement the whole stage before its verification ladder, except where the plan requires RED/GREEN test-first order.
- Choose the verification ladder once:
  - compile or a narrow test during development;
  - the required stage tests at the gate;
  - the full suite only when the stage or the final audit requires it.
- A successful verification stays valid until a later change touches code, project configuration, generated inputs, or another surface that can affect it. Documentation and checklist edits do not invalidate compiled test evidence.
- After a successful build of the current code state, use `--no-build` for compatible follow-up test commands. Never use it after unbuilt code or project changes.
- Do not rerun a green command for reassurance. Reuse current-state evidence and record it at the checkpoint.
- Run anything longer than about 2 minutes in the background and keep working on non-dependent steps while it runs.
- Message the user only at stage start, stage completion, or when a real failure or blocker changes the plan. Do not narrate searches, edits, or passing micro-tests.
- Do not add speculative tests, experiments, abstractions, or documentation audits beyond what closes a real checklist item or gate.

## Execute a Batch

### 1. Revalidate scope

- Do one consolidated scope pass for the active stage. Delegate discovery to explorers, and use Roslyn MCP for C# references when connected.
- Read every C# file completely before editing it. Use partial reads only after full context is known.
- Inspect only the definitions, references, tests, and public docs plausibly affected by the stage.
- Preserve user changes and unrelated dirty-worktree changes.
- Do not expand into non-goals or unrelated cleanup. Report unrelated smells separately, without fixing them.
- If you find a real Cerneala defect outside the plan, invoke `github-issue-raise` for it and continue the stage. Do not fix it inside the plan.

### 2. Implement

- Follow the plan's test-first order when specified. A RED test must fail for the intended reason before the production change.
- Prefer current Cerneala patterns, simple ownership, and explicit lifecycle handling.
- Keep public API changes exactly within the approved plan.
- If a public API changes, update `docs-site/documentation/classes/` in the same batch with the `writing-api-documentation` skill. This is mandatory current-stage collateral, not permission to start a later documentation stage.
- Update `docs-site/documentation/manifest.json` when API pages are added or renamed.
- Complete every implementation and test task of the active stage before its gate.

### 3. Verify

- Run the narrowest tests that prove the stage behavior, then the related regression tests the plan names.
- Run formatting, build, source-generator, API, visual, benchmark, or full-suite verification when the stage requires it. Use the commands and prerequisites in `CLAUDE.md`, and run the full suite in the background.
- Run each required verification once in the latest relevant code state. Reuse the result at the gate and the final audit when nothing relevant changed afterwards.
- When a narrow test fails, fix it and rerun that failing subset first. Do not pay for the full suite while diagnosing a local failure.
- Investigate failures instead of checking the task and calling them "probably unrelated". An environmental cause needs evidence, for example the same failure on unchanged `master`.
- Never mark a task complete only because code was written. It must satisfy its stated verification.

### 4. Check off the stage immediately (MANDATORY)

After the entire active stage passes, the plan update is a blocking part of the batch. Do not run any implementation command for the next stage until every step below is done:

- Edit the source plan with `Edit`.
- Change `[ ]` to `[x]` for every active-stage item implemented and verified in this batch.
- Mark an active-stage gate `[x]` only when all its conditions are proven.
- A conditional task that was evaluated and found unnecessary gets `[x]` plus a short reason, such as `(Nu a fost necesar: ...)`.
- Leave blocked or unfinished work unchecked.
- Never check tasks from another stage. Preserve checkmarks that already existed, and do not rewrite historical checked items unless repository evidence proves them false.
- When the plan tracks status, set it to `in progres` after the first completed batch. Set `finalizat` only after the final audit.
- Run `git diff --check` on the files touched by the batch.
- Re-read the entire updated stage section. Confirm that completed items and gates are checked, unfinished ones are not, and no neighboring stage changed.

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
- A full-suite task is complete only after the full suite passes in the current implementation state.

## Failure and Resume Rules

- If a test fails, keep the affected tasks unchecked and fix the failure inside the current stage before moving on.
- Retry a transient tooling failure once with the narrowest corrective action. If it repeats, diagnose that tool directly instead of restarting unrelated verification.
- If the plan conflicts with current architecture, stop that batch, record the contradiction, and ask the user before changing the approved design.
- If a user decision blocks the current stage, ask with `AskUserQuestion` (concrete example per option). Do not skip to a later stage while waiting.
- After an interruption, read the plan, inspect unchecked items, and resume from the first incomplete stage. After a compaction, follow the mandatory recovery sequence above.
- The plan file and repository state are authoritative, not memory of an older turn.

## Final Completion Audit

Before declaring the plan done:

- Re-read the entire plan.
- Confirm every applicable task and gate is `[x]`, conditional and non-goal items have explicit resolutions, and required prerequisite plans are complete.
- Run the plan's final targeted verification once after the last relevant implementation change, or reuse an identical current-state run from the final stage.
- Run the full suite once in the final code state, unless the plan defines a different final suite. Use the `CLAUDE.md` command and run it in the background. Do not repeat it if it already passed after the last code or project change.
- Run final API documentation and manifest checks for public API changes, or reuse results produced after the last relevant change.
- Run `git diff --check` across all changed files.
- Review `git diff` for accidental scope, leftover debug code, generated churn, and unchecked required work.
- Set the plan status to `finalizat`.
- Report in the user's language:
  - the completed stages
  - the verification results, with real counts
  - the plan path
  - any gate that still needs a human, for example a visual check

Do not declare the plan done while required work remains. A long diff or a tiring plan is not a definition of done.

## Invocation Example

```text
/cerneala-implement-plan Implementeaza planul 2026-07-13-repeat-button end-to-end. Pastreaza un batch per etapa, bifeaza etapa dupa verificare si optimizeaza pentru throughput fara verificari redundante.
```
