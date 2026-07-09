# TextLine Class

## Definition
Namespace: `Cerneala.UI.Text`

Assembly/Project: `Cerneala`

Source: `UI/Text/TextLine.cs`

Represents one measured line of text.

```csharp
public readonly record struct TextLine
```

Inheritance:
`ValueType` -> `TextLine`

## Examples

Create a measured text line:

```csharp
using Cerneala.UI.Text;

TextLine line = new("Hello", 40);

string text = line.Text;
float width = line.Width;
```

## Remarks

`TextLine` stores the line text and measured width. The constructor throws `ArgumentNullException` when `text` is `null`.

The constructor also throws `ArgumentOutOfRangeException` when `width` is negative, infinite, or `NaN`.

## Constructors

| Signature | Description |
| --- | --- |
| `TextLine(string text, float width)` | Initializes a text line with non-null text and a finite, non-negative width. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Text` | `string` | Gets the line text. |
| `Width` | `float` | Gets the measured line width. |

## Applies To

Cerneala UI text measurement and line breaking.

## See Also

- `Cerneala.UI.Text.LineBreakService`
- `Cerneala.UI.Text.TextMeasureResult`
