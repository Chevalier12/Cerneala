## ADDED Requirements

### Requirement: Retained rendering composes advanced media commands
Cerneala SHALL compose advanced media and shape drawing commands through existing retained render caches and `DrawCommandList`.

#### Scenario: Shape local cache emits advanced commands
- **WHEN** a shape control is render-dirty
- **THEN** its local retained render cache records shape drawing commands without composing child visuals

#### Scenario: Advanced commands preserve retained visual order
- **WHEN** advanced shape commands are composed into the root command list
- **THEN** their order follows the existing parent-before-children and sibling visual order rules

#### Scenario: Advanced media commands remain backend-neutral in rendering
- **WHEN** retained rendering composes advanced media output
- **THEN** `UI/Rendering` does not reference MonoGame, Skia, HarfBuzz, `Texture2D`, `SpriteBatch`, or concrete drawing backends
