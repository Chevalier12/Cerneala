# IObservableCommand Interface

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/IObservableCommand.cs`

Extends `ICommand` with a notification that asks attached command controls to re-query executable state.

```csharp
public interface IObservableCommand : ICommand
```

## Remarks

`CanExecuteChanged` is a state notification rather than a delta. On the UI thread, an attached `ButtonBase` queues a command-state refresh that runs in the next frame's command-state phase. Off-thread bursts are coalesced through the root Relay. The command's `CanExecute` method is not called on the producer thread.

The control does not read the event's `sender`. A command may raise `CanExecuteChanged?.Invoke(null, EventArgs.Empty)`, or a wrapper command may forward an inner command's event with the inner command as `sender`; in both cases a `Button` whose `Command` is that command re-queries `CanExecute` in the next frame and updates `IsEnabled`. For example, when `CanExecute` now returns `false`, the button's `IsEnabled` becomes `false` after the next frame.

Controls unsubscribe when detached or when their command changes, and queued callbacks from an inactive subscription do not update the control.

## Events

| Name | Description |
| --- | --- |
| `CanExecuteChanged` | Requests that active command sources re-query `CanExecute`. The event may be raised from a worker thread when the consumer supports Relay dispatch. |

## Applies to

Cerneala UI runtime and framework API consumers.
