## ADDED Requirements

### Requirement: Retained elements expose layout state
Cerneala SHALL expose layout state on retained elements without making drawing primitives the source of layout truth.

#### Scenario: Element exposes desired size
- **WHEN** an element has been measured
- **THEN** retained element state exposes its desired layout size

#### Scenario: Element exposes arranged bounds
- **WHEN** an element has been arranged
- **THEN** retained element state exposes its arranged layout bounds

#### Scenario: Visual children participate in layout
- **WHEN** a retained element measures or arranges its visual subtree
- **THEN** layout uses the retained visual child relationship

### Requirement: Retained roots own layout management
Cerneala SHALL make retained roots the owner of layout management for attached visual trees.

#### Scenario: Root exposes layout manager
- **WHEN** a retained root exists
- **THEN** it exposes or owns the layout manager used by frame scheduling

#### Scenario: Root viewport supplies layout constraint
- **WHEN** root layout is processed
- **THEN** root viewport width, height, and scale are available to layout processing
