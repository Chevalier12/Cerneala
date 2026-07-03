## ADDED Requirements

### Requirement: Host draw submits retained root cache
Cerneala SHALL expose retained rendering cache submission through `UiHost.Draw(...)`.

#### Scenario: Host draw uses retained renderer
- **WHEN** `UiHost.Draw(...)` is called
- **THEN** it submits the retained root command cache through the root retained renderer and the provided `IDrawingBackend`

#### Scenario: Cached root commands survive unchanged frames
- **WHEN** host update and draw run repeatedly without retained invalidation
- **THEN** the retained root command cache remains reusable across draw frames

#### Scenario: Host draw keeps backend-neutral cache contract
- **WHEN** cached retained commands are submitted by the host
- **THEN** the submission remains expressed as `DrawCommandList` rendered by `IDrawingBackend`
