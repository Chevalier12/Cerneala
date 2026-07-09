# DragStartedEventArgs Class

## Definition
Namespace: `Cerneala.UI.Controls.Primitives`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Primitives/Thumb.cs`

Provides the starting pointer coordinates for a `Thumb.DragStarted` event.

```csharp
public sealed class DragStartedEventArgs : EventArgs
```

Inheritance:
`Object` -> `EventArgs` -> `DragStartedEventArgs`

## Examples
Subscribe to `Thumb.DragStarted` to read the pointer position captured when a drag begins.

```csharp
using Cerneala.UI.Controls.Primitives;

Thumb thumb = new();

thumb.DragStarted += (_, args) =>
{
    Console.WriteLine($"Drag started at ({args.X}, {args.Y}).");
};
```

## Remarks
`DragStartedEventArgs` is used by `Thumb` when a left-button drag starts. `Thumb.BeginDrag` creates an instance with the current pointer coordinates, raises `DragStarted`, marks the input event as handled, and resets the thumb's last and total drag-change values.

The `X` and `Y` values represent the drag start position from the input event that began the drag. They do not report movement deltas or completion state. Use `DragDeltaEventArgs` for per-update and total movement, and `DragCompletedEventArgs` for completion or cancellation information.

## Constructors
| Name | Description |
| --- | --- |
| `DragStartedEventArgs(float x, float y)` | Initializes a new instance with the pointer coordinates where the drag started. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `X` | `float` | Gets the horizontal pointer coordinate at drag start. |
| `Y` | `float` | Gets the vertical pointer coordinate at drag start. |

## Applies to
`Cerneala.UI.Controls.Primitives.Thumb` drag input handling.

## See also
- `Thumb`
- `DragDeltaEventArgs`
- `DragCompletedEventArgs`
