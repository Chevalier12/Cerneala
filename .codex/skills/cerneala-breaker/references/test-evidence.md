# Cerneala Breaker test evidence

Read this reference when a test result, coverage report, mutation result, repeated run, or existing assertion is being used as evidence. The question is not whether code ran. The question is whether the experiment could distinguish the contractual behavior from a relevant wrong behavior.

## Reachability is not observation

Coverage can identify statements, branches, conditions, or paths that were not executed. It cannot establish that executed behavior was checked. Trace whether an incorrect value or state would propagate to an independent observable boundary: return value, persisted state, emitted event, rendered frame, hit-test result, resource count, exact generated artifact, or forbidden side effect.

If the difference cannot reach the oracle, add a faithful observation path or choose a stronger reproduction rung. Do not expose private implementation details merely to make a weak test easy.

## Audit oracle strength

Challenge whether the test would fail for the defect being alleged:

- Does it assert the correct subject, type, path, backend, frame, state, and non-effect?
- Does it require the exact contractual outcome instead of truthy/non-null/broad-exception success?
- Could setup fail early and accidentally satisfy a broad assertion?
- Does a snapshot or golden artifact make the material semantic difference explicit, or bury it in unrelated volume?
- Does a mock observe a real boundary contract, or only repeat implementation calls?
- Is the expected value calculated by the same algorithm as production, allowing a shared error?
- Does success include consequential state changes, cleanup, emissions, invalidations, and forbidden side effects?
- For input behavior, did the test use real click/key/text/focus/pointer routing rather than direct property assignment?
- For rendering, did it use the real backend and application-owned screenshot path rather than a mock or OS capture?

Assertion count is logistics, not strength. Several assertions may jointly prove one coherent behavior; one precise invariant may be enough.

## False-green attacks on the harness

When relevant, execute the focused case:

- alone and in the normal suite;
- before and after likely stateful neighbors;
- reordered or with runner randomization;
- under supported parallel execution;
- with explicit seeds, locale, time zone, backend, configuration, and runtime variations;
- with unique files, handles, ports, caches, native resources, and persistent identities;
- with cleanup forced through assertion/setup failure when the harness supports it.

Look for shared fixtures, leaked framework/native state, uncontrolled wall time/randomness/network, ignored exit codes, wrong test selection, stale build artifacts, and child work that outlives the test.

## Flakes are defects until classified

A flake means identical code and declared configuration produced both pass and fail. Preserve the first useful failure and the passing counterpart. Investigate asynchronous completion, synchronization, order dependence, global state, handles/resources, wall clock, locale, random seed, floating-point assumptions, unordered collections, parallel pressure, and real product concurrency bugs.

Reruns can measure frequency but do not repair evidence. Arbitrary sleeps, retry wrappers, quarantine, and wider timeouts hide the symptom unless a reproduced cause and explicit temporary containment contract justify them. Breaker does not add such containment.

## Coverage and mutation interpretation

Use structural coverage to locate unexercised decisions. Use mutation to ask whether tests distinguish selected behavioral changes. Neither is a pass/fail quality score.

For mutation survivors, separate reachability, infection, propagation, and revealability gaps. Review equivalent or out-of-contract mutations rather than forcing a target percentage. Record scope and tool configuration so results are comparable.

Generated-case count is equally weak without named partitions and realized distributions. Repetition reduces observed uncertainty only for the exact scenario and environment run; it does not establish race freedom or determinism.

## Existing tests are not pruning candidates

`cerneala-breaker` is an adversarial product audit, not a suite-reduction campaign. Do not delete or consolidate existing tests even when they appear redundant or weak. Two tests reaching the same lines may protect different requirements, partitions, transitions, platforms, historical failures, or diagnostic signals.

If an existing test cannot detect the confirmed failure:

1. preserve it;
2. explain its evidence limitation;
3. add the smallest distinct permanent RED regression through the owning behavior path;
4. leave any broader suite redesign for separately authorized work.

## Converging evidence

Select evidence proportional to impact rather than forcing every tool:

| Evidence | Supports | Does not establish alone |
|---|---|---|
| Contract and risk ledger | relevance and intended behavior | implementation correctness |
| Boundaries and state partitions | deliberate scenario coverage | every combination or sequence |
| Structural coverage | reachability | oracle strength |
| Mutation | detection of selected injected fault classes | all real faults |
| Property/model/metamorphic tests | broad invariant checks | correctness of the property/model/relation |
| Repeated/reordered runs | observed frequency and isolation evidence | determinism or race freedom |
| Race/sanitizer tooling | defects on executed paths | absence on unexecuted paths |
| Historical regressions | protection from known failures | unknown failure modes |

A finding is confirmed only when direct evidence contradicts a sourced contract and the harness is faithful. A ledger row is `covered` only within its declared technique, oracle, budget, environment, and limitations.

## Handoff integrity

Before reporting:

- every ledger row has an explicit disposition;
- every confirmed Cerneala defect has a minimized permanent RED regression or an exact blocker;
- every RED failure is caused by the target defect rather than fixture/environment damage;
- intentional RED, pre-existing failures, unexpected failures, and blocked evidence are counted separately;
- no existing test was weakened, skipped, quarantined, removed, or re-baselined;
- residual high-risk rows and unsupported claims are visible.

## Method provenance

This reference adapts compatible evidence-quality lessons from [`jpcaparas/skills`' `adversarial-test-sweep` v1.0.0 at commit `05d0911`](https://github.com/jpcaparas/skills/tree/05d09114c7bc9330e7501a8412ddb96bbdc399da/skills/adversarial-test-sweep) and its suite-evidence guide. Cerneala's evidence-first workflow and audit-only constraints take precedence where the workflows differ.
