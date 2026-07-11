# Control.Foreground Property

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Control.cs`

Gets or sets the inherited brush used for foreground content, including text.

```csharp
public Brush? Foreground { get; set; }
```

### Property Value

`Brush?`. The default is `new SolidColorBrush(Color.Black)`. Assigning `null` suppresses foreground text drawing.

## Examples

```csharp
TextBlock text = new()
{
    Text = "Gradient",
    Foreground = new LinearGradientBrush(
        new DrawPoint(0, 0),
        new DrawPoint(120, 0),
        [new GradientStop(0, Color.Tomato), new GradientStop(1, Color.AliceBlue)])
};
```

```xml
<TextBlock Foreground="Tomato" Text="Salut" />
```

```xml
<TextBlock Text="Gradient">
  <TextBlock.Foreground>
    <LinearGradientBrush StartPoint="0,0" EndPoint="120,0">
      <GradientStop Offset="0" Color="Tomato" />
      <GradientStop Offset="1" Color="AliceBlue" />
    </LinearGradientBrush>
  </TextBlock.Foreground>
</TextBlock>
```

## Remarks

`Foreground` is inherited and affects rendering without changing text measurement or shaping. Text rendering caches glyph coverage independently from the brush. Solid brushes use the subpixel fast path; gradient, image, drawing, and visual brushes are composed through the glyph mask.

The short markup form converts a color to `SolidColorBrush`. Resource references and property elements must resolve to `Brush`; values of another type produce a markup type diagnostic. Brush opacity is composed once with command and element opacity.

There is no `ForegroundColor` compatibility alias and no implicit `Color` conversion. Migrate `control.Foreground = color` to `control.Foreground = new SolidColorBrush(color)`.

### Property Information

| Item | Value |
| --- | --- |
| Identifier field | `ForegroundProperty` |
| Property type | `UiProperty<Brush?>` |
| Default value | `new SolidColorBrush(Color.Black)` |
| Metadata/options | `Inherits`, `AffectsRender` |

## Applies to

Project: `Cerneala`

## See also

- `Cerneala.UI.Media.Brush`
- `Cerneala.UI.Media.SolidColorBrush`
- `Cerneala.UI.Text.TextRenderer`
