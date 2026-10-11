# ICursorService Interface

## Definition
Namespace: `Cerneala.UI.Platform`

Assembly/Project: `Cerneala`

Source: `UI/Platform/ICursorService.cs`

Shows a system mouse cursor for a UI root.

```csharp
public interface ICursorService
```

## Examples

Install a cursor service on a root:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Platform;

RecordingCursorService cursor = new();
UIRoot root = new(200, 200);
root.SetPlatformServices(new PlatformServices(Cursor: cursor));

Console.WriteLine(root.PlatformServices.Cursor == cursor);
// True

sealed class RecordingCursorService : ICursorService
{
    public CursorShape Current { get; private set; } = CursorShape.Arrow;

    public void SetCursor(CursorShape shape) => Current = shape;
}
```

## Remarks

`UiHost` reads `IPlatformServices.Cursor` from the current root during every update. When it is not `null`, the host resolves the cursor of the element under the pointer and calls `SetCursor` once per frame, after the retained renderer commits. It calls `SetCursor` even when the shape did not change, and it never reads `Current`. When the service is `null`, the host does not resolve a cursor at all. See `CursorShape` for the shapes the host publishes.

`Cerneala.UI.Input.CursorService` is a different type. It resolves an element's `Cursor` value and does not implement this interface.

The SDL platform supplies an implementation through its `PlatformServices`. That implementation starts with `Current` equal to `Arrow` and updates `Current` only after SDL accepts the cursor. It throws `ObjectDisposedException` after the window platform is disposed, and `InvalidOperationException` when SDL fails to create or select the cursor. It caches one native cursor per SDL system cursor and destroys them when it is disposed.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Current` | `CursorShape` | Gets the shape the service shows now. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `SetCursor(CursorShape shape)` | `void` | Shows `shape`. |

## Applies to

`IPlatformServices.Cursor`.

## See also

- `Cerneala.UI.Platform.CursorShape`
- `Cerneala.UI.Platform.IPlatformServices`
- `Cerneala.UI.Platform.PlatformServices`
- `Cerneala.UI.Hosting.UiHost`
