# Control.BorderBrush Property

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Control.cs`

Gets or sets the brush used to draw a control border.

```csharp
public Brush? BorderBrush { get; set; }
```

### Property Value

`Brush?`. The default is `null`, which draws no border.

## Examples

```csharp
Border border = new()
{
    BorderBrush = new SolidColorBrush(Color.Red),
    BorderThickness = new Thickness(1)
};
```

```xml
<Border BorderBrush="Tomato" BorderThickness="1" />
```

## Remarks

The short markup form converts a named, hexadecimal, RGB, or RGBA color explicitly to `SolidColorBrush`. Resource references resolve to `Brush`, and property-element markup accepts composite brush elements such as `LinearGradientBrush`.

Changing the property invalidates render and input-visual state. A `null` value or zero border thickness suppresses border drawing. The former `BorderColor` name is not retained as an alias; migrate color assignments to `new SolidColorBrush(color)`.

### Property Information

| Item | Value |
| --- | --- |
| Identifier field | `BorderBrushProperty` |
| Property type | `UiProperty<Brush?>` |
| Default value | `null` |
| Metadata/options | `AffectsRender`, `AffectsInputVisual` |

## Applies to

Project: `Cerneala`
