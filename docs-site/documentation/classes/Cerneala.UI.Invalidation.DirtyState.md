# DirtyState Class

## Definition
Namespace: `Cerneala.UI.Invalidation`  
Assembly/Project: `Cerneala`  
Source: `UI/Invalidation/DirtyState.cs`

Tracks the current retained invalidation flags for an element and records a version when new dirty flags are added.

```csharp
public sealed class DirtyState
```

Inheritance:  
`object` -> `DirtyState`

## Examples

Use `Mark` to add one or more dirty flags and `Clear` to remove the flags that were processed.

```csharp
using Cerneala.UI.Invalidation;

DirtyState state = new();

bool marked = state.Mark(InvalidationFlags.Measure | InvalidationFlags.Render);
bool needsRender = state.Has(InvalidationFlags.Render);

bool cleared = state.Clear(InvalidationFlags.Render);
bool stillNeedsMeasure = state.Has(InvalidationFlags.Measure);

long version = state.Version;
```

Repeated marks for flags that are already present are idempotent.

```csharp
using Cerneala.UI.Invalidation;

DirtyState state = new();

bool first = state.Mark(InvalidationFlags.Render);  // true
bool second = state.Mark(InvalidationFlags.Render); // false

long version = state.Version; // 1
```

## Remarks

`DirtyState` is the per-element dirty flag container used by `UIElement.DirtyState`. It stores the currently active `InvalidationFlags` and exposes `IsDirty` as a convenience check for whether any flag is set.

`Mark` merges requested flags into the current state. It returns `true` only when the call adds at least one flag that was not already present, and increments `Version` only in that case. Requests that contain only `InvalidationFlags.None`, or only flags that are already present, return `false`.

`Clear` removes requested flags from the current state. It returns `true` only when the call actually changes `Flags`. Clearing one flag does not affect unrelated flags. `Clear` does not increment `Version`.

`ClearAll` resets `Flags` to `InvalidationFlags.None` and returns no change status. It does not increment `Version`.

`Has` requires all requested flags to be present and returns `false` for `InvalidationFlags.None`.

## Constructors

| Name | Description |
| --- | --- |
| `DirtyState()` | Initializes a clean state with `Flags` set to `InvalidationFlags.None` and `Version` set to `0`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Flags` | `InvalidationFlags` | Gets the currently active invalidation flags. |
| `Version` | `long` | Gets the number of successful `Mark` calls that added new dirty flags. |
| `IsDirty` | `bool` | Gets whether `Flags` is not `InvalidationFlags.None`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Has(InvalidationFlags flags)` | `bool` | Returns `true` when `flags` is not `None` and all requested flags are present in `Flags`. |
| `Mark(InvalidationFlags flags)` | `bool` | Adds requested flags, increments `Version` when the stored flags change, and returns whether anything changed. |
| `Clear(InvalidationFlags flags)` | `bool` | Removes requested flags and returns whether the stored flags changed. |
| `ClearAll()` | `void` | Resets `Flags` to `InvalidationFlags.None`. |

## Related Types

### InvalidationFlags

`InvalidationFlags` is a `[Flags]` enum used by `DirtyState` to represent retained UI work that still needs processing.

```csharp
[Flags]
public enum InvalidationFlags
```

| Name | Value | Description |
| --- | --- | --- |
| `None` | `0` | No invalidation work is marked. |
| `Measure` | `1 << 0` | Measure invalidation. |
| `Arrange` | `1 << 1` | Arrange invalidation. |
| `Render` | `1 << 2` | Render invalidation. |
| `Text` | `1 << 3` | Text invalidation. |
| `Image` | `1 << 4` | Image invalidation. |
| `Resource` | `1 << 5` | Resource invalidation. |
| `Aspect` | `1 << 6` | Aspect invalidation. |
| `InputVisual` | `1 << 7` | Input visual invalidation. |
| `HitTest` | `1 << 8` | Hit-test invalidation. |
| `Subtree` | `1 << 9` | Subtree invalidation. |
| `Inherited` | `1 << 10` | Inherited property invalidation. |
| `Semantics` | `1 << 11` | Semantics invalidation. |

## Applies to

Cerneala retained UI invalidation state tracking.

## See also

- `Cerneala.UI.Elements.UIElement`
- `Cerneala.UI.Invalidation.InvalidationFlags`
- `Cerneala.UI.Invalidation.UiFrameScheduler`
