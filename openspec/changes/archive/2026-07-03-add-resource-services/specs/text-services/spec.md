## ADDED Requirements

### Requirement: Text services resolve font resources explicitly
Cerneala SHALL allow text services to resolve font resources through explicit resource services.

#### Scenario: Font resolver resolves font resource
- **WHEN** a text style or text control references a font resource id
- **THEN** `FontResolver` resolves it through an explicit resource provider and returns an `IDrawFont`

#### Scenario: Font resource replacement changes text cache identity
- **WHEN** a font resource used for measured text is replaced
- **THEN** the text layout cache identity changes and dependent measurement is recomputed

#### Scenario: Font resource lookup remains non-global
- **WHEN** text services resolve resource-backed fonts
- **THEN** lookup occurs through explicitly supplied resource services rather than hidden global state
