## ADDED Requirements

### Requirement: Controls render through retained render cache
Cerneala SHALL render retained controls by rebuilding only dirty local element command lists and composing them through existing retained render caches.

#### Scenario: Control render hook emits local commands
- **WHEN** a control is render-dirty
- **THEN** its render hook emits local drawing commands into its element render cache

#### Scenario: Unchanged control render cache is reused
- **WHEN** a control has unchanged render-affecting state across frames
- **THEN** its local render command list is reused

#### Scenario: Control child composition remains retained renderer owned
- **WHEN** a control renders local visuals
- **THEN** child command composition remains owned by the retained renderer
