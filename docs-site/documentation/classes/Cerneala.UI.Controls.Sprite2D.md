# Sprite2D Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Sprite2D.cs`

Records one retained image sprite into an owning `RenderSurface2D`.

```csharp
[ContentProperty(nameof(Collider))]
public sealed class Sprite2D : SceneNode2D
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `SceneNode2D` -> `Sprite2D`

## Examples

The resource declaration below is shared by both sprites. The generated markup assigns an `ImageReference` containing a typed `ResourceId<ImageResource>`; it does not load the atlas separately for each sprite.

```xml
<RenderSurface2D xmlns:resources="clr-namespace:Cerneala.UI.Resources;assembly=Cerneala">
    <RenderSurface2D.Resources>
        <resources:ImageResource Name="WorldAtlas" Source="Assets/world.png" />
    </RenderSurface2D.Resources>
    <RenderSurface2D.Scene>
        <Scene2D>
            <Sprite2D Image="$WorldAtlas" />
            <Sprite2D Image="$WorldAtlas" />
        </Scene2D>
    </RenderSurface2D.Scene>
</RenderSurface2D>
```

Aspect, Motion, and Prism can still target the sprite declared from markup:

```xml
<Sprite2D
    Name="CurrentPiece"
    Image="$WorldAtlas"
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

`X` and `Y` position the sprite in scene coordinates. `Width` and `Height` are inherited UI properties used as draw dimensions, not scene layout. Each omitted dimension (`float.NaN`) uses the corresponding selected source-region dimension. `SourceX` and `SourceY` default to zero; omitted `SourceWidth` and `SourceHeight` (`float.NaN`) use the remaining image extent from that coordinate. There is no aspect-ratio inference when just one destination dimension is explicit. Source coordinates must be finite and nonnegative, explicit source dimensions finite and positive, and the selected region must fit inside the image. Invalid regions are rejected when geometry is resolved, not clamped. `X` and `Y` must be finite; destination dimensions accept zero or positive finite values, or `NaN` for automatic size. `Rotation` is inherited from `UIElement` and is passed to image drawing in radians. `Origin` uses source-image pixels. Inherited `Layer` controls ordering in a parent `Scene2D` whose `OrderMode` is not `Source`. `LayerDepth` is separate: it is forwarded to the image drawing options, never changes scene-tree ordering, and must satisfy their `0` through `1` contract when the sprite is recorded.

`Image` accepts an immutable [ImageReference](Cerneala.UI.Resources.ImageReference.md): either a direct image (`new ImageReference(image)`) or a typed resource ID (`new ImageReference(new ResourceId<ImageResource>("WorldAtlas"))`). A resource reference resolves an `ImageResource` from the nearest element resource dictionary or the owning root resource provider. A missing resource skips drawing; there is no second source or fallback. `Image = null` clears the image. Sprites that resolve the same path-backed image through one root reuse that root's image cache with independent acquisitions. A sprite releases its acquisition when the image is no longer needed; the cache disposes an owned image only after the last consumer, including retained commands, releases it. Direct and embedded images remain borrowed from their caller.

### Image sampling

`Sampling` selects [DrawSamplingMode](Cerneala.Drawing.DrawSamplingMode.md) for this sprite's image command. It defaults to `Linear`, which blends neighboring texels. Set `Point` to sample the nearest texel for pixel-art imagery. Only `Point` and `Linear` are accepted; an unsupported enum value is rejected. Changing the value invalidates rendering, not the sprite's geometry, layout, or collider. The selected mode also applies when animation changes the source frame and when sprite and frame flips compose; it does not change those crop or flip rules.

```csharp
var sprite = new Sprite2D
{
    Image = new ImageReference(new ResourceId<ImageResource>("WorldAtlas")),
    SourceX = 16,
    SourceWidth = 16,
    SourceHeight = 16,
    Sampling = DrawSamplingMode.Point
};
```

With `WorldAtlas` declared as in the markup example above, the same choice is:

```xml
<Sprite2D Image="$WorldAtlas" SourceX="16" SourceWidth="16" SourceHeight="16" Sampling="Point" />
```

A source rectangle selects image geometry but is not a universal texel-isolation boundary within an atlas: `Linear` filtering can blend neighboring artwork near its edge. `Sprite2D` records an image command with `Clamp` addressing. On the current SDL_GPU path, choosing `Point` uses [coverage-aware image-edge sampling](Cerneala.Drawing.DrawImageOptions.md#point-and-clamp-image-edges-on-sdl-gpu) for covered pixels whose centers lie on or outside the sprite's *external* image boundary. True interior centers keep the ordinary nearest-texel choice. This handles the tested fractional and exact destination-edge cases without adding padding or changing the crop. Exact source-boundary ties can still choose an adjacent texel, so `Point` is not an unconditional atlas-isolation guarantee or a change to all compositing paths.

`Opacity` is inherited from `UIElement` and multiplies the alpha channel of `Tint`. A null resolved source, non-positive opacity, `IsVisible == false`, or non-visible `Visibility` skips the sprite. Other inherited UI-element transforms do not alter sprite recording; use the scene coordinates and sprite-specific properties listed below.

`Sprite2D` remains a `UIElement` even though it is recorded through the scene command stream. Its logical attachment supports `Aspect`, generated bindings, and Motion. Motion targets registered interpolatable properties or Prism parameters, not every UI property; the sprite-animation restrictions are listed below. Normal UI-property precedence still applies: a local value masks an animation value for the same UI property.

An inline `@prism` block wraps only this sprite's image command. Sibling sprites and imperative surface commands are outside that Prism scope. Prism bounds are derived from the actual destination, source-relative `Origin`, and `Rotation`, then composed with the owning scene transform, including a `ViewBox` mapping. Effects can expand beyond those input bounds; applying Prism does not change scene coordinates, layout, hit testing, or the destination rectangle itself.

In a hosted spatial scene, an active composition on the sprite without supported
automatic input selection requires
its own inherited [PrismInputDomain](Cerneala.UI.Controls.SceneNode2D.md#finite-prism-input-domains),
even when an ancestor composition has a declaration. This includes the seven
non-pointwise styles, wrapped edge modes and unclassified/global filters. That finite domain
replaces this composition's ordinary logical capture bounds. It is in local
destination units relative to the sprite anchor, before `Rotation`, `X` and `Y`;
it does not change `Origin` or rescale the image. A missing required declaration
is a surface presentation error, not permission to omit the nested effect.
Input-domain validation still runs when it causes empty input selection, without
starting image acquisition merely to report the configuration error.

In a spatial scene, covered local filters on this sprite or its scene ancestors
use the shared finite sampling-input region without shrinking their logical
coordinate bounds. Capture intersects that region with the sprite's actual
input bounds, unless its own active non-pointwise composition declares a domain;
an ancestor's larger interest does not enlarge the sprite's source boundary.
This can retain an image just outside the camera when a
visible filtered pixel needs it. See [Scene2D's exact input coverage](Cerneala.UI.Controls.Scene2D.md#prism-input-and-streamed-children);
unclassified effects require their own declared domain, and this does not detach or stop an
offscreen simulated sprite.

### Asynchronous image presentation

The owning surface prepares required cold path-backed images through [IAsyncImageLoader](Cerneala.UI.Resources.IAsyncImageLoader.md). The sprite and its live Prism image resources, including masks, share the existing root cache; referencing one atlas in both places does not start two decodes. Each use keeps its own acquisition. Scene recording and sprite bounds queries use resident images only: they do not perform synchronous path loading or wait for a pending decode.

Until all required map chunks and sprite/Prism/atlas images are ready, the surface reports [PresentationState](Cerneala.UI.Controls.RenderSurface2D.md#scene-preparation-and-input-availability) as `Loading` and withholds the entire retained scene, including ready siblings, and its input routes. A load failure reports `Error` and `PresentationError`; polling an unchanged failed acquisition does not retry it. A cold path backed by a synchronous-only loader fails through that state rather than falling back to blocking decoding. Ordinary UI, imperative drawing, attached animation, and collision participation continue. A missing resource lookup or `Image = null` still means no image, not a loading error.

Explicit destination dimensions, static crop dimensions, or an animation frame can provide bounds before decoding. Images outside proven required coverage are not prepared, and unused sprite and Prism acquisitions are retired without detaching the sprite or its collider. Unknown natural dimensions cannot prove exclusion; Prism can conservatively require off-camera input because effects may extend beyond sprite bounds. [SceneItems2D](Cerneala.UI.Controls.SceneItems2D.md) realizes its collection independently of the viewport; only presentation work for its realized children is culled. Removing a sprite's resolved content also retires its no-longer-used Prism images, including pending work. Other consumers may still keep a shared atlas resident.

## Collider ownership

`Collider` is the nullable `Collider2D` content property. A sprite owns zero or one live collider. Assigning the property owns logical attachment, surface invalidation, and registration in the containing scene's collision world; it does not add a visual layout child. Assign a replacement or `null` through this property, not through generic UI child collections. A collider must be cleared from its current owner before moving to another sprite. Assigning a collider that already has an owner is rejected before the current collider is removed.

```xml
<Sprite2D Image="$WorldAtlas" X="64" Y="32" Width="32" Height="32">
  <BoxCollider2D Width="32" Height="8" OffsetY="24" />
</Sprite2D>
```

Collider coordinates are destination units relative to the sprite anchor. `X`, `Y`, `Rotation`, and ancestor scene transforms place both the image and its collider. Image `Width`/`Height`, natural dimensions, crop, source-pixel `Origin`, `Flip`, and animation frames affect drawing only. A `32 × 32` collider stays `32 × 32` when the sprite is drawn at `64 × 64`; applications explicitly update or bind the collider geometry when needed. A collider can exist on a sprite without a resolved image and does not cause an image to be synthesized.

## Sprite-sheet animation

`Animations` selects a shared immutable [SpriteAnimationSet](Cerneala.UI.Controls.SpriteAnimationSet.md); `AnimationState` selects a case-sensitive clip name. Each sprite owns its playback progress. Null definitions, a null state, or an unresolved runtime state use the static source-coordinate properties and `Flip` instead. Replacing the set resets progress and saved state positions; replacing the image or data context alone does not.

The selected frame supplies the effective source rectangle without changing the static source-coordinate properties. Automatic destination dimensions follow that selected frame. Frame flip and sprite `Flip` compose by XOR on each axis. The destination stays in scene coordinates; the full atlas does not become the sprite's bounds. Prism wraps the selected frame and does not alter the collider.

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

The application must also supply the sprite image. Position and destination size may be omitted. `SourceX`, `SourceY`, `SourceWidth`, and `SourceHeight` above are declarative constructor inputs for a frame's `DrawRect`, not mutable frame properties. Durations use `ms` or `s`; each duration must be positive. There is no separate FPS setting or ping-pong mode.

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
| `ImageProperty` | `UiProperty<ImageReference?>` | Identifies the single image reference. |
| `XProperty`, `YProperty` | `UiProperty<float>` | Identify scene position. |
| `SourceXProperty`, `SourceYProperty` | `UiProperty<float>` | Identify the static crop offset. |
| `SourceWidthProperty`, `SourceHeightProperty` | `UiProperty<float>` | Identify the static crop dimensions. |
| `TintProperty` | `UiProperty<Color>` | Identifies the multiplicative sprite tint. |
| `OriginProperty` | `UiProperty<DrawPoint>` | Identifies the rotation origin in source-image pixels. |
| `FlipProperty` | `UiProperty<RenderSurface2DSpriteFlip>` | Identifies horizontal or vertical mirroring. |
| `SamplingProperty` | `UiProperty<DrawSamplingMode>` | Identifies this sprite's image sampling mode. |
| `LayerDepthProperty` | `UiProperty<float>` | Identifies the sprite layer depth. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Collider` | `Collider2D?` | Gets or sets the single owner-managed live collision shape. This is the markup content property. |
| `Image` | `ImageReference?` | Gets or sets a direct image or typed resource reference. |
| `X`, `Y` | `float` | Scene position; source-relative `Origin` remains the anchor. |
| `Width`, `Height` | `float` | Draw size; inherited from `UIElement`. Omitted dimensions use natural selected-region size. |
| `SourceX`, `SourceY` | `float` | Static crop offset in source-image pixels. |
| `SourceWidth`, `SourceHeight` | `float` | Static crop size in source-image pixels; omitted dimensions use the remaining image extent. |
| `Tint` | `Color` | Gets or sets the multiplicative color tint. |
| `Origin` | `DrawPoint` | Gets or sets the rotation origin in source-image pixels. |
| `Flip` | `RenderSurface2DSpriteFlip` | Gets or sets sprite mirroring. |
| `Sampling` | `DrawSamplingMode` | Gets or sets image filtering for this sprite; default `Linear`. |
| `LayerDepth` | `float` | Gets or sets the layer depth forwarded to image drawing. |
| `Layer` | `int` | Gets or sets the parent-scene ordering layer. Inherited from `SceneNode2D`. |
| `PrismInputDomain` | `DrawRect?` | Optional complete local input of this sprite's non-pointwise composition. Inherited from `SceneNode2D`; required when active effects lack supported automatic input selection in a hosted spatial scene. |
| `Rotation` | `float` | Gets or sets rotation in radians. Inherited from `UIElement`. |
| `Opacity` | `float` | Gets or sets the alpha multiplier. Inherited from `UIElement`. |

## Property Information

| Property | Identifier field | Default value | Metadata/options |
| --- | --- | --- | --- |
| `Image` | `ImageProperty` | `null` | `AffectsRender` |
| `X`, `Y` | `XProperty`, `YProperty` | `0` | `AffectsRender` |
| `Width`, `Height` | Inherited `WidthProperty`, `HeightProperty` | `float.NaN` | `AffectsMeasure`, `AffectsArrange`; scene invalidation also invalidates the surface frame. |
| `SourceX`, `SourceY` | `SourceXProperty`, `SourceYProperty` | `0` | `AffectsRender` |
| `SourceWidth`, `SourceHeight` | `SourceWidthProperty`, `SourceHeightProperty` | `float.NaN` | `AffectsRender` |
| `Tint` | `TintProperty` | `Color.White` | `AffectsRender` |
| `Origin` | `OriginProperty` | `default(DrawPoint)` | `AffectsRender` |
| `Flip` | `FlipProperty` | `RenderSurface2DSpriteFlip.None` | `AffectsRender` |
| `Sampling` | `SamplingProperty` | `DrawSamplingMode.Linear` | `AffectsRender`; accepts `Point` or `Linear`. |
| `LayerDepth` | `LayerDepthProperty` | `0` | `AffectsRender` |

## Applies to

Project: `Cerneala`

## Constructors

| Name | Description |
| --- | --- |
| `Sprite2D()` | Creates a sprite with no collider and independent animation state. |

## Breaking-change migration

Move a collider formerly declared beside a sprite under a scene group into that sprite's `Collider`. If an old owner had multiple shapes, split the represented objects into separate `Sprite2D` owners; a second collider child is invalid markup and is not silently dropped. Keep geometry in destination units relative to the sprite anchor; subtract any position previously duplicated in collider offsets. Scene groups no longer accept colliders directly. Use a tile's immutable descriptor for static tile collision geometry, not an unrelated scene-level collider.

`Source`, `SourceResourceId`, `Destination`, and `SourceRect` have been removed without deprecated aliases. Replace a direct `Source` with `Image = new ImageReference(image)` and a resource ID with `Image = new ImageReference(id)` (markup: `Image="$Atlas"`). Split `Destination` into `X`, `Y`, `Width`, and `Height`; split a static `SourceRect` into `SourceX`, `SourceY`, `SourceWidth`, and `SourceHeight`. Rectangle-valued bindings become bindings to scalar properties exposed by the view model. Live `OneWay` paths cannot traverse `DrawRect` members: each CLR path owner must implement `INotifyPropertyChanged`. Notify changes for the exposed scalar properties. To restore the full-image static crop, set source offsets to zero and source dimensions to `float.NaN`, or clear their local UI values.

Custom path loaders used by cold scene images must implement `IAsyncImageLoader`, not only `IImageLoader`. Do not assume that attaching a sprite or reading its bounds synchronously loads the atlas. Keep the normal UI update loop running and observe the surface's presentation state; direct caller-prepared images remain available without asynchronous path preparation.

## See also

- `RenderSurface2D`
- `RenderSurface2DFrame`
- `Scene2D`
- `SceneItems2D`
- `SceneOrderMode`
