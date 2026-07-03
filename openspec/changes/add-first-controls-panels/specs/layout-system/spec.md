## ADDED Requirements

### Requirement: Controls-facing panels reuse layout panel behavior
Cerneala SHALL expose controls-facing panel types without duplicating or weakening existing retained layout semantics.

#### Scenario: Controls Panel uses visual children for layout
- **WHEN** `UI/Controls/Panel` is measured or arranged
- **THEN** it lays out retained visual children using the same behavior as the layout panel base

#### Scenario: Controls Canvas matches layout Canvas behavior
- **WHEN** `UI/Controls/Canvas` arranges children
- **THEN** child arranged bounds match `UI/Layout/Panels/Canvas` behavior for the same inputs

#### Scenario: Controls StackPanel matches layout StackPanel behavior
- **WHEN** `UI/Controls/StackPanel` measures and arranges children
- **THEN** desired size and arranged bounds match `UI/Layout/Panels/StackPanel` behavior for the same orientation and children
