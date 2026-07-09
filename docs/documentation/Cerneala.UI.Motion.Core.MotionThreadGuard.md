# MotionThreadGuard Class

## Definition
Namespace: `Cerneala.UI.Motion.Core`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Core/MotionThreadGuard.cs`

Provides a small thread-affinity guard for motion APIs that must run on the owning UI thread.

```csharp
public sealed class MotionThreadGuard
```

Inheritance:
`object` -> `MotionThreadGuard`

## Examples

Create a guard for the current thread and verify access before mutating motion state:

```csharp
using Cerneala.UI.Motion.Core;

MotionThreadGuard guard = new(Environment.CurrentManagedThreadId);

if (guard.CheckAccess())
{
    guard.VerifyAccess();
}
```

Use the guard when constructing a low-level motion graph:

```csharp
using Cerneala.UI.Motion.Core;

MotionThreadGuard guard = new(Environment.CurrentManagedThreadId);
MotionGraph graph = new(guard);
MotionValue<double> opacity = graph.CreateValue(0d);
```

## Remarks

`MotionThreadGuard` captures an owner thread ID supplied by the caller. `MotionSystem` creates one with `Environment.CurrentManagedThreadId` during construction and exposes it through `MotionSystem.ThreadGuard`.

The guard does not marshal work to the UI thread. It only checks whether the current managed thread ID matches the stored owner ID. Callers that need to mutate motion state from another thread must marshal the request through the platform UI dispatcher before calling guarded APIs.

`CheckAccess` is the non-throwing test. `VerifyAccess` throws `InvalidOperationException` when the current thread is not the owner thread. Motion APIs such as `MotionGraph`, `MotionFrameCoordinator`, layout motion, presence motion, property bindings, and transactions use this guard before mutating or sampling motion state.

## Constructors

| Name | Description |
| --- | --- |
| `MotionThreadGuard(int ownerThreadId)` | Initializes the guard with the managed thread ID that is allowed to access guarded motion APIs. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `CheckAccess()` | `bool` | Returns `true` when the current managed thread ID matches the owner thread ID. |
| `VerifyAccess()` | `void` | Verifies access for the current thread and throws when the caller is not on the owner thread. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `VerifyAccess()` | `InvalidOperationException` | The current managed thread ID does not match the owner thread ID supplied to the constructor. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `Cerneala.UI.Motion.Core.MotionSystem`
- `Cerneala.UI.Motion.Core.MotionGraph`
- `Cerneala.UI.Motion.Core.MotionFrameCoordinator`
- `UI/Motion/Core/MotionThreadGuard.cs`
