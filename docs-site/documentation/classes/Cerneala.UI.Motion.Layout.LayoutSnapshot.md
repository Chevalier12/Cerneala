# LayoutSnapshot Struct

## Definition
Namespace: `Cerneala.UI.Motion.Layout`

Assembly/Project: `Cerneala`

Source: `UI/Motion/Layout/LayoutSnapshot.cs`

Stores one layout-motion snapshot for an element, including its root-space bounds, visual parent, and optional layout motion id.

```csharp
public readonly record struct LayoutSnapshot(
    UIElement Element,
    LayoutRect Bounds,
    UIElement? Parent,
    LayoutMotionId? Id)
```

Inheritance:
`Object` -> `ValueType` -> `LayoutSnapshot`

Implements:
`IEquatable<LayoutSnapshot>`

## Examples

Create a snapshot from an element's current arranged bounds and layout-motion identity:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Motion.Layout;

UIElement element = GetElement();

LayoutSnapshot snapshot = new(
    element,
    element.ArrangedBounds,
    element.VisualParent,
    element.LayoutMotionId);

LayoutRect capturedBounds = snapshot.Bounds;
```

## Remarks

`LayoutSnapshot` is the value stored by `LayoutMotionCoordinator` while coordinating snapshot-based layout motion. The coordinator captures first snapshots before layout and later compares them with the element's post-layout bounds to decide whether a render-only correction is needed.

`Element` is the element observed by the snapshot. `Bounds` stores the captured layout rectangle, normally in the root visual coordinate space when produced by the coordinator. `Parent` records the visual parent at capture time, and `Id` records the element's `LayoutMotionId`.

The coordinator uses the snapshot id and parent to preserve visual continuity when the same element instance moves between visual parents. It also keeps previous snapshots by `LayoutMotionId` so a participating element can be matched across frames.

The struct does not validate the captured values. Validation of layout bounds, attachment, and layout-motion participation is performed by `LayoutMotionCoordinator`.

Because `LayoutSnapshot` is a `readonly record struct`, values are immutable after construction and use value-based equality. The compiler provides record-struct members such as equality, deconstruction, hashing, and string formatting based on the primary constructor components.

## Constructors

| Name | Description |
| --- | --- |
| `LayoutSnapshot(UIElement Element, LayoutRect Bounds, UIElement? Parent, LayoutMotionId? Id)` | Initializes a layout snapshot for an element, its captured bounds, visual parent, and optional layout motion id. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Element` | `UIElement` | Gets the element captured by the snapshot. |
| `Bounds` | `LayoutRect` | Gets the captured layout bounds for the element. |
| `Parent` | `UIElement?` | Gets the visual parent observed when the snapshot was captured. |
| `Id` | `LayoutMotionId?` | Gets the layout motion id observed when the snapshot was captured, or `null` when none was present. |

## Methods

| Name | Description |
| --- | --- |
| `Deconstruct(out UIElement Element, out LayoutRect Bounds, out UIElement? Parent, out LayoutMotionId? Id)` | Deconstructs the snapshot into its captured components. |
| `Equals(LayoutSnapshot other)` | Determines whether another snapshot has the same component values. |
| `GetHashCode()` | Returns a hash code based on the snapshot components. |
| `ToString()` | Returns the compiler-generated record string representation. |

## Applies to

Cerneala retained UI layout motion in the `Cerneala` project.

Target framework: `net8.0`

## See also

- `Cerneala.UI.Motion.Layout.LayoutMotionCoordinator`
- `Cerneala.UI.Motion.Layout.LayoutMotionId`
- `Cerneala.UI.Motion.Layout.LayoutMotionOptions`
- `Cerneala.UI.Elements.UIElement`
- `Cerneala.UI.Layout.LayoutRect`
