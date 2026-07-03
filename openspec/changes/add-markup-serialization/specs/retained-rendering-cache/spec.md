## ADDED Requirements

### Requirement: Markup-created trees use retained invalidation and render-cache paths
Cerneala SHALL ensure retained trees created from markup factories use the same invalidation and render-cache paths as trees created directly in code.

#### Scenario: Markup-created property change invalidates render cache
- **WHEN** a render-affecting property changes on an element created from markup
- **THEN** the element invalidates retained rendering state through the existing render-cache pipeline

#### Scenario: Generated factory tree renders through retained renderer
- **WHEN** a generated factory creates a retained tree
- **THEN** the tree can be measured, arranged, and submitted through the retained renderer without a separate markup rendering path
