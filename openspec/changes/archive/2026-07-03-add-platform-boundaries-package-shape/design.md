## Context

Cerneala currently ships as a single project, with adapter-specific MonoGame code isolated mostly under `UI/Hosting/MonoGame`, `UI/Drawing/MonoGame`, and resource adapter folders. Core UI already has platform seams for text input and accessibility, but platform services are not grouped and common platform concerns such as clipboard, cursor, file dialogs, and DPI do not have explicit contracts.

Section 24 is about making boundaries obvious before any package split. The implementation should add interfaces and tests, not create partial project splits that would be mostly fake.

## Goals / Non-Goals

**Goals:**
- Add core platform service contracts under `UI/Platform`.
- Provide an aggregate `IPlatformServices` that groups optional platform seams.
- Keep existing `ITextInputPlatform` and `IAccessibilityPlatform` as platform service members.
- Add tests for service registration/composition and backend dependency boundaries.
- Document that package split project files remain optional future work.

**Non-Goals:**
- Creating `Cerneala.Core.csproj`, `Cerneala.MonoGame.csproj`, or test split projects in this phase.
- Implementing native OS clipboard, cursor, file dialog, or DPI adapters.
- Moving existing MonoGame code between projects.

## Decisions

1. Platform services are interfaces plus a simple immutable aggregate.
   - Rationale: callers can pass explicit fake or adapter services without a DI framework.
   - Alternative considered: add a service locator or container. That is unnecessary and hides dependencies.

2. New service members are nullable where a platform capability is optional.
   - Rationale: a game host may not have file dialogs or clipboard support.
   - Alternative considered: require no-op implementations for every service. That adds noise and makes unavailable capabilities harder to distinguish.

3. Package split remains documented and tested as not required.
   - Rationale: the repo is still one project; boundary tests are the real guardrail right now.
   - Alternative considered: create empty csproj files. That would be theatre, nu arhitectură.

## Risks / Trade-offs

- [Risk] Platform aggregate becomes a service locator dumping ground. -> Mitigation: keep it to cross-platform seams listed by roadmap and test its members explicitly.
- [Risk] Boundary tests become brittle source-string tests. -> Mitigation: use them only for dependency boundary rules where runtime behavior cannot observe compile-time references.
- [Risk] Optional project split files being unchecked may look incomplete. -> Mitigation: roadmap marks them as deferred/optional, and tests assert no false completion claim.
