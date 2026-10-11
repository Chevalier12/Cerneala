# InputMouseButton Enum

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/InputMouseButton.cs`

Identifies a mouse button in pointer snapshots, input frames, and mouse button events.

```csharp
public enum InputMouseButton
```

## Examples

Build a pointer frame where the left button goes down:

```csharp
using Cerneala.UI.Input;

PointerSnapshot current = PointerSnapshot.Empty
    .WithButton(InputMouseButton.Left, true)
    .WithButton(InputMouseButton.None, true); // ignored

InputFrame frame = new(PointerSnapshot.Empty, current, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);

Console.WriteLine(frame.Pointer.IsPressed(InputMouseButton.Left)); // True
Console.WriteLine(frame.Pointer.IsDown(InputMouseButton.Right));   // False
Console.WriteLine(current.IsDown(InputMouseButton.None));          // False
```

## Remarks

`None` is not a button. `PointerSnapshot.WithButton` ignores it, `PointerSnapshot.IsDown` returns `false` for it, and input dispatch skips it. Servo pointer input rejects it with `ArgumentOutOfRangeException`.

The SDL input source maps SDL buttons `1` to `5` to `Left`, `Middle`, `Right`, `XButton1`, and `XButton2`, and any other button to `None`.

For every button except `None`, the retained input bridge raises `PreviewMouseDown` and `MouseDown` when the button goes down, and `PreviewMouseUp` and `MouseUp` when it goes up. `MouseButtonEventArgs.ChangedButton` holds the button. Only two buttons have extra behavior:

| Button | Extra behavior |
| --- | --- |
| `Left` | Raises `MouseLeftButtonDown` and `MouseLeftButtonUp` pairs, moves keyboard focus on press, counts clicks, and executes button commands on release. Controls such as `ButtonBase`, `Thumb`, `TextBox`, and `MenuItem` react only to it. |
| `Right` | Raises `MouseRightButtonDown` and `MouseRightButtonUp` pairs. |

`Middle`, `XButton1`, and `XButton2` produce only the general mouse down and mouse up events.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `None` | `0` | No button. Ignored by pointer snapshots and input dispatch. |
| `Left` | `1` | The primary (left) button. |
| `Middle` | `2` | The middle button or wheel button. |
| `Right` | `3` | The secondary (right) button. |
| `XButton1` | `4` | The first extra button, often "back". |
| `XButton2` | `5` | The second extra button, often "forward". |

## Applies to

`PointerSnapshot`, `InputFrame.PointerFrame`, and `MouseButtonEventArgs`.

## See also

- `Cerneala.UI.Input.PointerSnapshot`
- `Cerneala.UI.Input.InputFrame`
- `Cerneala.UI.Input.MouseButtonEventArgs`
- `Cerneala.UI.Input.InputEvents`
