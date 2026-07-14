# InkCanvas Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/InkCanvas.cs`

Represents a canvas-style control that records stylus and touch input into ink strokes.

```csharp
public sealed class InkCanvas : Cerneala.UI.Layout.Panels.Canvas
```

Inheritance:
`object` -> `Cerneala.UI.Core.UiObject` -> `Cerneala.UI.Elements.UIElement` -> `Cerneala.UI.Layout.Panels.Panel` -> `Cerneala.UI.Layout.Panels.Canvas` -> `Cerneala.UI.Controls.InkCanvas`

## Examples
Record a stylus stroke:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Input;

InkCanvas canvas = new();

canvas.ApplyStylus(new StylusInputPoint(1, 1, 2, StylusInputAction.Down));
canvas.ApplyStylus(new StylusInputPoint(1, 3, 4, StylusInputAction.Move));
canvas.ApplyStylus(new StylusInputPoint(1, 5, 6, StylusInputAction.Up));

int strokeCount = canvas.Strokes.Count;
int pointCount = canvas.Strokes[0].Points.Count;
```

Record concurrent touch strokes. Touches with different IDs are tracked as separate active strokes:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Input;

InkCanvas canvas = new();

canvas.ApplyTouch(new TouchInputPoint(1, 0, 0, TouchInputAction.Down));
canvas.ApplyTouch(new TouchInputPoint(2, 10, 10, TouchInputAction.Down));
canvas.ApplyTouch(new TouchInputPoint(1, 1, 1, TouchInputAction.Move));
canvas.ApplyTouch(new TouchInputPoint(2, 11, 11, TouchInputAction.Move));
canvas.ApplyTouch(new TouchInputPoint(1, 2, 2, TouchInputAction.Up));
canvas.ApplyTouch(new TouchInputPoint(2, 12, 12, TouchInputAction.Up));
```

## Remarks
`InkCanvas` derives from `Cerneala.UI.Layout.Panels.Canvas`, so it keeps the same canvas layout behavior for child elements. Child elements can be positioned with the inherited canvas positioning helpers on `Cerneala.UI.Layout.Panels.Canvas`.

The `Strokes` collection is created by the control and exposed as a read-only property. Mutating the collection through `StrokeCollection.Add` or `StrokeCollection.Remove` raises `StrokeCollection.Changed`; `InkCanvas` listens to that event and invalidates measure and render.

Ink state is UI-owned. When the canvas is attached, `ApplyStylus`, `ApplyTouch`, and observed `Strokes` mutations must run on the root's owner thread. An off-thread collection notification throws before `InkCanvas` invalidates retained state. Use `await root.Relay.InvokeAsync(() => canvas.Strokes.Add(stroke), cancellationToken)` for a worker-produced stroke.

`ApplyStylus` and `ApplyTouch` translate input actions into stroke mutations. A `Down` action creates a new `Stroke`, adds the first point, stores it under the input kind and pointer ID, and adds it to `Strokes`. `Move` and `Up` add points only when a matching active stroke already exists. `Up` also removes the active stroke tracking entry after adding the final point.

Stylus actions other than `Down`, `Move`, and `Up` are treated as move actions by `ApplyStylus`. If no active stroke exists for that stylus ID, the point is ignored. `ApplyTouch` accepts the three current `TouchInputAction` values: `Down`, `Move`, and `Up`.

## Constructors
| Name | Description |
| --- | --- |
| `InkCanvas()` | Initializes a new `InkCanvas` and subscribes to `Strokes.Changed` so stroke collection changes invalidate measure and render. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `Strokes` | `StrokeCollection` | Gets the mutable collection of recorded ink strokes. |

## Methods
| Name | Description |
| --- | --- |
| `ApplyStylus(StylusInputPoint point)` | Applies a stylus input point on the owning UI thread, creating, extending, or finishing a stroke based on `point.Action`. |
| `ApplyTouch(TouchInputPoint point)` | Applies a touch input point on the owning UI thread, creating, extending, or finishing a stroke based on `point.Action`. |

## Applies to
Cerneala UI controls in the `Cerneala` project.

## See also
- `Cerneala.UI.Ink.Stroke`
- `Cerneala.UI.Ink.StrokeCollection`
- `Cerneala.UI.Input.StylusInputPoint`
- `Cerneala.UI.Input.TouchInputPoint`
- `Cerneala.UI.Layout.Panels.Canvas`
