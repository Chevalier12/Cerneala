# KeyGesture Class

## Definition
Namespace: `Cerneala.UI.Input`

Assembly/Project: `Cerneala`

Source: `UI/Input/KeyGesture.cs`

Represents a keyboard gesture made from a concrete key and an optional exact set of keyboard modifiers.

```csharp
public sealed class KeyGesture : InputGesture
```

Inheritance:
`Object` -> `InputGesture` -> `KeyGesture`

## Examples

Create a key gesture for a command binding or input binding.

```csharp
using Cerneala.UI.Input;

KeyGesture saveGesture = new(InputKey.S, KeyModifiers.Control);

if (saveGesture.Matches(inputFrame))
{
    SaveDocument();
}
```

## Remarks

`KeyGesture` matches when the configured key is pressed in the supplied `InputFrame` and the current modifier state exactly matches `Modifiers`.

The constructor rejects `InputKey.None` and `InputKey.Unknown`, because a gesture must target a concrete key.

Modifier matching checks left and right variants for Shift, Control, and Alt. A modifier must be down when it is included in `Modifiers`, and must be up when it is not included.

`Matches` throws when `frame` is `null`.

## Constructors

| Name | Description |
| --- | --- |
| `KeyGesture(InputKey, KeyModifiers)` | Initializes a key gesture for a key and optional modifiers. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Key` | `InputKey` | Gets the key that must be pressed for the gesture to match. |
| `Modifiers` | `KeyModifiers` | Gets the exact modifier set required by the gesture. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Matches(InputFrame)` | `bool` | Returns whether the frame contains the configured key press and exact modifier state. |

## Applies to

- `Cerneala.UI.Input.KeyGesture`

## See also

- `Cerneala.UI.Input.InputGesture`
- `Cerneala.UI.Input.KeyModifiers`
- `Cerneala.UI.Input.InputFrame`
