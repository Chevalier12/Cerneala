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
            <local:HouseView X="30" Y="20" Scale="2" Layer="10" />
            <local:HouseView X="90" Y="40" Layer="20" />
        </Scene2D>
    </RenderSurface2D.Scene>
</RenderSurface2D>
```

A component can also be the single direct child of `RenderSurface2D.Scene`.
Position and transform channels affect the entire instance; `Layer` is interpreted
by its containing scene's `OrderMode`, just like an ordinary `Scene2D` group.

### Placing a group with X and Y

`X` and `Y` place the group's local origin in its parent's coordinates. They are
applied after the group's own scale, skew, rotation and translation, so rotation and
scale still turn around `TransformOrigin` in the group's local coordinates:

```xml
<local:HouseView X="256" Y="96" Rotation="1.5708" TransformOrigin="16,12" />
```

Here the house first turns about a quarter turn around its local point
`(16, 12)`, then moves by `(256, 96)`. The pivot `(16, 12)` therefore ends at
`(272, 108)`, and the local corner `(0, 0)` ends at about `(284, 92)`.

`X`/`Y` and `TranslateX`/`TranslateY` add up: `X="100" TranslateX="5"` moves the
group by 105. Use `X`/`Y` for where the group stands in the world, and
`TranslateX`/`TranslateY` for an extra offset such as an animated bounce.

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

`OrderMode="Layer"` sorts by each child's `Layer` value. `OrderMode="LayerThenY"` sorts first by `Layer`, then by the bottom edge of the child's transformed scene-space bounds. Smaller values draw first. Both modes are stable: equal keys retain source collection order, and neither mode mutates `Children`. A missing or unknown bound uses a Y anchor of `0` rather than removing the node. `Opacity` does not change these bounds: a sprite with `Opacity="0"` at `Y="100"` and `Height="10"` keeps its bottom edge of `110`, as it has at `Opacity="0.01"`, and a group keeps the bottom edge of all its rendered children whatever their opacity.

Picking uses that same effective order in reverse, so the last visible eligible node at a point wins. The chosen node enters the ordinary UI route through its scene ancestors and the owning `RenderSurface2D`; scene groups do not create a second router or event family.

Children belong to the logical tree and inherit data context and attachment state. Setting either `IsVisible` to `false` or `Visibility` to a non-visible value skips the group and all of its descendants.

When recording skips a hidden or zero-opacity group, its descendant render/image acquisitions are released without detaching those nodes. Retained command owners and other consumers can still keep shared images alive until they release their own acquisitions. Opacity does not disable colliders or stop attached sprite-animation playback.

The root group owns a `CollisionWorld2D`. `CollisionWorld` on any nested group resolves to the same root-owned world. Structural and collider-property mutations update that world incrementally; removing a subtree removes its indexed colliders before the next query.

For source-backed collision preparation without UI, give an unowned root group an explicit [SceneSimulationContext2D](Cerneala.UI.Controls.SceneSimulationContext2D.md). The inherited `SimulationContext` identifies that owner throughout its scene-node subtree. Data observation, source preparation and queries then use its owner thread and ordinary `Update` loop, without creating rendering or UI lifecycle services. Dispose that independent context before reparenting or attaching the root to a surface. A UI-attached surface automatically supplies the same common lifecycle using its existing root relay.

The world also owns prepared spatial collision-region interests. Detaching the owning scene or changing its surface invalidates those leases and cancels unfinished preparation; reattachment does not revive old leases. See [CollisionWorld2D](Cerneala.UI.Controls.CollisionWorld2D.md) for readiness checks and explicit preparation before querying unloaded terrain.

The scene owns the query world, not the collision shapes themselves. A `Sprite2D` owns zero or one live collider through its `Collider` property. `Scene2D.Children` rejects collider nodes, including in derived markup components. The one collider a group can own is its `Vision`, described below. A static `Tile` or `TileDefinition2D` likewise owns zero or one immutable descriptor through `Collider`.

### Vision

`Vision` is a nullable [ConeCollider2D](Cerneala.UI.Controls.ConeCollider2D.md), `null` by default: the cone the whole group sees with. It is a logical child of the group in a slot of its own. It is never one of `Children`: `Children.Add(cone)` throws `InvalidOperationException`, and so does adding or removing it through `LogicalChildren`. Assign or clear it only through the property.

```csharp
Scene2D dragon = new() { X = 100, Y = 50 };
dragon.Children.Add(new Sprite2D { X = -40, Y = -5, Collider = new BoxCollider2D { Width = 10, Height = 10 } });

dragon.Vision = new ConeCollider2D { Radius = 150, Angle = 90, Direction = 0 };
// dragon.Children.Count is still 1; the cone is not a child.
// dragon.LogicalChildren.Count is 2; the cone's LogicalParent is dragon.
dragon.Vision = null; // the cone leaves the group; the sprite stays
```

- The cone uses the group's transform: `X`, `Y`, `TransformOrigin` and the inherited transform channels place and turn it, as they place and turn the children. In the example the apex is at `(100, 50)` in the parent's coordinates. In another example, a group with `X = 0`, `Y = 0`, `TransformOrigin = new DrawPoint(100, 0)` and `Rotation = MathF.PI` moves its cone's apex from `(0, 0)` to `(200, 0)`, and the cone, which looked right, now looks left.
- Adding, inserting, replacing, removing or clearing `Children` never touches the cone.
- The cone is a sensor. It does not block, it is not picked, and only the group learns what it touches, through `dragon.Vision.Entered` and `Exited` and through `CollisionWorld.Overlap(dragon.Vision)`. It ignores every collider inside the group, also inside nested groups, and a body it sees receives no event for the cone. A cone on a root scene therefore sees nothing, because every collider of that scene is inside it. A sprite inside the group can have a `Vision` of its own, which sees the group's other parts. The rules are in [ConeCollider2D](Cerneala.UI.Controls.ConeCollider2D.md#a-vision-is-a-sensor).
- A cone is the `Vision` of one owner at a time, sprite or group. Assigning it to a second owner throws `InvalidOperationException` and leaves it where it is; set the first owner's `Vision` to `null` first.
- The cone draws nothing and is not part of the group's content. It does not change which children the group draws, their drawing order, or the bounds the group reports. A group with a `Vision` records and orders the same children as the same group without one, so its offscreen children are still skipped.

#### Markup usage

`<Scene2D.Vision>` holds exactly one [ConeCollider2D](Cerneala.UI.Controls.ConeCollider2D.md#markup-usage). The generated code assigns it to the `Vision` property of the group. It is never added to `Children`:

```xml
<Scene2D>
    <Scene2D X="100" Y="50">
        <Scene2D.Vision>
            <ConeCollider2D Radius="150" Angle="90" Direction="0" />
        </Scene2D.Vision>
        <Sprite2D X="-40" Y="-5" Width="10" Height="10">
            <BoxCollider2D Width="10" Height="10" />
        </Sprite2D>
    </Scene2D>
</Scene2D>
```

The inner group has one child, the sprite, and its `Vision` is the cone. The outer root has no `Vision`. On the root of a paired scene component (see the `HouseView` example above), `<Scene2D.Vision>` is the `Vision` of the component itself. Any element other than a cone inside `<Scene2D.Vision>`, and a cone anywhere that is not a `Vision`, is `CERNEALAUI005`; the rules are listed in [ConeCollider2D, Markup usage](Cerneala.UI.Controls.ConeCollider2D.md#markup-usage).

`Scene2D` applies the inherited `Scale`, `ScaleX`, `ScaleY`, `SkewX`, `SkewY`, `Rotation`, `TranslateX`, `TranslateY`, and `RenderTransform` channels to the entire descendant group. `TransformOrigin` is an absolute point in the group's local scene coordinates, not a normalized layout point. The local transform is composed in this order: translate away from the origin, scale, skew, rotate, translate, apply `RenderTransform`, translate back to the origin, and finally translate by the group's `X` and `Y` placement. Nested groups compose their transforms from child to parent. Changing `X` or `Y` moves the group's drawing, picking and colliders together, like the other channels.

These transform channels and `TransformOrigin` are `UiProperty` values, so Aspect and Motion can control them. `X` and `Y` are also `UiProperty<float>` values, but Motion has no implicit spec for them; animate `TranslateX`/`TranslateY` instead. `X` and `Y` must be finite numbers; `NaN` or infinity is rejected. A group `Opacity` scopes its descendants. Prism attached to a group captures exactly that group's descendant commands and uses their aggregate transformed scene bounds; Prism attached to a child remains nested inside the group scope.

Aspect can also assign the structural `OrderMode` and inherited `Layer` properties. Motion deliberately rejects those two properties because no ordering mixer or interpolation contract exists. Animating `TranslateY`, other transform channels, or `Opacity` remains supported and updates ordering or presentation in the same logical frame.

A transform with no inverse, such as `ScaleX="0"`, still records the group. Forward rendering and conservative bounds remain available, while world-to-local conversion is unavailable internally for that transform.

### Awake

`IsAwake` is `false` by default. While it is `true`, the group receives `Awake` once per time advance of the render surface that shows its scene. `Scene2D.Awake` is for the group itself. The children of the group do not receive it through the group: each child gets its own `Awake` only when that child is awake itself. A group and its children are registered independently, each by its own `IsAwake` (`Scene2D.cs:104`, `RenderSurface2D.cs:376`; no dedicated test for an asleep group with awake children).

```csharp
Scene2D flock = new();
flock.Children.Add(new Sprite2D());
int ticks = 0;
flock.Awake += (sender, e) => ticks++; // sender is flock; e is a FrameTimeEventArgs

flock.IsAwake = true;
// flock is in the Scene of a RenderSurface2D attached to a UI root.
// With host updates of 16 ms, Awake is raised once per update, with e.Elapsed 0 ms, 16 ms, 16 ms, ...
// The Sprite2D child is not awake, so it receives nothing.
```

The rules of [Sprite2D, Awake](Cerneala.UI.Controls.Sprite2D.md#awake) apply to a group in the same way. These cases are tested for `Scene2D` as well as for `Sprite2D`, in `SceneAwakeTests`:

- `IsAwake` defaults to `false` (`IsAwakeDefaultsToFalse`), and a group that is not awake gets no event (`NoAwakeIsRaisedWhileIsAwakeIsFalse`).
- `Elapsed` is `0` on the first event, then the time of the update (`AwakeIsRaisedOncePerUpdateWithZeroThenTheFrameTime`); setting `IsAwake` to `false` stops the events, and waking again restarts at `0` (`SettingIsAwakeFalseStopsTheEventsAndWakingAgainRestartsAtZero`, `ANodeThatSleepsInItsOwnHandlerGetsNoEventFromTheNextUpdate`).
- A group removed from its parent scene stops receiving `Awake`; added back, it starts at `0` again (`ANodeRemovedFromTheSceneStopsAndWhenAddedBackStartsAtZeroAgain`, `ANodeAlreadyAwakeWhenItEntersTheSceneStartsAtZero`).
- A group woken inside a handler gets its first `Awake` in the next update (`ANodeWokenInAHandlerGetsItsFirstAwakeOnTheNextUpdate`).
- A group is visited in the order of waking, and a parent group before its children when they were awake before the scene reached the surface (`AwakeNodesOfASceneAttachedToASurfaceRegisterInSceneOrderParentFirst`).

Setting `IsAwake` on a group requests no frame by itself: `IsAwakeProperty` has no options (`Scene2D.cs:39-43`). The pump test that shows this (`WindowRuntimeTests.AnAwakeSpriteGetsAwakeOnEveryPumpWithoutRequestingFrames`) uses a sprite.

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
| `Scene2D()` | Creates an empty owned child collection and no vision. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Children` | `Collection<SceneNode2D>` | Ordered retained nodes recorded by the group. |
| `Vision` | `ConeCollider2D?` | Gets or sets the cone the group sees with. It is a logical child in a slot of its own, never one of `Children`. The default is `null`. |
| `IsAwake` | `bool` | Gets or sets whether the group receives `Awake` once per time advance of its surface. The default is `false`. See [Awake](#awake). |
| `CollisionWorld` | `CollisionWorld2D` | Root-owned collision query world shared by this group and its nested scene groups. |
| `OrderMode` | `SceneOrderMode` | Selects source, layer, or stable layer-then-Y recording order. The default is `Source`. |
| `TransformOrigin` | `DrawPoint` | Absolute transform pivot in local scene-space units. The default is `(0, 0)`. |
| `X` | `float` | Horizontal placement of the group's local origin in its parent, applied after the local transform. The default is `0`. |
| `Y` | `float` | Vertical placement of the group's local origin in its parent, applied after the local transform. The default is `0`. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `OrderModeProperty` | `UiProperty<SceneOrderMode>` | Identifies the `OrderMode` UI property. Changes affect rendering and ordering. |
| `TransformOriginProperty` | `UiProperty<DrawPoint>` | Identifies the `TransformOrigin` UI property. Changes affect rendering. |
| `XProperty` | `UiProperty<float>` | Identifies the `X` UI property. Changes affect rendering, picking and collision geometry. |
| `YProperty` | `UiProperty<float>` | Identifies the `Y` UI property. Changes affect rendering, picking and collision geometry. |
| `IsAwakeProperty` | `UiProperty<bool>` | Identifies the `IsAwake` UI property. It has no options; changing it requests no frame. |

## Events

| Name | Type | Description |
| --- | --- | --- |
| `Awake` | `EventHandler<FrameTimeEventArgs>` | Occurs once per time advance of the surface that shows the group's scene, while `IsAwake` is `true`. The sender is the group. See [Awake](#awake). |

## Applies to

Project: `Cerneala`

## See also

- `RenderSurface2D`
- `SceneNode2D`
- `SceneOrderMode`
- `SceneItems2D`
- `Sprite2D`
- `ConeCollider2D`
- `CollisionWorld2D`
- `MouseEventArgs`
- `FrameTimeEventArgs`
