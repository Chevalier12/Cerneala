# ServoTargetNotActionableException Class

## Definition
Namespace: `Cerneala.UI.Servo`
Assembly/Project: `Cerneala`
Source: `UI/Servo/ServoException.cs`

Indicates that a resolved target cannot perform the requested Servo action.

```csharp
public sealed class ServoTargetNotActionableException : ServoException
```

## Examples

```csharp
ServoException failure = new ServoTargetNotActionableException("Target is hidden.");
```

## Remarks

Actionability checks are owned by Servo actions rather than by query snapshots.

Target input actions (`ClickAsync`, `HoverAsync`, `DragAsync`, `ScrollAsync`, `TypeIntoAsync`, `ReplaceTextAsync`) do not throw this exception to the caller. They retry after each rendered frame while the target is hidden, disabled, detached, without usable bounds, or covered at its center. When `ServoOptions.DefaultTimeout` expires first, they throw `ServoTimeoutException`, and its `InnerException` is the last `ServoTargetNotActionableException`. For example, a click on a button that stays disabled throws `ServoTimeoutException` whose `InnerException.Message` is `"The Servo target is disabled."`.

A target screenshot (`SaveScreenshotAsync(ServoTarget, string, CancellationToken)`) does not retry and still throws this exception at once when the target is hidden or has no visible pixels.

## Constructors

| Name | Description |
| --- | --- |
| `ServoTargetNotActionableException(string)` | Creates the exception with a diagnostic message. |

## Applies to

Servo input and capture actions.
