# InputGesture Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/InputGesture.cs`

Defines the abstract base contract for input gestures that decide whether an `InputFrame` matches a command binding.

```csharp
public abstract class InputGesture
```

Inheritance:
`object` -> `InputGesture`

Derived:
`KeyGesture`

## Examples

Use a concrete gesture, such as `KeyGesture`, with an `InputBinding`:

```csharp
using Cerneala.UI.Input;

InputBinding saveBinding = new(
    new ActionCommand(_ => SaveDocument()),
    new KeyGesture(InputKey.S, KeyModifiers.Control));
```

Custom gestures derive from `InputGesture` and implement `Matches` against the supplied frame:

```csharp
using Cerneala.UI.Input;

public sealed class EnterPressGesture : InputGesture
{
    public override bool Matches(InputFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        return frame.Keyboard.IsPressed(InputKey.Enter);
    }
}
```

## Remarks

`InputGesture` does not implement matching behavior itself. Subclasses provide the matching rule by overriding `Matches(InputFrame)`.

`InputBinding.Matches(InputFrame)` delegates directly to the binding's `Gesture.Matches(frame)`. `InputBinding.TryExecute` uses that result before attempting command execution.

The built-in concrete gesture visible in this API surface is `KeyGesture`, which matches a newly pressed key with an exact modifier set.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Matches(InputFrame frame)` | `bool` | When overridden in a derived class, returns whether the gesture matches the supplied input frame. |

## Applies to

Cerneala retained UI input binding and command routing.

## See also

- `InputBinding`
- `KeyGesture`
- `KeyBinding`
- `InputFrame`
