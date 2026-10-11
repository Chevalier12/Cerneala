# KeyModifiers Enum

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/KeyGesture.cs`

Specifies the modifier keys a `KeyGesture` requires. Values can be combined.

```csharp
[Flags]
public enum KeyModifiers
```

## Examples

`Control` matches either Ctrl key, and the match is exact:

```csharp
using Cerneala.UI.Input;

static InputFrame Press(params InputKey[] keys) =>
    new(PointerSnapshot.Empty, PointerSnapshot.Empty, KeyboardSnapshot.Empty, KeyboardSnapshot.FromDownKeys(keys), []);

KeyGesture save = new(InputKey.S, KeyModifiers.Control);
Console.WriteLine(save.Matches(Press(InputKey.LeftCtrl, InputKey.S)));                     // True
Console.WriteLine(save.Matches(Press(InputKey.RightCtrl, InputKey.S)));                    // True
Console.WriteLine(save.Matches(Press(InputKey.LeftCtrl, InputKey.LeftShift, InputKey.S))); // False, Shift is extra
Console.WriteLine(save.Matches(Press(InputKey.S)));                                        // False, Ctrl is missing

KeyGesture redo = new(InputKey.Z, KeyModifiers.Control | KeyModifiers.Shift);
Console.WriteLine(redo.Matches(Press(InputKey.LeftCtrl, InputKey.RightShift, InputKey.Z))); // True
```

## Remarks

`KeyGesture.Matches` checks each of the three modifiers separately. A modifier counts as down when its left or right key is down: `LeftShift` or `RightShift`, `LeftCtrl` or `RightCtrl`, `LeftAlt` or `RightAlt`. The gesture matches only when the main key went down in this frame and every modifier's down state equals what the gesture requires. A modifier the gesture does not name must be up.

`None` is the default for `KeyGesture` and `KeyBinding`. It requires that Shift, Ctrl, and Alt are all up.

`KeyEventArgs` does not use this type. It reports modifier state through `IsShiftDown`, `IsControlDown`, and `IsAltDown`.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `None` | `0` | No modifier. Shift, Ctrl, and Alt must all be up. |
| `Shift` | `1` | Either Shift key must be down. |
| `Control` | `2` | Either Ctrl key must be down. |
| `Alt` | `4` | Either Alt key must be down. |

## Applies to

`KeyGesture` and `KeyBinding`.

## See also

- `Cerneala.UI.Input.KeyGesture`
- `Cerneala.UI.Input.KeyBinding`
- `Cerneala.UI.Input.InputKey`
- `Cerneala.UI.Input.KeyEventArgs`
