---
name: cerneala-fix-bug
description: Reproduce, diagnose, fix, and verify Cerneala defects end-to-end with evidence-first debugging, a focused RED regression test, CSI, Servo-driven runtime reproduction, Detective observability, or a native harness when needed, the smallest architecture-correct production change, and a final green test suite. Use when the user reports broken behavior, a regression, an exception, incorrect rendering, input or layout failures, performance bugs, flaky behavior, or asks to investigate and fix a Cerneala issue rather than implement a new capability.
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
- If CSI cannot exercise the real window, frame loop, layout, rendering, input, graphics, or Motion behavior, reproduce through the owning application with Servo and observe it through Detective when the application can host it faithfully. Otherwise use the temporary native runtime harness workflow below.
- Record the failing observation. Do not accept a theory-only reproduction.

### Servo and Detective Runtime Reproduction

Prefer this over a separate harness when the bug belongs to an application view or window that can reproduce itself inside the real application runtime.

1. Add an explicitly opt-in temporary scenario entry point in the view, its code-behind, or the owning window. Guard it with a unique environment variable, command-line switch, or internal test entry point so normal runs remain unchanged.
2. Create `Cerneala.UI.Servo.Servo` for the real `Window` or retained `UiHost`. Use `ServoTarget` queries plus Servo click, hover, drag, scroll, key, text, and wait operations so input traverses the real Cerneala pipeline; use `SaveScreenshotAsync` only from a window-backed Servo. Servo is the exclusive user-interaction path: do not invoke handlers directly or assign control state to imitate input.
3. Prefer an existing stable `Servo.Id`, semantic name, role, or scoped target. If the temporary scenario has no unambiguous semantic selector, add the narrowest opt-in `Servo.Id` needed by the probe and remove it during cleanup; do not reach generated private fields merely to drive input.
4. Inspect the root-owned `UIRoot.Detective` surface before inventing a probe. Capture the relevant built-in frame, input, layout, render, Aspect, Motion, invalidation, resource, or platform evidence. Add temporary instrumentation only for a signal that Detective and external tools cannot expose.
5. Add temporary observations at the narrowest useful boundaries when still needed: event-handler phases, model mutation, invalidation, measure/arrange/render, frame hooks, allocations and GC counts, backend state, or relevant snapshots. Capture component, trigger, and owning-layer evidence separately.
6. Run a bounded scenario, use Servo waits for the observable state actually required, write a concise structured report outside the repository, surface asynchronous failures, and close the window automatically.
7. Keep temporary instrumentation observational. Do not mix a speculative fix into the probe or permanently alter product behavior merely to gather evidence.
8. After capturing the result, remove every temporary branch, `Servo.Id`, probe, environment variable, report, and generated artifact. Re-index after source changes, rebuild, and verify the temporarily instrumented production files have no remaining diff. Permanent Detective additions approved by the policy below are retained and verified instead of cleaned up.

Use a separate native runtime harness instead when direct instrumentation would distort the failing ownership boundary, cannot start the required host/backend, or would require broad product changes.

### Temporary Native Runtime Harness

Use this workflow for intermittent or hosted-runtime defects that a unit test cannot reproduce faithfully:

1. Use public Servo for in-process Cerneala UI queries, input, and waits, use a window-backed Servo for application-owned screenshots, and use the root-owned Detective surface for framework state. Do not create a parallel input or interaction path.
2. Create source only under `tests/Codex<Scenario>Harness/`. Never put harness `.cs` files under the repository-level `tmp/` directory: SDK default compile globs can pull them into an unrelated project. Use `tmp/` only for generated reports and other outputs.
3. Keep the project minimal: target the required Windows framework, reference the exact project under test, and add no reusable abstraction unless the harness proves it is needed.
4. Start the real application through its native generated application/startup descriptors. Wait for `ContentRendered` or an equivalent tree-ready signal, then create Servo for the real window and let Servo operations synchronize through the application frame lifecycle.
5. Drive controls through Servo targets and Servo input operations. Prefer existing IDs or semantic selectors; when none can identify the probe target, add and later remove a unique temporary `Servo.Id` instead of using reflection or visual-tree traversal to fake user interaction.
6. Use Servo queries for observable semantic and live-layout state. When the invariant needs retained framework state that Servo deliberately does not expose, capture it through `UIRoot.Detective`; keep a direct element reference inside the harness only when a Detective element-level capture requires one.
7. Capture the values that express the violated invariant, not screenshots alone: Servo element state and bounds, `DesiredSize`, `ArrangedBounds`, `Visibility`, dirty state, queue sizes, Detective snapshots and traces, frame statistics, hit-test/render counts, Motion writes, cache state, or trace entries as applicable.
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
- Never commit the temporary harness unless the user explicitly asks to promote it. If its observation has lasting framework value, apply the permanent Detective policy below instead of preserving an application-specific harness.

### Permanent Detective Diagnostics

The skill is authorized to add a permanent diagnostic to `Cerneala.UI.Detective` when existing Detective APIs and external tools leave a demonstrated observability gap that is reusable beyond the current one-off scenario.

- Keep the diagnostic owned by `UI/Detective/` and reachable through the root-owned `UIRoot.Detective` model when root context is required. Do not scatter permanent debug hooks across views, controls, or backends merely because that is the easiest observation point.
- Make the diagnostic observational: it may expose a snapshot, trace, counter, formatter, or dumper, but it must not change product behavior, repair the bug, invalidate state, rebuild caches, or become an input backdoor.
- Define the diagnostic contract and add the smallest focused test before implementing it. Cover enablement, bounded retention, reset/lifetime behavior, and hot-path allocation or timing risk when those apply.
- A public or protected addition must use the repository API-documentation workflow, update canonical pages under `docs-site/documentation/classes/`, synchronize the manifest when required, and pass the applicable API-compatibility review. Do not invent public surface solely to make a single harness convenient.
- Prefer disabled-by-default or snapshot-on-demand behavior when continuous collection would add hot-path cost. Measure overhead when performance can be affected; do not claim the diagnostic is cheap without evidence.
- Keep the permanent diagnostic, its tests, and its documentation during cleanup. Remove all temporary callers, switches, reports, and scenario-specific instrumentation around it.

### Repair Servo and Detective When They Fail

Servo and Detective are part of this workflow, not assumed-correct test equipment. If using either subsystem exposes its own contract violation, separate that failure from the reported defect with the smallest independent reproduction.

- When the Servo or Detective contract is established and the reproduction is RED for the subsystem defect, fix it immediately without asking the user for separate permission.
- Add a focused regression test, repair the owning invariant, run the subsystem's focused and affected verification, then repeat the blocked workflow operation and continue the original investigation.
- Do not classify documented behavior, harness misuse, or an unresolved product-semantic question as a subsystem bug. A fix without a known expected contract is guesswork.
- Report the additional Servo or Detective defect, fix, and verification separately from the original bug.

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
- A justified permanent Detective diagnostic is a separate observability deliverable, not the behavioral fix. Keep its contract and verification distinct from the defect regression.
- Preserve unrelated user changes in the working tree.
- Keep public API documentation synchronized when the fix changes public behavior or surface area. Use the repository-mandated API documentation workflow.
- Reindex after every code or project-file modification as required by `AGENTS.md`.

## 7. Verify in Layers

Run verification in this order:

1. Run the RED regression test and confirm it is now green.
2. Repeat the original CSI or runtime harness with the same inputs, cycle count, and timing pattern; compare the previously failing state and aggregate metrics.
3. Run the affected test project or focused test group.
4. Run the complete repository test suite. If another contract regresses, diagnose it, fix the damage, and rerun the full suite until green.

When a permanent Detective diagnostic was added, also run its focused tests, documentation/manifest checks, API compatibility gate, and any measured overhead gate required by its collection model.

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
- any permanent Detective diagnostic added, the evidence gap it closes, its tests and documentation/API result, and its measured or bounded runtime cost.
- any Servo or Detective defect encountered while running the workflow, its independent RED-to-GREEN result, and confirmation that the original operation was rerun.

Do not commit, push, or open a pull request unless the user asks. Never hide a failing test behind optimistic wording.

## Failure Protocol

- If the reproduction cannot be obtained, keep investigating observable state instead of guessing at a fix.
- If the reported expectation conflicts with an existing documented contract, stop and surface the conflict.
- If the full suite cannot run because of an environmental blocker, report the exact command and blocker. Do not call the work fully verified.
- If the proposed fix requires a broader architectural change, explain why the narrow fix is unsafe before expanding scope.
