---
name: cerneala-checklist-plan
description: Create evidence-backed, semantically audited Markdown checklist plans for the Cerneala repository after requirements, architecture, bugs, or desired behavior have already been discussed. Use when the user asks to turn the completed discussion into one or more `.md` plans with stages, `[ ]` tasks, gates, dependencies, tests, documentation work, and a definition of done, especially for files under `docs/plans/`. Do not use for implementing the planned code.
---

# Cerneala Checklist Plan

Transform the settled discussion into executable planning artifacts. Inspect the repository enough to ground every task in the current code, falsify the proposed architecture where possible, and verify that every gate is observable and reachable. Do not implement the planned feature.

A clean Markdown file is not evidence that a plan is correct. Mechanical validation and semantic validation are separate mandatory gates.

## Workflow

### 1. Recover the decision record

- Read the recent discussion and extract the requested behavior, decisions, constraints, rejected alternatives, uncertainties, and dependencies.
- Treat decisions already made with the user as settled. Do not restart the design conversation without a concrete contradiction from the repository.
- Record any necessary inference as an explicit assumption in the plan.
- Ask a question only when the missing answer blocks a safe plan and cannot be discovered locally.

### 2. Inspect Cerneala before planning

- Follow the repository `AGENTS.md` instructions.
- Run `Tools/scripts/New-FileTree.ps1`, then read `FileTree.md` before inspecting structure.
- Use RoslynRepoIndexer as the primary tool for status, search, symbols, definitions, references, and reads.
- Refresh a stale Roslyn index before source reasoning. Do not plan against stale semantic data merely because search snippets look plausible.
- Inspect the relevant production code, tests, public API docs, and at least one current plan for local conventions.
- Identify existing extension points before proposing new abstractions.
- Read the complete definition of the primary type being changed and trace its ownership cone: all semantic callers, factories/composers, resource owners, lifecycle/disposal paths, failure paths, platform/native adapters, and at least one consumer outside the most recently discussed subsystem when one exists.
- Before changing or removing a shared method/type in the plan, enumerate every current call site and state how each caller migrates or why it is intentionally unchanged.
- For shader, native interop, serialization, memory layout, generated-code, cache, or multi-window work, inspect the concrete low-level implementation rather than inferring capability from the high-level API.
- Surface nearby design defects only when they affect the plan; put unrelated smells in a separate note rather than expanding scope.

For architecture, performance, rendering, native interop, shared-resource lifecycle, caching, concurrency, or cross-platform plans, read and apply [references/semantic-audit.md](references/semantic-audit.md) before drafting.

### 3. Build an evidence ledger

- Classify every material statement as one of: observed repository fact, explicit user decision, planned artifact, falsifiable hypothesis, or unresolved unknown.
- Attach an exact source to observed facts: file/type/member, test, benchmark artifact, documentation contract, or command result.
- Do not write a hypothesis as target architecture merely because it is coherent. Add a discovery/RED stage first, or ask the user when the unknown is a material architecture/product decision.
- Do not claim that a metric, mutation path, API, platform capability, diagram source, or verification tool exists until it has been found.
- If a future artifact is named, identify its owner and dependency direction. Mark file inventories as estimates.
- Keep the ledger in working notes unless the unresolved assumptions materially affect implementers; put those assumptions in the plan.

### 4. Choose the plan split

- Create one plan for one independently deliverable concern.
- Create multiple files when concerns have different lifecycles, verification surfaces, or dependency order.
- Use an umbrella index plus dependent plans when the overall initiative contains independently shippable architecture, native/shader, caching, or optional performance work.
- Apply the delivery test: if concern A can be completed, verified, and shipped without concern B, they normally belong in separate plan files.
- State dependencies explicitly at the top of dependent plans.
- Avoid a single giant plan that mixes infrastructure, feature behavior, unrelated cleanup, and optional polish.
- Keep profiler-dependent candidates out of the mandatory main path. Record them as follow-ups unless current evidence already proves they are required.
- Default to `docs/plans/YYYY-MM-DD-<short-slug>.md` unless the user specifies another location or filename.

### 5. Define and falsify the architecture before the checklist

- Describe the baseline problem using the actual classes and behavior found in the repository.
- State the target composition, ownership, data flow, and lifecycle.
- Name the ownership scope for mutable state and resources: process, device, application, window/session, frame, control instance, or command list. Do not leave shared-versus-instance ownership implicit.
- Prefer the simplest solution compatible with existing Cerneala patterns and clean architecture.
- Separate public API, internal contracts, template parts, event flow, layout behavior, and input behavior when relevant.
- Include non-goals to prevent scope creep.
- List expected new and modified files as estimates, not promises to create decorative abstractions.
- Run a hostile contradiction pass before accepting the design: look for another consumer of the shared API, an eager lifecycle transition, a native hardcoded layout, a failure/retry path, a multi-instance path, an existing version/invalidation token, and a stated non-goal contradicted by later tasks.
- When evidence contradicts the proposed architecture, change the architecture. Do not preserve the proposal through wrappers, duplicated paths, or vague wording.

### 6. Write implementation stages

Use `- [ ]` syntax for all actionable work and acceptance gates. Keep every item independently checkable.

Order stages by dependency. Prefer this general sequence when applicable:

1. Baseline and RED characterization tests.
2. Small foundational contract or lifecycle change.
3. Core implementation.
4. Integration with dependent controls or services.
5. Edge cases, cancellation, detach, replacement, and failure behavior.
6. Markup/source-generator integration.
7. API documentation and full verification.

For each stage:

- Name the exact types or files involved.
- Describe observable behavior, not vague work such as "handle edge cases".
- Include focused tests near the implementation tasks they validate. For changed behavior, require the smallest RED test and confirmation that it fails for the intended reason before production changes. Label existing GREEN behavior as characterization, not RED.
- Add a `Gate` subsection with conditions that must be true before continuing.
- Include Roslyn reindexing after future code or project-file modifications.
- Include exact targeted and full-suite verification commands where useful.
- Confirm that the current or explicitly planned harness can observe every gate. If instrumentation does not exist, plan instrumentation before the baseline and do not claim unavailable metrics.
- Distinguish deterministic gates (draw counts, binds, invalidations, resource counts, API surface) from noisy measurements. Define warmup, repetitions, variation handling, and inconclusive results for timing gates.
- State platform policy explicitly: required and executed, required but blocked, deliberately waived by an existing decision, or compile-only. "Not run" is not GREEN.

### 7. Cover Cerneala-specific obligations

- For every planned public API change, require matching documentation under `docs-site/documentation/classes/` using `writing-api-documentation`.
- Require `docs-site/documentation/manifest.json` updates when API pages are added or renamed.
- Preserve current public nullability, ownership, routing, layout invalidation, and template lifecycle contracts unless the discussion explicitly changes them.
- Add template-swap and detach tests for controls that subscribe to template parts.
- Add idle-frame regression tests for layout or invalidation work.
- Add markup/source-generator tests when new public controls, properties, template parts, or markup syntax are introduced.
- Require the repository's actual public API enforcement test/tool when one exists; do not write a vague "run API diff" task without identifying a mechanism. Add a public API review when the plan adds or changes public/protected members.

### 8. Run a semantic audit of the draft

- Reread every plan file completely after drafting.
- Re-run semantic reference searches for every shared type/member the plan moves, changes, renames, or deletes. Verify that the file inventory and migration tasks cover every caller.
- Compare objectives, non-goals, tasks, gates, risks, platform policy, and definition of done for contradictions.
- Verify that every baseline claim still matches the repository and that every future capability is marked as planned or gated by discovery.
- Verify that every RED is actually RED on the current implementation, while characterization tests remain GREEN.
- Verify that every definition-of-done item is reachable under the stated waivers and available harnesses.
- Check for invented mutation paths, metrics, source artifacts, command-line switches, native capabilities, version tokens, or test utilities.
- Do not fix semantic uncertainty with softer wording. Resolve it from the repository, add a discovery gate, split the plan, or ask the user.

### 9. Close the plan

- Add an ordered implementation sequence when multiple stages or plan files depend on each other.
- Add stop conditions when a tempting expansion is explicitly outside scope.
- End with a concrete `Definitia de gata` describing observable completion.
- Keep the plan in the language used by the user; default to Romanian for Cerneala.
- Follow the tone requested by the repository, but keep checklist items technically precise. A joke may season the plan; it must not replace an acceptance criterion.

## Required Plan Shape

Adapt sections to the task instead of filling them mechanically, but normally include:

```markdown
# Plan: <name>

> Data: YYYY-MM-DD
> Status: planificat
> Dependenta: <optional>
> Scop: <one sentence>

## 1. Rezumat
## 2. Baseline si problema actuala
## 3. Obiective
## 4. Non-obiective
## 5. Arhitectura propusa
## 6. Fisiere estimate
## 7. Etape de implementare

### Etapa 0 - Baseline si plasa de siguranta
- [ ] ...

**Gate etapa 0**
- [ ] ...

## 8. Ordinea recomandata
## 9. Definitia de gata
```

Omit sections that add no value. Add sections for public contracts, event semantics, state machines, migration, compatibility, or performance only when the feature needs them.

## Quality Rules

- Do not write implementation code while creating the plan.
- Do not mark future tasks as complete.
- Do not invent files, APIs, or framework capabilities without checking the repository.
- Do not infer low-level feasibility from a neighboring subsystem. A shader with many samplers, for example, does not prove that another shader supports dynamic indexing or the same vertex layout.
- Do not hide unresolved architectural choices inside checklist wording.
- Do not prescribe subagents, commits, or pull requests unless the user explicitly asks.
- Do not duplicate the same task across stages or plan files; reference the dependency instead.
- Do not use generic filler such as "add tests" or "update docs" without naming the required behavior or artifact.
- Keep YAGNI: optional future capabilities belong in non-goals or follow-up notes, not the current checklist.
- Do not declare "zero work", "zero allocations", "one pass", parity, portability, or similar absolutes without naming the exact measured counter and scope.
- Do not let a plan change a shared owner while omitting another current consumer from estimated files and migration tasks.
- Do not declare the plan implementation-ready merely because headers, checkboxes, links, whitespace, and `git diff --check` are correct.

## Verification

After writing the plan files:

### Mechanical verification

- Read back their headers and representative stages.
- Run `git diff --check -- <plan-files>`.
- Check new/untracked plan files separately for trailing whitespace because `git diff --check` does not inspect untracked content.
- Confirm every actionable line uses `- [ ]` and no task is accidentally pre-checked.
- Confirm dependent plan files name their prerequisite.
- Confirm no production code or API documentation was modified.

### Semantic verification

- Report which ownership cones and shared call sites were checked.
- Report contradictions found and how the plan changed because of them.
- Report which repository tests, shader/tool verifications, command parsers, or benchmark harnesses were actually exercised during planning; note what was not run.
- State any remaining unknown, waiver, or blocked gate precisely.
- If semantic verification is incomplete, say the plan is not implementation-ready even when mechanical checks pass.

Report the created file paths and note whether code tests were run. Documentation-only edits do not require rerunning unrelated suites, but a focused existing test/tool may be run when it validates a material planning assumption.
