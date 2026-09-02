# Cerneala Breaker attack techniques

Read this reference only when direct example and boundary tests cannot economically falsify a named risk-ledger row. Choose the smallest technique that reaches the relevant contract. A fashionable technique without a contract, oracle, budget, and replay artifact is bullshit.

## Oracle ladder

Choose the strongest independent oracle available:

1. exact output or state required by a documented contract;
2. a deliberately small independent model or reference implementation;
3. a domain invariant or algebraic property;
4. a specification-backed relation between multiple executions;
5. comparison between backends, modes, versions, or reference paths that claim compatible semantics;
6. bounded robustness: no crash, hang, leak, invalid output, forbidden side effect, or pathological growth.

Combine oracles when impact warrants it. A no-crash test cannot prove semantic correctness. Two implementations with shared lineage can agree on the same bug. A copied model is not independent.

## Partitions, boundaries, and decision tables

Partition by expected behavior, not by convenient data types. Cover valid, invalid, transition, and valid-but-surprising representatives. For ordered limits, use just below, exactly at, and just above where meaningful. Consider empty, missing, duplicate, reordered, normalized, encoded, degenerate, maximum-size, precision-edge, `NaN`, and infinity only when the target contract distinguishes them.

Use a decision table when several rules control the result. Use constrained pairwise or higher-strength combinations when parameters plausibly interact but the full Cartesian product is wasteful. Encode impossible combinations, pin known dangerous combinations, and record the chosen interaction strength as a limitation rather than pretending it is exhaustive.

## Property-based generation

Use generated tests when many inputs or sequences must obey one stable law. Cerneala candidates include:

- parse/serialize or conversion round trips;
- layout, geometry, color, or transform invariants;
- idempotent normalization or repeated lifecycle operations;
- accounting conservation for queues, caches, resources, subscriptions, and invalidations;
- equivalence between optimized and simple reference paths;
- preconditions, transitions, and postconditions for stateful APIs.

Generate three populations where applicable: valid structured values, intentionally invalid values, and valid action sequences. Bias toward risk-ledger partitions and historical failures. Measure the realized distribution; a large count that never reaches rare states is fake coverage. Avoid discard-heavy assumptions. Shrink while preserving the validity needed to reach the same contract.

Persist the minimized concrete case, generator version, relevant configuration, and seed. A seed alone may stop replaying after the generator changes.

## Fuzzing

Use fuzzing for parsers, decoders, validators, markup or language front ends, protocol surfaces, and other broad input spaces. The harness must reset persistent/global state, isolate one case from the runner, terminate child work, and impose explicit time, memory, recursion, and output caps.

Seed with varied valid and invalid inputs. Prefer grammar- or structure-aware generation when raw bytes cannot reach meaningful states. Include effective inputs such as configuration, locale, timing, dependency responses, and event order when they alter behavior.

Normal verification may retain a small deterministic corpus. Broader stochastic exploration needs a separate declared budget. Coverage guides search toward new execution; it does not decide correctness.

## Metamorphic testing

Use metamorphic relations when exact outputs are unavailable. Define a source input, a contract-backed transformation, and the required relationship between the two results. Possible relations include equivalent serialization, permutation where order is irrelevant, adding a neutral element, partitioning and recombining, or applying a valid translation/scale transformation.

First validate the relation against the documented semantics and trusted examples. An invalid relation manufactures false bugs; a weak relation allows correlated wrong results to agree.

## Model-based lifecycle testing

Use a small abstract state machine for lifecycle-heavy controls, windows, render resources, caches, input capture, animations, queues, or backend recreation. Define:

- initial abstract state;
- actions/events and their preconditions;
- expected transitions and observable outputs;
- invariants after every action;
- forbidden transitions and required non-effects.

Generate only sequences valid from the current model state unless invalid-use behavior is itself documented. Weight rare transitions, cycles, disposal/recreation, cancellation/completion races, and repeated operations. Shrink failing traces without making them invalid. Measure states and transitions reached. Model coverage covers the model, not reality; keep the model smaller and independent from production code.

## Differential testing

Compare the same deterministic scene, input, or action trace across implementations that claim compatible semantics: reference and optimized algorithms, MonoGame and SDL GPU paths, retained and rebuilt work, supported platforms, or old and new versions.

Normalize only differences the contract explicitly permits, such as generated identifiers, timestamps, unspecified ordering, or justified numeric/pixel tolerances. Record versions, flags, environment, and normalization rules. A mismatch establishes disagreement. Resolve which side violates the contract using a specification, invariant, or third oracle; never crown the older backend correct by default.

## Systematic concurrency

Prefer explicit barriers, controlled timers, deterministic schedulers, and bounded schedule exploration over sleeps or brute-force repetition. Keep the scenario small: few actors, few operations, explicit synchronization, and a declared number of scheduling choices.

Check both safety and liveness: no lost or duplicated work, no externally visible forbidden intermediate state, one coherent winner between cancellation and completion, no deadlock/livelock/starvation, and no child work left behind. Preserve the exact event or schedule trace. Race detectors see only executed paths; schedule exploration sees only the schedules it controls.

## Dependency faults and resource boundaries

Inject at explicit acquisition, use, and release seams. Distinguish immediate failure, delayed failure, timeout, cancellation, blackhole, partial completion, stale/duplicate/reordered/corrupt response, cleanup failure, exhausted pool/queue/handle budget, and termination at a meaningful boundary.

Declare the recovery oracle before injecting: intact state, no duplicate visible effect, bounded retry, released resources, correct degraded result, or eventual stable state. Start with one fault and combine faults only when the combination represents a different plausible recovery path. Use local disposable environments and synthetic limits; production or machine-wide stress is not authorized.

## Mutation diagnostics

Use mutation only when it can answer whether an existing oracle distinguishes an important fault class. For a surviving change, classify the gap:

- **reachability:** the relevant decision was never executed;
- **infection:** the mutation produced no different state for tested inputs or is equivalent;
- **propagation:** the difference never reached an observable boundary;
- **revealability:** the difference reached an observable result but assertions ignored it.

Record tool/version, operators, target, exclusions, timeout, invalid/equivalent mutants, and survivor dispositions. Never demand 100%. Mutation is a diagnostic proxy, not a catalog of all real defects.

## Reduction and replay packet

Minimize every generated, schedule-dependent, or multi-fault failure by deleting irrelevant input, actions, actors, faults, and environment differences while preserving the same violated contract.

The replay packet must contain:

- minimized concrete input or action sequence;
- expected and observed behavior;
- exact environment, tool/runtime versions, backend, and configuration;
- generator/corpus version and seed where applicable;
- schedule or event trace for concurrency;
- injected fault and seam for recovery tests;
- stable permanent RED regression location.

Minimization proves a smaller reproducer, not a unique root cause. Diagnose after reduction.

## Method provenance

This reference adapts compatible testing lessons from [`jpcaparas/skills`' `adversarial-test-sweep` v1.0.0 at commit `05d0911`](https://github.com/jpcaparas/skills/tree/05d09114c7bc9330e7501a8412ddb96bbdc399da/skills/adversarial-test-sweep) and its research-grounded technique guide. Cerneala's repository contract, runtime paths, audit-only authority, and permanent-RED handoff remain controlling.
