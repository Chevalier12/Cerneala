## ADDED Requirements

### Requirement: Host update drives retained scheduler
Cerneala SHALL use `UiHost.Update(...)` as the application-facing driver for retained scheduler work.

#### Scenario: Host update reports scheduler stats
- **WHEN** `UiHost.Update(...)` processes retained dirty work
- **THEN** the resulting host frame exposes the `FrameStats` produced by the retained root scheduler

#### Scenario: Host update preserves no-work behavior
- **WHEN** no retained work is dirty during a host update
- **THEN** the retained scheduler reports a no-work frame through host diagnostics

#### Scenario: Host draw does not drive scheduler
- **WHEN** `UiHost.Draw(...)` submits retained output
- **THEN** scheduler phase work is not processed from the draw path
