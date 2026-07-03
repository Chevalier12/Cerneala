## ADDED Requirements

### Requirement: Animation ticks integrate with retained invalidation
Cerneala SHALL let animation ticks raise retained invalidation through existing property metadata and frame queues.

#### Scenario: Render-only animation tick skips measure queue
- **WHEN** an animation tick changes a render-only property
- **THEN** the retained measure queue is not scheduled by that tick

#### Scenario: Layout animation tick schedules measure only on value change
- **WHEN** an animation tick does not change the effective value of a layout-affecting property
- **THEN** no new measure work is scheduled for that no-op tick
