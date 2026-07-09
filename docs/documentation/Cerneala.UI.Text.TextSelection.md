# TextSelection Class

## Definition
Namespace: `Cerneala.UI.Text`

Assembly/Project: `Cerneala`

Source: `UI/Text/TextSelection.cs`

Represents a text selection using anchor and active positions.

```csharp
public readonly record struct TextSelection(int Anchor, int Active)
```

Inheritance:
`ValueType` -> `TextSelection`

## Examples

Create and clamp a selection:

```csharp
using Cerneala.UI.Text;

TextSelection selection = new(anchor: 8, active: 2);
TextSelection clamped = selection.Clamp(documentLength: 5);

int start = clamped.Start;
int length = clamped.Length;
```

Create a caret selection:

```csharp
TextSelection caret = TextSelection.Caret(3);
```

## Remarks

`TextSelection` keeps the original anchor and active endpoints. `Start`, `End`, and `Length` derive the normalized selection range from those endpoints.

`Caret` creates an empty selection at one position. `Clamp` clamps both endpoints into the document range and throws `ArgumentOutOfRangeException` when `documentLength` is negative.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Anchor` | `int` | Gets the fixed selection endpoint. |
| `Active` | `int` | Gets the active selection endpoint. |
| `Start` | `int` | Gets the lower endpoint. |
| `End` | `int` | Gets the higher endpoint. |
| `Length` | `int` | Gets the normalized selection length. |
| `IsEmpty` | `bool` | Gets whether the selection length is zero. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Caret(int position)` | `TextSelection` | Creates an empty selection at `position`. |
| `Clamp(int documentLength)` | `TextSelection` | Returns a selection with both endpoints clamped to the document range. |

## Applies To

Cerneala UI text editing APIs.

## See Also

- `Cerneala.UI.Text.TextEditor`
- `Cerneala.UI.Text.TextCaret`
