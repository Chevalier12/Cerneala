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

While the drag owns pointer capture, `Thumb` handles mouse movement, wheel, generic button transitions, and left/right button-specific events. This prevents ancestor controls such as `ScrollViewer` from acting on pointer input until the left button is released. Hover tracking also remains on the captured thumb rather than moving to the element physically under the cursor.

Completing a left-button drag performs a final update, clears `IsDragging`, releases capture through the supplied route map, raises `DragCompleted` with `Canceled` set to `false`, and marks the mouse-up event as handled. `CancelDrag` clears the drag state and raises `DragCompleted` with `Canceled` set to `true`. The constructor also registers a lost-mouse-capture handler that cancels the active drag, and detaching the thumb cancels any active drag before base detachment runs.

Without a template child, the fallback layout reports a desired size of `10 x 10`. Fallback rendering fills the arranged bounds with `Background` when it is not `null`, then draws a border using the maximum side of `BorderThickness` when `BorderBrush` is not `null`. The constructor initializes `Background` to `new SolidColorBrush(new Color(180, 180, 180))`, `BorderBrush` to `new SolidColorBrush(new Color(80, 80, 80))`, and `BorderThickness` to `new Thickness(1)` at the `AspectBase` value source so markup aspects can replace those visual defaults.

`BeginDrag`, `CompleteDrag`, `BeginPointerDrag`, and `CompletePointerDrag` require non-null `PointerCaptureManager`, `ElementInputRouteMap`, and `MouseButtonEventArgs` arguments. `UpdateDrag` and `UpdatePointerDrag` require a non-null `MouseEventArgs` argument.

## Constructors

| Name | Description |
| --- | --- |
| `Thumb()` | Initializes a new thumb with default fallback background, border color, border thickness, and a lost-capture handler that cancels active drags. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `IsDragging` | `bool` | Gets whether a drag is currently active. |
| `LastHorizontalChange` | `float` | Gets the horizontal movement reported by the most recent active drag update. |
| `LastVerticalChange` | `float` | Gets the vertical movement reported by the most recent active drag update. |
| `TotalHorizontalChange` | `float` | Gets the horizontal movement from the drag start point to the most recent active drag position. |
| `TotalVerticalChange` | `float` | Gets the vertical movement from the drag start point to the most recent active drag position. |

## Events

| Name | Event Type | Description |
| --- | --- | --- |
| `DragStarted` | `EventHandler<DragStartedEventArgs>?` | Raised after a left-button drag starts and pointer capture is requested. |
| `DragDelta` | `EventHandler<DragDeltaEventArgs>?` | Raised during an active drag when the latest horizontal or vertical movement is non-zero. |
| `DragCompleted` | `EventHandler<DragCompletedEventArgs>?` | Raised when a drag completes normally or is canceled. |

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
| `BorderThickness` | `Thickness` | `Control` | Gets or sets the fallback border thickness used by the thumb renderer. |
| `IsEnabled` | `bool` | `UIElement` | Controls whether `BeginDrag` can start a drag. |
| `ComponentTemplate` | `ComponentTemplate?` | `Control` | Gets or sets the control template inherited by the thumb. |
| `ComponentTemplate` | `ComponentTemplate?` | `Control` | Gets or sets the component template inherited by the thumb. |

## Applies to

Project: `Cerneala`

## See also

- `UI/Controls/Primitives/Thumb.cs`
- `UI/Controls/Primitives/Track.cs`
- `UI/Input/IPointerDragSource.cs`
- `UI/Input/ElementInputBridge.cs`
