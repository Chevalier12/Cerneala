## MODIFIED Requirements

### Requirement: Retained-mode frame loop is documented
Cerneala SHALL document and implement the foundation that allows the game loop to run every frame while layout and render command generation are invalidation-driven.

#### Scenario: Unchanged frame work is avoided
- **WHEN** no retained UI state changes between frames
- **THEN** the architecture documentation and retained invalidation scheduler state that layout and render command generation are reused rather than recomputed

#### Scenario: Dirty state drives work
- **WHEN** retained UI state changes
- **THEN** the architecture documentation and retained invalidation scheduler identify dirty flags, propagation, layout queues, render queues, hit-test queues, and cache-update hooks as the mechanism for recomputing only needed work

#### Scenario: Scheduler is game-loop friendly
- **WHEN** update and draw are called every frame
- **THEN** the retained frame scheduler processes no dirty phase work on unchanged frames while still allowing draw to use cached output

#### Scenario: Layout phase is concrete
- **WHEN** retained measure or arrange work is queued
- **THEN** the retained layout system processes that work before render-cache work in the frame loop
