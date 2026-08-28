# Cerneala Roadmap

Last audited: **2026-08-28**

Cerneala is a retained realtime UI framework for .NET applications and complete
2D games. The goal is one application model for ordinary controls, windows,
tools, HUDs, and realtime game rendering.

This roadmap records direction and maturity. It is not a release calendar and
it is not a promise to clone WPF, Avalonia, or every API with a familiar name.

## How To Read This Roadmap

Cerneala is too large for a checklist of type names to prove progress. A public
class can exist while the real scenario is still incomplete.

Roadmap claims therefore use these levels:

- **Type exists**: an API shape exists.
- **Integrated**: the retained runtime actually uses it.
- **Backend-supported**: the selected backend implements the contract.
- **Scenario-proven**: a realistic path is covered by tests or a deterministic
  runtime harness.
- **Conformant**: applicable visual, performance, lifecycle, and API gates pass.

Only the level stated by a roadmap item is claimed.

## Product Direction

Cerneala keeps useful retained UI ideas such as typed properties, logical and
visual trees, measure and arrange, routed input, commands, resources, templates,
and reusable controls.

It does not preserve old architecture merely for compatibility. Realtime frame
ownership, invalidation, retained command reuse, typed authoring, explicit
backend boundaries, and evidence-driven verification are first-class concerns.

The game view belongs inside the UI model. `RenderSurface2D` is a
`ContentControl`, not a foreign engine surface bolted beside the interface.

## Current Baseline

The following foundation exists in the repository now.

### Authoring And Tooling

- `.crn` syntax, parsing, recovery, semantic analysis, and diagnostics
- source generation into typed C# UI trees
- generated `Application`, `Window`, and `UserControl` paths
- typed bindings, resources, Aspect, Motion, Prism, and templates in markup
- language server services for completion, navigation, structure, formatting,
  semantic tokens, and code actions
- Visual Studio integration and preview infrastructure

Maturity: integrated and covered by language, generator, language server,
preview host, and external consumer tests. `.crn` is still a Cerneala-specific
language, not a general XAML compatibility promise.

### Retained Runtime

- typed `UiProperty<T>` state and explicit value sources
- logical and visual trees with attach and detach lifecycle
- Relay UI-thread scheduling and binding refresh
- inherited-property, Aspect, layout, render-cache, and hit-test queues
- measure and arrange layout
- retained local and root drawing command caches
- routed input, focus, navigation, pointer capture, gestures, commands, and
  input bindings
- resources, images, fonts, themes, semantics, diagnostics, and automation
- root-owned Motion

Maturity: integrated. Core invariants such as no-work frames, render-only
invalidation, draw purity, tree mutation, input routing, focus, and lifecycle
cleanup have permanent tests.

### Controls And Application UI

The repository contains retained implementations for application and window
lifecycle, panels, content controls, text and input controls, selection and
items controls, scrolling, menus, tabs, overlays, tooltips, dialogs, ink, color
selection, images, SVG, and common button and range primitives.

Maturity: mixed. A type name means the Cerneala contract documented for that
type, not automatic WPF parity. Canonical member behavior belongs in
`docs-site/documentation/classes/` and its tests.

### Realtime 2D And Visual Composition

- `RenderSurface2D` as a retained content control
- continuous and on-demand surface scheduling
- shapes, paths, strokes, transforms, clipping, layers, blend state, images,
  sprite batches, meshes, point and line batches, and text layouts
- retained surface command and raster reuse
- Prism definitions, catalog, runtime instances, Motion integration, retained
  results, filters, styles, masks, blending, and backdrop composition
- Aspect tokens, rules, states, variants, resources, and templates through one
  unified runtime model

Maturity: integrated on the implemented backend paths with focused unit,
runtime, visual, and benchmark evidence. Backend agreement remains a
conformance requirement, not an assumption.

### Desktop Backends

The repository currently contains:

- a WindowsDX presentation path;
- a MonoGame and `SpriteBatch` adapter path;
- SDL3 native desktop platform integration;
- an SDL3 GPU drawing and Prism backend;
- native SDL3 smoke coverage on Windows, Linux, and macOS;
- WindowsDX and SDL3 GPU differential and pixel-conformance coverage.

Maturity: SDL3 GPU has completed its initial end-to-end backend plan and is the
strategic backend going forward. MonoGame remains available during transition,
but it will be discontinued gradually. No removal release or date is committed
yet.

### AI-Native Repository Workflow

The repository includes agent instructions, semantic navigation, scripts,
skills, tests, native harnesses, visual comparison, benchmarks, API diffs, and
documentation workflows.

This is user-facing project infrastructure. Contributors can use it to produce
the reproduction and verification evidence expected by issues and pull
requests.

## Active Direction

### 1. Make SDL3 GPU The Primary Backend

The target direction is SDL3 for native platform ownership and SDL3 GPU for
rendering.

Required outcomes:

- keep native window, input, cursor, clipboard, and lifecycle behavior covered
  across Windows, Linux, and macOS;
- keep drawing and Prism behavior aligned through deterministic conformance;
- make SDL3 the documented default once repository defaults and samples are
  actually changed;
- retire MonoGame incrementally without breaking an undocumented set of users;
- document the compatibility and removal policy before deleting the adapter.

The current project files still default some samples to MonoGame. Documentation
must state that fact until the defaults change.

### 2. Cerberus V2

Cerberus V2 is the next planned architecture for the SDL3 GPU drawing path. Its
checked-in plans split the work into:

1. a top-level core, compiler, encoder, and state shadow;
2. frame scheduling and upload arenas;
3. retained compiled packets and GPU geometry;
4. multi-texture material pages.

This initiative is planned, not delivered. The plan checklists remain RED or
unchecked until each stage passes its tests, conformance, lifecycle, and
benchmark gates.

Plan index:
[`docs/plans/2026-08-27-cerberus-v2.md`](docs/plans/2026-08-27-cerberus-v2.md)

### 3. Preserve Retained Frame Behavior While The Renderer Changes

Renderer work must not move retained ownership into the backend.

Required invariants:

- UI state and layout remain owned by the retained runtime;
- unchanged elements do not regenerate local commands;
- backend batching does not reorder visible semantics;
- render and upload caches release resources deterministically;
- warmup and cache behavior are measured before allocation or performance
  claims are made;
- WindowsDX and SDL3 GPU disagreement is investigated against the semantic
  contract before a baseline is updated.

### 4. Improve The External Developer Experience

Cerneala is still source-first. Before it can claim normal framework onboarding,
the project needs explicit decisions and verified work for:

- package boundaries and versioning;
- project templates;
- backend selection defaults;
- installation and upgrade instructions;
- a minimal maintained application sample;
- compatibility policy for public API changes during preview.

Package splitting is not authorized merely because a future package name can be
imagined. The split must follow real ownership boundaries and consumer needs.

## Known Incomplete Areas

These are known gaps, not hidden promises:

- native accessibility adapters are incomplete;
- full IME, multiline editing, and rich text are incomplete;
- platform-backed advanced touch, stylus, drag and drop, and manipulation
  scenarios have uneven maturity;
- package distribution and project templates are unfinished;
- backend maturity and feature parity are uneven;
- the public compatibility policy for the MonoGame transition is not yet
  versioned;
- some older planning and scope documents describe superseded architecture and
  must not override current source, tests, completed plans, or this roadmap.

## Deferred Until Proven Necessary

The following work must not expand the core by default:

- general WPF or Avalonia XAML compatibility;
- runtime interpretation of `.crn`;
- duplicate geometry, color, text, input, or tree systems;
- new rendering abstractions that bypass `DrawingContext`, `DrawCommandList`,
  or `IDrawingBackend`;
- speculative package splits;
- metadata-only advanced input APIs presented as working platform features;
- visual effects that have no backend implementation and conformance scenario.

## Verification Gates

A roadmap item is not complete because code was written. Depending on the
surface, completion can require:

- a valid RED regression or explicit pre-change baseline;
- focused tests;
- affected project suites;
- the full solution suite;
- the original runtime reproduction;
- native platform smokes;
- screenshot and pixel or color conformance;
- performance and resource-lifetime measurements;
- public API diff review;
- canonical API documentation and manifest synchronization.

If an applicable gate cannot run, the item remains unverified and the blocker
must be recorded.

## Planning Records

Detailed work is executed through dated checklist plans under `docs/plans/`.
Those files record stage gates and implementation evidence. A checked stage
means implemented and verified, not merely attempted.

Important current records:

- [SDL3 and SDL3 GPU backend](docs/plans/2026-08-25-sdl3-sdlgpu-backend-and-explicit-generator-selection.md)
- [`RenderSurface2D` complete drawing API](docs/plans/2026-08-24-rendersurface2d-complete-drawing-api.md)
- [Unified Aspect runtime](docs/plans/2026-08-27-unify-aspect-runtime.md)
- [Cerberus V2 plan index](docs/plans/2026-08-27-cerberus-v2.md)

`ROADMAPv2.md` and `ROADMAPv2_AUDIT.md` remain historical planning and audit
records. They contain useful evidence, but stale paths and maturity statements
inside them do not override this document or the current repository.

## Community And Contributions

Issues and pull requests are welcome when they document the observed behavior,
expected contract, reproduction, ownership argument, verification, and
remaining uncertainty.

- [Cerneala website](https://chevalier12.github.io/Cerneala/)
- [Contributors](https://chevalier12.github.io/Cerneala/contributors.html)
- [Discord](https://discord.gg/p6SbqByd59)
