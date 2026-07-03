## ADDED Requirements

### Requirement: Concrete controls participate in retained input
Cerneala SHALL let concrete retained controls participate in hit testing, hover, press, focus, click, and text input using existing retained input bridge behavior.

#### Scenario: Button receives retained pointer state
- **WHEN** pointer input targets a retained `Button`
- **THEN** hover and pressed visual state are updated through retained input tracking

#### Scenario: Disabled control is excluded from input
- **WHEN** a retained control is disabled
- **THEN** retained hit testing and input routing exclude it according to existing input bridge policy

#### Scenario: Focusable control receives keyboard focus
- **WHEN** retained input focuses a concrete control
- **THEN** keyboard focus state and focus-within state update through the existing focus manager
