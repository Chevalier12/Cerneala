# TextCaret Class

## Definition
Namespace: `Cerneala.UI.Text`

Assembly/Project: `Cerneala`

Source: `UI/Text/TextCaret.cs`

Represents a caret position within text.

```csharp
public readonly record struct TextCaret(int Position)
```

Inheritance:
`ValueType` -> `TextCaret`

## Examples

Clamp a caret position to a document length:

```csharp
using Cerneala.UI.Text;

TextCaret caret = TextCaret.At(position: 24, documentLength: 10);

int position = caret.Position;
```

## Remarks

`TextCaret` stores a zero-based text position. The `At` factory clamps the requested position between `0` and `documentLength`.

`At` throws `ArgumentOutOfRangeException` when `documentLength` is negative.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Position` | `int` | Gets the caret position. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `At(int position, int documentLength)` | `TextCaret` | Creates a caret with `position` clamped into the document range. |

## Applies To

Cerneala UI text editing and caret APIs.

## See Also

- `Cerneala.UI.Text.TextCompositionManager`
