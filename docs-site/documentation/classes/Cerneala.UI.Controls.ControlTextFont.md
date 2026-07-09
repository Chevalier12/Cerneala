# ControlTextFont Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/ControlTextFont.cs`

Represents an immutable drawing font descriptor for control text.

```csharp
public sealed class ControlTextFont : IDrawFont
```

Inheritance:
`object` -> `ControlTextFont`

Implements:
`IDrawFont`

## Examples

Create a control text font and use it in a text run:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;

ControlTextFont font = new("Arial", 16f);
DrawTextRun run = new(font, "Save", font.Size);
```

## Remarks

`ControlTextFont` is a small `IDrawFont` implementation used by controls that need to pass a font family and size into the drawing text pipeline.

Instances are immutable after construction. The constructor stores the supplied family name as-is after validating that it is not `null`, empty, or whitespace. It also validates that the font size is positive and finite.

`ControlTextFont` does not resolve, load, rasterize, or measure the font by itself. It only carries the font family name and size expected by APIs that consume `IDrawFont`, such as `DrawTextRun`.

## Constructors

| Name | Description |
| --- | --- |
| `ControlTextFont(string familyName, float size)` | Initializes a font descriptor with a required font family name and a positive finite size. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `FamilyName` | `string` | Gets the font family name supplied to the constructor. |
| `Size` | `float` | Gets the positive finite font size supplied to the constructor. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `ControlTextFont(string familyName, float size)` | `ArgumentException` | `familyName` is `null`, empty, or whitespace. |
| `ControlTextFont(string familyName, float size)` | `ArgumentOutOfRangeException` | `size` is less than or equal to `0`, `NaN`, or infinite. |

## Applies to

Project: `Cerneala`

## See also

- `UI/Controls/ControlTextFont.cs`
- `Drawing/IDrawFont.cs`
- `Drawing/DrawTextRun.cs`
