# DragDropController.DragSession Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/DragDropController.cs`

Stores the source element, drag data, and current hit-test target for an active `DragDropController` drag operation.

```csharp
private sealed class DragSession(UIElement source, DataTransfer data)
```

Containing type:
`DragDropController`

Inheritance:
`Object` -> `DragSession`

## Remarks

`DragSession` is a private nested implementation detail of `DragDropController`. A session is created by `DragDropController.Begin(UIElement, DataTransfer)` after that method validates the source element and transfer data.

During pointer movement, `DragDropController.Move(UIRoot, float, float)` compares `CurrentTarget` with the latest hit-test result to raise drag leave, enter, and over routed events. When a drop is requested, `DragDropController.Drop(UIRoot, float, float)` raises drop events for the current hit-test target, when one exists, and then clears the active session.

The stored `Data` instance is passed into `DragEventArgs` for drag and drop routed events. `CurrentTarget` may be `null` when no element is currently under the drag position.

## Constructors

| Name | Description |
| --- | --- |
| `DragSession(UIElement, DataTransfer)` | Initializes a drag session with the source element and transfer data supplied by `DragDropController.Begin`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Source` | `UIElement` | Gets the element that started the drag session. |
| `Data` | `DataTransfer` | Gets the transfer payload used when raising drag and drop events. |
| `CurrentTarget` | `HitTestResult?` | Gets or sets the most recent hit-test target for the active drag session. |

## Applies to

Project: `Cerneala`

## See also

- `DragDropController`
- `DragEventArgs`
- `DataTransfer`
- `HitTestResult`
