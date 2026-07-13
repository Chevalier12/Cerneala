---
name: cerneala-checklist-plan
description: Create implementation-ready Markdown checklist plans for the Cerneala repository after requirements, architecture, bugs, or desired behavior have already been discussed. Use when the user asks to turn the completed discussion into one or more `.md` plans with stages, `[ ]` tasks, gates, dependencies, tests, documentation work, and a definition of done, especially for files under `docs/plans/`. Do not use for implementing the planned code.
---

# Cerneala Checklist Plan

Transform the settled discussion into executable planning artifacts. Inspect the repository enough to ground every task in the current code, but do not implement the planned feature.

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
- Inspect the relevant production code, tests, public API docs, and at least one current plan for local conventions.
- Identify existing extension points before proposing new abstractions.
- Surface nearby design defects only when they affect the plan; put unrelated smells in a separate note rather than expanding scope.

### 3. Choose the plan split

- Create one plan for one independently deliverable concern.
- Create multiple files when concerns have different lifecycles, verification surfaces, or dependency order.
- State dependencies explicitly at the top of dependent plans.
- Avoid a single giant plan that mixes infrastructure, feature behavior, unrelated cleanup, and optional polish.
- Default to `docs/plans/YYYY-MM-DD-<short-slug>.md` unless the user specifies another location or filename.

### 4. Define the architecture before the checklist

- Describe the baseline problem using the actual classes and behavior found in the repository.
- State the target composition, ownership, data flow, and lifecycle.
- Prefer the simplest solution compatible with existing Cerneala patterns and clean architecture.
- Separate public API, internal contracts, template parts, event flow, layout behavior, and input behavior when relevant.
- Include non-goals to prevent scope creep.
- List expected new and modified files as estimates, not promises to create decorative abstractions.

### 5. Write implementation stages

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
- Include focused tests near the implementation tasks they validate.
- Add a `Gate` subsection with conditions that must be true before continuing.
- Include Roslyn reindexing after future code or project-file modifications.
- Include exact targeted and full-suite verification commands where useful.

### 6. Cover Cerneala-specific obligations

- For every planned public API change, require matching documentation under `docs-site/documentation/classes/` using `writing-api-documentation`.
- Require `docs-site/documentation/manifest.json` updates when API pages are added or renamed.
- Preserve current public nullability, ownership, routing, layout invalidation, and template lifecycle contracts unless the discussion explicitly changes them.
- Add template-swap and detach tests for controls that subscribe to template parts.
- Add idle-frame regression tests for layout or invalidation work.
- Add markup/source-generator tests when new public controls, properties, template parts, or markup syntax are introduced.
- Require a public API diff review when the plan adds or changes public/protected members.

### 7. Close the plan

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
- Do not hide unresolved architectural choices inside checklist wording.
- Do not prescribe subagents, commits, or pull requests unless the user explicitly asks.
- Do not duplicate the same task across stages or plan files; reference the dependency instead.
- Do not use generic filler such as "add tests" or "update docs" without naming the required behavior or artifact.
- Keep YAGNI: optional future capabilities belong in non-goals or follow-up notes, not the current checklist.

## Verification

After writing the plan files:

- Read back their headers and representative stages.
- Run `git diff --check -- <plan-files>`.
- Confirm every actionable line uses `- [ ]` and no task is accidentally pre-checked.
- Confirm dependent plan files name their prerequisite.
- Confirm no production code or API documentation was modified.
- Report the created file paths and note that code tests were not run for documentation-only changes.
