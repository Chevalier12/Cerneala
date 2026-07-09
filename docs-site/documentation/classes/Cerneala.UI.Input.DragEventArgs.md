# DragEventArgs Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: [UI/Input/DragDropController.cs](../../UI/Input/DragDropController.cs)

Provides data for drag-and-drop routed events raised by `DragDropController`.

```csharp
public sealed class DragEventArgs : RoutedEventArgs
```

Inheritance:
`object` -> `RoutedEventArgs` -> `DragEventArgs`

## Examples
```csharp
UIElement target = new();

target.Handlers.AddHandler(InputEvents.DropEvent, (_, args) =>
{
    if (args is not DragEventArgs dragArgs)
    {
        return;
    }

    if (dragArgs.Data.TryGetData<string>("text/plain", out string? text))
    {
        float x = dragArgs.X;
        float y = dragArgs.Y;

        // Use text with the drop coordinates.
    }
});
```

## Remarks
`DragEventArgs` carries the `DataTransfer` payload and pointer coordinates for drag/drop routing. `DragDropController` creates it when raising the preview and bubble pairs for drag enter, drag over, drag leave, and drop events.

The `Data` property is the same `DataTransfer` instance supplied to `DragDropController.Begin`. `X` and `Y` are stored exactly as supplied to the controller's `Move` or `Drop` call, and those values are also used for hit testing the current target.

The constructor requires a non-null `DataTransfer`. The inherited `RoutedEventArgs` constructor also requires non-null `routedEvent` and `originalSource` values.

## Constructors
| Name | Description |
| --- | --- |
| `DragEventArgs(RoutedEvent routedEvent, object originalSource, DataTransfer data, float x, float y)` | Initializes a new instance for the specified routed event, original source, data payload, and drag coordinates. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `Data` | `DataTransfer` | Gets the drag/drop data payload. |
| `X` | `float` | Gets the X coordinate supplied for the drag event. |
| `Y` | `float` | Gets the Y coordinate supplied for the drag event. |

## Inherited Properties
| Name | Type | Description |
| --- | --- | --- |
| `RoutedEvent` | `RoutedEvent` | Gets the routed event associated with this event data. |
| `OriginalSource` | `object` | Gets the original event source supplied to the constructor. |
| `Source` | `object` | Gets or sets the current event source. It is initialized to `OriginalSource`. |
| `Handled` | `bool` | Gets or sets whether the routed event has been handled. |

## Applies to
`Cerneala.UI.Input` in the `Cerneala` project.

## See also
- `DragDropController`
- `DataTransfer`
- `InputEvents`
- `RoutedEventArgs`
