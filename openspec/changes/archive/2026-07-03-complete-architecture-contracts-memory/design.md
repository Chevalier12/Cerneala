## Context

Section 1 of `ROADMAPv2.md` was created before the OpenSpec capability names settled. The repo now has the actual specs under names such as `retained-element-tree`, `retained-invalidation-frame-scheduler`, `typed-state-model`, `layout-system`, `retained-rendering-cache`, `retained-input-bridge`, and `styling-theme-engine`, while section 1 still lists older placeholder names.

The repo already has `MonoGameDependencyBoundaryTests`, but section 1 calls for two broader architecture guard files:

- `RepositoryShapeTests`
- `NamespaceBoundaryTests`

## Goals / Non-Goals

**Goals:**

- Reconcile section 1 roadmap checkboxes with the actual spec files.
- Add architecture tests that prove repo shape and UI core namespaces remain backend-neutral.
- Keep the tests file-system based and deterministic.
- Keep the change limited to documentation, tests, and roadmap state.

**Non-Goals:**

- Do not create duplicate legacy spec folders just to satisfy stale roadmap names.
- Do not move runtime files or split projects.
- Do not change production UI behavior.

## Decisions

- Treat the existing OpenSpec capability folders as canonical. Creating legacy aliases such as `openspec/specs/layout/spec.md` would duplicate meaning and make future planning worse.
- Implement `RepositoryShapeTests` as a roadmap/spec existence guard. It should verify that section 1's architecture memory points at files that actually exist and that optional/deferred project split items are not accidentally claimed.
- Implement `NamespaceBoundaryTests` as a source scan over non-adapter UI core namespaces. It should reject direct references to MonoGame, Skia, HarfBuzz, `SpriteBatch`, `Texture2D`, and direct platform polling APIs outside known adapter folders.
- Keep `MonoGameDependencyBoundaryTests` for existing focused adapter checks; do not fold every architecture rule into one massive file.

## Risks / Trade-offs

- File-system architecture tests can be noisy if they scan generated `bin` or `obj` files. Mitigation: enumerate repository `.cs` files while excluding build output and generated folders.
- String-based forbidden-term tests can produce false positives in comments. Mitigation: keep the forbidden list focused on backend API names and allow tests/playground/adapter folders explicitly.
- Roadmap reconciliation changes checklist wording. Mitigation: only update section 1 items to the actual spec names and mark them complete if the files exist.
