# InputFrame.KeyboardFrame Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/InputFrame.cs`

Represents keyboard state changes between the previous and current keyboard snapshots in an `InputFrame`.

```csharp
public sealed class InputFrame.KeyboardFrame
```

Inheritance:
`Object` -> `InputFrame.KeyboardFrame`

## Examples

Use the keyboard frame to detect key transitions during input dispatch.

```csharp
using Cerneala.UI.Input;

InputFrame frame = CaptureFrame();

if (frame.Keyboard.IsPressed(InputKey.Enter))
{
    SubmitFocusedControl();
}
```

## Remarks

`InputFrame.KeyboardFrame` compares a previous `KeyboardSnapshot` with a current `KeyboardSnapshot`. It exposes current key state and transition helpers for pressed and released keys.

The constructor is internal. Instances are created by `InputFrame`.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `IsDown(InputKey)` | `bool` | Returns whether the key is down in the current snapshot. |
| `IsPressed(InputKey)` | `bool` | Returns whether the key is down in the current snapshot and was up in the previous snapshot. |
| `IsReleased(InputKey)` | `bool` | Returns whether the key is up in the current snapshot and was down in the previous snapshot. |

## Applies to

- `Cerneala.UI.Input.InputFrame.KeyboardFrame`

## See also

- `Cerneala.UI.Input.InputFrame`
- `Cerneala.UI.Input.KeyboardSnapshot`
- `Cerneala.UI.Input.InputKey`
