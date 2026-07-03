## Context

Cerneala already has retained elements, typed `UiProperty<T>` state, layout, invalidation, render caching, code-first templates, resources, styling, controls, and item virtualization. The project intentionally stays code-first, so markup must be optional and must not recreate WPF's broad runtime reflection model.

Roadmap section 25 is experimental. The useful near-term slice is a deterministic runtime markup representation plus explicit factories. A standalone source-generator project is a later optimization unless reflection or tooling cost becomes proven.

## Goals / Non-Goals

**Goals:**

- Represent UI markup as a small document/node/value model that can be read, written, diagnosed, and loaded deterministically.
- Create retained `UIElement` trees through explicit type/property registrations.
- Route property assignment through existing typed property setters so validation, coercion, invalidation, and render-cache behavior stay identical to code-first creation.
- Provide a `GeneratedUiFactory` seam that precompiled/generated code can implement without requiring a source generator in this phase.
- Keep parsing, serialization, and diagnostics backend-neutral and dependency-free.

**Non-Goals:**

- No XAML compatibility.
- No runtime binding expressions, resource dictionaries, styles, templates, animations, event handlers, or arbitrary reflection invocation from markup.
- No standalone `Cerneala.SourceGen` project in this phase.
- No requirement that applications use markup; code-first APIs remain primary.

## Decisions

### Markup document is an explicit model

`UiMarkupDocument` should own a root `UiMarkupNode`. Nodes should hold an element type name, ordered attributes, ordered child nodes, optional text content, and diagnostic/source information when available.

Rationale: explicit nodes make reader/writer/factory tests deterministic and keep the format independent from XML implementation details.

Alternative considered: load XML directly into controls. Rejected because diagnostics, round-tripping, and factory validation would become tangled with parsing.

### XML is the initial wire format

`UiMarkupReader` and `UiMarkupWriter` should use `System.Xml.Linq` for a tiny XML subset: element names map to registered UI types, attributes map to registered properties, text content maps to a registered content property, and nested elements become children/content depending on the registered schema.

Rationale: XML is built into .NET, human-readable, and close enough to familiar UI markup without committing to XAML.

Alternative considered: JSON. Rejected for now because UI trees are naturally nested and XML attributes map cleanly to simple typed properties.

### Registry is explicit, not reflection-first

`UiMarkupTypeRegistry` should register element names with constructor delegates and property setter/converter delegates. `UiMarkupSchema` should expose default runtime registrations for the controls that are stable enough to serialize now.

Rationale: explicit registration avoids hidden reflection costs and makes unsupported types/properties produce useful diagnostics.

Alternative considered: discover every `UiProperty<T>` with reflection. Rejected because it would expose too much surface area and could bypass intentional public API boundaries.

### Factory uses typed assignment

`UiFactory` should create instances from registered element factories and apply attributes/content through registered setters that call existing public properties or `SetValue` paths. Invalid values must be reported as `MarkupDiagnostic` errors and must not silently create partially invalid trees unless options explicitly allow recovery.

Rationale: typed-state validation remains the single source of truth.

Alternative considered: assign backing fields or property-store entries directly. Rejected because it bypasses validation, coercion, and invalidation.

### Generated factories are a runtime seam

`GeneratedUiFactory` should be a small abstraction for already-compiled factories that produce retained trees and diagnostics. It should prove that generated factories can share the same retained pipeline, without adding the source-generator project yet.

Rationale: the project gets the architectural seam without pretending source generation is required before runtime markup is useful.

Alternative considered: build `Cerneala.SourceGen` now. Rejected as premature because the roadmap marks it optional future work and the current repo has no source-generator project shape.

## Risks / Trade-offs

- [Risk] Markup can grow into a WPF clone. -> Keep this phase limited to tree construction, primitive value conversion, deterministic serialization, and explicit registries.
- [Risk] Diagnostics may hide invalid typed values. -> Treat conversion/validation failures as errors by default and cover them with tests.
- [Risk] Content assignment can accidentally reparent children incorrectly. -> Use existing public control properties and child collection APIs so retained tree validation remains active.
- [Risk] Serialization can imply full round-trip support for every control. -> Only guarantee round-trip for registered simple properties and child/content relationships.
- [Risk] Source generation scope can explode. -> Add only the runtime `GeneratedUiFactory` seam and leave the source-generator project unchecked/deferred in `ROADMAPv2.md`.
