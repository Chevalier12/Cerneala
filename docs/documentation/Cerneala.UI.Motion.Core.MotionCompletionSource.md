# MotionCompletionSource Class

## Definition
Namespace: `Cerneala.UI.Motion.Core`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Core/MotionCompletionSource.cs`

Provides a small completion primitive used by motion handles to expose a `Task` that completes or cancels exactly once.

```csharp
public sealed class MotionCompletionSource
```

Inheritance:
`object` -> `MotionCompletionSource`

## Examples

```csharp
using Cerneala.UI.Motion.Core;

MotionCompletionSource completion = new();

Task observedCompletion = completion.Task;
bool completed = completion.TrySetResult();
bool completedAgain = completion.TrySetCanceled();

await observedCompletion;
```

`completed` is `true` for the first terminal transition. `completedAgain` is `false` because the completion source has already been completed.

## Remarks

`MotionCompletionSource` wraps a `TaskCompletionSource<object?>` configured with `TaskCreationOptions.RunContinuationsAsynchronously`. Consumers observe the public `Task`, while motion internals use `TrySetResult` for natural completion and `TrySetCanceled` for canceled motion.

The `TrySet...` methods are idempotent in the same way as `TaskCompletionSource.TrySetResult` and `TaskCompletionSource.TrySetCanceled`: they return `true` only when they successfully move the task into that terminal state. Later attempts to complete or cancel the same instance return `false`.

`MotionHandle` uses this type to back its `Completion` value task. A completed motion calls `TrySetResult`; a canceled motion calls `TrySetCanceled`.

## Constructors

| Name | Description |
| --- | --- |
| `MotionCompletionSource()` | Initializes a completion source whose `Task` has not yet reached a terminal state. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Task` | `Task` | Gets the task observed by callers waiting for motion completion or cancellation. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `TrySetResult()` | `bool` | Attempts to complete `Task` successfully and returns whether the transition was applied. |
| `TrySetCanceled()` | `bool` | Attempts to cancel `Task` and returns whether the transition was applied. |

## Applies to

Cerneala motion core completion tracking.

## See also

- `Cerneala.UI.Motion.Core.MotionHandle`
- `Cerneala.UI.Motion.Core.MotionCompletedEventArgs`
