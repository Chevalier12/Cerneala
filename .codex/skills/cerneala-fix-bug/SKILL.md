---
name: cerneala-fix-bug
description: Reproduce, diagnose, fix, and verify Cerneala defects end-to-end with evidence-first debugging, a focused RED regression test, CSI or a temporary native runtime harness when needed, the smallest architecture-correct production change, and a final green test suite. Use when the user reports broken behavior, a regression, an exception, incorrect rendering, input or layout failures, performance bugs, flaky behavior, or asks to investigate and fix a Cerneala issue rather than implement a new capability.
---

# Cerneala Fix Bug

Fix the reported behavior from reproduction through final verification. Treat the observed failure as the starting fact and theories as disposable until evidence supports them.

## 1. Capture the Contract

- Restate the smallest known reproduction, expected behavior, actual behavior, environment, and frequency.
- Preserve diagnostics supplied by the user: exceptions, traces, screenshots, frame statistics, or custom-view details.
- Ask only when a missing detail prevents a reliable reproduction. Otherwise proceed with the most reasonable interpretation and record it.
- Do not edit production code before the failure and its intended contract are understood.

## 2. Orient in the Repository

- Follow `AGENTS.md` and any narrower repository instructions.
- Generate and read `FileTree.md` before reasoning about structure.
- Use RoslynIndexer as the primary navigation and reading tool. Read a full C# file before editing it.
- Locate the owning contract, callers, tests, documentation, and adjacent state transitions. Do not scan unrelated subsystems for sport.
- Do not modify `AGENTS.md` or RoslynIndexer unless the bug explicitly concerns that tooling.

## 3. Reproduce Before Fixing

- Reproduce the bug with the narrowest deterministic path available.
- For runtime C# behavior, prefer a temporary CSI `.csx` experiment when it can expose the relevant state faster than a throwaway project.
- Run CSI with a short timeout, clean up the script, and check for a stuck `csi` process after suspicious execution.
- If CSI cannot exercise the real window, frame loop, layout, rendering, input, or Motion behavior, use the temporary native runtime harness workflow below.
- Record the failing observation. Do not accept a theory-only reproduction.

### Temporary Native Runtime Harness

Use this workflow for intermittent or hosted-runtime defects that a unit test cannot reproduce faithfully:

1. Prefer an existing automation or probe API. Do not create another driver when the application already exposes deterministic navigation, reports, snapshots, or frame hooks.
2. Create source only under `tests/Codex<Scenario>Harness/`. Never put harness `.cs` files under the repository-level `tmp/` directory: SDK default compile globs can pull them into an unrelated project. Use `tmp/` only for generated reports and other outputs.
3. Keep the project minimal: target the required Windows framework, reference the exact project under test, and add no reusable abstraction unless the harness proves it is needed.
4. Start the real application through its native generated application/startup descriptors. Wait for `ContentRendered` or an equivalent tree-ready signal, then observe frames through `FrameRendered` or the closest real frame hook.
5. Drive named controls through automation peers or the application's public automation API. Reflection is acceptable only inside the temporary harness to reach generated private named fields when no public route exists; use exact member lookup and fail loudly when it is absent.
6. Find unnamed retained elements by traversing `VisualChildren` with a stable semantic predicate such as type plus text prefix. Do not modify production markup merely to make the probe convenient.
7. Capture the values that express the violated invariant, not screenshots alone: `DesiredSize`, `ArrangedBounds`, `Visibility`, dirty state, queue sizes, frame statistics, hit-test/render counts, Motion writes, cache state, or trace entries as applicable.
8. Compute an explicit failure signal. For overflow, for example:

   ```text
   contentBottom = body.Y + body.Height + padding.Bottom
   overflowPixels = contentBottom - (border.Y + border.Height)
   ```

9. Turn randomness into a bounded stress sequence. Repeat the exact transition many times with deterministic short timing variations, a fixed cycle count, and a command timeout. Never use an unbounded loop.
10. Print a concise aggregate such as observed frames, failure count, and maximum delta, plus the first representative failure with enough state to diagnose it. Avoid drowning the useful frame in thousands of trace lines.
11. Save the exact command, cycle count, timing pattern, and pre-fix result. After the fix, run the identical harness and compare the same metrics.

The harness supplies high-fidelity reproduction evidence; it does not replace the permanent RED regression test. Once the owning invariant is known, encode the smallest stable version of that contract in the appropriate test project before changing production code.

### Harness Cleanup

- Close the native window on success, failure, and timeout paths. Surface asynchronous exceptions in the report instead of silently hanging.
- Reindex after creating, modifying, or deleting harness C# or project files, as required by `AGENTS.md`.
- Delete only the exact resolved harness directory and its generated report after verification. Guard the resolved path before recursive deletion.
- Confirm with `git status` that no harness source, project, binaries, or reports remain.
- Never commit the temporary harness unless the user explicitly asks to promote it. If it has lasting diagnostic value, discuss turning it into a permanent test or supported automation probe instead.

## 4. Add a RED Regression Test

- Add the smallest focused test that expresses the violated contract before changing production behavior.
- Run it and confirm it fails for the reported reason, not because the fixture, build, or assertion is broken.
- Prefer public behavior over implementation details unless the contract is intentionally internal.
- If a deterministic automated test is genuinely infeasible, state why and define a diagnostic gate plus a user-validation checklist. Do not claim automated regression coverage that does not exist.

## 5. Diagnose the Root Cause

- Trace the failing state from the public symptom to the owning implementation.
- Distinguish the root cause from collateral symptoms and stale derived state.
- Check neighboring contracts likely to share the same invariant, but do not expand scope without evidence.
- Surface unrelated design smells separately. Do not smuggle cleanup into the fix.

## 6. Implement the Smallest Correct Fix

- Respect existing ownership, architecture, and local patterns.
- Fix the violated invariant without speculative abstractions, broad rewrites, or compatibility theater.
- Preserve unrelated user changes in the working tree.
- Keep public API documentation synchronized when the fix changes public behavior or surface area. Use the repository-mandated API documentation workflow.
- Reindex after every code or project-file modification as required by `AGENTS.md`.

## 7. Verify in Layers

Run verification in this order:

1. Run the RED regression test and confirm it is now green.
2. Repeat the original CSI or runtime harness with the same inputs, cycle count, and timing pattern; compare the previously failing state and aggregate metrics.
3. Run the affected test project or focused test group.
4. Run the complete repository test suite. If another contract regresses, diagnose it, fix the damage, and rerun the full suite until green.

Do not perform or claim manual validation of the real application scenario. That gate belongs to the user. After automated verification, provide concise reproduction steps, the expected corrected result, and any diagnostics the user should inspect. Record the manual result only when the user explicitly reports it.

Do not declare success because the code compiles or one assertion passes. The automated reproduction must be green and the repository must remain healthy. Until the user confirms the real scenario, report its status as awaiting user validation.

## 8. Report Evidence

Summarize:

- the reproduced failure and root cause;
- the focused regression test and its RED-to-GREEN result;
- the production fix;
- CSI or equivalent reproduction results before and after;
- focused and full-suite automated verification;
- the exact user-validation steps and whether the user has confirmed them;
- documentation changes, remaining uncertainty, or limitations.

Do not commit, push, or open a pull request unless the user asks. Never hide a failing test behind optimistic wording.

## Failure Protocol

- If the reproduction cannot be obtained, keep investigating observable state instead of guessing at a fix.
- If the reported expectation conflicts with an existing documented contract, stop and surface the conflict.
- If the full suite cannot run because of an environmental blocker, report the exact command and blocker. Do not call the work fully verified.
- If the proposed fix requires a broader architectural change, explain why the narrow fix is unsafe before expanding scope.
