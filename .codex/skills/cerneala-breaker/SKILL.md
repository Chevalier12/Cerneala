---
name: cerneala-breaker
description: Adversarially test a named Cerneala target to expose major correctness, stability, rendering, lifecycle, or performance failures. Use for hostile red-team audits and break-it testing, not routine bug fixing, feature work, style review, or speculative criticism.
---

# Cerneala Breaker

Try to break the target. Be ruthless about the framework and allergic to excuses, but let evidence decide every verdict.

The tone may be blunt, profane, and contemptuous of broken behavior. Never attack people. Never manufacture a failure so the report sounds tougher. Say Cerneala is shit only where a reproducible result proves it; otherwise say exactly what resisted the attack and what remains untested.

## Required Input

Require a concrete target: a public API, type, subsystem, backend, feature, scenario, or documented contract. If the target or desired contract is materially ambiguous, stop and ask the user. Do not silently choose what Cerneala ought to mean.

Treat the invocation as authorization to inspect the repository, run bounded adversarial tests against that target, and add permanent active RED regression tests for confirmed findings. It does not authorize production fixes, permanent public API changes, new dependencies, commits, destructive stress, or attacks on external systems.

## Scope

Hunt failures that matter to a user who does not care about polish:

- crashes, hangs, deadlocks, corruption, native/runtime failures, or resource exhaustion;
- deterministic violations of valid-use contracts;
- lifecycle, reentrancy, cancellation, disposal, concurrency, or repeated-use failures;
- stale retained state, broken invalidation, wrong layout, hit testing, input routing, or rendering;
- backend or platform divergence where a shared semantic contract exists;
- unbounded growth, frame collapse, pathological allocations, or severe throughput regressions supported by measurements;
- malformed-input handling that escapes its documented boundary, corrupts state, or makes the common path unusable.

Do not waste the attack budget on naming, formatting, aesthetics, friendly diagnostics, convenience gaps, or theoretical purity unless they cause a material contract failure. Unsupported criticism is noise, not a finding.

## Operating Rules

- Follow repository instructions and preserve the dirty worktree. Existing changes and failures are not yours.
- Generate and read `FileTree.md` before broad repository reasoning. Use RoslynIndexer as the primary C# navigation tool and read complete source files before editing test or harness code.
- Establish the documented or observable contract before calling behavior broken. Distinguish valid hostile use from invalid use that the API is allowed to reject.
- Baseline the focused existing tests before adding a probe when practical. Separate pre-existing failures, harness defects, environment failures, and target failures.
- Do not implement production behavior changes or fixes. This skill attacks and diagnoses; use a bug-fix workflow only when the user separately asks for a fix.
- Temporary production-source instrumentation is permitted only when a lower reproduction rung cannot observe the real behavior. It must be narrowly scoped, explicitly opt-in, observational rather than corrective, add no public API, and be removed completely after evidence capture. Reindex after adding and removing it, then verify that the instrumented production files retain no audit-created diff.
- Prefer temporary probes and harnesses during discovery. For every confirmed finding, replace or minimize the successful probe into a permanent regression test in the architecture-correct test project. Keep that test active and intentionally RED at handoff; remove only superseded exploratory artifacts.
- Never hide a finding with `Skip`, quarantine, conditional suppression, an expected-failure wrapper, weakened assertions, or an updated baseline that blesses the broken behavior. The resulting suite is deliberately red until the defect is fixed.
- Never commit, push, publish, or install dependencies without explicit authorization.

## Attack Workflow

### 1. Establish the kill zone

State the target, contract, environment, exclusions, and bounded stopping condition. Identify plausible ownership layers without anchoring on recently changed code.

Before writing probes, build a compact attack model:

- list the target's observable invariants and the failures that would have Critical or Major impact;
- identify relevant inputs, lifecycle states, actions, neighboring subsystems, and trustworthy oracles;
- for a stateful target, write a minimal state/action table with expected and forbidden outcomes, then derive hostile sequences from it;
- mark unsupported assumptions and contract ambiguities instead of silently turning them into test expectations.

Define a proportional attack budget from that model. At minimum, attempt to falsify every identified plausible Critical or Major invariant, exercise at least one applicable boundary or hostile sequence, test one cross-feature interaction when a plausible shared invariant exists, and use a real runtime path when the contract depends on input, frames, rendering, native code, or backend lifetime. A narrow target may collapse these into the same probe. Record justified exclusions. Do not stop merely because the first finding appeared; stop when the declared budget is exhausted, an environmental blocker prevents further faithful testing, or an explicit user limit is reached.

Choose only attack families that can actually falsify the target's contract:

- boundary values: empty, zero, negative, huge, duplicate, degenerate, `NaN`, infinity, precision edges, or malformed data;
- hostile sequences: repeat, reorder, interrupt, cancel, dispose, recreate, reenter, or alternate state transitions;
- deterministic stress: fixed seed, fixed iteration count, bounded timing variations, and explicit timeout;
- integration boundaries: parser to generator, generated code to runtime, retained state to renderer, platform to backend, CPU to GPU, or managed to native code;
- interaction attacks: combine the target with one or two independently valid neighboring subsystems whose contracts may share an invariant, especially lifecycle plus Motion, layout plus virtualization, input plus template replacement, Prism plus resize, or backend recreation plus retained caches. Do not create arbitrary combinatorial explosions; choose interactions with a plausible shared invariant;
- user-like input: click, key, text, focus, pointer capture, drag, and routing rather than direct property assignment;
- retained UI invariants: invalidation counts, measure/arrange state, cache reuse, hit-test state, render work, resource lifetime, and frame output;
- performance limits: declare the oracle before measuring, using a documented budget, a controlled same-environment baseline, a justified reference implementation/backend, or a scaling curve across realistic valid workloads. Warm up first, take repeated samples, report variability, then measure CPU time, allocations, work counts, resource churn, and GPU time when available;
- visual contracts: deterministic scenes, application-owned `Window.SaveScreenshot`, pixel/color diffs, and backend conformance with justified tolerances.

Do not fire a generic test shotgun. Rank hypotheses by impact and likelihood, then use the smallest experiment that can kill each hypothesis.

### 2. Build hostile but faithful tests

Use the cheapest reproduction rung that can exercise the real behavior:

1. focused unit/runtime test;
2. isolated CSI experiment;
3. deterministic focused harness;
4. instrumented real application view;
5. native/runtime harness using the real frame, input, rendering, or platform path.

Escalate when a weaker layer cannot reproduce the relevant ownership boundary. A mock that skips the renderer does not test rendering. Direct property assignment does not test input. Compilation does not test runtime correctness.

For every probe record:

- exact input, sequence, seed, iteration count, timing pattern, timeout, backend, and environment;
- the invariant and explicit failure signal;
- observed versus expected values;
- whether the result reproduced on an identical rerun;
- measurements rather than adjectives for performance claims.

Do not grade an isolated performance number as a defect without a comparison oracle. If no defensible threshold or baseline exists, report the measurement as an observation, not a Major or Critical finding. Demonstrated unbounded growth, pathological scaling, or exhaustion under a realistic valid workload can itself supply the oracle when the scaling experiment and machine limits are explicit.

Keep stress bounded and local. Do not attempt machine-wide denial of service, uncontrolled allocation, unbounded loops, network attacks, or destructive filesystem tests.

### 3. Confirm before accusing

A finding needs a faithful reproduction that fails for the intended reason. Confirm deterministic failures with the same scenario again. For intermittent behavior, report the exact failure count over a fixed number of iterations and preserve the first representative trace.

For every Major or Critical finding, attempt one materially different reproduction path when practical. Change an ownership boundary, execution layer, input route, backend, or observation method; renaming or lightly rearranging the same harness does not count. If a second path is not practical, record why and reduce the claimed confidence.

Minimize every confirmed reproduction without removing the failure. Preserve its exact command, environment, inputs, seed, timing, and expected failure signal. The final permanent RED test is the canonical rerunnable artifact; remove a temporary probe only after the permanent test reproduces the same violated contract.

Trace confirmed behavior far enough to identify the violated invariant and likely owner. Call it a root cause only when source inspection or a distinguishing experiment supports that conclusion. Otherwise label it a hypothesis.

If two targeted experiments fail to support the same theory, discard the theory and reset the hypothesis set. Do not keep torturing one subsystem because the story sounds good.

### 4. Materialize Permanent RED Tests

Every confirmed finding, including Material findings, must finish with the smallest permanent automated regression test that faithfully expresses the violated contract.

- Put the test in the existing project that owns the contract. If no faithful test home exists and adding one would require a new project, dependency, or architecture decision, stop and ask rather than dumping the test into a convenient unrelated suite.
- Test observable behavior through the real owning path. Do not replace a rendering, input, frame, backend, or native failure with a weaker mock-level assertion merely to obtain a test file.
- For process crashes or hangs, isolate the scenario in a bounded child-process or native harness so the test runner can assert the exit, timeout, and captured diagnostics.
- For visual findings, use deterministic scenes, application-owned screenshots, and explicit pixel/color comparisons with justified tolerance.
- For performance findings, prefer deterministic work/allocation/resource counters or a dedicated performance gate with a declared oracle. Do not add noisy wall-clock assertions to an ordinary unit suite.
- Keep the test active and unskipped. Its failure must be an assertion or explicit failure signal caused by the target defect, not a broken fixture, compilation error, missing asset, unavailable environment, or unrelated exception.
- Run each new RED test in isolation at least twice when deterministic. Then run its affected test project to inventory the intentional failures and detect collateral fixture or compilation damage. Run broader repository gates required by repository policy and report their expected nonzero result separately from any unexpected failure.
- If a faithful permanent automated test is technically impossible, report the exact blocker. The evidence may still be reported, but the finding is not fully delivered under this skill and the audit remains incomplete for it.

Do not change production behavior to make the RED test easier to express. The test records the indictment; it does not smuggle in the fix.

### 5. Grade the damage

Use this severity filter:

- **Critical:** corruption, data loss, security boundary failure, process/native instability, deadlock, or broad framework unusability.
- **Major:** reproducible valid-use contract failure, severe common-path rendering/input/layout breakage, fundamental backend divergence, or measured unbounded/pathological resource behavior.
- **Material:** real localized defect with bounded impact. Report it, but do not pretend it is framework-ending.
- **Noise:** subjective polish, invalid expectations, unsupported suspicion, or a harness/environment failure. Exclude it from the indictment.

Severity follows impact and reproducibility, not how angry the prose sounds.

### 6. Clean up and report

Remove temporary probes, reports, generated files, instrumentation, and superseded harness projects using exact validated paths. Preserve the permanent RED regression tests for every confirmed finding. Refresh the Roslyn index after C# or project-file changes, including cleanup. Confirm with `git status` that the retained audit changes are exactly the intended RED tests and that unrelated user changes are untouched.

Lead with a blunt verdict:

- If a major failure is proven: say precisely where Cerneala is broken and why that is unacceptable.
- If only localized issues are proven: do not inflate them into a framework-wide condemnation.
- If nothing breaks: say "I did not break this target under these attacks," never "the target is correct" or "safe."

Report:

- target and tested contract;
- attack model, declared budget, exhausted items, and justified exclusions;
- attack matrix actually executed;
- confirmed findings ordered by severity, each with exact repro, evidence, impact, and owner/root-cause status;
- for every Major or Critical finding, the materially different reproduction result or the reason it was impractical and the resulting confidence limitation;
- for every confirmed finding, the permanent RED test path, test name, isolated command, repeated RED result, and affected-project result;
- negative results and discarded hypotheses;
- commands and verification run;
- cleanup status and the exact intentionally retained RED test files;
- untested surfaces, environmental blockers, and remaining uncertainty;
- the expected red-suite status, intentional failure count, pre-existing failure count, and every unexpected failure separately.

No compliments as filler. No softened verdicts. No bullshit certainty.
