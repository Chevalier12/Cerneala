# command-router-actions Specification

## Purpose
TBD - created by archiving change add-command-router-actions. Update Purpose after archive.
## Requirements
### Requirement: Retained elements own command bindings
Cerneala SHALL allow retained elements to own command bindings through a deterministic `CommandBindingCollection`.

#### Scenario: Binding collection stores bindings in insertion order
- **WHEN** command bindings are added to a retained element
- **THEN** command routing observes those bindings in insertion order for that element

#### Scenario: Binding collection rejects null bindings
- **WHEN** null is added to a command binding collection
- **THEN** the collection rejects the value with an argument error

#### Scenario: Element exposes command bindings
- **WHEN** a retained `UIElement` is created
- **THEN** it exposes a command binding collection for route-based command handlers

### Requirement: Command router queries retained command routes
Cerneala SHALL provide an explicit `CommandRouter` that evaluates `CanExecute` over retained command routes without global state.

#### Scenario: CanExecute uses retained route
- **WHEN** `CanExecute` is queried for a routed command with a retained target
- **THEN** the router raises preview and bubble can-execute command events through the retained route

#### Scenario: Handled preview can-execute suppresses bubble
- **WHEN** preview can-execute is handled
- **THEN** the matching bubble can-execute event is not raised

#### Scenario: Matching binding sets can-execute
- **WHEN** a command binding on the route matches the queried command
- **THEN** the binding can set the can-execute result for the query

#### Scenario: Missing target cannot execute
- **WHEN** a routed command query has no retained command target
- **THEN** the router returns false

### Requirement: Command router executes retained commands
Cerneala SHALL execute routed commands through explicit `CommandRouter` APIs and retained command routes.

#### Scenario: Execute uses retained route
- **WHEN** `Execute` is requested for a routed command with a retained target that can execute
- **THEN** the router raises preview and bubble executed command events through the retained route

#### Scenario: Execute is skipped when command cannot execute
- **WHEN** a routed command cannot execute for the retained target
- **THEN** the router does not raise executed command events

#### Scenario: Handled preview execute suppresses bubble
- **WHEN** preview executed is handled
- **THEN** the matching bubble executed event is not raised

#### Scenario: RoutedCommand direct execute requires router
- **WHEN** `RoutedCommand.Execute(object?)` is called without a retained routing context
- **THEN** it fails with a clear error directing callers to use `CommandRouter`

### Requirement: Routed command context is explicit
Cerneala SHALL provide `RoutedCommandContext` to carry command routing inputs without ambient state.

#### Scenario: Context contains command target
- **WHEN** a routed command context is created
- **THEN** it carries the retained command target and route map used by `CommandRouter`

#### Scenario: Context contains command parameter
- **WHEN** a routed command context is created with a parameter
- **THEN** command event args raised by the router expose that parameter

### Requirement: ActionCommand provides delegate commands
Cerneala SHALL provide `ActionCommand` for direct delegate-backed command usage.

#### Scenario: Action command executes delegate
- **WHEN** an action command is executed and can execute
- **THEN** it invokes the configured execute delegate with the parameter

#### Scenario: Action command respects can-execute delegate
- **WHEN** an action command has a can-execute delegate
- **THEN** `CanExecute` returns that delegate's result

#### Scenario: Action command defaults to executable
- **WHEN** an action command has no can-execute delegate
- **THEN** `CanExecute` returns true

### Requirement: ButtonBase integrates commands
Cerneala SHALL expose command properties on `ButtonBase` and execute them from retained click behavior.

#### Scenario: ButtonBase exposes command properties
- **WHEN** the command phase is implemented
- **THEN** `ButtonBase.CommandProperty` and `ButtonBase.CommandParameterProperty` exist as typed properties

#### Scenario: Button click executes direct command
- **WHEN** a retained `ButtonBase` receives a valid click and its direct command can execute
- **THEN** the command executes with `ButtonBase.CommandParameter`

#### Scenario: Button click executes routed command through router
- **WHEN** a retained `ButtonBase` receives a valid click and its command is routed
- **THEN** the command executes through `CommandRouter` using the button as command target

#### Scenario: Button click does not execute disabled command
- **WHEN** a retained `ButtonBase` receives a click but its command cannot execute
- **THEN** no command execution occurs

### Requirement: Command state invalidates visual state explicitly
Cerneala SHALL update command-driven visual state through explicit retained invalidation instead of hidden global requery.

#### Scenario: Command state refresh can affect enabled state
- **WHEN** a button command state refresh determines that a command cannot execute
- **THEN** the button can expose disabled visual/input state through retained typed state

#### Scenario: Command state change invalidates input visual state
- **WHEN** command state changes a button visual state
- **THEN** the affected element invalidates input visual or render state through existing invalidation metadata

### Requirement: Command routing is tested
Cerneala SHALL include focused command tests for routing, bindings, delegate commands, routed command execution, and button command integration.

#### Scenario: Required command tests exist
- **WHEN** this implementation phase is complete
- **THEN** command tests exist for `CommandRouter`, `CommandBindingCollection`, `ActionCommand`, routed command execution, and `ButtonBase` command behavior

#### Scenario: Full tests pass
- **WHEN** this implementation phase is complete
- **THEN** `dotnet test` passes

