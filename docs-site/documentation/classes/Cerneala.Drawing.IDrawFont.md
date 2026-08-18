# IDrawFont Interface

## Definition
Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/IDrawFont.cs`

Describes the font identity and nominal size carried by drawing text runs.

```csharp
public interface IDrawFont
```

## Examples

```csharp
using Cerneala.Drawing;

sealed record DemoFont(string FamilyName, float Size) : IDrawFont;

IDrawFont font = new DemoFont("Demo Sans", 16);
DrawTextRun run = new(font, "Cerneala", 16);
```

## Remarks

`IDrawFont` is the drawing-layer font contract consumed by `DrawTextRun` and exposed on `DrawCommand.Font`. Implementations provide only the family name and nominal font size; font loading is handled separately by `IFontSource`.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `FamilyName` | `string` | Gets the font family identity. |
| `Size` | `float` | Gets the nominal font size represented by the font instance. |

## Applies to

Cerneala drawing text commands and font-loading integrations.

## See also

- `Cerneala.Drawing.DrawTextRun`
- `Cerneala.Drawing.IFontSource`
