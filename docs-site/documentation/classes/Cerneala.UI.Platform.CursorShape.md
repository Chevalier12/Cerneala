# CursorShape Enum

## Definition
Namespace: `Cerneala.UI.Platform`

Assembly/Project: `Cerneala`

Source: `UI/Platform/CursorShape.cs`

Specifies the system mouse cursor that an `ICursorService` shows.

```csharp
public enum CursorShape
```

## Examples

A cursor service receives one of these values from the host:

```csharp
using Cerneala.UI.Platform;

RecordingCursorService service = new();
service.SetCursor(CursorShape.IBeam);

Console.WriteLine(service.Current);
// IBeam

sealed class RecordingCursorService : ICursorService
{
    public CursorShape Current { get; private set; } = CursorShape.Arrow;

    public void SetCursor(CursorShape shape) => Current = shape;
}
```

## Remarks

`UiHost` publishes a cursor shape once per frame when the root's `PlatformServices.Cursor` is not `null`. It resolves the element cursor under the pointer and converts it: `Cursor.Hand` becomes `Hand`, `Cursor.IBeam` becomes `IBeam`, `Cursor.Crosshair` becomes `Crosshair`, and every other cursor becomes `Arrow`. The host never publishes `Default`, `ResizeHorizontal`, `ResizeVertical`, or `Hidden`; only application code that calls `ICursorService.SetCursor` directly uses them.

With several SDL windows, only the window that has the pointer changes the cursor; a frame of another window does not replace it. See `ICursorService`.

The SDL platform maps the values to SDL system cursors as the Fields table shows. `Default`, `Arrow`, and any value outside the enum map to the same default system cursor. For `Hidden`, the SDL platform passes a null cursor handle to `SDL_SetCursor`; it does not call an SDL function that hides the cursor.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `Default` | `0` | The platform default cursor. SDL: the default system cursor. |
| `Arrow` | `1` | The arrow cursor. SDL: the default system cursor, the same as `Default`. |
| `Hand` | `2` | The hand cursor used over clickable content. SDL: the pointer system cursor. |
| `IBeam` | `3` | The text-insertion cursor. SDL: the text system cursor. |
| `Crosshair` | `4` | The crosshair cursor. SDL: the crosshair system cursor. |
| `ResizeHorizontal` | `5` | The left-right resize cursor. SDL: the east-west resize system cursor. |
| `ResizeVertical` | `6` | The up-down resize cursor. SDL: the north-south resize system cursor. |
| `Hidden` | `7` | Requests no visible cursor. SDL: a null cursor handle is passed to `SDL_SetCursor` (see Remarks). |

## Applies to

`ICursorService.Current` and `ICursorService.SetCursor(CursorShape)`.

## See also

- `Cerneala.UI.Platform.ICursorService`
- `Cerneala.UI.Platform.IPlatformServices`
- `Cerneala.UI.Input.Cursor`
- `Cerneala.UI.Input.CursorService`
