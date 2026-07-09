# MonoGameInputSource Class

## Definition
Namespace: `Cerneala.UI.Input.MonoGame`

Assembly/Project: `Cerneala`

Source: `UI/Input/MonoGame/MonoGameInputSource.cs`

Provides input frames by reading MonoGame mouse and keyboard state.

```csharp
public sealed class MonoGameInputSource : IInputSource
```

Inheritance:
`object` -> `MonoGameInputSource`

Implements:
`IInputSource`

## Examples

Read an input frame from MonoGame:

```csharp
using Cerneala.UI.Input.MonoGame;

MonoGameInputSource inputSource = new()
{
    CoordinateScale = 2
};

inputSource.QueueTextInput("a");

InputFrame frame = inputSource.GetFrame();
```

## Remarks

`MonoGameInputSource` reads current mouse and keyboard state from MonoGame and returns an `InputFrame` containing previous and current snapshots.

The default constructor reads from `Mouse.GetState` and `Keyboard.GetState`. `CoordinateScale` is validated through `UiCoordinateMapper.ValidateScale` and is used to convert physical pointer coordinates to logical coordinates.

`QueueTextInput` stores text input events until the next `GetFrame` call. `GetFrame` returns queued text input events and clears the queue.

## Constructors

| Signature | Description |
| --- | --- |
| `MonoGameInputSource()` | Initializes the input source using MonoGame mouse and keyboard state readers. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `CoordinateScale` | `float` | Gets or sets the scale used to convert physical pointer coordinates to logical coordinates. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `QueueTextInput(string text)` | `void` | Queues a text input event for the next input frame. |
| `GetFrame()` | `InputFrame` | Reads current MonoGame input state and returns an input frame. |

## Applies To

Cerneala MonoGame UI hosting and input integration.

## See Also

- `Cerneala.UI.Input.IInputSource`
- `Cerneala.UI.Input.InputFrame`
- `Cerneala.UI.Hosting.UiCoordinateMapper`
