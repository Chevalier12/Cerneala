# SpriteAnimationSet Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/SpriteAnimation.cs`

Groups immutable sprite-animation clips under unique ordinal names.

```csharp
public sealed class SpriteAnimationSet
```

Inheritance: `Object` -> `SpriteAnimationSet`

## Examples

```csharp
var animations = new SpriteAnimationSet([idle, walk, attack], version: 2);

if (animations.TryGetClip("Walk", out SpriteAnimationClip? clip))
{
    Console.WriteLine(clip.Duration);
}
```

## Remarks

The set requires at least one non-null clip and rejects duplicate names using ordinal, case-sensitive comparison. The constructor defensively copies the clip sequence. A set contains definitions only; elapsed time and current state belong to each consuming sprite instance.

## Declarative states

Declare the set inside the consuming element's resource property element. This is the same constructor grammar exercised by the SourceGen `Idle`/`Walk`/`Attack` tests:

```xml
<RenderSurface2D>
  <RenderSurface2D.Resources>
    <SpriteAnimationSet Name="HeroAnimations">
      <SpriteAnimationClip Name="Idle">
        <SpriteAnimationFrame SourceX="0" SourceY="0" SourceWidth="16" SourceHeight="16" Duration="240ms" />
        <SpriteAnimationFrame SourceX="16" SourceY="0" SourceWidth="16" SourceHeight="16" Duration="240ms" />
      </SpriteAnimationClip>
      <SpriteAnimationClip Name="Walk">
        <SpriteAnimationFrame SourceX="0" SourceY="16" SourceWidth="16" SourceHeight="16" Duration="90ms" />
        <SpriteAnimationFrame SourceX="16" SourceY="16" SourceWidth="16" SourceHeight="16" Duration="110ms" Flip="Horizontal" />
      </SpriteAnimationClip>
      <SpriteAnimationClip Name="Attack" IsLooping="false">
        <SpriteAnimationFrame SourceX="0" SourceY="32" SourceWidth="16" SourceHeight="16" Duration="60ms" />
        <SpriteAnimationFrame SourceX="16" SourceY="32" SourceWidth="16" SourceHeight="16" Duration="140ms" />
      </SpriteAnimationClip>
    </SpriteAnimationSet>
  </RenderSurface2D.Resources>
  <RenderSurface2D.Scene>
    <Scene2D>
      <Sprite2D Animations="$HeroAnimations" AnimationState="Idle" />
    </Scene2D>
  </RenderSurface2D.Scene>
</RenderSurface2D>
```

This fragment defines animation, not the image or destination: supply those through the sprite's existing image/destination APIs. `Name` is the resource key on the set and the state name on a clip. Frame `SourceX`/`SourceY`/`SourceWidth`/`SourceHeight` and `Duration` are immutable constructor inputs. A frame `Duration` must convert to at least one 100 ns `TimeSpan` tick and fit in `TimeSpan`: `0.0001ms` (one tick) builds, while `0ms`, `0.00001ms` (zero ticks), `1e20ms`, and `1e20s` produce `CERNEALAUI016`. No mutable frame collection, FPS setting, or automatic transition from `Attack` to `Idle` is implied.

For binding, declare the actual application data type on the markup root and use `AnimationState="$DataContext.AnimationState:OneWay"`; the application owns that notifying string property. Static unknown states and invalid definitions produce `CERNEALAUI016`; an unresolved runtime-bound state uses the target's static visual fallback. A state is static when it is a literal string: in the `AnimationState` attribute, or in an assignment such as `AnimationState = "Attack";` in the sprite's inline `<Sprite2D.Aspect>` (`@default` or `@when`) or in a reactive `@when` block on the sprite. The build compares that exact string, case-sensitive, with the clip names of the sprite's `Animations` set, and reports a missing one at the value. A clip whose frame durations add up past `TimeSpan.MaxValue` is an invalid definition: the build reports `CERNEALAUI016` at the `Duration` of the frame that makes the total too long.

The same immutable resource can be shared by multiple sprites; each sprite keeps independent playback. Select and style the sprite with normal Aspect, Motion, and Prism syntax. Static `Tile` data has no animation state, and the resource set itself is not an animated UI target.

### Frames from a grid of cells

When every frame of the atlas is a cell of the same size, set `CellWidth` and `CellHeight` on the set and address each frame with `Column` and `Row`, both counted from 0:

```xml
<SpriteAnimationSet Name="HeroAnimations" CellWidth="32" CellHeight="32">
  <SpriteAnimationClip Name="Walk">
    <SpriteAnimationFrame Column="0" Row="2" Duration="150ms" />
    <SpriteAnimationFrame Column="1" Row="1" Duration="150ms" Flip="Horizontal" />
  </SpriteAnimationClip>
</SpriteAnimationSet>
```

The first frame samples `(0, 64, 32, 32)` and the second `(32, 32, 32, 32)`. Each cell frame becomes a [`SpriteAnimationFrame.FromCell`](Cerneala.UI.Controls.SpriteAnimationFrame.md) call, so it computes the same rectangle as the C# method.

`CellMargin` is the border around the whole grid and `CellSpacing` the gap between cells, in pixels. Both default to `0` and apply to both axes. With `CellMargin="2" CellSpacing="1"`, `Column="1" Row="0"` samples `(35, 2, 32, 32)`: x = 2 + 1 * (32 + 1).

A set may mix cell frames and `SourceX`/`SourceY`/`SourceWidth`/`SourceHeight` frames, even inside one clip. One frame uses one form only. These are `CERNEALAUI016` build errors:

- `Column` or `Row` in a set without `CellWidth` and `CellHeight`;
- `CellWidth` without `CellHeight`, or the reverse;
- `CellMargin` or `CellSpacing` without `CellWidth` and `CellHeight`;
- `Column` without `Row`, or the reverse;
- in a set with `CellWidth` and `CellHeight`, a frame with neither `Column`/`Row` nor any `Source*` attribute;
- `Column` or `Row` that is not a non-negative integer, for example `1.5`, `-1`, or `a`;
- `CellWidth` or `CellHeight` that is not a number greater than zero;
- `CellMargin` or `CellSpacing` that is negative or not a number;
- `Column` or `Row` together with any `Source*` attribute on the same frame;
- any `Cell*` attribute together with `Source` on the set.

The generator cannot know the image size, so a cell outside the image is not a build error.

### Clips from an Aseprite export

`Source` reads the JSON sprite-sheet data of an Aseprite export at build time. Every frame tag becomes one clip with the tag's name. Export the sheet with its tags, for example:

```text
aseprite -b hero.aseprite --sheet Assets/hero.png --data Assets/hero.json --format json-array --list-tags
```

List the JSON file as an `AdditionalFiles` item in the project file, so the generator can read it:

```xml
<ItemGroup>
  <AdditionalFiles Include="Assets\hero.json" />
</ItemGroup>
```

Then point the set at it, and give the sprite the sheet image as usual:

```xml
<RenderSurface2D xmlns:resources="clr-namespace:Cerneala.UI.Resources;assembly=Cerneala">
  <RenderSurface2D.Resources>
    <resources:ImageResource Name="HeroSheet" Source="Assets/hero.png" />
    <SpriteAnimationSet Name="HeroAnimations" Source="Assets/hero.json" />
  </RenderSurface2D.Resources>
  <RenderSurface2D.Scene>
    <Scene2D>
      <Sprite2D Image="$HeroSheet" Animations="$HeroAnimations" AnimationState="Bob" />
    </Scene2D>
  </RenderSurface2D.Scene>
</RenderSurface2D>
```

With a tag `Bob` from frame 0 to 3 in `pingpong` direction, the sprite shows frames 0, 1, 2, 3, 2, 1 and starts again.

`Source` selects the `AdditionalFiles` item whose path ends with `/` + `Source`, ignoring case and `\` versus `/`. `Assets/hero.json` matches `C:\Game\Assets\hero.json` but not `C:\Game\Assets\superhero.json`.

What the import keeps:

- Both export layouts: `json-hash` (`frames` is an object) and `json-array` (`frames` is an array). The order of `frames` is the frame index that tags use.
- Each frame's `frame` rectangle and `duration` in milliseconds.
- The tag direction, expanded into a fixed frame list. For a tag `from: 0`, `to: 3`: `forward` gives 0, 1, 2, 3; `reverse` gives 3, 2, 1, 0; `pingpong` gives 0, 1, 2, 3, 2, 1; `pingpong_reverse` gives 3, 2, 1, 0, 1, 2.
- `repeat`: no `repeat` makes a looping clip. `"repeat": "1"` makes a clip with `IsLooping="false"` that plays one pass in the tag's first direction, so `pingpong` 0..3 gives 0, 1, 2, 3. Any other `repeat` value is a build error.
- Trimmed frames (`"trimmed": true`): the frame keeps `spriteSourceSize.x`/`y` as its `Offset` and `sourceSize` as its `FrameSize`, so the sprite draws each pixel where the untrimmed frame would. See [SpriteAnimationFrame](Cerneala.UI.Controls.SpriteAnimationFrame.md).
- Frames outside every tag are ignored.

The set `Version` still applies. These are `CERNEALAUI016` build errors, each naming the problem:

- an empty `Source`, or `Source` together with `SpriteAnimationClip` children;
- `Source` together with any `Cell*` attribute;
- no `AdditionalFiles` item matches, or more than one matches (use a longer `Source` path);
- the file is not valid JSON, has no `frames`, or has no `meta.frameTags` or no tags;
- a tag without a name, a duplicated tag name, a tag outside the frame list, or an unknown direction;
- a frame used by a tag that is rotated, has a missing or non-positive size or duration, or is trimmed and does not fit inside `sourceSize`.

## Constructors

| Name | Description |
| --- | --- |
| `SpriteAnimationSet(IEnumerable<SpriteAnimationClip>)` | Creates a version-1 set. |
| `SpriteAnimationSet(IEnumerable<SpriteAnimationClip>, long)` | Creates a set with an explicit positive version. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Clips` | `IReadOnlyList<SpriteAnimationClip>` | Defensively copied clip definitions. |
| `Version` | `long` | Positive caller-supplied definition version. |

## Methods

| Name | Description |
| --- | --- |
| `TryGetClip(string, out SpriteAnimationClip?)` | Resolves a clip by ordinal, case-sensitive name. Returns `false` for `null` or an unknown name. |

## Applies to

Cerneala retained 2D scenes.

## See also

- [SpriteAnimationClip](Cerneala.UI.Controls.SpriteAnimationClip.md)
- [SpriteAnimationStateChangeMode](Cerneala.UI.Controls.SpriteAnimationStateChangeMode.md)
