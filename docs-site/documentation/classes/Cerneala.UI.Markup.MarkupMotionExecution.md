# MarkupMotionExecution Class

## Definition
Namespace: `Cerneala.UI.Markup`
Assembly/Project: `Cerneala`
Source: `UI/Markup/MarkupMotionExecution.cs`

Represents one generated Motion execution with the same completion and
cancellation contract for leaf handles and composed groups.

```csharp
public sealed class MarkupMotionExecution
```

## Examples
```csharp
MarkupMotionExecution execution = MarkupMotionExecution.Sequence(
    () => MarkupMotionExecution.From(firstHandle),
    () => MarkupMotionExecution.Parallel(
        () => MarkupMotionExecution.From(secondHandle),
        () => MarkupMotionExecution.From(thirdHandle)));

execution.Cancel();
```

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `IsCompleted` | `bool` | Indicates that every required child completed naturally. |
| `IsCanceled` | `bool` | Indicates that the execution was canceled. |
| `Completion` | `ValueTask` | Completes successfully after natural completion and is canceled after cancellation. |

## Methods
| Signature | Return Type | Description |
| --- | --- | --- |
| `From(MotionHandle handle)` | `MarkupMotionExecution` | Adapts a leaf handle and uses `KeepCurrent` when the execution is canceled. |
| `From(MotionGroupHandle handle)` | `MarkupMotionExecution` | Adapts a runtime group without inventing leaf-only completion or cancellation options. |
| `Parallel(params Func<MarkupMotionExecution>[] children)` | `MarkupMotionExecution` | Starts every child immediately and completes after all children complete naturally. |
| `Sequence(params Func<MarkupMotionExecution>[] children)` | `MarkupMotionExecution` | Starts each child only after the previous child completes naturally. |
| `Cancel()` | `void` | Idempotently cancels active children and prevents deferred sequence children from starting. |

## Events
| Name | Type | Description |
| --- | --- | --- |
| `Completed` | `EventHandler` | Raised exactly once when the execution reaches either terminal state. |

## Remarks
This adapter is intended for source-generated markup composition. Empty groups
complete immediately, while a one-child group preserves the child's terminal
state. A canceled child cancels its parent composition. `Sequence` advances
only after natural completion, so cancellation never starts a later child.

The class deliberately exposes no generic `Complete` operation and no
selectable cancellation behavior. `MotionGroupHandle` does not support those
leaf-only operations.

## Exceptions
| Member | Exception | Condition |
| --- | --- | --- |
| `From`, `Parallel`, `Sequence` | `ArgumentNullException` | A required handle, child array, or child factory is `null`. |
| `Parallel`, `Sequence` | `InvalidOperationException` | A child factory returns `null`. |

## Applies to
Source-generated Motion markup execution trees.

## See Also
- `Cerneala.UI.Markup.GeneratedMarkup`
- `Cerneala.UI.Motion.Core.MotionHandle`
- `Cerneala.UI.Motion.Core.MotionGroupHandle`
