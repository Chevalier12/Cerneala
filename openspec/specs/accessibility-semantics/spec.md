# Accessibility Semantics

## Purpose

Defines platform-neutral semantics nodes, semantic roles/properties, retained semantics tree building, control semantics providers, automation peer adapters, and accessibility platform boundaries for Cerneala.
## Requirements
### Requirement: Semantics nodes describe retained UI meaning
Cerneala SHALL provide platform-neutral semantics nodes with role, accessible name, retained element id, semantic properties, and child nodes.

#### Scenario: Node captures role and name
- **WHEN** a semantic node is created for a retained element
- **THEN** it records the element id, role, accessible name, and properties without referencing platform APIs

### Requirement: Semantics tree follows retained visual structure
Cerneala SHALL build deterministic semantics trees from retained visual structure while allowing controls to provide semantic metadata.

#### Scenario: Tree preserves semantic child order
- **WHEN** a semantics tree is built for a retained root
- **THEN** semantic children follow retained visual child order

#### Scenario: Hidden elements are omitted
- **WHEN** a retained element is not visible for UI semantics
- **THEN** it is omitted from the semantics tree

### Requirement: Accessible names are explicit and deterministic
Cerneala SHALL provide accessible name helpers that prefer explicit semantic names and fall back to deterministic control text when available.

#### Scenario: Explicit name wins
- **WHEN** a retained element has an explicit accessible name
- **THEN** semantics use that name instead of content-derived text

#### Scenario: Button text can provide name
- **WHEN** a button has text content and no explicit accessible name
- **THEN** button semantics use that text as the accessible name

### Requirement: Control semantics providers expose common roles
Cerneala SHALL provide semantic providers or automation peer adapters for button, textbox, and items controls.

#### Scenario: Button exposes button role
- **WHEN** semantics inspect a retained button
- **THEN** the node role is button and reports enabled state

#### Scenario: TextBox exposes editable text role and value
- **WHEN** semantics inspect a retained textbox
- **THEN** the node role is editable text and reports its text value

#### Scenario: ItemsControl exposes list semantics
- **WHEN** semantics inspect a retained items control
- **THEN** the node role identifies list-like content and can include item children

### Requirement: Accessibility platform boundary is adapter-only
Cerneala SHALL provide an accessibility platform abstraction that consumes semantic tree snapshots without adding native OS dependencies to core UI code.

#### Scenario: Platform boundary receives semantics tree
- **WHEN** a host wants to publish accessibility state
- **THEN** it can pass a semantics tree snapshot through `IAccessibilityPlatform`

#### Scenario: Accessibility core remains backend-neutral
- **WHEN** accessibility code is compiled
- **THEN** `UI/Accessibility` and the accessibility platform abstraction do not reference MonoGame, Skia, HarfBuzz, `Texture2D`, `SpriteBatch`, or native accessibility APIs

### Requirement: Accessibility semantics are tested
Cerneala SHALL include focused tests for semantics tree construction, provider behavior, button semantics, and textbox semantics.

#### Scenario: Required accessibility tests exist
- **WHEN** this implementation phase is complete
- **THEN** tests exist for semantics tree construction, semantics provider behavior, button semantics, and textbox semantics

#### Scenario: Full tests pass
- **WHEN** this implementation phase is complete
- **THEN** `dotnet test` passes

### Requirement: Accessibility platform participates in platform services
Cerneala SHALL expose `IAccessibilityPlatform` through the platform service aggregate without coupling accessibility semantics to native accessibility APIs.

#### Scenario: Accessibility service is optional platform member
- **WHEN** platform services are created with an accessibility platform
- **THEN** callers can retrieve that `IAccessibilityPlatform` through the aggregate

