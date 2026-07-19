# Cerneala Prism Markup Syntax Proposal

## Status

This document is a design proposal for discussion. The syntax and runtime behavior
described here are not implemented.

The proposal deliberately describes the author-facing language before prescribing
renderer or backend implementation details. Prism markup should be easy to read even
for an author who does not know what a render pass, texture, shader, or DAG is.

## First Implementation Scope

The first implementation includes retained GPU result caching across frames for
stable Prism inputs. It may reuse a captured control result, a deterministic
intermediate node, or a final composition only when a complete dependency stamp
proves that every pixel-affecting input is unchanged.

The first implementation also supports only the built-in filter and style catalog.
It exposes no public registration, assembly discovery, kernel factory, or SDK for
application-provided or third-party operations. Public operation extensibility
requires a separate design decision backed by a real use case.

These scope limits change no markup grammar. Prism still has exactly the eight
directives defined below, and filter/style types still use bare semantic identifiers.

## Foundation Rendering Contract

The following rules are normative for the definition model, generated markup and
every drawing backend:

- Declaration order matches a Photoshop layer panel: the first normal node is in
  front and the last normal node is in back. Evaluation walks the normal stack
  bottom-up, from the last declared node toward the first, without reordering or
  duplicating nodes.
- The only implicit source for the normal stack is one immutable capture of the
  attached control's normal rendered subtree. The bottom node receives that image;
  every higher node receives only the accumulated result directly below it. A node
  name is an address for Motion and diagnostics, never an image source.
- `@layer` is a leaf. It may own filters, styles and at most one mask, but never a
  layer or group child. `@group` is the only normal-stack container and must contain
  at least one layer or nested group.
- A mask applies to the complete prepared contribution of its layer, group or
  backdrop after filters and styles, but before scope opacity and blending.
- `ClipToBelow=true` clips a normal layer to the effective alpha of the nearest
  unclipped normal sibling beneath it in the same scope. It never creates an
  arbitrary dependency and is invalid when no such base sibling exists.
- `PassThrough` is valid only as the group blending default. It lets child blend
  operations interact with the accumulated result outside the group. Any other
  group blend mode isolates the prepared group before compositing it.
- `Visible=false` bypasses the complete scope and its work. It is not equivalent to
  `Opacity=0`, which preserves evaluation and multiplies the complete prepared
  contribution before blending.
- `Fill` multiplies the prepared source content before styles are composed.
  `Opacity` multiplies content and styles together. On groups and backdrops, which
  do not expose layer fill semantics, `Fill` is invalid.
- `BlendIf` gates contribution by source and underlying luminance ranges after
  masks and fill are prepared and before the final opacity/blend operation. It does
  not change stack order or make the underlying image an addressable source.
- A composition uses `LinearSrgb` as its working color profile unless explicitly
  configured otherwise.
- Prism is presentation-only. It never changes measure, arrange, desired or
  arranged bounds, hit testing, pointer or keyboard routing, focus, automation or
  accessibility. Explicit visual clips still constrain the final pixels.

## Goal

Prism describes how the visual result of a UI element is built from an optional
backdrop plane and a control-processing layer stack.

The authoring model is inspired by a Photoshop layer panel:

- A Prism contains layers and groups.
- Layers are stacked visually.
- An optional backdrop plane processes the game world and UI physically behind the control.
- Prism captures the normal visual result of the attached control as one base image.
- The bottom layer receives that base image.
- Every higher layer receives and processes the accumulated result below it.
- A layer has filters, styles, an optional mask, opacity, fill, and a blend mode.
- A layer is always one concrete visual sheet; it cannot contain other layers or groups.
- A group contains layers or nested groups and can process their combined result.
- Named layers, groups, and backdrops are statically addressable Motion targets,
  never arbitrary image sources.

The markup language exposes only eight directives:

```text
@prism
@parameter
@layer
@group
@filter
@style
@mask
@backdrop
```

Reusable Prism markup is declared as a `PrismComposition` resource, not through a named
`@prism` directive. This mirrors the existing Motion separation:

| System | Reusable resource | Directive that uses it |
| --- | --- | --- |
| Motion | `MotionClip` | `@run` |
| Prism | `PrismComposition` | `@prism` |

`@prism` is therefore an application directive. It may attach a named
`PrismComposition` resource to an element or declare an inline composition, but it does not
declare a named reusable resource.

`PrismComposition` is intentionally not called `PrismClip`. `Clip` already means
visual clipping in the rendering model, while a Prism resource may also contain a
backdrop plane. `PrismStack` is also too narrow: the reusable object is the complete
composition, not only its control layer stack.

Pixel-processing operations such as blur, color adjustment, distortion, or
chromatic aberration are filter types:

```text
@filter Blur
@filter Color
@filter Distort
@filter ChromaticAberration
```

This keeps the language small. Adding a new filter does not add another top-level
language keyword. A filter type is always a bare identifier; a `$` resource
reference is never valid after `@filter`.

Photoshop-style decorations that generate pixels around or over a layer use
`@style`:

```text
@style DropShadow
@style OuterGlow
@style Stroke
@style ColorOverlay
```

`@filter` transforms the image flowing through a layer. `@style` decorates the
prepared layer without pretending to be another step in the pixel-filter chain.
`@mask` controls where the complete layer, group, or backdrop result is visible.
`@backdrop` processes the pixels physically behind the attached control.

`@style` is valid only inside Prism layer, group, and backdrop bodies. It means a
Photoshop-like layer style and does not introduce a second general-purpose UI
styling system beside Cerneala Aspect.

## Mental Model

Imagine several transparent sheets placed on a table.

Prism first takes one picture of the control exactly as it would normally render,
including its visual children. That picture sits beneath the entire Prism stack.

Prism starts at the bottom of the layer panel. The lowest layer receives the control
picture, processes it, and passes its result upward. The next layer receives that
result, processes it again, and passes the new result upward. Every layer therefore
adjusts the visual result accumulated beneath it.

A filter transforms the incoming picture. A style adds decorations such as a shadow,
glow, or stroke. A mask controls where that layer contributes. Prism continues
upward until the top layer produces the final picture.

This makes every Prism layer adjustment-like by design. Prism does not need a
separate adjustment-layer kind because arbitrary pixel layers are outside its scope:
the attached control is the one implicit picture being processed.

When present, `@backdrop` forms a separate plane beneath that control picture. It
does not enter the normal layer accumulation and does not change what the bottom
layer receives. Prism prepares the backdrop plane and the control layer stack
independently, then composites the processed control above the processed backdrop.

## First Example

The smallest useful Prism applies a shadow to the normal content of an element:

```xml
<Border>
    @prism
    {
        @layer
        {
            @style DropShadow
            {
                Size = 18;
                Distance = 8;
                Angle = 90;
                Color = #66000000;
            }
        }
    }
</Border>
```

In plain language, this means:

> Take the Border exactly as it would normally look, add a shadow to that picture,
> and display the result.

## Reusable PrismComposition Resources

A reusable Prism composition is declared as a resource, in the same way that
`MotionClip` stores a reusable Motion recipe:

```xml
<PrismComposition Name="ElevatedCard">
    @parameter ShadowSize: float = 18;
    @parameter ShadowColor: color = #66000000;

    @layer
    {
        @style DropShadow
        {
            Size = ShadowSize;
            Distance = 8;
            Angle = 90;
            Color = ShadowColor;
        }
    }
</PrismComposition>
```

Declaring the resource does not attach it to an element and does not render
anything by itself. It only stores a reusable, immutable composition recipe.

The `@prism` directive applies that resource to an element:

```xml
<Border>
    @prism $ElevatedCard;
</Border>
```

Parameters may be overridden for one application:

```xml
<Border>
    @prism $ElevatedCard(
        ShadowSize = 24,
        ShadowColor = #80000000
    );
</Border>
```

Every element receives its own Prism parameter values. Changing the parameters on
one element must not change another element using the same definition.

`PrismComposition` has three composition-level properties:

| Property | Default | Meaning |
| --- | --- | --- |
| `WorkingColorSpace` | `LinearSrgb` | Color profile used by filters, styles, and blending. |
| `GlobalLightAngle` | `120` | Shared Photoshop-style light angle in degrees. |
| `GlobalLightAltitude` | `30` | Shared Photoshop-style light altitude in degrees. |

They may be written as normal properties on a reusable resource or as assignments
at the start of an inline `@prism` body.

## Layer Order

Layers are written like they appear in a Photoshop layer panel:

- The first layer is visually in front.
- The last layer is visually in the back.

```xml
<PrismComposition Name="Example">
    @layer
    {
        Opacity = 0.85;
        BlendMode = Screen;

        @filter Color
        {
            Saturation = 1.2;
        }
    }

    @layer
    {
        @style OuterGlow
        {
            Size = 16;
            Color = #80FFFFFF;
        }
    }

    @layer
    {
        @style DropShadow
        {
            Size = 20;
            Distance = 8;
            Angle = 90;
            Color = #66000000;
        }
    }
</PrismComposition>
```

Prism evaluates the stack from the back toward the front:

1. The bottom layer adds a shadow to the captured control.
2. The middle layer receives that result and adds a glow.
3. The top layer receives the accumulated result and applies the color treatment.

## The Control Image

Prism has one implicit image source: the normal rendered output of the control that
owns the `@prism` directive.

```xml
<Button>
    @prism
    {
        @layer
        {
            @filter Blur
            {
                Radius = 12;
            }
        }
    }
</Button>
```

Prism renders the complete Button, including its content, into the base image. The
bottom layer receives that image. Each higher layer receives the accumulated result
produced by the layers below it.

The base image is captured once for one Prism evaluation and treated as immutable.
The renderer must not redraw or recapture the control for every layer. Intermediate
layer results are part of the ordered stack evaluation, not independently addressable
sources.

There is deliberately no `Source` assignment in layer markup:

```text
@layer
{
    Source = $OtherLayer;
}
```

This is invalid. Allowing it would turn the Photoshop-like stack into a hidden node
graph with arbitrary dependencies and cycles. A layer may consume only the result
immediately accumulated beneath it.

Filters may accept typed auxiliary inputs such as a displacement map. Masks may
accept a mask image. Those auxiliary inputs never replace the ordered image flowing
up through the Prism stack.

## Backdrop Plane

`@backdrop` processes everything physically rendered behind the attached control,
including the game world and lower UI composition. It is a dedicated Prism plane,
not a special layer and not a `Source` value:

```xml
<PrismComposition Name="GlassCard">
    @layer Content
    {
        @style DropShadow
        {
            Size = 18;
            Distance = 8;
            Angle = 90;
            Color = #66000000;
        }
    }

    @backdrop Glass
    {
        @parameter BlurRadius: float = 24;

        Visible = true;
        Opacity = 1;

        @filter Color
        {
            Saturation = 1.18;
            Contrast = 1.04;
        }

        @filter Blur
        {
            Radius = BlurRadius;
        }

        @style ColorOverlay
        {
            Color = #18FFFFFF;
        }
    }
</PrismComposition>
```

The declaration is written last because it is visually behind the entire control,
matching the top-to-bottom layer-panel order. Backdrop filters still execute from
the bottom of their list toward the top:

```text
pixels physically behind the control
-> Blur
-> Color
-> styles
-> mask
-> backdrop opacity
-> processed backdrop plane

captured control image
-> normal Prism layer and group stack
-> processed control plane

processed backdrop plane
-> processed control plane
-> final visual result
```

The backdrop never becomes the input of the bottom `@layer`. The control layer stack
continues to process only the captured control image. This separation prevents a
background blur from accidentally blurring the control content placed above it.

A Prism may declare at most one `@backdrop`. It must be the last direct child of an
inline `@prism` body or `PrismComposition`; it cannot be nested inside `@layer`, `@group`,
or another `@backdrop`. A Prism may contain only a backdrop and no explicit layers;
in that case the control renders normally above the processed backdrop.

A backdrop may declare:

| Property | Default | Meaning |
| --- | --- | --- |
| `Visible` | `true` | Whether the backdrop plane is acquired and processed. |
| `Opacity` | `1` | Opacity of the complete processed backdrop plane. |

`Fill`, `BlendMode`, and `ClipToBelow` are invalid on a backdrop. It may contain
typed parameters, filters, styles, and at most one mask. It must contain at least
one filter or style.

The backdrop covers the attached control's arranged rectangle and follows its
effective visual clip. A backdrop mask may reduce that coverage further. Backdrop
processing never changes layout or hit testing.

A named backdrop is addressable through the same Motion target grammar as a named
layer or group:

```text
$self.prism.Glass.BlurRadius
$owner.prism.Glass.Opacity
$PauseMenu.prism.Glass.BlurRadius
```

### Backdrop Runtime Contract

Backdrop acquisition uses one optional host capability, `IBackdropFrameSource`.
The source is configured on the host, not on a view:

```csharp
public interface IBackdropFrameSource
{
    bool TryAcquire(
        in BackdropFrameRequest request,
        out BackdropFrameLease frame);
}

public readonly record struct BackdropFrameRequest(
    long UiFrameId,
    PixelSize ViewportSize,
    PrismColorProfile OutputColorProfile);

public abstract class BackdropFrameLease : IDisposable
{
    public abstract IBackdropSurface Surface { get; }
    public abstract long ContentVersion { get; }
    public abstract PixelSize PixelSize { get; }
    public abstract Matrix3x2 ScreenToSurface { get; }
    public abstract PrismColorProfile ColorProfile { get; }
}
```

`IBackdropSurface` is an opaque, GPU-readable backend surface. It is deliberately
not a MonoGame `Texture2D` in the framework contract. The MonoGame adapter unwraps
its own surface implementation without leaking MonoGame types into Prism.

The capability is wired explicitly:

```csharp
public interface IUiBackend
{
    IBackdropFrameSource? BackdropFrameSource { get; }
}

public readonly record struct DrawingFrameContext(
    long UiFrameId,
    PixelSize ViewportSize,
    PrismColorProfile OutputColorProfile,
    BackdropFrameLease? Backdrop);

public interface IDrawingBackend
{
    void Render(
        DrawCommandList commands,
        in DrawingFrameContext frame);
}

public sealed class MonoGameUiHostOptions
{
    public IBackdropFrameSource? BackdropFrameSource { get; init; }
}
```

These members join the interfaces' existing properties. The
`IDrawingBackend.Render` signature replaces the context-free submission contract so
frame inputs are explicit rather than hidden in mutable backend state. The host
calls `TryAcquire` at most once for one `UiHost.Draw`, even when the tree contains
many backdrops, then passes the lease through `DrawingFrameContext`. A successful
lease remains immutable and valid until the draw submission completes; the host
then disposes the lease. Prism never retains or disposes the underlying game render
target.

The provider returns the source in its native `ColorProfile`. It is not asked to
convert for one Prism because a single frame may contain compositions with different
working profiles. The compositor performs and frame-locally shares the appropriate
conversion for each working profile.

For a MonoGame game, the supplied surface is normally the resolved scene render
target produced immediately before UI composition. The renderer imports it into a
frame-local composition graph as an external read-only resource. Lower UI commands
are added to that graph in paint order. A backdrop reads the graph node that exists
immediately before its owning control, so it sees the game plus lower UI, but never
itself or later UI.

Backdrop acquisition is a hosting and renderer responsibility. A view must never
take screenshots, read pixels back to the CPU, copy the back buffer, or create its
own render targets as a workaround.

### Shared And Retained Backdrop Cache

Backdrop work sharing belongs to the drawing backend's compositor, not to
`PrismComposition`, the element, or the host source.

The compositor builds a frame-local render graph and uses a fence-aware transient
surface pool with an explicit byte budget. It samples only the screen-space region
needed by a backdrop, expanded by the exact support radius of its filters and mask.
The region is clamped only after expansion, preventing blur and distortion from
developing cropped edges.

Within one frame, compatible work is shared by a frame-local cache keyed by:

```text
lower-composition node identity
expanded pixel region
pixel scale and downsample level
working color profile
filter-prefix descriptor and parameter values
mask descriptor when it affects the sampled pixels
```

This allows several glass controls to reuse a downsample pyramid or an identical
blur prefix without incorrectly sharing their final tint, mask, or opacity.
Filtering remains deterministic because the key contains the complete immutable
input identity and every pixel-affecting value.

Compatible deterministic results may also be promoted into a retained GPU cache
after the draw. A cross-frame key additionally contains:

```text
composition structural version and stable node identity
Prism parameter-value version or equivalent pixel fingerprint
control/backdrop source identity, ContentVersion, and every lower-UI render version
all referenced image, mask, LUT, pattern, and auxiliary-resource identities/versions
viewport, pixel scale, transforms that affect rasterization, and output format
backend capability set and shader package version
```

A retained hit is legal only when the complete key matches. A final-result hit skips
control capture and all covered Prism passes; an intermediate hit skips only the
covered graph prefix. Animated game frames naturally miss backdrop entries because
their `ContentVersion` changes, while static menus can reuse expensive blur and
composition results.

Retained entries never own or strongly reference UI elements. They own backend GPU
surfaces under a separate byte budget, are pinned only while used by the current
draw, and are evicted by least-recent use after their lease is released. Detach,
composition replacement, referenced-resource replacement, device loss, viewport or
output-profile changes, and shader-package changes invalidate affected entries.
Hidden or collapsed controls perform no lookup or promotion; their entries become
immediately evictable.

Version numbers are meaningful only together with their source identity. Control
captures therefore include an opaque, non-reused attachment token; backdrop and
auxiliary resources include provider/resource identities. Two unrelated controls at
the same numeric version can never collide or borrow each other's pixels.

A backdrop can see only pixels that were composed before its control. It never sees
itself, its control, or UI above it, so feedback cycles are impossible.

When the active host cannot provide backdrop pixels, Prism omits only the backdrop
plane, renders the control normally, and reports a runtime
`BackdropUnavailable` diagnostic. It must not reuse pixels from an older frame or
throw from the middle of rendering.

## Layer Names

A layer name is an optional stable address for Motion, diagnostics, and design
tooling. It does not change rendering and cannot be used as an image source.

```text
@layer SoftGlow
{
    @parameter GlowRadius: float = 16;

    @style OuterGlow
    {
        Size = GlowRadius;
    }
}
```

The layer-scoped parameter can then be targeted through the existing Motion target
prefixes:

```text
$self.prism.SoftGlow.GlowRadius
$owner.prism.SoftGlow.GlowRadius
$Card.prism.SoftGlow.GlowRadius
```

`$self`, `$owner`, and `$Card` select the control. `.prism` enters the Prism instance,
`SoftGlow` selects the named layer, and `GlowRadius` selects the layer-scoped
parameter.

Names are required only when a layer must be addressed independently. Unnamed layers
remain the concise default. Names must be unique among siblings because they become
compile-time symbols.

Named groups create hierarchical scopes. A layer inside a group is addressed through
the complete path:

```text
$self.prism.Highlights.SoftGlow.GlowRadius
```

Layer and group properties that Prism declares animatable, such as `Opacity`, use the
same path:

```text
$self.prism.SoftGlow.Opacity
```

Raw filter and style properties are not automatically flattened into magic names.
An author exposes a value to Motion by declaring a typed parameter in the containing
layer, group, or backdrop and using that parameter in the filter or style. This
avoids collisions when a processing body contains multiple operations with a
property named `Radius`.

## Layer Properties

A leaf layer may declare:

```text
@layer
{
    Visible = true;
    Opacity = 1;
    Fill = 1;
    BlendMode = Normal;
    ClipToBelow = false;

    @filter Blur
    {
        Radius = 8;
    }
}
```

Defaults:

| Property | Default | Meaning |
| --- | --- | --- |
| `Visible` | `true` | Whether the layer participates in Prism evaluation. |
| `Opacity` | `1` | Opacity of the complete layer result, including styles. |
| `Fill` | `1` | Opacity of the filtered content without reducing styles. |
| `BlendMode` | `Normal` | How the layer result is mixed with the accumulated result below it. |
| `ClipToBelow` | `false` | Joins the layer to the clipping chain anchored beneath it. |
| `BlendChannels` | `RGBA` | Color and alpha channels written by this layer. |
| `Knockout` | `None` | `None`, `Shallow`, or `Deep` knockout behavior. |
| `BlendInteriorStylesAsGroup` | `false` | Blends inner styles with content before the layer blend. |
| `BlendClippedLayersAsGroup` | `true` | Applies the base blend mode to the complete clipping chain. |
| `TransparencyShapesLayer` | `true` | Uses layer transparency to shape layer effects and knockout. |
| `LayerMaskHidesStyles` | `true` | Lets the layer mask hide generated styles. |
| `VectorMaskHidesStyles` | `false` | Lets a vector mask hide generated styles. |
| `BlendIfChannel` | `Gray` | Composite or component channel used by Blend If. |
| `ThisLayerRange` | `(0,0,1,1)` | Feathered black-start, black-end, white-start, white-end thresholds. |
| `UnderlyingRange` | `(0,0,1,1)` | Equivalent feathered thresholds for accumulated pixels below. |
| `DissolveSeed` | `0` | Stable seed used only by the `Dissolve` blend mode. |

Every layer must contain at least one filter or style. An empty layer would return
its input unchanged and has no purpose.

`Visible = false` bypasses the layer completely. The renderer must not allocate
temporary surfaces or run filters and styles for an invisible layer.

`Fill` affects only the filtered layer content. Styles such as shadow, glow, and
stroke are derived from the prepared content but remain visible when `Fill = 0`.
`Opacity` is applied later to the complete content-plus-styles result.

`ClipToBelow = true` joins a Photoshop-style clipping chain. Consecutive clipped
layers all use the alpha of the nearest lower sibling whose `ClipToBelow` is false;
that sibling is the clipping base. A clipped layer is invalid when no base sibling
exists beneath it in the same scope.

## Blend Modes

Prism supports the complete Photoshop layer blend-mode set:

| Family | Modes |
| --- | --- |
| Normal | `Normal`, `Dissolve` |
| Darken | `Darken`, `Multiply`, `ColorBurn`, `LinearBurn`, `DarkerColor` |
| Lighten | `Lighten`, `Screen`, `ColorDodge`, `LinearDodge`, `LighterColor` |
| Contrast | `Overlay`, `SoftLight`, `HardLight`, `VividLight`, `LinearLight`, `PinLight`, `HardMix` |
| Comparative | `Difference`, `Exclusion`, `Subtract`, `Divide` |
| Component | `Hue`, `Saturation`, `Color`, `Luminosity` |
| Group-only | `PassThrough` |

`LinearDodge` is the canonical markup name for Photoshop's `Linear Dodge (Add)`.
`Add` is accepted only as a parser alias and generated code always normalizes it to
`LinearDodge`. `PassThrough` is valid only on groups. Layers and individual filters
or styles default to `Normal`; groups default to `PassThrough`.

Photoshop's `Behind` and `Clear` modes belong to painting tools rather than the
layer panel, so they are not Prism layer blend modes. `Dissolve` uses a stable hash
of pixel position, layer identity, and `DissolveSeed`; it never flickers between
frames unless the seed or layer geometry changes.

## Color Processing

Prism uses `LinearSrgb` as its canonical default working color space. Linear-light
processing is the physically correct default for blur, resampling, lighting, glow,
opacity, and compositing, and avoids the dark fringes produced by gamma-encoded
blending.

The author may select another built-in working space per reusable or inline
composition:

```xml
<PrismComposition
    Name="LegacyPhotoshopLook"
    WorkingColorSpace="Srgb">
    ...
</PrismComposition>
```

```text
@prism
{
    WorkingColorSpace = DisplayP3;

    @layer
    {
        @filter Vibrance
        {
            Amount = 0.2;
        }
    }
}
```

Built-in values are:

| Value | Transfer function | Gamut | Intended use |
| --- | --- | --- | --- |
| `LinearSrgb` | linear | sRGB | Default SDR UI processing. |
| `Srgb` | sRGB | sRGB | Gamma-encoded compatibility and Photoshop-like legacy results. |
| `LinearDisplayP3` | linear | Display P3 | Wide-gamut physically correct processing. |
| `DisplayP3` | Display P3 | Display P3 | Wide-gamut encoded compatibility. |
| `ScRgb` | linear floating point | extended sRGB | HDR and values outside the SDR range. |

A typed custom profile resource is also valid:

```text
WorkingColorSpace = $StudioProfile;
```

The source control, backdrop, masks, gradients, patterns, and auxiliary images are
converted from their declared profiles into the selected working profile before
processing. Prism converts the final result into the host output profile exactly
once. Missing or incompatible profiles produce a diagnostic rather than silently
reinterpreting channel values.

Intermediate pixels use premultiplied alpha. Operations that are defined on
unassociated color, including HSL blend modes and color adjustments, temporarily
unpremultiply with a zero-alpha guard and premultiply again. Alpha representation is
an implementation invariant and is not author-selectable.

## Filters

A filter changes the current picture of one layer:

```text
@layer
{
    @filter Color
    {
        Saturation = 0.75;
        Contrast = 1.1;
    }

    @filter Blur
    {
        Radius = 8;
    }
}
```

Filters are displayed like Photoshop Smart Filters and execute from the bottom of
the list toward the top. In the example, `Blur` runs first and `Color` runs second:

```text
accumulated image from below
-> Blur
-> Color
-> prepared content

prepared content -> Fill -> content contribution
prepared content -> styles -> style contribution

content contribution + style contribution
-> mask and ClipToBelow
-> layer opacity
-> layer blend
```

Changing the order may change the result.

Each filter type owns a typed property schema. The generator must reject properties
that do not belong to that filter:

```text
@filter Blur
{
    Saturation = 1.2;
}
```

The example is invalid because `Saturation` belongs to `Color`, not `Blur`.

### Filter Schema Rules

Every built-in filter has these common properties:

| Property | Default | Meaning |
| --- | --- | --- |
| `Visible` | `true` | Bypasses the filter without removing its definition. |
| `Opacity` | `1` | Mixes the filtered result over that filter's input. |
| `BlendMode` | `Normal` | Blend mode used for the per-filter mix. |

Lengths are device-independent pixels unless a property explicitly says `Pixels`.
Angles are degrees clockwise. Percentages are normalized floats from `0` to `1`.
Normalized positions use `0,0` at the top-left and `1,1` at the bottom-right.
Random filters always expose `Seed`; the default `0` is deterministic and never
means "pick a random value every frame."

The machine-readable
[`prism-catalog.json`](../Cerneala.SourceGen/Prism/Catalog/prism-catalog.json)
is the single normative built-in catalog. The family tables below record the
approved author-facing surface for this proposal, but generators, runtime,
backends, tests and generated documentation must consume the JSON catalog rather
than copying these rows. Values shown after `=` mirror the catalog for design
review; when they differ, the validated JSON wins. `required` means that the
generator rejects the filter when the author does not provide that value.
Photoshop lets users redefine some application defaults; Prism deliberately does
not inherit mutable machine preferences.

### Color And Adjustment Filters

These cover Photoshop's adjustment-layer surface while retaining the same Prism
`@filter` syntax:

| Filter | Type-specific properties and defaults |
| --- | --- |
| `BrightnessContrast` | `Brightness=0`; `Contrast=0`; `UseLegacy=false` |
| `Levels` | `Channel=Composite`; `InputBlack=0`; `InputWhite=1`; `Gamma=1`; `OutputBlack=0`; `OutputWhite=1` |
| `Curves` | `Channel=Composite`; `Curve=Linear`; `Interpolation=Smooth` |
| `Exposure` | `Exposure=0`; `Offset=0`; `Gamma=1` |
| `Vibrance` | `Amount=0`; `Saturation=0` |
| `HueSaturation` | `Channel=Master`; `Hue=0`; `Saturation=0`; `Lightness=0`; `Colorize=false` |
| `ColorBalance` | `Shadows=(0,0,0)`; `Midtones=(0,0,0)`; `Highlights=(0,0,0)`; `PreserveLuminosity=true` |
| `BlackWhite` | `Reds=0.40`; `Yellows=0.60`; `Greens=0.40`; `Cyans=0.60`; `Blues=0.20`; `Magentas=0.80`; `Tint=false`; `TintColor=#FFFFFFFF` |
| `PhotoFilter` | `Color=#FFFF9A30`; `Density=0.25`; `PreserveLuminosity=true` |
| `ChannelMixer` | `Red=(1,0,0)`; `Green=(0,1,0)`; `Blue=(0,0,1)`; `Constant=(0,0,0)`; `Monochrome=false` |
| `ColorLookup` | `Lookup=required`; `Intensity=1`; `Interpolation=Tetrahedral` |
| `Invert` | no type-specific properties |
| `Posterize` | `Levels=4` |
| `Threshold` | `Level=0.5` |
| `GradientMap` | `Gradient=BlackToWhite`; `Reverse=false`; `Dither=false`; `Method=Perceptual` |
| `SelectiveColor` | `Reds=(0,0,0,0)`; `Yellows=(0,0,0,0)`; `Greens=(0,0,0,0)`; `Cyans=(0,0,0,0)`; `Blues=(0,0,0,0)`; `Magentas=(0,0,0,0)`; `Whites=(0,0,0,0)`; `Neutrals=(0,0,0,0)`; `Blacks=(0,0,0,0)`; `Method=Relative` |

### Blur And Sharpen Filters

| Filter | Type-specific properties and defaults |
| --- | --- |
| `Average` | no type-specific properties |
| `Blur` | `Radius=1`; `Quality=Good`; `EdgeMode=Clamp` |
| `BlurMore` | `Radius=4`; `Quality=Good`; `EdgeMode=Clamp` |
| `BoxBlur` | `Radius=2`; `Iterations=1`; `EdgeMode=Clamp` |
| `GaussianBlur` | `Radius=2`; `Quality=Good`; `EdgeMode=Clamp` |
| `LensBlur` | `Radius=15`; `BladeCount=6`; `BladeCurvature=0`; `Rotation=0`; `SpecularBrightness=0`; `SpecularThreshold=1`; `DepthMap=null`; `DepthChannel=Luminance`; `FocalDistance=0.5`; `InvertDepth=false`; `Noise=0`; `NoiseDistribution=Uniform`; `MonochromaticNoise=false` |
| `MotionBlur` | `Distance=10`; `Angle=0`; `Quality=Good`; `EdgeMode=Transparent` |
| `RadialBlur` | `Mode=Spin`; `Amount=0.1`; `Center=(0.5,0.5)`; `Quality=Good` |
| `ShapeBlur` | `Kernel=required`; `Radius=5`; `EdgeMode=Clamp` |
| `SmartBlur` | `Radius=5`; `Threshold=0.1`; `Quality=Good`; `Mode=Normal` |
| `SurfaceBlur` | `Radius=5`; `Threshold=0.1`; `Quality=Good` |
| `FieldBlur` | `Pins=required`; `BokehAmount=0`; `BokehColor=0`; `LightRange=(0,1)`; `Noise=0` |
| `IrisBlur` | `Center=(0.5,0.5)`; `Radius=(0.25,0.25)`; `Feather=0.5`; `Rotation=0`; `Blur=15`; `BokehAmount=0`; `BokehColor=0`; `LightRange=(0,1)`; `Noise=0` |
| `TiltShift` | `Center=(0.5,0.5)`; `Angle=0`; `FocusWidth=0.25`; `Feather=0.25`; `Blur=15`; `Distortion=0`; `SymmetricDistortion=true`; `Noise=0` |
| `PathBlur` | `Path=required`; `Speed=20`; `Taper=0`; `CenteredBlur=true`; `EndSpeed=20`; `Shape=Basic`; `FlashSync=Rear`; `Noise=0` |
| `SpinBlur` | `Center=(0.5,0.5)`; `Radius=(0.25,0.25)`; `Rotation=15`; `Feather=0.5`; `StrobeStrength=0`; `StrobeFlashes=0`; `StrobeDuration=0`; `Noise=0` |
| `Sharpen` | `Amount=0.25` |
| `SharpenMore` | `Amount=0.5` |
| `SharpenEdges` | `Amount=0.5`; `Threshold=0.1` |
| `UnsharpMask` | `Amount=0.5`; `Radius=1`; `Threshold=0` |
| `SmartSharpen` | `Amount=1`; `Radius=1`; `ReduceNoise=0.1`; `Remove=GaussianBlur`; `Angle=0`; `ShadowFade=0`; `ShadowTonalWidth=0.5`; `ShadowRadius=1`; `HighlightFade=0`; `HighlightTonalWidth=0.5`; `HighlightRadius=1` |
| `HighPass` | `Radius=10`; `EdgeMode=Clamp` |

### Distort, Geometry, And Morphology Filters

| Filter | Type-specific properties and defaults |
| --- | --- |
| `Transform` | `Translate=(0,0)`; `Scale=(1,1)`; `Rotation=0`; `Skew=(0,0)`; `Origin=(0.5,0.5)`; `Sampling=Linear`; `EdgeMode=Transparent` |
| `AdaptiveWideAngle` | `Projection=Auto`; `FocalLength=Auto`; `CropFactor=1`; `Constraints=required`; `Scale=1`; `Rotation=0`; `Translate=(0,0)` |
| `LensCorrection` | `Distortion=0`; `ChromaticRedCyan=0`; `ChromaticBlueYellow=0`; `VignetteAmount=0`; `VignetteMidpoint=0.5`; `PerspectiveVertical=0`; `PerspectiveHorizontal=0`; `Angle=0`; `Scale=1`; `EdgeMode=Transparent` |
| `DiffuseGlow` | `Grain=0`; `GlowAmount=0.1`; `ClearAmount=0.15`; `Color=#FFFFFFFF` |
| `Displace` | `Map=required`; `HorizontalScale=10`; `VerticalScale=10`; `MapFit=Stretch`; `UndefinedAreas=RepeatEdgePixels`; `ChannelX=Red`; `ChannelY=Green` |
| `Glass` | `Distortion=0.2`; `Smoothness=0.5`; `Texture=Frosted`; `TextureImage=null`; `Scaling=1`; `Invert=false` |
| `OceanRipple` | `RippleSize=0.5`; `RippleMagnitude=0.5`; `Seed=0` |
| `Pinch` | `Amount=0.5`; `Center=(0.5,0.5)` |
| `PolarCoordinates` | `Mode=RectangularToPolar`; `Center=(0.5,0.5)` |
| `Ripple` | `Amount=1`; `Size=Medium`; `Seed=0`; `EdgeMode=Repeat` |
| `Shear` | `Curve=Linear`; `UndefinedAreas=RepeatEdgePixels` |
| `Spherize` | `Amount=1`; `Mode=Normal`; `Center=(0.5,0.5)` |
| `Twirl` | `Angle=50`; `Center=(0.5,0.5)` |
| `Wave` | `Generators=5`; `Wavelength=(10,120)`; `Amplitude=(5,35)`; `Scale=(1,1)`; `Type=Sine`; `UndefinedAreas=RepeatEdgePixels`; `Seed=0` |
| `ZigZag` | `Amount=0.5`; `Ridges=5`; `Style=PondRipples`; `Center=(0.5,0.5)` |
| `Liquify` | `Mesh=required`; `Reconstruct=0`; `Mask=null`; `MaskInvert=false`; `EdgeMode=Clamp` |
| `Maximum` | `Radius=1`; `Preserve=Roundness` |
| `Minimum` | `Radius=1`; `Preserve=Roundness` |
| `Offset` | `Offset=(0,0)`; `UndefinedAreas=WrapAround`; `FillColor=#00000000` |

### Noise, Pixelate, Render, And Video Filters

| Filter | Type-specific properties and defaults |
| --- | --- |
| `AddNoise` | `Amount=0.1`; `Distribution=Uniform`; `Monochromatic=false`; `Seed=0` |
| `Despeckle` | `Threshold=0.1`; `Radius=1` |
| `DustScratches` | `Radius=1`; `Threshold=0` |
| `Median` | `Radius=1` |
| `ReduceNoise` | `Strength=0.5`; `PreserveDetails=0.6`; `ReduceColorNoise=0.45`; `SharpenDetails=0.25`; `RemoveJpegArtifact=false` |
| `ColorHalftone` | `MaxRadius=4`; `Angles=(108,162,90,45)` |
| `Crystallize` | `CellSize=10`; `Seed=0` |
| `Facet` | `Iterations=1` |
| `Fragment` | `Offset=1` |
| `Mezzotint` | `Type=MediumDots`; `Seed=0` |
| `Mosaic` | `CellSize=(10,10)`; `PreserveEdges=false` |
| `Pointillize` | `CellSize=10`; `Background=#00000000`; `Seed=0` |
| `Clouds` | `Foreground=#FF000000`; `Background=#FFFFFFFF`; `Scale=1`; `Seed=0` |
| `DifferenceClouds` | `Foreground=#FF000000`; `Background=#FFFFFFFF`; `Scale=1`; `Seed=0` |
| `Fibers` | `Foreground=#FF000000`; `Background=#FFFFFFFF`; `Variance=16`; `Strength=4`; `Seed=0` |
| `LensFlare` | `Center=(0.5,0.5)`; `Brightness=1`; `Lens=50To300Zoom` |
| `LightingEffects` | `Lights=required`; `Ambient=0`; `Metallic=0`; `Gloss=0.5`; `Exposure=0`; `Texture=null`; `TextureHeight=0` |
| `Deinterlace` | `Field=Odd`; `Replacement=Interpolation` |
| `NtscColors` | `Standard=NTSC`; `Method=ReduceLuminance` |

### Artistic, Brush, Sketch, Stylize, And Texture Filters

The complete Photoshop Filter Gallery catalog is built in. These effects have
deterministic defaults and never read implicit application foreground/background
swatches:

| Filter | Type-specific properties and defaults |
| --- | --- |
| `ColoredPencil` | `PencilWidth=3`; `StrokePressure=8`; `PaperBrightness=0.25`; `PaperColor=#FFFFFFFF` |
| `Cutout` | `Levels=8`; `EdgeSimplicity=4`; `EdgeFidelity=3` |
| `DryBrush` | `BrushSize=2`; `BrushDetail=8`; `Texture=1` |
| `FilmGrain` | `Grain=4`; `HighlightArea=0`; `Intensity=10`; `Seed=0` |
| `Fresco` | `BrushSize=2`; `BrushDetail=8`; `Texture=1` |
| `NeonGlow` | `GlowSize=5`; `GlowBrightness=15`; `GlowColor=#FF00FFFF` |
| `PaintDaubs` | `BrushSize=1`; `Sharpness=5`; `BrushType=Simple` |
| `PaletteKnife` | `StrokeSize=3`; `StrokeDetail=1`; `Softness=0` |
| `PlasticWrap` | `HighlightStrength=15`; `Detail=9`; `Smoothness=7` |
| `PosterEdges` | `EdgeThickness=2`; `EdgeIntensity=1`; `Posterization=2` |
| `RoughPastels` | `StrokeLength=6`; `StrokeDetail=4`; `Texture=Canvas`; `Scaling=1`; `Relief=0.2`; `LightDirection=Top`; `Invert=false` |
| `SmudgeStick` | `StrokeLength=2`; `HighlightArea=0`; `Intensity=10` |
| `Sponge` | `BrushSize=2`; `Definition=12`; `Smoothness=5` |
| `Underpainting` | `BrushSize=6`; `TextureCoverage=0.2`; `Texture=Canvas`; `Scaling=1`; `Relief=0.04`; `LightDirection=Top`; `Invert=false` |
| `Watercolor` | `BrushDetail=9`; `ShadowIntensity=1`; `Texture=3` |
| `AccentedEdges` | `EdgeWidth=2`; `EdgeBrightness=38`; `Smoothness=5` |
| `AngledStrokes` | `DirectionBalance=0.5`; `StrokeLength=15`; `Sharpness=3` |
| `Crosshatch` | `StrokeLength=9`; `Sharpness=6`; `Strength=1` |
| `DarkStrokes` | `Balance=5`; `BlackIntensity=6`; `WhiteIntensity=2` |
| `InkOutlines` | `StrokeLength=4`; `DarkIntensity=20`; `LightIntensity=10` |
| `Spatter` | `SprayRadius=10`; `Smoothness=5`; `Seed=0` |
| `SprayedStrokes` | `StrokeLength=12`; `SprayRadius=7`; `Direction=RightDiagonal`; `Seed=0` |
| `SumiE` | `StrokeWidth=10`; `StrokePressure=2`; `Contrast=2` |
| `BasRelief` | `Detail=13`; `Smoothness=3`; `LightDirection=BottomLeft`; `Foreground=#FF000000`; `Background=#FFFFFFFF` |
| `ChalkCharcoal` | `CharcoalArea=6`; `ChalkArea=6`; `StrokePressure=1`; `Foreground=#FF000000`; `Background=#FFFFFFFF` |
| `Charcoal` | `CharcoalThickness=1`; `Detail=5`; `LightDarkBalance=50`; `Foreground=#FF000000`; `Background=#FFFFFFFF` |
| `Chrome` | `Detail=4`; `Smoothness=7` |
| `ConteCrayon` | `ForegroundLevel=11`; `BackgroundLevel=7`; `Texture=Canvas`; `Scaling=1`; `Relief=0.2`; `LightDirection=Top`; `Foreground=#FF000000`; `Background=#FFFFFFFF` |
| `GraphicPen` | `StrokeLength=15`; `LightDarkBalance=50`; `StrokeDirection=RightDiagonal`; `Foreground=#FF000000`; `Background=#FFFFFFFF` |
| `HalftonePattern` | `Size=1`; `Contrast=5`; `PatternType=Dot`; `Foreground=#FF000000`; `Background=#FFFFFFFF` |
| `NotePaper` | `ImageBalance=25`; `Graininess=10`; `Relief=11`; `Foreground=#FF000000`; `Background=#FFFFFFFF` |
| `Photocopy` | `Detail=2`; `Darkness=8`; `Foreground=#FF000000`; `Background=#FFFFFFFF` |
| `Plaster` | `ImageBalance=20`; `Smoothness=2`; `LightDirection=TopLeft`; `Foreground=#FF000000`; `Background=#FFFFFFFF` |
| `Reticulation` | `Density=12`; `ForegroundLevel=40`; `BackgroundLevel=5`; `Seed=0` |
| `Stamp` | `LightDarkBalance=25`; `Smoothness=5`; `Foreground=#FF000000`; `Background=#FFFFFFFF` |
| `TornEdges` | `ImageBalance=25`; `Smoothness=11`; `Contrast=17`; `Foreground=#FF000000`; `Background=#FFFFFFFF` |
| `WaterPaper` | `FiberLength=15`; `Brightness=60`; `Contrast=80`; `Seed=0` |
| `Diffuse` | `Mode=Normal`; `Iterations=1`; `Seed=0` |
| `Emboss` | `Angle=135`; `Height=3`; `Amount=1` |
| `Extrude` | `Type=Blocks`; `Size=30`; `Depth=30`; `DepthMode=Random`; `SolidFrontFaces=true`; `MaskIncompleteBlocks=false`; `Seed=0` |
| `FindEdges` | `Threshold=0.1` |
| `GlowingEdges` | `EdgeWidth=2`; `EdgeBrightness=6`; `Smoothness=5` |
| `Solarize` | `Threshold=0.5` |
| `Tiles` | `Tiles=10`; `MaximumOffset=0.1`; `Fill=Background`; `Background=#00000000`; `Seed=0` |
| `TraceContour` | `Level=0.5`; `Edge=Lower` |
| `Wind` | `Method=Wind`; `Direction=FromRight`; `Strength=1`; `Seed=0` |
| `Craquelure` | `CrackSpacing=10`; `CrackDepth=6`; `CrackBrightness=9`; `Seed=0` |
| `Grain` | `Intensity=40`; `Contrast=50`; `Type=Regular`; `Seed=0` |
| `MosaicTiles` | `TileSize=12`; `GroutWidth=2`; `LightenGrout=9` |
| `Patchwork` | `SquareSize=4`; `Relief=8`; `Seed=0` |
| `StainedGlass` | `CellSize=2`; `BorderThickness=4`; `LightIntensity=3`; `BorderColor=#FF000000`; `Seed=0` |
| `Texturizer` | `Texture=Canvas`; `TextureImage=null`; `Scaling=1`; `Relief=0.04`; `LightDirection=Top`; `Invert=false` |
| `OilPaint` | `Stylization=0.5`; `Cleanliness=0.5`; `Scale=1`; `BristleDetail=0.5`; `Lighting=true`; `Angle=0`; `Shine=0.5` |

### General And Cerneala-Native Filters

| Filter | Type-specific properties and defaults |
| --- | --- |
| `CustomConvolution` | `Kernel=required`; `Scale=1`; `Offset=0`; `EdgeMode=Clamp`; `AffectAlpha=false` |
| `ColorMatrix` | `Matrix=Identity`; `Clamp=true` |
| `Color` | `Brightness=0`; `Contrast=1`; `Exposure=0`; `Saturation=1`; `Hue=0`; `Temperature=0`; `Tint=#00000000`; `Matrix=Identity`; `Clamp=true` |
| `ChromaticAberration` | `Amount=0`; `Direction=(1,0)`; `Radial=false`; `Center=(0.5,0.5)`; `Sampling=Linear` |
| `Scanlines` | `Frequency=320`; `Thickness=0.5`; `Phase=0`; `Color=#FF000000`; `LineOpacity=0.18`; `Softness=0` |

Photoshop editor workflows that require painting, document mutation, cloud inference,
or copyright metadata are not fake real-time filters. `NeuralFilters`, Digimarc,
Vanishing Point editing, and Camera Raw's file-development workflow are therefore
not built-in Prism filter types. Their resulting images, masks, LUTs, or meshes may
be consumed only through built-in typed properties that explicitly accept those
resource kinds. Application-provided semantic filter types are outside the first
implementation. This boundary preserves the complete deterministic Photoshop
image-processing and layer-effects model without stuffing an image editor and cloud
service into the UI renderer.

All filter types use the same bare identifier syntax:

```text
@filter ChromaticAberration
{
    Amount = 0.012;
}
```

`$` continues to mean a resource, value, or target reference elsewhere in Cerneala.
It is not part of filter type syntax. A filter position expects a bare filter type
identifier, not a resource. Whether a filter type is implemented by one shader,
several render passes, or another backend mechanism is not exposed in Prism layer
markup.

## Layer Styles

A style decorates the filtered content without becoming another filter in the pixel
transformation chain:

```text
@layer
{
    @style Stroke
    {
        Size = 2;
        Position = Inside;
        Color = #FFFFFFFF;
    }

    @style DropShadow
    {
        Size = 18;
        Distance = 8;
        Angle = 90;
        Color = #66000000;
    }
}
```

Styles are displayed and composed from the bottom of the list toward the top, using
the same panel-order rule as filters. Each style type owns a typed property schema.
A style type is a bare identifier; resource-reference syntax is not valid in style
position.

Prism includes the complete Photoshop layer-style family. Every style has
`Visible=true`; all remaining properties and defaults in the table are a
human-readable design view of the normative machine-readable catalog:

| Style | Properties and defaults |
| --- | --- |
| `DropShadow` | `BlendMode=Multiply`; `Color=#FF000000`; `Opacity=0.75`; `UseGlobalLight=true`; `Angle=120`; `Distance=5`; `Spread=0`; `Size=5`; `Contour=Linear`; `AntiAlias=false`; `Noise=0`; `LayerKnocksOut=true` |
| `InnerShadow` | `BlendMode=Multiply`; `Color=#FF000000`; `Opacity=0.75`; `UseGlobalLight=true`; `Angle=120`; `Distance=5`; `Choke=0`; `Size=5`; `Contour=Linear`; `AntiAlias=false`; `Noise=0` |
| `OuterGlow` | `BlendMode=Screen`; `Color=#FFFFFFBE`; `Gradient=null`; `Opacity=0.75`; `Noise=0`; `Technique=Softer`; `Spread=0`; `Size=5`; `Contour=Linear`; `AntiAlias=false`; `Range=0.5`; `Jitter=0` |
| `InnerGlow` | `BlendMode=Screen`; `Color=#FFFFFFBE`; `Gradient=null`; `Opacity=0.75`; `Noise=0`; `Technique=Softer`; `Origin=Edge`; `Choke=0`; `Size=5`; `Contour=Linear`; `AntiAlias=false`; `Range=0.5`; `Jitter=0` |
| `BevelEmboss` | `Style=InnerBevel`; `Technique=Smooth`; `Depth=1`; `Direction=Up`; `Size=5`; `Soften=0`; `UseGlobalLight=true`; `Angle=120`; `Altitude=30`; `GlossContour=Linear`; `AntiAlias=false`; `HighlightMode=Screen`; `HighlightColor=#FFFFFFFF`; `HighlightOpacity=0.75`; `ShadowMode=Multiply`; `ShadowColor=#FF000000`; `ShadowOpacity=0.75`; `ContourEnabled=false`; `Contour=Linear`; `ContourAntiAlias=false`; `ContourRange=0.5`; `TextureEnabled=false`; `Pattern=null`; `TextureScale=1`; `TextureDepth=1`; `TextureInvert=false`; `TextureLinkWithLayer=true`; `TextureOffset=(0,0)` |
| `Satin` | `BlendMode=Multiply`; `Color=#FF000000`; `Opacity=0.5`; `Angle=19`; `Distance=11`; `Size=14`; `Contour=Gaussian`; `AntiAlias=false`; `Invert=true` |
| `ColorOverlay` | `BlendMode=Normal`; `Color=#FFFF0000`; `Opacity=1` |
| `GradientOverlay` | `BlendMode=Normal`; `Opacity=1`; `Gradient=BlackToWhite`; `Method=Perceptual`; `Style=Linear`; `Angle=90`; `Scale=1`; `AlignWithLayer=true`; `Reverse=false`; `Dither=false`; `Offset=(0,0)` |
| `PatternOverlay` | `BlendMode=Normal`; `Opacity=1`; `Pattern=required`; `Scale=1`; `LinkWithLayer=true`; `Offset=(0,0)` |
| `Stroke` | `Size=3`; `Position=Outside`; `BlendMode=Normal`; `Opacity=1`; `FillType=Color`; `Color=#FF000000`; `Gradient=BlackToWhite`; `GradientMethod=Perceptual`; `GradientStyle=Linear`; `GradientAngle=90`; `GradientScale=1`; `GradientAlignWithLayer=true`; `GradientReverse=false`; `GradientDither=false`; `GradientOffset=(0,0)`; `Pattern=null`; `PatternScale=1`; `PatternLinkWithLayer=true`; `PatternOffset=(0,0)` |

`Color` and `Gradient` are mutually exclusive on glow styles: setting a gradient
selects gradient fill, while `Gradient=null` uses `Color`. `Stroke.FillType`
selects which one of `Color`, `Gradient`, or `Pattern` contributes pixels. A
required pattern is validated at generation time.

`UseGlobalLight=true` reads `GlobalLightAngle=120` and
`GlobalLightAltitude=30` from the containing `PrismComposition`. A style's local
`Angle` or `Altitude` is used when `UseGlobalLight=false`. The two global-light
properties may be overridden on a reusable or inline composition and can be
exposed through typed Prism parameters.

Multiple instances of the same style type are legal. This supports Photoshop-like
multiple shadows, strokes, and overlays without inventing another directive or a
special collection syntax.

## Layered Glow Example

Every layer processes the accumulated result beneath it:

```xml
<PrismComposition Name="NeonControl">
    @layer
    {
        @style DropShadow
        {
            Size = 10;
            Distance = 12;
            Angle = 90;
            Color = #66000000;
        }
    }

    @layer SoftGlow
    {
        @parameter GlowRadius: float = 24;

        Opacity = 0.75;
        BlendMode = Screen;

        @style OuterGlow
        {
            Size = GlowRadius;
            Color = #A060D8FF;
        }
    }

    @layer
    {
        Opacity = 0.35;
        BlendMode = Screen;

        @filter Blur
        {
            Radius = 12;
        }

        @filter Color
        {
            Saturation = 1.3;
            Tint = #3060D8FF;
        }
    }
</PrismComposition>
```

The scoped parameter may also be overridden when the reusable composition is attached:

```xml
<Border>
    @prism $NeonControl(
        SoftGlow.GlowRadius = 30
    );
</Border>
```

Read from top to bottom like a layer panel:

1. The bottom layer blurs and tints the captured control.
2. The middle layer adds a glow to that accumulated result.
3. The top layer adds a shadow to the final accumulated result.

## Layer, Group, And Backdrop Masks

`@mask` is a structural child of a layer, group, or backdrop, not a filter:

```xml
<PrismComposition Name="MaskedControl">
    @layer
    {
        @filter Color
        {
            Saturation = 0;
        }

        @mask
        {
            Image = $ControlMask;
            Channel = Luminance;
            Feather = 4;
            Density = 0.9;
            Invert = false;
        }
    }
</PrismComposition>
```

The captured control image is converted to grayscale, then its visibility is
controlled by the brightness of the auxiliary `$ControlMask` image.

A layer, group, or backdrop may contain at most one `@mask`. The mask applies to the
complete content-plus-styles contribution before opacity and blending.

Mask properties:

| Property | Default | Meaning |
| --- | --- | --- |
| `Image` | required | Typed alpha or luminance image used by the mask. |
| `Channel` | `Alpha` | `Alpha` or `Luminance`. |
| `Feather` | `0` | Softens the mask edge in device-independent pixels. |
| `Density` | `1` | Strength of the mask from `0` to `1`. |
| `Invert` | `false` | Reverses visible and hidden mask regions. |

## Layer Groups

A group is the explicit container for layers and nested groups:

```xml
<PrismComposition Name="GroupedControl">
    @layer
    {
        @style DropShadow
        {
            Size = 12;
            Distance = 8;
            Angle = 90;
            Color = #66000000;
        }
    }

    @group Highlights
    {
        Opacity = 0.65;
        BlendMode = Normal;

        @layer
        {
            @filter Blur
            {
                Radius = 8;
            }
        }

        @layer
        {
            @filter Color
            {
                Saturation = 1.25;
                Tint = #2860D8FF;
            }
        }

        @mask
        {
            Image = $HighlightMask;
            Channel = Luminance;
        }
    }
</PrismComposition>
```

This follows the same mental model as a folder in a Photoshop layer panel. The
group receives the accumulated image beneath it, then evaluates its children from
the bottom of the group toward the top. Filters and styles declared directly on
`@group` process and decorate the combined child result. The group mask, opacity,
and blend mode are applied afterward.

`@group` may contain `@layer` and nested `@group` declarations. It has no independent
control image and no source assignment; it operates on the combined result of its
children. An empty group is an error.

`@layer` is always a leaf. It may contain parameters, assignments, filters, styles,
and one mask, but never `@layer` or `@group`. Nesting either one inside a layer is a
build-time error; the generator must not infer that the layer was intended to be a
group.

Group properties follow Photoshop semantics:

| Property | Default | Meaning |
| --- | --- | --- |
| `Visible` | `true` | Whether the complete group participates in evaluation. |
| `Opacity` | `1` | Opacity of the complete group result. |
| `BlendMode` | `PassThrough` | Whether child blending interacts with content outside the group. |

`BlendMode = PassThrough` lets child blend modes interact with the accumulated image
outside the group. Any other group blend mode isolates the child stack first, then
blends the prepared group result as one image.

## Filter Chaining Example

Built-in filter types use the same concise syntax throughout the catalog:

```xml
<PrismComposition Name="Hologram">
    @parameter Shift: float = 0.012;

    @layer
    {
        @filter Scanlines
        {
            Frequency = 320;
            LineOpacity = 0.18;
        }

        @filter ChromaticAberration
        {
            Amount = Shift;
            DirectionX = 1;
            DirectionY = 0;
        }
    }
</PrismComposition>
```

`ChromaticAberration` clearly means that the layer receives chromatic aberration.
`Scanlines` clearly means that scanlines are added afterward. Because filter lists
execute bottom-up, `ChromaticAberration` is written below `Scanlines`. The author
does not need to know whether either filter uses a shader, a compute pass, or
several intermediate surfaces.

Each filter type publishes a generated property schema. The generator therefore
knows that `Amount` belongs to `ChromaticAberration` and that `Frequency` belongs to
`Scanlines`. Raw shader source and generic `Program` plumbing are not
embedded in Prism layer markup.

Prism markup consumes only semantic type names and typed properties from the
built-in catalog. The first implementation exposes no registration or backend
contract for application-provided filter or style types. Such extensibility requires
a separate future design; it is not hidden implementation work for this proposal.

## Motion Integration

Prism parameters are designed to be animated through the existing Motion language:

```text
@when IsMouseOver
{
    @animate with Spring(420, 32)
    {
        @to
        {
            $self.prism.SoftGlow.GlowRadius = 30;
        }
    }
}
```

The target prefix follows the existing Motion rules:

```text
$self.prism.SoftGlow.GlowRadius
$owner.prism.SoftGlow.GlowRadius
$Card.prism.SoftGlow.GlowRadius
```

The reserved `.prism.` path segment enters the Prism instance attached to the
selected control. The next segment selects a named layer, group, or backdrop.
Additional group and layer segments walk a nested group hierarchy. The final segment
selects an animatable property or typed scoped parameter.

The generator resolves the parameter statically:

- the selected control must have a statically known Prism definition;
- that Prism must contain the named `SoftGlow` layer;
- `SoftGlow` must define `GlowRadius`;
- the target element must have a compatible Prism;
- the value type must match;
- Motion must have a compatible interpolator.

Animating a parameter changes only that parameter. It does not rebuild the
`PrismComposition` or compile a new filter pipeline every frame.

## Conceptual Grammar

```text
prism-composition-resource :=
    <PrismComposition Name="Name" WorkingColorSpace="ColorSpace?">
        @parameter*
        prism-assignment*
        (layer | group)*
        backdrop?
    </PrismComposition>

prism-inline :=
    @prism
    {
        prism-assignment*
        (layer | group)*
        backdrop?
    }

prism-reference :=
    @prism $Name(arguments?);

layer :=
    @layer Name?
    {
        @parameter*
        layer-assignment*
        @filter*
        @style*
        @mask?
    }

group :=
    @group Name?
    {
        @parameter*
        group-assignment*
        (layer | group)+
        @filter*
        @style*
        @mask?
    }

filter :=
    @filter FilterType
    {
        filter-assignment*
    }

FilterType := Identifier

style :=
    @style StyleType
    {
        style-assignment*
    }

StyleType := Identifier

mask :=
    @mask
    {
        mask-assignment*
    }

backdrop :=
    @backdrop Name?
    {
        @parameter*
        backdrop-assignment*
        @filter*
        @style*
        @mask?
    }
```

A Prism body must contain at least one layer, group, or backdrop. `backdrop?` is
written after the normal layer stack because a backdrop declaration is always the
last direct entry.

Layer assignments use the existing Cerneala directive assignment form:

```text
Name = Value;
```

No pipe operator, method chaining, positional argument list, or operation-specific
directive syntax is introduced.

## Static Validation

The source generator should report build-time diagnostics for:

- unknown `PrismComposition` resources;
- unsupported working color profiles or color-space conversions;
- invalid global-light values;
- unknown parameters;
- unknown filter types;
- unknown style types;
- properties unsupported by a filter type;
- properties unsupported by a style type or mask;
- invalid property value types;
- a `Source` assignment declared on an `@layer`, `@group`, or `@backdrop`;
- an `@layer` with no filters or styles;
- an `@backdrop` with no filters or styles;
- `@layer` or `@group` declared inside an `@layer`;
- an `@group` with no child layers or groups;
- more than one `@mask` on the same layer, group, or backdrop;
- more than one `@backdrop` in one Prism body;
- an `@backdrop` nested inside a layer, group, or backdrop;
- an `@backdrop` that is not the last direct entry in its Prism body;
- `Fill`, `BlendMode`, or `ClipToBelow` declared on an `@backdrop`;
- `ClipToBelow = true` without an unclipped base sibling beneath it in the same scope;
- duplicate layer, group, or backdrop names in one address scope;
- unknown layer, group, backdrop, property, or scoped parameter segments in a Prism target;
- a Prism target that traverses an unnamed layer, group, or backdrop;
- incompatible filter parameters;
- incompatible style or mask parameters;
- a resource reference used where a filter or style type identifier is required.

Generated code should use typed definitions and parameter identifiers. Runtime
reflection and per-frame string lookup are not part of the proposal.

## Visual Bounds, Layout, And Input

Prism is visual post-processing. It never participates in measure or arrange and
never changes the control's desired size, arranged bounds, or hit-test geometry.

Filters and styles may render beyond the control's arranged bounds when their
visual result requires it. Shadow, glow, blur, stroke, distortion, and transform
are not silently cropped to the original control rectangle. Explicit clips and
ancestor viewport clips still constrain the final rendered output.

Pointer, keyboard, focus, automation, and accessibility continue to target the
original control and its normal visual tree. Prism layers, groups, and backdrops are
not UI elements and never become independent input targets.

## Lifecycle Expectations

The Prism authoring model is syntax, not resource ownership.

- UI elements do not own backend textures or shaders.
- Detaching an element releases its Prism instance bindings.
- Hiding or collapsing an element cancels Motion targeting its Prism parameters.
- Temporary surfaces belong to the renderer and are pooled under explicit budgets.
- Retained Prism results belong to the backend cache, carry no strong UI references,
  and are evicted under a separate explicit byte budget.
- Backdrop frames belong to the host or compositor; Prism holds only frame-scoped
  read access and never disposes the host's scene target.
- Invisible, hidden, collapsed, clipped-out, or detached backdrops do not acquire
  pixels or schedule filter work.
- Device loss clears backend resources without invalidating the authoring definition.
- The captured control image, backdrop references, and all temporary layer images
  are released when the Prism instance detaches.
- Detach or composition replacement invalidates retained entries through an opaque
  owner generation; hidden or collapsed instances perform no lookup or promotion
  and make their entries immediately evictable.

## Resolved Design Decisions

- The reusable resource is named `PrismComposition`.
- The built-in filter catalog and its canonical defaults are normative in this
  proposal and cover deterministic Photoshop adjustments, filters, and Filter
  Gallery effects.
- Prism includes all ten Photoshop layer-style families with typed properties and
  stable framework defaults.
- Prism includes every Photoshop layer blend mode; painting-tool-only modes are not
  misrepresented as layer modes.
- `LinearSrgb` is the default working color space. Built-in wide-gamut, encoded,
  HDR, and typed custom profiles are selectable per composition.
- Backdrop acquisition uses one frame-scoped `IBackdropFrameSource` lease per draw.
  The drawing backend owns ordered composition, region expansion, render-graph
  sharing, transient pooling, and retained GPU result caching with complete
  dependency stamps and explicit memory budgets.
- Public application-provided or third-party filter/style registration is explicitly
  deferred. This scope decision does not change the approved markup grammar.

No open question in this section requires another markup directive. Remaining work
belongs to the implementation planning specification: backend capability rollout,
GPU algorithm selection, quality tiers, resource budgets, diagnostics identifiers,
and conformance images for every catalog entry.

## Reference Model

The Photoshop parity surface in this proposal follows Adobe's current public
documentation:

- [Filter effects reference](https://helpx.adobe.com/photoshop/using/filter-effects-reference.html)
- [Layer style effects and options](https://helpx.adobe.com/ca/photoshop/desktop/create-manage-layers/apply-layer-effects/layer-style-effects-and-options-overview.html)
- [Blending mode descriptions](https://helpx.adobe.com/photoshop/desktop/repair-retouch/adjust-light-tone/blending-mode-descriptions.html)
- [Color settings](https://helpx.adobe.com/photoshop/using/color-settings.html)

Adobe names define the author-facing concepts, not an obligation to reproduce
private Adobe algorithms byte-for-byte. Prism conformance is defined by its own
documented schemas, deterministic defaults, reference images, and backend tolerance
thresholds.
