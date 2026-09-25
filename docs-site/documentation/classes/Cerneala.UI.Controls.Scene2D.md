# Scene2D Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Scene2D.cs`

Groups retained 2D scene nodes in deterministic drawing order.

```csharp
[ContentProperty(nameof(Children))]
public class Scene2D : SceneNode2D
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `SceneNode2D` -> `Scene2D`

## Examples

```xml
<RenderSurface2D.Scene>
    <Scene2D OrderMode="LayerThenY"
             TranslateX="32"
             Scale="1.5"
             Rotation="0.1"
             TransformOrigin="128,96">
        <Sprite2D Layer="0" />
        <Sprite2D Layer="10" />
    </Scene2D>
</RenderSurface2D.Scene>
```

### Reusable scene components

Pair `HouseView.crn` with `HouseView.crn.cs` to define a reusable scene group.
The markup root initializes the component itself, not an extra nested group.

`HouseView.crn`:

```xml
<Scene2D xmlns:resources="clr-namespace:Cerneala.UI.Resources;assembly=Cerneala">
    <Scene2D.Resources>
        <resources:ImageResource Name="HouseArt" Source="Assets/house.png" />
    </Scene2D.Resources>
    <Sprite2D Image="$HouseArt" Width="32" Height="24" />
    <Sprite2D Name="Door" X="4" Y="16" Width="8" Height="8" />
</Scene2D>
```

`HouseView.crn.cs`:

```csharp
using Cerneala.UI.Controls;

namespace Game;

public partial class HouseView : Scene2D
{
}
```

Import its CLR namespace in the consuming document:

```xml
<RenderSurface2D xmlns:local="clr-namespace:Game" RedrawMode="OnDemand">
    <RenderSurface2D.Scene>
        <Scene2D OrderMode="Layer">
            <local:HouseView TranslateX="30" TranslateY="20" Scale="2" Layer="10" />
            <local:HouseView TranslateX="90" TranslateY="40" Layer="20" />
        </Scene2D>
    </RenderSurface2D.Scene>
</RenderSurface2D>
```

A component can also be the single direct child of `RenderSurface2D.Scene`.
Position and transform channels affect the entire instance; `Layer` is interpreted
by its containing scene's `OrderMode`, just like an ordinary `Scene2D` group.

## Remarks

### Component construction and scope

The paired class must match the file name, derive from `Scene2D`, and be a
concrete, non-nested, non-generic, non-file-local `partial` class. Do not declare
instance constructors: the generator supplies a public parameterless constructor
that initializes the instance's properties, resources, handlers, and logical
children. A paired root cannot declare `Name`; it represents `this`.

Named descendants become private generated members in the paired class. They
must not conflict with members declared in code-behind. Each new component has
its own child instances, local resource declarations, and name scope. `$root`
inside its markup refers to that component, including custom UI properties
declared in its companion class. To type `$DataContext` bindings, declare a root
`DataType`. Generated `$DataContext` bindings resolve the nearest context through
the logical ancestors and rebind when that context changes. This lookup does not
require copying the ancestor's `DataContext` value onto every logical node.

The component uses the existing scene attachment, binding, Aspect, Motion, Prism,
template, collision, and invalidation paths. Detaching and reattaching does not
rerun its constructor or rebuild its permanent children. Deriving from `Scene2D`
does not expose a new custom recording API; applications compose the provided
scene nodes. A paired document emits the partial component, not a standalone
`HouseViewFactory`. An unpaired `.crn` file retains the ordinary factory behavior.

### Scene behavior

Direct markup children are added to `Children`. `OrderMode="Source"`, the default, records them from first to last, so later children draw after earlier children. Adding, replacing, removing, or clearing children invalidates the owning surface.

`OrderMode="Layer"` sorts by each child's `Layer` value. `OrderMode="LayerThenY"` sorts first by `Layer`, then by the bottom edge of the child's transformed scene-space bounds. Smaller values draw first. Both modes are stable: equal keys retain source collection order, and neither mode mutates `Children`. A missing or unknown bound uses a Y anchor of `0` rather than removing the node.

Picking uses that same effective order in reverse, so the last visible eligible node at a point wins. The chosen node enters the ordinary UI route through its scene ancestors and the owning `RenderSurface2D`; scene groups do not create a second router or event family.

Children belong to the logical tree and inherit data context and attachment state. Setting either `IsVisible` to `false` or `Visibility` to a non-visible value skips the group and all of its descendants.

When recording skips a hidden or zero-opacity group, its descendant render/image acquisitions are released without detaching those nodes. Retained command owners and other consumers can still keep shared images alive until they release their own acquisitions. Opacity does not disable colliders or stop attached sprite-animation playback.

The root group owns a `CollisionWorld2D`. `CollisionWorld` on any nested group resolves to the same root-owned world. Structural and collider-property mutations update that world incrementally; removing a subtree removes its indexed colliders before the next query.

For source-backed collision preparation without UI, give an unowned root group an explicit [SceneSimulationContext2D](Cerneala.UI.Controls.SceneSimulationContext2D.md). The inherited `SimulationContext` identifies that owner throughout its scene-node subtree. Data observation, source preparation and queries then use its owner thread and ordinary `Update` loop, without creating rendering or UI lifecycle services. Dispose that independent context before reparenting or attaching the root to a surface. A UI-attached surface automatically supplies the same common lifecycle using its existing root relay.

The world also owns prepared spatial collision-region interests. Detaching the owning scene or changing its surface invalidates those leases and cancels unfinished preparation; reattachment does not revive old leases. See [CollisionWorld2D](Cerneala.UI.Controls.CollisionWorld2D.md) for readiness checks and explicit preparation before querying unloaded terrain.

The scene owns the query world, not the collision shapes themselves. A `Sprite2D` owns zero or one live collider through its `Collider` property. `Scene2D.Children` rejects collider nodes, including in derived markup components. A static `Tile` or `TileDefinition2D` likewise owns zero or one immutable descriptor through `Collider`.

`Scene2D` applies the inherited `Scale`, `ScaleX`, `ScaleY`, `SkewX`, `SkewY`, `Rotation`, `TranslateX`, `TranslateY`, and `RenderTransform` channels to the entire descendant group. `TransformOrigin` is an absolute point in the group's local scene coordinates, not a normalized layout point. The local transform is composed in this order: translate away from the origin, scale, skew, rotate, translate, apply `RenderTransform`, and translate back to the origin. Nested groups compose their transforms from child to parent.

These transform channels and `TransformOrigin` are `UiProperty` values, so Aspect and Motion can control them. A group `Opacity` scopes its descendants. Prism attached to a group captures exactly that group's descendant commands and uses their aggregate transformed scene bounds; Prism attached to a child remains nested inside the group scope.

Aspect can also assign the structural `OrderMode` and inherited `Layer` properties. Motion deliberately rejects those two properties because no ordering mixer or interpolation contract exists. Animating `TranslateY`, other transform channels, or `Opacity` remains supported and updates ordering or presentation in the same logical frame.

A transform with no inverse, such as `ScaleX="0"`, still records the group. Forward rendering and conservative bounds remain available, while world-to-local conversion is unavailable internally for that transform.

### Prism input and streamed children

For source-backed children, a pointwise operation reads the same scene pixel rather than neighboring scene pixels or an input-wide distribution. Compositions containing only the following active filters and paint styles therefore preserve ordinary viewport selection and offscreen resource retirement:

- `BrightnessContrast`, `Curves`, `Exposure`, `Vibrance`, `HueSaturation`, `ColorBalance`, `BlackWhite`, `PhotoFilter`, `ChannelMixer`, `ColorLookup`, `Invert`, `Posterize`, `GradientMap`, and `SelectiveColor`;
- `Levels` when its live `Auto` value is `false`;
- the `ColorOverlay`, `GradientOverlay`, and `PatternOverlay` styles.

An image mask also does not expand scene-payload interest. Alpha/luminance extraction, inversion and feathering sample the mask's own image, not neighboring scene pixels. Feathering retains the finite intermediate raster margin required by its kernel; that margin does not acquire additional scene objects or map chunks. Existing raster allocation limits still apply.

Color lookup, paint and mask resources remain independent dependencies; this classification does not eliminate their acquisition, preparation or leases. Paint, mask and dithering coordinates retain their logical reference when physical input is cropped, including at non-unit DPI. The composition's logical bounds do not shrink merely because off-camera drawing is skipped: `SceneItems2D` contributes the bounds of its already-realized children, and `TileMap2D` contributes the bounds of its complete prepared map index or in-memory model. Ordinary direct children retain their usual bounds contract.

Selection reads live layer/group/filter visibility and opacity, style visibility, nested groups, and the current definition. Hidden operations and zero-opacity layers/groups/filters do not expand input interest; a style's effect-specific opacity parameter alone does not remove its input requirement. `Levels.Auto = true` and `Threshold` require input-wide analysis. In a hosted scene containing spatial materializers, declare the inherited [PrismInputDomain](Cerneala.UI.Controls.SceneNode2D.md#finite-prism-input-domains) on each node owning such a composition. Its finite rectangle uses that owner's local coordinates and becomes its logical capture domain. Missing declarations report a presentation error instead of preparing the whole world.

Declared input is prepared in full even when part of it is outside the camera. The backend evaluates the effect using that input before applying the final camera clip. Changing the camera therefore does not redefine a histogram's input. Changing the declaration reconciles payload/image interest; simulation and explicit collision interest are independent. Nested global-effect owners need their own declarations. Unsupported raster extents or allocation failure produce an explicit rendering error rather than an effect-free or lower-resolution fallback.

Without a declared domain, hosted spatial scenes automatically include the finite sampling neighborhood of these filters:

- `Average`, `Blur`, `BlurMore`, `BoxBlur`, `GaussianBlur`, and `MotionBlur`;
- `SmartBlur`, `SurfaceBlur`, `Sharpen`, `SharpenMore`, `SharpenEdges`, and `UnsharpMask`;
- `HighPass`, `DustScratches`, and `Median`.

For filters exposing `EdgeMode`, this automatic path covers `Clamp` and `Transparent`, not `Wrap` or `Mirror`/`Reflect`. Input selection uses the existing kernel passes' sampling support, including fractional-sample texels and sequential passes; output expansion alone is insufficient for filters such as `HighPass`. Filter chains, isolated/pass-through groups and nested scene compositions combine their dependencies before selection. The raster neighborhood is projected back through the current surface and scene transforms. Live parameters, visibility, opacity, definition changes and DPI are reconsidered; a zero-radius/no-op kernel does not retain its former neighborhood. The composition's logical coordinate bounds remain unchanged.

These required neighbors are prepared and recorded even when off camera. They are effect input, not extra displayed content. A declared non-pointwise `PrismInputDomain` still selects its complete input rather than this automatic camera neighborhood. Independent collision/simulation interests and the map's bounded optional warm set can retain additional data.

Required sampling interest is not synthetic source content. When the composition's
content bounds are known, capture intersects that interest with its actual input
bounds; the kernel planner separately owns output expansion. Thus a small bounded
child does not acquire a camera-sized transparent border merely because an
ancestor needs a larger region. An explicit domain on the composition's own owner
defines its input boundary instead. Unknown content bounds remain conservative.

The remaining styles (`DropShadow`, `InnerShadow`, `OuterGlow`, `InnerGlow`, `BevelEmboss`, `Satin`, and `Stroke`), boundary-wrapping kernels and every other unclassified filter **require a finite `PrismInputDomain`** in a hosted spatial scene. Without it, presentation enters `Error` and the unsupported composition does not start full-catalog presentation acquisition. This is an explicit compatibility restriction, not a visual approximation or automatic support for those operations. Declare the complete intended input on each effect owner, including nested sprite owners; effects are still evaluated normally before the camera clip. Changing a live edge mode or replacing a definition can introduce or remove this requirement. Clearing a required domain reports the error again.

Non-invertible or unknown transforms can still make geometric selection conservative even for supported operations; this restriction does not invent an inverse or promise bounded residency for invalid/unbounded geometry. A finite declared domain can also exceed available memory or raster limits and fail explicitly. An effect attached only to an ordinary UI ancestor is outside this scene-node input classification.

Simulation and collision interests remain independent: a pointwise effect does not detach an offscreen simulated actor or remove required terrain. These input rules apply to automatic source selection, not virtualization of direct caller-owned children or ordinary UI controls.

## Constructors

| Name | Description |
| --- | --- |
| `Scene2D()` | Creates an empty owned child collection. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Children` | `Collection<SceneNode2D>` | Ordered retained nodes recorded by the group. |
| `CollisionWorld` | `CollisionWorld2D` | Root-owned collision query world shared by this group and its nested scene groups. |
| `OrderMode` | `SceneOrderMode` | Selects source, layer, or stable layer-then-Y recording order. The default is `Source`. |
| `TransformOrigin` | `DrawPoint` | Absolute transform pivot in local scene-space units. The default is `(0, 0)`. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `OrderModeProperty` | `UiProperty<SceneOrderMode>` | Identifies the `OrderMode` UI property. Changes affect rendering and ordering. |
| `TransformOriginProperty` | `UiProperty<DrawPoint>` | Identifies the `TransformOrigin` UI property. Changes affect rendering. |

## Applies to

Project: `Cerneala`

## See also

- `RenderSurface2D`
- `SceneNode2D`
- `SceneOrderMode`
- `SceneItems2D`
- `Sprite2D`
- `CollisionWorld2D`
- `MouseEventArgs`
