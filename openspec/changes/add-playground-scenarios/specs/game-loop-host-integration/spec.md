## ADDED Requirements

### Requirement: Game1 wires retained playground samples
Cerneala SHALL wire `Game1` to retained playground samples through `MonoGameUiHost`.

#### Scenario: Game1 creates sample selector root
- **WHEN** playground content is loaded
- **THEN** `Game1` creates a retained root containing the sample selector and active retained sample

#### Scenario: Game1 update forwards to host
- **WHEN** the MonoGame update loop runs
- **THEN** `Game1.Update(...)` forwards viewport and elapsed time to `MonoGameUiHost.Update(...)`

#### Scenario: Game1 draw forwards to host
- **WHEN** the MonoGame draw loop runs
- **THEN** `Game1.Draw(...)` calls `MonoGameUiHost.Draw(...)` so cached retained commands are submitted every frame

#### Scenario: Playground avoids custom immediate demo element
- **WHEN** the playground sample UI is rendered
- **THEN** retained controls and sample classes own the UI content instead of a single local custom immediate-style demo element
