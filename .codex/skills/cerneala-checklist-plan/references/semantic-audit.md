# Semantic audit for Cerneala plans

Use this audit for nontrivial architecture, performance, rendering, native interop, shared-resource lifecycle, caching, concurrency, or cross-platform plans.

The purpose is to falsify a plausible plan before implementation makes Cerneala carry it permanently.

## 1. Evidence ledger

Classify each material claim:

| Class | Meaning | Allowed use |
|---|---|---|
| Observed fact | Directly supported by current source, tests, docs, artifact, or command output | Baseline and constraints |
| User decision | Explicit product/architecture/scope choice | Target contract |
| Planned artifact | Does not exist yet; owner and dependency direction are stated | Checklist task |
| Hypothesis | Falsifiable explanation or possible design | Discovery experiment only |
| Unknown | Material fact not resolved | Blocking question or discovery gate |

Rules:

- A symbol name is not a contract.
- A file existing is not proof that its diagram, generated artifact, benchmark, or documentation source is reproducible.
- A test threshold is not a benchmark result.
- A neighbor subsystem demonstrating a capability is evidence to investigate, not proof of portability.
- A type firing an event is not proof that every imagined mutation path exists.

## 2. Ownership cone

For the primary type/member, inspect:

1. complete definition;
2. every semantic caller/reference;
3. constructor/factory/composition root;
4. state and resource owner;
5. begin/end/reset/dispose paths;
6. exception, cancellation, retry, and recovery paths;
7. multi-instance, multi-window, reentrancy, or concurrent-frame behavior;
8. platform/native implementation and hardcoded layouts;
9. tests, fakes, benchmarks, docs, generated artifacts, and manifests;
10. at least one consumer outside the subsystem that made the problem visible.

For every shared method/type planned for removal or semantic change, produce a caller table in working notes:

| Caller | Current dependency | Planned migration | Verification |
|---|---|---|---|

If any caller lacks a migration or explicit unchanged contract, the plan is incomplete.

## 3. Architecture falsification matrix

Try to break the proposed ownership with these questions:

- Does another subsystem use the resource or method?
- Is the proposed per-instance state currently shared per device/process, or the reverse?
- Is a render pass, transaction, frame, subscription, or cache already opened earlier than the proposal assumes?
- Does the native adapter hardcode a pitch, layout, count, enum mapping, ABI, or resource limit?
- Does failure after "consume" require rollback or requeue?
- Can two windows/sessions/controls be active or interleaved?
- Does disposal act as invalidation, and is there actually a mutation API?
- Are version/stable-id tokens already available?
- Does the proposed cache hold a pooled/weak key alive through its value?
- Does a stated non-goal remain contradicted by a gate such as "all transitions" or "complete parity"?

When a contradiction appears, choose one:

1. change the target ownership;
2. narrow the scope and gate language;
3. expand scope only when the architecture-correct migration requires it and explain why;
4. split an independent concern into a dependent plan;
5. ask the user when the choice is materially architectural or product-facing.

Do not hide the contradiction with a temporary callback, duplicate general path, vague "integration" task, or an owner-named wrapper around the same coupling.

## 4. Gate observability

For every gate, identify the observer:

| Gate kind | Acceptable observer |
|---|---|
| Ordering/state | Focused deterministic fake/runtime test |
| Draw/bind/pass/upload counts | Structured counters, not string logs in allocation benchmarks |
| Managed allocations | Warmed allocation harness or BenchmarkDotNet |
| CPU time | Fixed scenario, warmup, repeated processes, median and variability |
| GPU time | Actual GPU query/harness; unavailable must be reported unavailable |
| Visual parity | `Window.SaveScreenshot` and deterministic pixel/color diff |
| Resource lifetime | Handle/resource counters through failure, resize, multi-instance, and disposal |
| Public API | Existing architecture/API test or named diff tool |
| Shader/native layout | Manifest/reflection plus native pipeline creation where available |

If the observer does not exist:

- add instrumentation before claiming a baseline;
- separate pre-instrumentation and post-instrumentation metrics;
- do not backfill unavailable historical values;
- do not call a green focused test proof of full-system health.

Timing gates must specify warmup, sample count, environment, variability handling, and what result is inconclusive. Deterministic operation counts should be preferred over timing when they directly express the invariant.

## 5. RED and characterization audit

- A RED test must fail on the current implementation for the intended missing behavior.
- A characterization test records current behavior and should be GREEN.
- Do not group a GREEN characterization and a RED expectation under a gate saying both are RED.
- Confirm RED before the production task in the stage ordering.
- Failure from missing fixture data, unsupported environment, compilation unrelated to the contract, or stale artifacts is not valid RED.

## 6. Plan split audit

Split when concerns differ in any of:

- independently shippable value;
- owner or dependency direction;
- native/shader versus managed implementation;
- public API versus internal mechanics;
- correctness/conformance versus optional performance;
- lifecycle or invalidation model;
- platform verification surface.

Use an umbrella index for sequence and initiative-wide definition of done. Dependent plans must name exact prerequisite files. Do not duplicate detailed tasks in the index.

Profiler-dependent candidates remain follow-ups until evidence selects them. A request to include "everything discussed" does not turn speculative candidates into mandatory architecture.

## 7. Post-draft cold read

Read every plan from top to bottom as if another engineer will implement it without conversation context.

Check:

- every observed claim has evidence;
- every future capability is clearly planned;
- every shared caller is covered;
- ownership scope is explicit;
- estimated files include affected native adapters, fakes, manifests, generated artifacts, and external consumers;
- objectives and non-goals do not conflict with tasks or gates;
- platform waivers do not become GREEN validation claims;
- definition of done is reachable;
- commands point to real projects/tools or are explicitly created earlier;
- cache invalidation uses real mutation/version contracts;
- absolute performance statements name exact counters and scope;
- documentation source/workflow actually exists, otherwise the blocker is reported;
- mechanical validation is not presented as semantic proof.

If this cold read finds a contradiction, repair and repeat the semantic audit before reporting completion.
