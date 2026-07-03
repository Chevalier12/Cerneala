# Diagnostics Devtools

## Purpose

Defines retained UI diagnostics, debug dumpers, routed-event/style tracing, and developer overlays for Cerneala.

## Requirements

### Requirement: Frame diagnostics expose retained frame counters
Cerneala SHALL provide frame diagnostics that expose per-frame measure, arrange, render-cache, hit-test, reused-cache, and no-work counters from retained scheduler stats.

#### Scenario: Snapshot captures frame counters
- **WHEN** diagnostics inspect a frame stats instance after retained scheduler work
- **THEN** the snapshot reports the measure, arrange, render-cache, hit-test, reused-cache, no-work, and has-work values from that frame

#### Scenario: Frame diagnostics are format-stable
- **WHEN** frame diagnostics are formatted for an overlay or log
- **THEN** the output contains deterministic counter names and values

### Requirement: Layout diagnostics expose retained element layout state
Cerneala SHALL provide layout diagnostics that expose an element's desired size, arranged bounds, layout version, visibility, and layout cache inputs when available.

#### Scenario: Layout snapshot captures element layout state
- **WHEN** diagnostics inspect a retained element
- **THEN** the snapshot reports desired size, arranged bounds, layout version, visibility, and known measure or arrange cache inputs

### Requirement: Render diagnostics expose cache state
Cerneala SHALL provide render diagnostics that expose root render cache validity, cache version, element render versions, render dependency state, and local command counts when available.

#### Scenario: Render cache dump includes root and element caches
- **WHEN** diagnostics dump a retained root render cache
- **THEN** the dump reports root cache validity, root version, and inspected element cache state in deterministic tree order

### Requirement: Input diagnostics expose route and target state
Cerneala SHALL provide input diagnostics that expose retained hit-test targets and routed event paths without requiring event handlers to run.

#### Scenario: Routed event trace follows routing strategy
- **WHEN** diagnostics trace a direct, bubble, or tunnel routed event for a retained target
- **THEN** the trace reports the same element order that retained routed input would use for that routing strategy

### Requirement: Dirty tree diagnostics expose dirty elements and reasons
Cerneala SHALL provide dirty tree diagnostics that list retained elements with active dirty flags, dirty versions, and known invalidation reasons or source property names.

#### Scenario: Dirty tree dump includes dirty flags
- **WHEN** a retained tree contains dirty elements
- **THEN** the dirty tree dump lists only dirty elements with their element id, dirty flags, dirty version, and available diagnostic reason data

#### Scenario: Clean tree dump is explicit
- **WHEN** a retained tree contains no dirty elements
- **THEN** the dirty tree dump reports that no dirty elements are present

### Requirement: Element tree diagnostics expose retained tree structure
Cerneala SHALL provide element tree diagnostics that dump retained logical and visual tree structure with stable indentation, element ids, element type names, visibility, layout bounds, and dirty flags.

#### Scenario: Element tree dump is deterministic
- **WHEN** diagnostics dump the same retained tree twice without structural changes
- **THEN** both dumps have the same order and text

### Requirement: Style tracing exposes value sources
Cerneala SHALL provide style tracing that reports matched rules, applied values, cleared values, and the effective source for inspected styled property values.

#### Scenario: Style trace reports effective source
- **WHEN** diagnostics inspect a styled property value
- **THEN** the trace identifies the property name, effective value, effective property source, and style rule information when style data exists

### Requirement: Debug overlays are retained UI
Cerneala SHALL provide debug overlay and debug adorner primitives that display diagnostics through retained UI elements without bypassing retained layout, invalidation, or render-cache behavior.

#### Scenario: Debug overlay uses retained content
- **WHEN** a debug overlay is updated with diagnostic text
- **THEN** the overlay stores the text in retained UI state and participates in normal layout and render invalidation

### Requirement: Diagnostics remain backend-neutral and tested
Cerneala SHALL keep diagnostics independent of concrete drawing and platform backends and SHALL include focused tests for diagnostics behavior.

#### Scenario: Diagnostics avoid backend references
- **WHEN** diagnostics code is compiled
- **THEN** `UI/Diagnostics` does not reference MonoGame, Skia, HarfBuzz, `Texture2D`, `SpriteBatch`, or concrete drawing backends

#### Scenario: Required diagnostics tests exist
- **WHEN** this implementation phase is complete
- **THEN** tests exist for frame diagnostics, dirty tree dumping, element tree dumping, render cache dumping, routed event tracing, and style tracing

#### Scenario: Full tests pass
- **WHEN** this implementation phase is complete
- **THEN** `dotnet test` passes
