## ADDED Requirements

### Requirement: Retained click behavior can invoke commands
Cerneala SHALL connect retained click synthesis to command execution for command-capable retained controls.

#### Scenario: Click target command is invoked after matching release
- **WHEN** retained input synthesizes a click for a `ButtonBase` with a command
- **THEN** command execution is attempted after the matching release is accepted as a click

#### Scenario: Canceled click does not invoke command
- **WHEN** retained click tracking cancels a press or release because the click target does not match
- **THEN** no button command is executed

#### Scenario: Disabled command target does not receive command execution
- **WHEN** retained input excludes a disabled command target from hit testing
- **THEN** the excluded target's command is not executed by the input bridge
