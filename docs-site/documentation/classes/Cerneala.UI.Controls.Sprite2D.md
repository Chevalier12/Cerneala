# Sprite2D Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Sprite2D.cs`

Records one retained image sprite into an owning `RenderSurface2D`.

```csharp
public sealed class Sprite2D : SceneNode2D
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `SceneNode2D` -> `Sprite2D`

## Examples

```xml
<Sprite2D
    Name="CurrentPiece"
    Source="$DataContext.CurrentImage:OneWay"
    SourceRect="$DataContext.CurrentSourceRect:OneWay"
    Destination="$DataContext.CurrentDestination:OneWay"
    Tint="$DataContext.CurrentTint:OneWay"
    IsVisible="$DataContext.HasCurrentPiece:OneWay">
    <Sprite2D.Aspect>
        <Aspect>
            @when $self.IsVisible
            {
                @if $self.IsVisible == true
                {
                    @animate with PingPong(Tween(900ms, EaseInOut), forever)
                    {
                        @from { $self.prism.Effects.GlowSize = 4; }
                        @to { $self.prism.Effects.GlowSize = 10; }
                    }
                }
            }
        </Aspect>
    </Sprite2D.Aspect>
    @prism
    {
        @layer Effects
        {
            @parameter GlowSize: float = 4;
            @style OuterGlow
            {
                Color = $self.Tint:OneWay;
                Size = GlowSize;
            }
        }
    }
</Sprite2D>
```

## Remarks

`Destination` uses scene coordinates and `SourceRect`, when supplied, uses source-image pixels. `Rotation` is inherited from `UIElement` and is passed to image drawing in radians. `Origin` uses source-image pixels. `LayerDepth` is passed to `RenderSurface2DFrame.DrawSprite` and must satisfy that API's `0` through `1` contract when the sprite is recorded.

`Opacity` is inherited from `UIElement` and multiplies the alpha channel of `Tint`. A null `Source`, non-positive opacity, `IsVisible == false`, or non-visible `Visibility` skips the sprite. Other inherited UI-element transforms do not alter sprite recording; use the scene coordinates and sprite-specific properties listed below.

`Sprite2D` remains a `UIElement` even though it is recorded through the scene command stream. Its logical attachment supports `Aspect`, generated bindings, and Motion. Motion can target the sprite UI properties in this page or Prism properties declared by the sprite. Normal UI-property precedence still applies: a local value masks an animation value for the same UI property.

An inline `@prism` block wraps only this sprite's image command. Sibling sprites and imperative surface commands are outside that Prism scope. Prism uses `Destination` as the control bounds and preserves the owning `RenderSurface2D` scene transform, including a `ViewBox` mapping, as the scope's effective transform. Effects can expand beyond `Destination`; applying Prism does not change scene coordinates, layout, hit testing, or the destination rectangle itself.

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `SourceProperty` | `UiProperty<IDrawImage?>` | Identifies the source image. |
| `DestinationProperty` | `UiProperty<DrawRect>` | Identifies the destination rectangle. |
| `SourceRectProperty` | `UiProperty<DrawRect?>` | Identifies the optional source-image rectangle. |
| `TintProperty` | `UiProperty<Color>` | Identifies the multiplicative sprite tint. |
| `OriginProperty` | `UiProperty<DrawPoint>` | Identifies the rotation origin in source-image pixels. |
| `FlipProperty` | `UiProperty<RenderSurface2DSpriteFlip>` | Identifies horizontal or vertical mirroring. |
| `LayerDepthProperty` | `UiProperty<float>` | Identifies the sprite layer depth. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Source` | `IDrawImage?` | Gets or sets the source image. |
| `Destination` | `DrawRect` | Gets or sets the destination rectangle in scene coordinates. |
| `SourceRect` | `DrawRect?` | Gets or sets the optional source-image rectangle. |
| `Tint` | `Color` | Gets or sets the multiplicative color tint. |
| `Origin` | `DrawPoint` | Gets or sets the rotation origin in source-image pixels. |
| `Flip` | `RenderSurface2DSpriteFlip` | Gets or sets sprite mirroring. |
| `LayerDepth` | `float` | Gets or sets the layer depth forwarded to image drawing. |
| `Rotation` | `float` | Gets or sets rotation in radians. Inherited from `UIElement`. |
| `Opacity` | `float` | Gets or sets the alpha multiplier. Inherited from `UIElement`. |

## Property Information

| Property | Identifier field | Default value | Metadata/options |
| --- | --- | --- | --- |
| `Source` | `SourceProperty` | `null` | `AffectsRender` |
| `Destination` | `DestinationProperty` | `default(DrawRect)` | `AffectsRender` |
| `SourceRect` | `SourceRectProperty` | `null` | `AffectsRender` |
| `Tint` | `TintProperty` | `Color.White` | `AffectsRender` |
| `Origin` | `OriginProperty` | `default(DrawPoint)` | `AffectsRender` |
| `Flip` | `FlipProperty` | `RenderSurface2DSpriteFlip.None` | `AffectsRender` |
| `LayerDepth` | `LayerDepthProperty` | `0` | `AffectsRender` |

## Applies to

Project: `Cerneala`

## See also

- `RenderSurface2D`
- `RenderSurface2DFrame`
- `Scene2D`
- `SceneItems2D`
