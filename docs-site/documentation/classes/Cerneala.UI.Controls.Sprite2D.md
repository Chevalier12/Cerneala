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

The resource declaration below is shared by both sprites. The generated markup assigns a typed `ResourceId<ImageResource>`; it does not load the atlas separately for each sprite.

```xml
<RenderSurface2D xmlns:resources="clr-namespace:Cerneala.UI.Resources;assembly=Cerneala">
    <RenderSurface2D.Resources>
        <resources:ImageResource Name="WorldAtlas" Source="Assets/world.png" />
    </RenderSurface2D.Resources>
    <RenderSurface2D.Scene>
        <Scene2D>
            <Sprite2D SourceResourceId="$WorldAtlas" />
            <Sprite2D SourceResourceId="$WorldAtlas" />
        </Scene2D>
    </RenderSurface2D.Scene>
</RenderSurface2D>
```

Aspect, Motion, and Prism can still target the sprite declared from markup:

```xml
<Sprite2D
    Name="CurrentPiece"
    SourceResourceId="$WorldAtlas"
    Tint="#FFFFFFFF">
    <Sprite2D.Aspect>
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

`Destination` uses scene coordinates and `SourceRect`, when supplied, uses source-image pixels. `Rotation` is inherited from `UIElement` and is passed to image drawing in radians. `Origin` uses source-image pixels. Inherited `Layer` controls ordering in a parent `Scene2D` whose `OrderMode` is not `Source`. `LayerDepth` is separate: it is passed to `RenderSurface2DFrame.DrawSprite`, never changes scene-tree ordering, and must satisfy that API's `0` through `1` contract when the sprite is recorded.

`SourceResourceId` resolves an `ImageResource` from the nearest element resource dictionary or the owning root resource provider. A non-null resource ID takes precedence over `Source`; if that ID cannot be resolved, the sprite is skipped rather than falling back to `Source`. Sprites that resolve the same image resource through one root reuse that root's image cache. The cache or graphics session owns the resolved image and its disposal; `Sprite2D` does not dispose shared images.

`Opacity` is inherited from `UIElement` and multiplies the alpha channel of `Tint`. A null resolved source, non-positive opacity, `IsVisible == false`, or non-visible `Visibility` skips the sprite. Other inherited UI-element transforms do not alter sprite recording; use the scene coordinates and sprite-specific properties listed below.

`Sprite2D` remains a `UIElement` even though it is recorded through the scene command stream. Its logical attachment supports `Aspect`, generated bindings, and Motion. Motion targets registered interpolatable properties or Prism parameters, not every UI property; the sprite-animation restrictions are listed below. Normal UI-property precedence still applies: a local value masks an animation value for the same UI property.

An inline `@prism` block wraps only this sprite's image command. Sibling sprites and imperative surface commands are outside that Prism scope. Prism bounds are derived from the actual destination, source-relative `Origin`, and `Rotation`, then composed with the owning scene transform, including a `ViewBox` mapping. Effects can expand beyond those input bounds; applying Prism does not change scene coordinates, layout, hit testing, or the destination rectangle itself.

## Sprite-sheet animation

`Animations` selects a shared immutable [SpriteAnimationSet](Cerneala.UI.Controls.SpriteAnimationSet.md); `AnimationState` selects a case-sensitive clip name. Each sprite owns its playback progress. Null definitions, a null state, or an unresolved runtime state use the static `SourceRect` and `Flip` instead. Replacing the set resets progress and saved state positions; replacing the image or data context alone does not.

The selected frame supplies the effective source rectangle without changing `SourceRect`. Frame flip and sprite `Flip` compose by XOR on each axis. The destination stays in scene coordinates; the full atlas does not become the sprite's bounds. Prism wraps the selected frame and does not alter colliders.

`AnimationStateChangeMode.Restart` starts a newly selected state at frame zero. `Resume` saves the outgoing state's position and restores a previously saved incoming position. `RestartAnimation()` resets the current state without changing the selected state. Pause and a zero playback rate preserve progress; negative or non-finite rates are rejected.

Aspect can set definitions, state, pause, mode, and visual properties through their normal UI-property value sources. Motion can interpolate `AnimationPlaybackRate` and supported visual properties; it does not interpolate clip definitions, state names, pause, source rectangles, or flip flags. It is not a second frame sampler.

Playback consumes the owning surface's UI-frame delta, not wall-clock time or a per-sprite timer. Hidden and offscreen attached sprites keep their progress. Detach stops time and preserves the current position; reattach resumes. A non-loop holds its final presentation and stops requesting time when no later frame can change it. In `OnDemand`, the surface invalidates only on effective source-rectangle/flip changes, coalescing changes from multiple instances.

Declare immutable resources using Cerneala resource property-element syntax:

```xml
<RenderSurface2D>
  <RenderSurface2D.Resources>
    <SpriteAnimationSet Name="HeroAnimations">
      <SpriteAnimationClip Name="Walk" IsLooping="true">
        <SpriteAnimationFrame SourceX="0" SourceY="0" SourceWidth="16" SourceHeight="16" Duration="90ms" />
        <SpriteAnimationFrame SourceX="16" SourceY="0" SourceWidth="16" SourceHeight="16" Duration="110ms" Flip="Horizontal" />
      </SpriteAnimationClip>
    </SpriteAnimationSet>
  </RenderSurface2D.Resources>
  <RenderSurface2D.Scene>
    <Scene2D>
      <Sprite2D Animations="$HeroAnimations" AnimationState="Walk" />
    </Scene2D>
  </RenderSurface2D.Scene>
</RenderSurface2D>
```

The application must also supply the sprite image and destination. `SourceX`, `SourceY`, `SourceWidth`, and `SourceHeight` above are declarative constructor inputs for a frame's `DrawRect`, not mutable frame properties. Durations use `ms` or `s`; each duration must be positive. There is no separate FPS setting or ping-pong mode.

## Animation properties

All five properties have matching public `<Name>Property` identifier fields and `AffectsRender` metadata.

| Name | Type | Default | Description |
| --- | --- | --- | --- |
| `Animations` | `SpriteAnimationSet?` | `null` | Shared immutable clip definitions. |
| `AnimationState` | `string?` | `null` | Selected case-sensitive clip name. |
| `AnimationPlaybackRate` | `double` | `1` | Finite nonnegative multiplier for playback time. |
| `IsAnimationPaused` | `bool` | `false` | Preserves the current playback position when true. |
| `AnimationStateChangeMode` | `SpriteAnimationStateChangeMode` | `Restart` | Restart or restore saved progress when state changes. |

## Methods

| Name | Description |
| --- | --- |
| `RestartAnimation()` | Resets the current clip to frame zero and invalidates its surface if the visual frame changes. |

## Sprite fields

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
| `SourceResourceId` | `ResourceId<ImageResource>?` | Gets or sets the typed image-resource ID. A non-null ID takes precedence over `Source`. |
| `Destination` | `DrawRect` | Gets or sets the destination rectangle in scene coordinates. |
| `SourceRect` | `DrawRect?` | Gets or sets the optional source-image rectangle. |
| `Tint` | `Color` | Gets or sets the multiplicative color tint. |
| `Origin` | `DrawPoint` | Gets or sets the rotation origin in source-image pixels. |
| `Flip` | `RenderSurface2DSpriteFlip` | Gets or sets sprite mirroring. |
| `LayerDepth` | `float` | Gets or sets the layer depth forwarded to image drawing. |
| `Layer` | `int` | Gets or sets the parent-scene ordering layer. Inherited from `SceneNode2D`. |
| `Rotation` | `float` | Gets or sets rotation in radians. Inherited from `UIElement`. |
| `Opacity` | `float` | Gets or sets the alpha multiplier. Inherited from `UIElement`. |

## Property Information

| Property | Identifier field | Default value | Metadata/options |
| --- | --- | --- | --- |
| `Source` | `SourceProperty` | `null` | `AffectsRender` |
| `SourceResourceId` | None; CLR property | `null` | Invalidates rendering when changed. |
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
- `SceneOrderMode`
