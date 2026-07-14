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

`CanExecuteChanged` is a state notification rather than a delta. Attached `ButtonBase` controls process it immediately on the UI thread and coalesce off-thread bursts through their root Relay. The command's `CanExecute` method is not called on the producer thread.

Controls unsubscribe when detached or when their command changes, and queued callbacks from an inactive subscription do not update the control.

## Events

| Name | Description |
| --- | --- |
| `CanExecuteChanged` | Requests that active command sources re-query `CanExecute`. The event may be raised from a worker thread when the consumer supports Relay dispatch. |

## Applies to

Cerneala UI runtime and framework API consumers.
