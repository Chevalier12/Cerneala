# MotionTimelineRegistry Class

## Definition

Namespace: `Cerneala.UI.Motion.Core`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Core/MotionTimelineRegistry.cs`

Stores named motion timelines for lookup and reuse.

```csharp
public sealed class MotionTimelineRegistry
```

Inheritance:
`object` -> `MotionTimelineRegistry`

## Examples

Register, resolve, enumerate, and remove a timeline:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Motion.Core;

UIRoot root = new();
ManualMotionTimeline progress = new();

root.Motion.Timelines.Register("page-progress", progress);
MotionTimeline sameTimeline = root.Motion.Timelines.Get("page-progress");
IReadOnlyList<string> names = root.Motion.Timelines.Names;
bool removed = root.Motion.Timelines.Remove("page-progress");
```

## Remarks

`MotionSystem.Timelines` exposes a root-owned registry. Names use ordinal, case-sensitive comparison and must be non-empty. Registration does not replace an existing name; remove the existing entry first when replacement is intended.

`Names` returns a snapshot of the registered names rather than a live dictionary view. `TryGet` returns `false` for a missing valid name, while `Get` throws `KeyNotFoundException`.

All properties and methods are thread-affine. A registry created by `MotionSystem` uses the owning root's relay. A registry created directly captures its constructing thread.

## Constructors

| Name | Description |
| --- | --- |
| `MotionTimelineRegistry()` | Creates an empty registry and captures the current thread for access verification. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Count` | `int` | Gets the number of registered timelines. |
| `Names` | `IReadOnlyList<string>` | Gets a snapshot of the registered names. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Register(string name, MotionTimeline timeline)` | `void` | Registers `timeline` under a unique name. |
| `TryGet(string name, out MotionTimeline? timeline)` | `bool` | Attempts to retrieve the timeline registered under `name`. |
| `Get(string name)` | `MotionTimeline` | Returns the named timeline or throws when it is absent. |
| `Remove(string name)` | `bool` | Removes the named timeline and reports whether an entry existed. |
| `Clear()` | `void` | Removes all registered timelines. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| Name-taking methods | `ArgumentException` | `name` is `null`, empty, or whitespace. |
| `Register` | `ArgumentNullException` | `timeline` is `null`. |
| `Register` | `InvalidOperationException` | `name` is already registered. |
| `Get` | `KeyNotFoundException` | No timeline is registered under `name`. |
| All properties and methods | `InvalidOperationException` | The current thread is not accepted by the registry's thread access owner. |

## Applies to

Project: `Cerneala`

Target framework: `net8.0`

## See also

- `Cerneala.UI.Motion.Core.MotionSystem`
- `Cerneala.UI.Motion.Core.MotionTimeline`
- `Cerneala.UI.Motion.Core.ManualMotionTimeline`
