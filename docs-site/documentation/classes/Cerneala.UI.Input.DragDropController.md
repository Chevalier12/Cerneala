# DragDropController Class

## Definition

Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/DragDropController.cs`

Coordinates a retained UI drag/drop session by hit-testing a `UIRoot` and raising drag/drop routed events on the current target.

```csharp
public sealed class DragDropController
```

Inheritance:
`Object` -> `DragDropController`

## Examples

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

UIRoot root = new(100, 100);

UIElement source = new();
source.Arrange(new ArrangeContext(new LayoutRect(0, 0, 20, 20)));

UIElement target = new();
target.Arrange(new ArrangeContext(new LayoutRect(40, 0, 20, 20)));

root.VisualChildren.Add(source);
root.VisualChildren.Add(target);

target.Drop += (_, args) =>
{
    bool hasText = args.Data.TryGetData("text/plain", out string? text);
};

DragDropController controller = new();
DataTransfer data = new DataTransfer().SetData("text/plain", "payload");

controller.Begin(source, data);
controller.Move(root, 45, 5);
controller.Drop(root, 45, 5);
```

## Remarks

`DragDropController` tracks one active drag session at a time. `Begin` stores the drag source and `DataTransfer`, and `IsDragging` reports whether a session is active.

`Move` refreshes the root input cache, hit-tests the supplied coordinates, and compares the hit element with the previous drag target. When the target changes, the controller raises `PreviewDragLeave`/`DragLeave` on the old target and `PreviewDragEnter`/`DragEnter` on the new target. It raises `PreviewDragOver`/`DragOver` for each move that hits a target.

An old target that is no longer in the input route gets no `PreviewDragLeave`/`DragLeave`, as `HoverTracker` raises no `MouseLeave` on an element that left the input tree. An element leaves the input route when it, or one of its ancestors, is disabled, collapsed, hidden, or removed from the tree. The same `Move` still raises `PreviewDragEnter`/`DragEnter` and `PreviewDragOver`/`DragOver` on the element under the pointer, and later moves and `Drop` work as usual. For example, a drop zone that rejects the data sets `IsEnabled = false` in its `DragEnter` handler; when the pointer then moves on to another zone, that zone gets `DragEnter` and `DragOver`, and the disabled zone gets no `DragLeave`.

`Drop` refreshes the root input cache, hit-tests the supplied coordinates, raises `PreviewDrop`/`Drop` when a target is hit, and then ends the session. Calling `Move` or `Drop` without an active session returns without raising events.

The routed events are raised as preview/bubble pairs with `DragEventArgs`. The event args carry the session `DataTransfer` and the coordinates passed to `Move` or `Drop`.

## Constructors

| Name | Description |
| --- | --- |
| `DragDropController(HitTestService?)` | Initializes the controller with an optional hit-test service. When `null`, a new `HitTestService` is created. |

## Properties

| Name | Description |
| --- | --- |
| `IsDragging` | Gets whether a drag session is currently active. |

## Methods

| Name | Description |
| --- | --- |
| `Begin(UIElement, DataTransfer)` | Starts a drag session for a source element and data payload. Throws `ArgumentNullException` when `source` or `data` is `null`. |
| `Move(UIRoot, float, float)` | Updates the drag target for the supplied root-relative coordinates and raises drag leave, enter, and over events as needed. Throws `ArgumentNullException` when `root` is `null`. |
| `Drop(UIRoot, float, float)` | Drops the active payload at the supplied root-relative coordinates, raises drop events when a target is hit, and clears the session. Throws `ArgumentNullException` when `root` is `null`. |

## Routed Events Raised

| Trigger | Preview event | Bubble event | Event args |
| --- | --- | --- | --- |
| Pointer leaves the previous drag target during `Move`, while that target is still in the input route | `InputEvents.PreviewDragLeaveEvent` | `InputEvents.DragLeaveEvent` | `DragEventArgs` |
| Pointer enters a new drag target during `Move` | `InputEvents.PreviewDragEnterEvent` | `InputEvents.DragEnterEvent` | `DragEventArgs` |
| Pointer moves over the current drag target during `Move` | `InputEvents.PreviewDragOverEvent` | `InputEvents.DragOverEvent` | `DragEventArgs` |
| Payload is dropped on a hit target during `Drop` | `InputEvents.PreviewDropEvent` | `InputEvents.DropEvent` | `DragEventArgs` |

## Applies to

Cerneala retained UI input routing.

## See also

- `Cerneala.UI.Input.DataTransfer`
- `Cerneala.UI.Input.DragEventArgs`
- `Cerneala.UI.Input.HitTestService`
- `Cerneala.UI.Input.InputEvents`
