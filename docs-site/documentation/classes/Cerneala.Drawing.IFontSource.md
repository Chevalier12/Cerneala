# IFontSource Interface

## Definition
Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/IFontSource.cs`

Loads drawing fonts for a requested family and size.

```csharp
public interface IFontSource
```

## Examples

```csharp
using Cerneala.Drawing;

sealed record DemoFont(string FamilyName, float Size) : IDrawFont;

sealed class DemoFontSource : IFontSource
{
    public IDrawFont LoadFont(string familyName, float size)
        => new DemoFont(familyName, size);
}

IFontSource source = new DemoFontSource();
IDrawFont font = source.LoadFont("Demo Sans", 16);
```

## Remarks

`IFontSource` separates font acquisition from the drawing font contract. `FontResolver` and platform-specific sources can request an `IDrawFont` without coupling drawing commands to a concrete font implementation. The repository's system implementation is `Cerneala.Drawing.Text.SystemFontSource`.

## Methods

| Name | Return type | Description |
| --- | --- | --- |
| `LoadFont(string familyName, float size)` | `IDrawFont` | Loads or creates a drawing font for the requested family and size. |

## Applies to

Cerneala drawing text and font-resolution integrations.

## See also

- `Cerneala.Drawing.IDrawFont`
- `Cerneala.Drawing.DrawTextRun`
- `Cerneala.Drawing.Text.SystemFontSource`
