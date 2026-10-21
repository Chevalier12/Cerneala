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

A source can also provide the bold, italic, or condensed faces of a family by overriding the five-argument overload. A source that does not override it needs no change: the default implementation returns `LoadFont(familyName, size)`, so a weight, style, or stretch request gets the plain face.

```csharp
using Cerneala.Drawing;

sealed class StyledDemoFontSource : IFontSource
{
    public IDrawFont LoadFont(string familyName, float size)
        => new DemoFont(familyName, size);

    public IDrawFont LoadFont(string familyName, float size, FontWeight weight, FontStyle style, FontStretch stretch)
        => new DemoFont(weight == FontWeight.Bold ? familyName + " Bold" : familyName, size);
}

sealed record DemoFont(string FamilyName, float Size) : IDrawFont;
```

`FontResolver` calls the five-argument overload for any aspect or request whose weight, style, or stretch is not `Normal`, and the two-argument overload otherwise. With all three `Normal`, `FontResolver` does not call the five-argument overload.

## Methods

| Name | Return type | Description |
| --- | --- | --- |
| `LoadFont(string familyName, float size)` | `IDrawFont` | Loads or creates a drawing font for the requested family and size. |
| `LoadFont(string familyName, float size, FontWeight weight, FontStyle style, FontStretch stretch)` | `IDrawFont` | Loads the face of the family with the requested weight, style, and stretch. Default interface method: returns `LoadFont(familyName, size)`, so a source without styled faces returns its plain font. |

## Applies to

Cerneala drawing text and font-resolution integrations.

## See also

- `Cerneala.Drawing.IDrawFont`
- `Cerneala.Drawing.DrawTextRun`
- `Cerneala.Drawing.FontWeight`
- `Cerneala.Drawing.FontStyle`
- `Cerneala.Drawing.FontStretch`
- `Cerneala.Drawing.Text.SystemFontSource`
