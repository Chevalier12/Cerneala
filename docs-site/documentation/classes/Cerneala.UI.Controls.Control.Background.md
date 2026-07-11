# Control.Background Property

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Control.cs`

Gets or sets the brush used to fill a control background.

```csharp
public Brush? Background { get; set; }
```

### Property Value

`Brush?`. The default is `null`, which draws no background.

## Examples

```csharp
Border border = new()
{
    Background = new SolidColorBrush(Color.White)
};
```

```xml
<Border Background="Tomato" />
```

## Remarks

The short markup form converts a named, hexadecimal, RGB, or RGBA color explicitly to `SolidColorBrush`. Resource references resolve to `Brush`, and property-element markup accepts composite brush elements such as `LinearGradientBrush`.

Changing the property invalidates render and input-visual state. A `null` value suppresses background drawing. Code that previously assigned `Color` must wrap solid colors explicitly with `new SolidColorBrush(color)`.

### Property Information

| Item | Value |
| --- | --- |
| Identifier field | `BackgroundProperty` |
| Property type | `UiProperty<Brush?>` |
| Default value | `null` |
| Metadata/options | `AffectsRender`, `AffectsInputVisual` |

## Applies to

Project: `Cerneala`
