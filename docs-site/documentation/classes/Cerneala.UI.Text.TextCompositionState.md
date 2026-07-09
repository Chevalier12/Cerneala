# TextCompositionState Class

## Definition
Namespace: `Cerneala.UI.Text`

Assembly/Project: `Cerneala`

Source: `UI/Text/TextCompositionState.cs`

Represents the current text composition state.

```csharp
public readonly record struct TextCompositionState(bool IsActive, int Start, string Text)
```

Inheritance:
`ValueType` -> `TextCompositionState`

## Examples

Inspect composition state from a manager:

```csharp
using Cerneala.UI.Text;

TextCompositionManager manager = new();
manager.Begin(2, "preedit");

TextCompositionState state = manager.State;
bool active = state.IsActive;
int start = state.Start;
string text = state.Text;
```

## Remarks

`TextCompositionState` stores whether a composition is active, the composition start position, and the current composition text.

`Inactive` returns a shared inactive state with start `0` and empty text.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `IsActive` | `bool` | Gets whether composition is active. |
| `Start` | `int` | Gets the start position of the composition. |
| `Text` | `string` | Gets the current composition text. |
| `Inactive` | `TextCompositionState` | Gets an inactive composition state. |

## Applies To

Cerneala UI text composition APIs.

## See Also

- `Cerneala.UI.Text.TextCompositionManager`
