## Why

The retained input bridge can now route pointer, keyboard, focus, and text input, but command execution still stops at low-level primitives. This change completes the MVP command path so controls can query and execute commands through explicit retained routes instead of hidden global requery behavior.

## What Changes

- Add an explicit retained `CommandRouter` that can query and execute commands along the current retained route.
- Add `CommandBindingCollection` so retained elements can own command bindings without ad-hoc handler wiring.
- Complete `RoutedCommand` behavior by routing `CanExecute` and `Execute` through explicit command router APIs.
- Add `RoutedCommandContext` to carry command target, parameter, route map, and routing options.
- Add `ActionCommand` for simple delegate-backed commands that do not need routed lookup.
- Add `ButtonBase.CommandProperty` and `ButtonBase.CommandParameterProperty` so press/click capable controls can trigger commands.
- Defer keyboard gesture and input binding implementation unless command routing exposes a concrete MVP need during implementation.

## Capabilities

### New Capabilities

- `command-router-actions`: Explicit retained command routing, command binding ownership, routed command execution, delegate commands, and button command integration.

### Modified Capabilities

- `retained-input-bridge`: Command execution is integrated with retained click/input behavior through `ButtonBase`.
- `retained-ui-mvp-foundation`: MVP roadmap progress includes retained commands and actions as the next input/control bridge phase.

## Impact

- Affected production code:
  - `UI/Input/ICommand.cs`
  - `UI/Input/RoutedCommand.cs`
  - `UI/Input/CommandBinding.cs`
  - `UI/Input/CommandEvents.cs`
  - `UI/Input/CommandBindingCollection.cs`
  - `UI/Input/CommandRouter.cs`
  - `UI/Input/RoutedCommandContext.cs`
  - `UI/Input/ActionCommand.cs`
  - `UI/Elements/UIElement.cs`
  - `UI/Controls/Primitives/ButtonBase.cs`
  - `UI/Input/ElementInputBridge.cs`
- Affected tests:
  - `tests/Cerneala.Tests/Input/CommandRouterTests.cs`
  - `tests/Cerneala.Tests/Input/CommandBindingCollectionTests.cs`
  - `tests/Cerneala.Tests/Input/ActionCommandTests.cs`
  - `tests/Cerneala.Tests/Input/RoutedCommandExecutionTests.cs`
  - `tests/Cerneala.Tests/Controls/Primitives/ButtonBaseCommandTests.cs`
- Affected planning:
  - `ROADMAPv2.md`
  - `openspec/specs/command-router-actions/spec.md`
  - `openspec/specs/retained-input-bridge/spec.md`
  - `openspec/specs/retained-ui-mvp-foundation/spec.md`
