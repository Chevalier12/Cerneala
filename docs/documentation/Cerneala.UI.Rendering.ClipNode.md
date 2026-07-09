# ClipNode Struct

## Definition
Namespace: `Cerneala.UI.Rendering`

Assembly/Project: `Cerneala`

Source: `UI/Rendering/ClipNode.cs`

Stores explicit rectangular clip bounds associated with a `UIElement` for retained rendering and hit testing.

```csharp
public readonly record struct ClipNode(LayoutRect Bounds)
```

Inheritance:
`ValueType` -> `ClipNode`

## Examples
Set a clip rectangle on an element, read it back, then clear it.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;

UIElement element = new();
LayoutRect bounds = new(0, 0, 100, 50);

ClipNode.SetClip(element, bounds);

if (ClipNode.TryGetClip(element, out ClipNode clip))
{
    LayoutRect activeBounds = clip.Bounds;
}

ClipNode.ClearClip(element);
```

## Remarks
`ClipNode` is a small value type that stores a `LayoutRect` in its `Bounds` property. The static `SetClip`, `TryGetClip`, and `ClearClip` methods keep clip data outside `UIElement` itself by using a `ConditionalWeakTable<UIElement, ClipBox>` keyed by the element instance.

The retained renderer checks `ClipNode` before falling back to `UIElement.ClipToBounds`. When a clip is present, `DrawCommandListBuilder` emits a `PushClip` command before the element subtree and a matching `PopClip` command after it.

Hit testing also respects explicit clip nodes. `HitTestService` rejects an element subtree when the tested point is outside the stored `Bounds`, which prevents overflowing descendants from receiving hits through a clipped parent.

`ScrollContentPresenter` sets a clip node during arrange using its rounded final rectangle and clears that clip when detached. This is the current production use that ties scrolling, rendering, and hit testing to the same viewport rectangle.

All public static methods throw `ArgumentNullException` when `element` is `null`.

## Constructors
| Name | Description |
| --- | --- |
| `ClipNode(LayoutRect Bounds)` | Initializes a new clip node with the specified layout bounds. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `Bounds` | `LayoutRect` | Gets the rectangular layout bounds used as the clip area. |

## Methods
| Name | Return Type | Description |
| --- | --- | --- |
| `SetClip(UIElement element, LayoutRect bounds)` | `void` | Associates `bounds` with `element`, replacing any existing explicit clip for that element. |
| `ClearClip(UIElement element)` | `void` | Removes the explicit clip associated with `element`. |
| `TryGetClip(UIElement element, out ClipNode clip)` | `bool` | Gets the explicit clip associated with `element`; returns `true` when one exists, otherwise assigns `default` to `clip` and returns `false`. |
| `Deconstruct(out LayoutRect Bounds)` | `void` | Deconstructs the record struct into its `Bounds` value. |

## Applies To
Project: `Cerneala`

UI area: retained rendering, clipping, scrolling, and hit testing.

## See Also
- `UIElement`
- `LayoutRect`
- `DrawCommandListBuilder`
- `HitTestService`
- `ScrollContentPresenter`
