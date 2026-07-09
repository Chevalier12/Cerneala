# RetainedInputBindingProcessor Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/RetainedInputBindingProcessor.cs`

Processes keyboard dispatch results and executes matching retained input bindings before keyboard navigation or default keyboard activation runs.

```csharp
internal sealed class RetainedInputBindingProcessor
```

Inheritance:
`Object` -> `RetainedInputBindingProcessor`

## Examples

`RetainedInputBindingProcessor` is used by `ElementInputBridge` after focused keyboard events have been routed.

```csharp
IReadOnlyList<KeyboardDispatchResult> keyboardResults =
    focusManager.DispatchKeyboardWithResults(inputFrame, routeMap);

IReadOnlyList<KeyboardDispatchResult> activationResults =
    retainedInputBindingProcessor.Process(keyboardResults, inputFrame, commandRouter, routeMap);

keyboardNavigationController.Process(activationResults, inputFrame, root, focusManager, routeMap);
keyboardActivationController.Process(activationResults, focusManager, commandRouter, routeMap);
```

## Remarks

`RetainedInputBindingProcessor` is a stateless internal input-pipeline helper. It receives the keyboard dispatch results produced by `FocusManager`, checks only unhandled key press results, and walks from the focused element toward its visual ancestors looking for matching `InputBinding` entries.

Bindings are attempted only on valid retained input elements. An owner must be attached, enabled, visible for input participation, and present in the supplied `ElementInputRouteMap`. The first binding that executes successfully stops the focused-route scan for that keyboard result.

When an input binding executes, the original keyboard dispatch result is omitted from the returned activation-results list. This prevents later keyboard navigation or default keyboard activation from also handling the same key press. Results that are already handled, are key releases, or do not execute a binding are preserved and returned for downstream processing.

For routed commands, binding execution delegates to `CommandRouter` through `InputBinding.TryExecute(InputFrame, CommandRouter, ElementInputRouteMap, UIElement)`. Direct commands use the binding's own `ICommand.CanExecute` and `ICommand.Execute` flow.

## Constructors

| Name | Description |
| --- | --- |
| `RetainedInputBindingProcessor()` | Initializes a stateless retained input binding processor. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Process(IReadOnlyList<KeyboardDispatchResult>, InputFrame, CommandRouter, ElementInputRouteMap)` | `IReadOnlyList<KeyboardDispatchResult>` | Executes the first matching input binding for each unhandled key press and returns the keyboard results that should continue to keyboard navigation and activation. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Process(IReadOnlyList<KeyboardDispatchResult>, InputFrame, CommandRouter, ElementInputRouteMap)` | `ArgumentNullException` | `results`, `inputFrame`, `commandRouter`, or `routeMap` is `null`. |

## Applies to

`Cerneala` retained UI keyboard input binding processing.

## See also

- `Cerneala.UI.Input.ElementInputBridge`
- `Cerneala.UI.Input.InputBinding`
- `Cerneala.UI.Input.KeyBinding`
- `Cerneala.UI.Input.CommandRouter`
- `Cerneala.UI.Input.FocusManager`
