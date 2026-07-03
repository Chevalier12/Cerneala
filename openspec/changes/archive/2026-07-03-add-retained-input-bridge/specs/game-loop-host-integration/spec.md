## ADDED Requirements

### Requirement: Host update dispatches retained input
Cerneala SHALL dispatch retained input through the input bridge during `UiHost.Update(...)` before retained scheduler processing.

#### Scenario: Input bridge runs before scheduler
- **WHEN** `UiHost.Update(...)` receives an input frame
- **THEN** retained input dispatch runs before layout, render-cache, and hit-test queue processing

#### Scenario: Input visual state can affect same frame work
- **WHEN** retained input dispatch changes hover, pressed, or focus visual state
- **THEN** the resulting invalidation is processed by the same update frame scheduler pass

#### Scenario: Host remains backend-neutral
- **WHEN** retained input dispatch is integrated into host update
- **THEN** core hosting continues to depend on input abstractions rather than MonoGame APIs
