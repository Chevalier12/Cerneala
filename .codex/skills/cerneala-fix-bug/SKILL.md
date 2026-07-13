---
name: cerneala-fix-bug
description: Reproduce, diagnose, fix, and verify Cerneala defects end-to-end with evidence-first debugging, a focused RED regression test, the smallest architecture-correct production change, CSI or another narrow automated experiment when applicable, and a final green test suite. Use when the user reports broken behavior, a regression, an exception, incorrect rendering, input or layout failures, performance bugs, flaky behavior, or asks to investigate and fix a Cerneala issue rather than implement a new capability.
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
- If CSI is not applicable, use the closest focused automated harness: an existing test fixture or a small repository-native experiment.
- Record the failing observation. Do not accept a theory-only reproduction.

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
2. Repeat the original CSI or focused reproduction and compare the previously failing state.
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
