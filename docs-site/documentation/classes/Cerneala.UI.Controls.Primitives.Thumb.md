# Thumb Class

## Definition
Namespace: `Cerneala.UI.Controls.Primitives`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Primitives/Thumb.cs`

Represents a draggable primitive control that captures left-pointer input and reports drag start, delta, and completion values.

```csharp
public class Thumb : Control, IPointerDragSource
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `Thumb`

Implements:
`IPointerDragSource`

## Examples

Observe drag activity from a `Track` thumb:

```csharp
using Cerneala.UI.Controls.Primitives;

Track track = new();

track.Thumb.DragStarted += (_, args) =>
{
    Console.WriteLine($"Started at {args.X}, {args.Y}");
};

track.Thumb.DragDelta += (_, args) =>
{
    Console.WriteLine($"Delta: {args.HorizontalChange}, {args.VerticalChange}");
};

track.Thumb.DragCompleted += (_, args) =>
{
    Console.WriteLine(args.Canceled ? "Canceled" : "Completed");
};
```

Create and arrange a standalone fallback-rendered thumb:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls.Primitives;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;

Thumb thumb = new()
{
    Background = new SolidColorBrush(new Color(210, 210, 210)),
    BorderBrush = new SolidColorBrush(new Color(70, 70, 70)),
    BorderThickness = new Thickness(1)
};

thumb.Measure(new MeasureContext(new LayoutSize(20, 20)));
thumb.Arrange(new ArrangeContext(new LayoutRect(0, 0, 20, 20)));
```

## Remarks

`Thumb` is the low-level drag handle used by controls such as `Track`. It starts dragging only when enabled and when the changed mouse button is `InputMouseButton.Left`. Starting a drag stores the start position, resets the last and total change values, captures the pointer through `PointerCaptureManager`, raises `DragStarted`, marks the input event as handled, and returns `true`.

During an active drag, `UpdateDrag` computes `LastHorizontalChange` and `LastVerticalChange` from the previous drag point, and `TotalHorizontalChange` and `TotalVerticalChange` from the drag start point. `DragDelta` is raised only when the latest horizontal or vertical change is non-zero. The update event is marked handled while dragging.

Drag points are measured in the layout space where the thumb is arranged, the space of its `ArrangedBounds`, not in root pixels. `Thumb` converts the unrounded root pointer position through the render transforms of its visual parent and every ancestor, so moving the thumb's arranged position by a change keeps the drawn thumb under the pointer. The thumb's own render transform is not applied, because it moves with the thumb. Without ancestor transforms the changes equal root-pixel movement. With a parent drawn 2x larger, a drag of `30` by `10` root pixels reports `TotalHorizontalChange == 15` and `TotalVerticalChange == 5`. A drag point that cannot be converted, for example through a non-invertible transform, reports no movement. `DragStartedEventArgs.X` and `Y` are still the rounded root coordinates of the press.

While the drag owns pointer capture, `Thumb` handles mouse movement, wheel, generic button transitions, and left/right button-specific events. This prevents ancestor controls such as `ScrollViewer` from acting on pointer input until the left button is released. Hover tracking also remains on the captured thumb rather than moving to the element physically under the cursor.

Completing a left-button drag performs a final update, clears `IsDragging`, releases capture through the supplied route map, raises `DragCompleted` with `Canceled` set to `false`, and marks the mouse-up event as handled. `CancelDrag` clears the drag state and raises `DragCompleted` with `Canceled` set to `true`. The constructor also registers a lost-mouse-capture handler that cancels the active drag, and detaching the thumb cancels any active drag before base detachment runs. A thumb that becomes hidden, collapsed or disabled while it drags, itself or through an ancestor, loses the pointer capture on the next pointer event, so its drag ends the same way: `IsDragging` becomes `false` and `DragCompleted` is raised once with `Canceled` set to `true`.

`MeasureCore` always reports a desired size of `10 x 10`, with or without a template child. Fallback rendering fills the arranged bounds with `Background` when it is not `null`, then paints the border as `Border` paints it when `BorderBrush` is not `null`: each side with its own signed `BorderThickness` side: a negative side `-t` is a band of `t` pixels inside the arranged bounds, a positive side `+t` is a band of `t` pixels outside them, and a side with thickness `0` is not painted. For example, a thumb arranged at `(10, 10, 20, 20)` with the default `BorderThickness = new Thickness(1)` draws a stroke of thickness 1 at `(9.5, 9.5, 21, 21)`, which covers the 1 pixel just outside each edge; with `BorderThickness = new Thickness(-2)` it draws a stroke of thickness 2 at `(11, 11, 18, 18)`, which covers the 2 pixels just inside each edge; with `BorderThickness = new Thickness(0, 0, 0, -2)` it paints only the bottom two rows inside the bounds, `y = 28` and `y = 29`. The constructor initializes `Background` to `new SolidColorBrush(new Color(180, 180, 180))`, `BorderBrush` to `new SolidColorBrush(new Color(80, 80, 80))`, and `BorderThickness` to `new Thickness(1)` as framework defaults at the `Default` value source, so any Aspect, local, or other higher-source value replaces them.

`BeginDrag`, `CompleteDrag`, `BeginPointerDrag`, and `CompletePointerDrag` require non-null `PointerCaptureManager`, `ElementInputRouteMap`, and `MouseButtonEventArgs` arguments. `UpdateDrag` and `UpdatePointerDrag` require a non-null `MouseEventArgs` argument.

## Constructors

| Name | Description |
| --- | --- |
| `Thumb()` | Initializes a new thumb with default fallback background, border color, border thickness, and a lost-capture handler that cancels active drags. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `IsDragging` | `bool` | Gets whether a drag is currently active. |
| `LastHorizontalChange` | `float` | Gets the horizontal movement reported by the most recent active drag update, in the layout space where the thumb is arranged. |
| `LastVerticalChange` | `float` | Gets the vertical movement reported by the most recent active drag update, in the layout space where the thumb is arranged. |
| `TotalHorizontalChange` | `float` | Gets the horizontal movement from the drag start point to the most recent active drag position, in the layout space where the thumb is arranged. |
| `TotalVerticalChange` | `float` | Gets the vertical movement from the drag start point to the most recent active drag position, in the layout space where the thumb is arranged. |

## Events

| Name | Event Type | Description |
| --- | --- | --- |
| `DragStarted` | `EventHandler<DragStartedEventArgs>` | Raised after a left-button drag starts and pointer capture is requested. |
| `DragDelta` | `EventHandler<DragDeltaEventArgs>` | Raised during an active drag when the latest horizontal or vertical movement is non-zero. |
| `DragCompleted` | `EventHandler<DragCompletedEventArgs>` | Raised when a drag completes normally or is canceled. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `BeginDrag(PointerCaptureManager captureManager, ElementInputRouteMap routeMap, MouseButtonEventArgs args)` | `bool` | Starts a left-button drag when the thumb is enabled, captures the pointer, raises `DragStarted`, marks the event handled, and returns `true`; otherwise returns `false`. |
| `BeginPointerDrag(PointerCaptureManager captureManager, ElementInputRouteMap routeMap, MouseButtonEventArgs args)` | `bool` | Implements `IPointerDragSource` by forwarding to `BeginDrag`. |
| `UpdateDrag(MouseEventArgs args)` | `bool` | Updates last and total drag changes during an active drag, raises `DragDelta` for non-zero movement, marks the event handled, and returns `true`; returns `false` when not dragging. |
| `UpdatePointerDrag(MouseEventArgs args)` | `bool` | Implements `IPointerDragSource` by forwarding to `UpdateDrag`. |
| `CompleteDrag(PointerCaptureManager captureManager, ElementInputRouteMap routeMap, MouseButtonEventArgs args)` | `bool` | Completes an active left-button drag, performs a final update, releases capture, raises `DragCompleted` with `Canceled == false`, marks the event handled, and returns `true`; otherwise returns `false`. |
| `CompletePointerDrag(PointerCaptureManager captureManager, ElementInputRouteMap routeMap, MouseButtonEventArgs args)` | `bool` | Implements `IPointerDragSource` by forwarding to `CompleteDrag`. |
| `CancelDrag()` | `void` | Cancels an active drag and raises `DragCompleted` with `Canceled == true`; does nothing when no drag is active. |

## Related Event Argument Types

| Type | Public Members | Description |
| --- | --- | --- |
| `DragStartedEventArgs` | `X`, `Y` | Provides the drag start coordinates. |
| `DragDeltaEventArgs` | `HorizontalChange`, `VerticalChange`, `TotalHorizontalChange`, `TotalVerticalChange` | Provides the latest movement delta and total movement from the drag start. |
| `DragCompletedEventArgs` | `HorizontalChange`, `VerticalChange`, `Canceled` | Provides the final total movement and whether the drag was canceled. |

## Important Inherited Properties

| Name | Type | Declared by | Description |
| --- | --- | --- | --- |
| `Background` | `Brush?` | `Control` | Gets or sets the fallback fill brush rendered by the thumb. |
| `BorderBrush` | `Brush?` | `Control` | Gets or sets the fallback border brush rendered by the thumb. |
| `BorderThickness` | `Thickness` | `Control` | Gets or sets the signed thickness of each side of the fallback border: `-t` is a band of `t` pixels inside the arranged bounds, `+t` a band of `t` pixels outside them. |
| `IsEnabled` | `bool` | `UIElement` | Controls whether `BeginDrag` can start a drag. |
| `ComponentTemplate` | `ComponentTemplate?` | `Control` | Gets or sets the component template inherited by the thumb. |

## Applies to

Project: `Cerneala`

## See also

- `UI/Controls/Primitives/Thumb.cs`
- `UI/Controls/Primitives/Track.cs`
- `UI/Input/IPointerDragSource.cs`
- `UI/Input/ElementInputBridge.cs`
