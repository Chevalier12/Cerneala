# BrushMarkupReader Class

## Definition
Namespace: `Cerneala.UI.Markup`  
Assembly/Project: `Cerneala`  
Source: `UI/Markup/BrushMarkupReader.cs`

Parses one standalone brush XML element at runtime.

```csharp
public sealed class BrushMarkupReader
```

## Examples
```csharp
BrushMarkupReader reader = new();
MarkupResult<Brush> result = reader.Read("""
    <LinearGradientBrush StartPoint="0,0" EndPoint="100,0">
      <GradientStop Offset="0" Color="White" />
      <GradientStop Offset="1" Color="Black" />
    </LinearGradientBrush>
    """);
```

## Remarks
The reader supports `SolidColorBrush`, `LinearGradientBrush`, `RadialGradientBrush`, `ImageBrush`, `DrawingBrush`, and `VisualBrush`. Supply resolvers for image identities and live visual names. Invalid XML or values return markup diagnostics instead of escaping as parser exceptions.

## Constructors
| Name | Description |
| --- | --- |
| `BrushMarkupReader(Func<string, IDrawImage?>?, Func<string, UIElement?>?)` | Creates a reader with optional image and visual resolvers. |

## Methods
| Name | Description |
| --- | --- |
| `Read(string)` | Parses the markup and returns `MarkupResult<Brush>`. |

## Remarks on Errors
Empty input reports `MARKUP030`. XML, value, and resolution failures report `MARKUP031` with a human-readable message.

The enum attributes `Stretch`, `AlignmentX`, `AlignmentY`, and `TileMode` accept only a member name, with the exact case: `Stretch="Uniform"` and `TileMode="FlipXY"` parse. A number or a comma list names no member of these enums, so `<ImageBrush Source="accent.png" TileMode="42" />` and `<ImageBrush Source="accent.png" Stretch="Uniform,Fill" />` report `MARKUP031` with the message `'42' is not a valid ImageBrush.TileMode value.` (or `'Uniform,Fill' is not a valid ImageBrush.Stretch value.`), and `Value` is `null`.

## Applies to
Runtime markup loading in `Cerneala`.

## See also
- `Cerneala.UI.Markup.UiMarkupReader`
- `Cerneala.UI.Media.Brush`
