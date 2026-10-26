---
name: cerneala-implement-plan
description: Implement a named Cerneala Markdown checklist plan end-to-end under a persistent `/goal`, one plan stage per batch, checking off each stage in the plan file immediately after it is verified. Use when the user says to implement a plan completely, continue an existing checklist plan, execute a named plan end-to-end, or check off stages as batches finish. Optimized for throughput — no redundant searches, edits, test runs, or status narration — while modifying code, tests, documentation, and the source plan until every applicable item and gate is complete.
---

# Cerneala Implement Plan

Execute the named checklist plan completely. The plan file is the only durable progress ledger: its checkmarks and status line say what is done. A Codex goal is the cross-turn execution contract. Keep working across stages and turns until the plan is finished or a real blocker needs the user.

Follow the Codex instructions in `.codex/config.toml`, especially "Parent ownership and mandatory Luna exploration" (Luna explorers own all file discovery and searches; the parent reads known paths, edits, builds, tests, and reviews) and "Verification commands" (the focused-test commands). Long-job waits are in "Files, destructive actions, and execution". The steps below say what must happen; "Parent ownership and mandatory Luna exploration" says who does it.

## Start

0. **Worktree (MANDATORY, before anything else):** start in a temporary worktree, as the Codex instructions describe under "Temporary worktree for skills", and work only there: run every later command with the worktree as its working directory, and give Luna explorers the worktree's paths. If the plan file is missing from the worktree, or differs from the main checkout's copy, copy the main checkout's copy into the worktree at the same path. After the final completion audit and its commit, finish the worktree the same way. If Windows then refuses to delete the empty worktree folder ("being used by another process") and `git worktree list` no longer shows it, leave the folder and report its path.
   - **The plan's README row:** when the main checkout's `docs/plans/README.md` has an uncommitted row for this plan, copy that one row into the worktree's README. Copy no other uncommitted row; those rows belong to the user's other plans.
   - **Index plans stay uncommitted in the main checkout** (user decision, 2026-10-25). An index plan only lists other plans, for example `2026-10-25-scene2d-autonomie-index.md`. Do not copy it into the worktree, commit it, or add its README row, even when the plan links it. The committed plan keeps its link, and its README note says that the index is missing, for example: "Plan 1 of the index `2026-10-25-scene2d-autonomie-index.md`, which is not in the repository." When the work changes what the index records, such as its status line or a decision, update the main checkout's copy directly, as the Codex instructions describe under "Files that must outlive the worktree", after saving the old copy in `<temp-dir>` (a temporary folder outside the repository). Say so in the final report.
1. Resolve the requested plan to one exact Markdown file. Resolve a bare name against `docs/plans/` first; have a Luna explorer search for it when the path is not known.
2. If several files match, ask the user which one as a blocking question in the final message; each option names one file and the first unchecked stage it would start with (a concrete example of what changes). Do not pick one.
3. Tell the user in one short message, in their language:
   - which plan file
   - which stage you start with
   - how many stages remain

The user's invocation of this skill authorizes implementing, verifying, documenting, and checking off the whole plan. It also authorizes creating the goal below, and running `$git-session-commit-push` once, at the end of the final completion audit.

## Start the Goal

1. Call `get_goal` before creating a goal.
2. If no unfinished goal exists, call `create_goal` with a concrete objective containing:
   - the resolved plan path and the worktree path;
   - the requirement to implement, verify, document, and check off the plan end-to-end;
   - the explicit requirement that, after every context compaction, Codex must re-read this complete `cerneala-implement-plan` skill from disk before resuming any work.
3. Do not set `token_budget` unless the user explicitly requested a budget.
4. If the active goal already targets the same plan, continue it instead of creating a duplicate.
5. If a different unfinished goal is active, do not falsely complete or replace it. Report the conflict and request that the user pause, cancel, or redirect the existing goal.
6. Keep the goal active across batches and turns. Call `update_goal(status: "complete")` only after the plan is genuinely finished (see "Final Completion Audit").

## Compaction Recovery (MANDATORY)

After every context compaction, re-read this complete `.codex/skills/cerneala-implement-plan/SKILL.md` file from disk. This is mandatory even when the compacted summary appears to contain all skill instructions or a reliable workflow recap.

Before any search, edit, test, checklist update, or stage-completion claim after a compaction:

1. Read this complete file from disk, not only this recovery section.
2. Call `get_goal` and recover the exact plan path and the worktree path from the active goal (and from the compaction summary). Enforce this recovery sequence for an older matching goal even if its objective predates the mandatory compaction wording.
3. Re-read the complete plan.
4. Rebuild the current stage from the plan's checkmarks and the repository state, not from memory.
5. Continue from "Resolve and Audit the Plan".

A compaction summary is never a substitute for this skill or for the plan file.

## Resolve and Audit the Plan

- When you do not know the structure of the area the plan touches, run `.\Tools\scripts\New-FileTree.ps1` and wait for it to finish, then read `FileTree.md` once for orientation (the generate-then-read sequence of "Parent ownership and mandatory Luna exploration").
- Read the complete plan and identify:
  - already checked work;
  - unchecked actionable items;
  - unchecked gates;
  - dependencies on other plans;
  - optional or conditional items;
  - public API and documentation obligations;
  - verification and stop conditions.
- Do not trust old baseline descriptions; the repository may have moved on. Have Luna explorers answer the "where does this live now / who calls it" questions for the active stage (they use Roslyn MCP and `rg`), in parallel with distinct bounded scopes when the questions are independent. Then read the decisive lines yourself.
- Roslyn MCP answers describe the main checkout, not your worktree: `.codex/config.toml` starts `Tools/scripts/Start-RoslynMcp.ps1` with `cwd` set to the main checkout, so it serves that checkout's `Cerneala.slnx`. Unless you switch it, Roslyn does not see the worktree's edits. Check code you changed by reading the files in the worktree and by having Luna explorers search the worktree's paths, or switch the server with `reload_workspace` and `solutionPath` set to the worktree's `Cerneala.slnx`; that loads the whole solution again (about 30–60 s). It returns the path it loaded; check that path, and switch back before you remove the worktree.
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
- In `update_plan`, keep only the current stage `in_progress`; later stages remain `pending`.
- If the stage cannot be completed, stop on it. Leave its unfinished items unchecked and do not jump to easier work in another stage.

Exploration may look at future stages for dependency awareness. Implementation, tests, and edits stay inside the active stage.

## Throughput-First Execution (MANDATORY)

Optimize elapsed time and tool round-trips after correctness, repository policy, and stage boundaries are satisfied. Throughput means less redundant work, never weaker gates.

Default shape per stage: one consolidated audit, one implementation pass, one verification ladder, and one mandatory checklist checkpoint.

- Build one work map for the active stage, then execute it. Do not rediscover scope before every task.
- Put independent tool calls in one message when the tool surface supports safe parallel execution: independent reads, status checks, and Luna explorer assignments.
- Read the complete content only of C# files you will edit (repository policy). For already-understood supporting files, use targeted reads of known paths or Luna's answers.
- Make related edits with as few `apply_patch` calls as stay readable. Do not rewrite whole files to change a few lines.
- Implement the whole stage before its verification ladder, except where the plan requires RED/GREEN test-first order.
- Choose the verification ladder once:
  - compile or a narrow test during development;
  - the required stage tests at the gate;
  - never the complete suite: where the stage or the final audit asks for it, run the focused tests for all code the plan changed instead (see "Verify").
- A successful verification stays valid until a later change touches code, project configuration, generated inputs, or another surface that can affect it. Documentation and checklist edits do not invalidate compiled test evidence.
- After a successful build of the current code state, use `--no-build` for compatible follow-up test commands. Never use it after unbuilt code or project changes.
- Do not rerun a green command for reassurance. Reuse current-state evidence and record it at the checkpoint.
- Run anything longer than about 2 minutes as a long job and wait with the native completion wait on the yielded session ("Files, destructive actions, and execution"); keep working on non-dependent steps while it runs. No sleep loops, no short polling.
- Message the user only at stage start, stage completion, or when a real failure or blocker changes the plan. Do not narrate searches, edits, or passing micro-tests. During a long silent step, such as a long test run, one short status line is fine.
- Do not add speculative tests, experiments, abstractions, or documentation audits beyond what closes a real checklist item or gate.

## Execute a Batch

### 1. Revalidate scope

- Do one consolidated scope pass for the active stage: Luna explorers search, using Roslyn MCP for C# references when it is connected (see "Resolve and Audit the Plan" for which checkout it serves).
- Read every C# file completely before editing it. Use partial reads only after full context is known.
- Inspect only the definitions, references, tests, and public docs plausibly affected by the stage.
- Preserve user changes and unrelated dirty-worktree changes.
- Do not expand into non-goals or unrelated cleanup. Report unrelated smells separately, without fixing them.
- If you find a real Cerneala defect outside the plan, invoke `$github-issue-raise` for it and continue the stage. Do not fix it inside the plan.

### 2. Implement

- Follow the plan's test-first order when specified. A RED test must fail for the intended reason before the production change. If it passes before the change, find out why before you go on: often a fixture default already produces the behavior. Example: `RenderSurface2D.RedrawMode` defaults to `Continuous`, and then the surface asks for a frame at every pump (`UI/Controls/RenderSurface2D.cs`). A test that pending work keeps `PumpOnce` rendering passes anyway, so it needs `RedrawMode = OnDemand`.
- Use `apply_patch` for manual edits.
- Prefer current Cerneala patterns, simple ownership, and explicit lifecycle handling.
- Keep public API changes exactly within the approved plan.
- If a public API changes, update `docs-site/documentation/classes/` in the same batch with `$writing-api-documentation`. This is mandatory current-stage collateral, not permission to start a later documentation stage.
- Update `docs-site/documentation/manifest.json` when API pages are added or renamed.
- Every behavior sentence in canonical or architecture docs needs a test or a source line that shows it. This includes facts in your own brief to a Luna explorer and facts in its answers: check them in the source first, because a wrong brief or a wrong summary becomes wrong documentation.
- When the stage moves lines in a file that the docs cite by line, have a Luna explorer search `docs/` and `docs-site/` for that file name followed by `:` (for example `UiHost.cs:`), and fix the citations your change moved. Citations that were already wrong before your change are not yours: list them in the final report.
- Complete every implementation and test task of the active stage before its gate.

### 3. Verify

- Run the narrowest tests that prove the stage behavior, then the related regression tests the plan names.
- Run formatting, build, source-generator, API, visual, or benchmark verification when the stage requires it, with the commands and prerequisites in `.codex/config.toml` under "Verification commands".
- Tests are focused only: each test project that covers the changed code, with a `--filter`, as the Codex instructions describe under "Verification commands" ("Focused tests"). Never run the complete suite (`dotnet test Cerneala.slnx`, or a test project without `--filter`), even when the plan asks for it: the user chose focused tests only. A plan item that requires the full suite is satisfied by the focused tests for all code the plan changed.
- A filter that matches no test prints `No test matches the given testcase filter` and still exits with code 0. Count a run only when it prints `Passed!` with `Total` above 0.
- Filter by class or method names, not file names. Many files in `tests/Cerneala.Tests.SourceGen`, such as `UiMarkupGeneratorColliderOwnershipTests.cs`, declare `partial class UiMarkupGeneratorTests`, so `FullyQualifiedName~UiMarkupGeneratorColliderOwnershipTests` matches no test.
- An empty result proves something only when the check can find something. Before you trust "no match" or "no output" from a new search pattern, glob, or check (a Luna explorer's: the parent runs no searches), run it once on an input that must match (a positive control).
- Run each required verification once in the latest relevant code state. Reuse the result at the gate and the final audit when nothing relevant changed afterwards.
- When a narrow test fails, fix it and rerun that failing subset first, before the stage's other focused tests.
- Investigate failures instead of checking the task and calling them "probably unrelated". An environmental cause needs evidence, for example the same failure on unchanged `master`.
- Never mark a task complete only because code was written. It must satisfy its stated verification.

### 4. Check off the stage immediately (MANDATORY)

After the entire active stage passes, the plan update is a blocking part of the batch. Do not run any implementation command for the next stage until every step below is done:

- Edit the source plan with `apply_patch`.
- Change `[ ]` to `[x]` for every active-stage item implemented and verified in this batch.
- Mark an active-stage gate `[x]` only when all its conditions are proven.
- A conditional task that was evaluated and found unnecessary gets `[x]` plus a short reason, such as `(Nu a fost necesar: ...)`.
- Leave blocked or unfinished work unchecked.
- Never check tasks from another stage. Preserve checkmarks that already existed, and do not rewrite historical checked items unless repository evidence proves them false.
- When the plan tracks status, set it to `in progres` after the first completed batch. Set `finalizat` only after the final audit.
- Run `git diff --check` on the files touched by the batch. It skips new, untracked files, so check each new file with `git diff --no-index --check /dev/null <file>`, one plain command per file. Exit code 0 or 1 with no `<file>:<line>:` message means clean; exit code 3 means a whitespace error, printed like `<file>:2: trailing whitespace.`. A `warning: ... LF will be replaced by CRLF` line is not an error.
- Write into the plan's `## Note de lucru` section, at the end of the plan (create it when it is missing): each deviation from the plan, each decision with its reason, and each known limitation. These notes survive compaction, and the pull request takes its known limitations from them.
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
- A task that asks for the full suite is complete when the focused tests for all code the plan changed pass in the current implementation state. Check it with a short note naming the filters, such as `(Doar teste focusate: FullyQualifiedName~GridTests)`.

## Failure and Resume Rules

- If a test fails, keep the affected tasks unchecked and fix the failure inside the current stage before moving on.
- Retry a transient tooling failure once with the narrowest corrective action. If it repeats, diagnose that tool directly instead of restarting unrelated verification.
- If the plan conflicts with current architecture, stop that batch, record the contradiction, and ask the user before changing the approved design.
- If a user decision blocks the current stage, ask the user as a blocking question in the final message, with a concrete example per option of what changes. Do not skip to a later stage while waiting. Keep the goal active; mark it `blocked` only under the goal tool's repeated-blocker rules, never merely because the work is difficult or large.
- After an interruption, call `get_goal`, read the plan, inspect unchecked items, and resume from the first incomplete stage. After a compaction, follow the mandatory recovery sequence above.
- If a blocker stops the plan before the final audit, do not invoke `$git-session-commit-push`. Report the completed stages and the blocker, and ask the user whether to commit the verified work anyway (a blocking question, with a concrete example per option).
- The plan file and repository state are authoritative, not memory of an older turn.

## Final Completion Audit

Before declaring the plan done:

- Re-read the entire plan.
- Confirm every applicable task and gate is `[x]`, conditional and non-goal items have explicit resolutions, and required prerequisite plans are complete.
- Run the plan's final targeted verification once after the last relevant implementation change, or reuse an identical current-state run from the final stage.
- Run the focused tests for all code the plan changed once in the final code state, each with a `--filter`. Never run the complete suite, even when the plan defines a final full suite. Do not repeat them if they already passed after the last code or project change.
  - Find every test project that covers the change: have Luna explorers search `tests/` for the changed public types and members, and include each project that uses them, not only the projects the plan names.
  - Run the projects one after another in one long-job script that prints, for each project, its exit code and its `Passed!`, `Failed!`, and `No test matches` lines.
- Run final API documentation and manifest checks for public API changes, or reuse results produced after the last relevant change. The manifest check is `ApiDocumentationManifestIsValidAndReferencesExistingFiles` in `tests/Cerneala.Tests.VisualStudio` (`Stage6ReleaseHarnessTests.cs`). In a fresh worktree, also run `dotnet restore Cerneala.VisualStudio/Cerneala.VisualStudio.csproj` before it: without that restore, the test failed with `NETSDK1004` for `Cerneala.VisualStudio/obj/project.assets.json` (2026-10-25).
- Run `git diff --check` across all changed files, and the new-file check from step 4 on every new file.
- Review `git diff` for accidental scope, leftover debug code, generated churn, and unchecked required work.
- Set the plan status to `finalizat`.
- Update or add the plan's row in `docs/plans/README.md`: its real status and its checked / open counts come from counting the plan's `- [x]` and `- [ ]` boxes, not from the plan's header.
- Invoke `$git-session-commit-push` (MANDATORY). Pass it:
  - the plan path and the completed stages;
  - the files to commit: the plan and its README row, never an index plan (see "Start");
  - the final verification results with real counts. Copy each count and its test project from the raw `Passed!` line, which ends with the test DLL, for example `- Cerneala.Tests.VisualStudio.dll (net8.0)`. Do not write them from memory: a commit message once listed a `Cerneala.Tests.VisualStudio` test under `Cerneala.Tests`;
  - any gate that still needs a human, the pending issue drafts, and the known limitations from `## Note de lucru`.

  If a required final gate failed or could not run, do not invoke it; report the blocker and ask the user whether to commit anyway (a blocking question, with a concrete example per option).
- Finish the worktree as step 0 says.
- Call `update_goal(status: "complete")` only now, when the plan is genuinely finished. Do not call it while required work remains or a required final gate failed or could not run.
- Report in the user's language:
  - the completed stages
  - the verification results, with real counts, and that the complete suite was not run
  - the plan path
  - the commit, the branch, and the pull-request URL, or the gate that blocked them
  - any gate that still needs a human, for example a visual check
  - the pending issue drafts (path and title) and the known limitations
  - the main-checkout files that will block the user's next `git pull` of `master` after the merge, because the merge changes them: the plan's old untracked copy, and `docs/plans/README.md` when it has uncommitted changes
  - the final token usage returned by the goal tool

Do not declare the plan done, and do not complete the goal, while required work remains. Near-zero budget, a long diff, or a tiring plan is not a definition of done.

## Invocation Example

```text
$cerneala-implement-plan Implementeaza planul 2026-07-13-repeat-button end-to-end. Pastreaza un batch per etapa, bifeaza etapa dupa verificare si optimizeaza pentru throughput fara verificari redundante.
```
