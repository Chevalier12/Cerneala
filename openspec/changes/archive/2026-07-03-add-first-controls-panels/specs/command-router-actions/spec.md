## ADDED Requirements

### Requirement: Concrete Button uses retained command routing
Cerneala SHALL integrate the concrete `Button` control with existing direct and routed command execution.

#### Scenario: Button executes direct command
- **WHEN** a retained button with an `ActionCommand` receives a valid click
- **THEN** the action command executes with the button command parameter

#### Scenario: Button executes routed command
- **WHEN** a retained button with a `RoutedCommand` receives a valid click
- **THEN** command execution uses `CommandRouter` and the retained route

#### Scenario: Button respects command availability
- **WHEN** a retained button command cannot execute
- **THEN** the button does not execute the command and can expose disabled visual state through retained typed state
