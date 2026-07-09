# MonoGameInputMapper Class

## Definition
Namespace: `Cerneala.UI.Input.MonoGame`

Assembly/Project: `Cerneala`

Source: `UI/Input/MonoGame/MonoGameInputMapper.cs`

Maps MonoGame keyboard and mouse button values into Cerneala input enums.

```csharp
public static class MonoGameInputMapper
```

Inheritance:
`Object` -> `MonoGameInputMapper`

## Examples

```csharp
using Cerneala.UI.Input;
using Cerneala.UI.Input.MonoGame;
using Microsoft.Xna.Framework.Input;

InputKey enter = MonoGameInputMapper.MapKey(Keys.Enter);
InputKey unknown = MonoGameInputMapper.MapKey((Keys)9999);

InputMouseButton left = MonoGameInputMapper.MapMouseButton(0);
InputMouseButton noButton = MonoGameInputMapper.MapMouseButton(-1);
```

## Remarks

`MonoGameInputMapper` is the translation layer used by the MonoGame input backend before input is exposed through Cerneala's retained UI input model.

`MapKey` maps supported `Microsoft.Xna.Framework.Input.Keys` values to `InputKey`. Supported keys include common editing and navigation keys, digits `D0` through `D9`, letters `A` through `Z`, left/right Shift, Control, and Alt modifiers, and function keys `F1` through `F12`. Keys that are not listed by the mapper return `InputKey.Unknown`.

`MapMouseButton` maps MonoGame-style button indexes to `InputMouseButton`: `0` is left, `1` is middle, `2` is right, `3` is XButton1, and `4` is XButton2. Other indexes return `InputMouseButton.None`.

`MonoGameInputSource` uses `MapKey` when building keyboard snapshots and filters out `InputKey.Unknown` and `InputKey.None` before creating the frame keyboard state.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `MapKey(Keys)` | `InputKey` | Maps a MonoGame `Keys` value to the corresponding Cerneala `InputKey`, or `InputKey.Unknown` when the key is not supported by the mapper. |
| `MapMouseButton(int)` | `InputMouseButton` | Maps button indexes `0` through `4` to Cerneala mouse buttons, or `InputMouseButton.None` for any other index. |

## Applies to

Cerneala MonoGame input backend.

## See also

- `Cerneala.UI.Input.InputKey`
- `Cerneala.UI.Input.InputMouseButton`
- `Cerneala.UI.Input.MonoGame.MonoGameInputSource`
