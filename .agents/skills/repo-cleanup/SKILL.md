---
name: repo-cleanup
description: Scan a repository or named area and apply evidence-backed, behavior-preserving cleanup, simplification, deduplication, and cohesive internal modularization. Use for engineering-principle-driven cleanup or refactoring, not ordinary feature work, bug diagnosis, formatting alone, or unsolicited repository rewrites.
---

# Repo Cleanup

Reduce unnecessary complexity and clarify responsibilities. Concision means less cognitive load, not the fewest lines. Leaving sound code unchanged is a valid result.

## Authority and boundaries

A cleanup request authorizes direct, small, verified refactoring batches within the requested scope; do not ask for approval of every safe batch. Skill selection alone does not authorize edits: review-only requests remain read-only. Repository instructions and narrower user requests take precedence.

Ask before changing architectural ownership, module/project dependency boundaries, public or protected contracts, observable behavior, compatibility, dependencies, or materially ambiguous user edits. Internal extraction within an established owner can proceed when equivalence is supported. Better modularity is not permission to redesign architecture.

Do not add features, fix unrelated bugs, replace algorithms, perform speculative optimization, mass-format, commit, push, publish, or delete broad directories. Report discovered defects separately; do not guess the intended contract of disputed behavior.

## Establish scope and evidence

1. Read applicable repository instructions, architecture/contracts, current plans, and verification procedures. Inspect working-tree status and preserve existing changes. If scope is omitted, survey repository-owned source; exclude generated, vendored, build-output, cache, and dependency directories. Edit generators rather than their output when appropriate.
2. Map responsibilities and dependencies with repository-preferred tooling. A broad scan is not an exhaustive audit: track inspected and uninspected areas. Prioritize concrete maintenance costs, not arbitrary complexity scores or file lengths.
3. Read each candidate's complete implementation, relevant callers, tests, and ownership context. Look for existing capabilities before introducing helpers. Check reflection, serialization, source generation, dynamic uses, platform paths, and public extensibility before declaring code unused.
4. Keep a compact work ledger using an existing project mechanism when available: location, observed problem, contract to preserve, proposed change, expected benefit, risk, and verification. Do not create a permanent report file unless requested or required by repository policy. Reject changes justified only by personal taste.
5. Establish relevant pre-change baselines. Investigate failing gates and record exact failures instead of labeling them unrelated. Do not proceed with a batch whose equivalence cannot be verified; request direction if a required gate remains blocked.

## Engineering decision rules

- **KISS / YAGNI:** prefer straightforward control flow and existing facilities. Do not invent extension points, layers, factories, interfaces, or configuration without a current need.
- **DRY:** consolidate duplicated knowledge or behavior with the same contract and reason to change. Similar syntax with different semantics need not share an abstraction. Avoid helpers that introduce dependencies between unrelated owners.
- **SOLID:** improve cohesive responsibilities, substitutability, narrow contracts, and dependency direction where a concrete problem exists. Do not create an interface for every class or turn every operation into a service.
- **Encapsulation / cohesion / coupling:** keep state, invariants, and policy with their owner. Extract internal units only when responsibility becomes clearer without exposing internals or moving policy into a generic utility bucket.
- **Readability:** prefer meaningful names, focused operations, explicit intent, and simpler nesting where equivalent. Avoid cryptic compression, excessive indirection, boolean-switch helpers, and splitting readable flows into trivial fragments.
- **Single source of truth:** reuse implementation paths when contracts match. Preserve comments explaining intent; remove comments only when demonstrably stale or redundant.
- **Proportionality:** weigh reduced complexity against new files, abstractions, dependencies, migration, and testing costs. Neither a large class nor a short method is inherently wrong.

Before simplifying, check what could change: evaluation order, exceptions, null/empty handling, enumeration count, mutation, identity, disposal, lifetime, thread affinity, cancellation, async scheduling, event order, allocations, invalidation, and performance-sensitive fast paths. A cleaner appearance is not evidence of equivalence.

## Execute one coherent batch at a time

1. Select bounded changes sharing one responsibility and verification story. Briefly state the benefit and preserved contract; no approval round is needed for an already authorized safe batch.
2. If tests do not cover the affected contract, add focused characterization tests and confirm they pass before refactoring. Do not canonize suspicious behavior without resolving intent. A pure refactor should not require changed behavioral expectations; if it does, stop and reassess whether this is a behavior change or defect fix.
3. Apply the smallest architecture-correct change. Avoid formatting churn, adjacent cleanup, and speculative generalization. Preserve user edits, public/protected signatures and semantics, serialized forms, lifecycle, and platform support.
4. Refresh generated metadata as repository policy requires. Inspect the diff for accidental changes and new coupling.
5. Rerun pre-change checks, focused tests, and affected suites. Run mandatory repository-wide gates before declaring completion. Use applicable API, generator, visual/conformance, and performance gates for affected paths; compilation and one green test are not enough. Measure performance claims with relevant warmup rather than inferring them from code shape.
6. Investigate new failures before the next batch. Correct the change or safely remove only your own isolated edits; never reset user work. If edits cannot be separated safely, stop and ask. Do not weaken assertions, suppress diagnostics, or change golden outputs merely to obtain green results.
7. Mark a batch verified only after its applicable gates pass, then reconsider remaining candidates. Continue within scope until supported candidates are resolved or a real blocker requires input; do not manufacture changes to meet a quota.

Synchronize canonical documentation and manifests when required. Internal refactoring does not justify rewriting unrelated API docs. Required human validation remains pending until an authorized human performs it.

## Cerneala integration

When operating in Cerneala, follow its repository rules in addition to this workflow:

- Before broad structure inspection, run `.\Tools\scripts\New-FileTree.ps1` from the root and read `FileTree.md`.
- Use direct reads and `rg` for text. Read complete files before editing, plus relevant caller/ownership context.
- Preserve retained-frame, layout, input, rendering, and lifetime contracts. Use existing harnesses and conformance matrices for affected paths. Capture application screenshots only through `Window.SaveScreenshot`, never OS capture.
- Public API documentation belongs in `docs-site/documentation/classes/`; synchronize `docs-site/documentation/manifest.json` when pages are added or renamed. Use the available `writing-api-documentation` skill when needed. A discovered defect is separate work; when repair is authorized, use the available Cerneala bug workflow rather than disguising a fix as refactoring.

## Report

State changes and their justification, scope actually inspected, verification commands/results, and what remains unverified or blocked. Distinguish demonstrated improvements from unmeasured expectations. List material candidates deferred for approval separately. Never equate fewer lines, compilation, or passing focused tests with proof of repository-wide health.
