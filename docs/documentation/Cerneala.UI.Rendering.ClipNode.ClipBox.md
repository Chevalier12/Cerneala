# ClipNode.ClipBox Class

## Definition
Namespace: `Cerneala.UI.Rendering`

Assembly/Project: `Cerneala`

Source: `UI/Rendering/ClipNode.cs`

Stores the optional clip value associated with a `UIElement` inside `ClipNode`'s internal weak table.

```csharp
private sealed class ClipBox
```

Inheritance:
`object` -> `ClipBox`

## Examples

`ClipBox` is private implementation detail and is not created by callers. It is used by `ClipNode` when setting or retrieving a clip:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;

UIElement element = GetElement();
LayoutRect bounds = new(0, 0, 100, 100);

ClipNode.SetClip(element, bounds);

if (ClipNode.TryGetClip(element, out ClipNode clip))
{
    LayoutRect activeBounds = clip.Bounds;
}
```

## Remarks

`ClipBox` holds a nullable `ClipNode` value for one element entry in the `ConditionalWeakTable<UIElement, ClipBox>` maintained by `ClipNode`.

The class is private to `ClipNode`. Public code interacts with clipping through `SetClip`, `ClearClip`, and `TryGetClip`; it does not access `ClipBox` directly.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Clip` | `ClipNode?` | Gets or sets the optional clip stored for the owning weak-table entry. |

## Applies To

Cerneala retained UI rendering internals.

## See Also

- `Cerneala.UI.Rendering.ClipNode`
- `Cerneala.UI.Elements.UIElement`
- `Cerneala.UI.Layout.LayoutRect`
