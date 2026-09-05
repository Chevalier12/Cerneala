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

This fragment defines animation, not the image or destination: supply those through the sprite's existing image/destination APIs. `Name` is the resource key on the set and the state name on a clip. Frame `SourceX`/`SourceY`/`SourceWidth`/`SourceHeight` and `Duration` are immutable constructor inputs. No mutable frame collection, FPS setting, or automatic transition from `Attack` to `Idle` is implied.

For binding, declare the actual application data type on the markup root and use `AnimationState="$DataContext.AnimationState:OneWay"`; the application owns that notifying string property. Static unknown states and invalid definitions produce `CERNEALAUI016`; an unresolved runtime-bound state uses the target's static visual fallback.

The same resource can be referenced by [TileInstance2D](Cerneala.UI.Controls.TileInstance2D.md) within a real map/layer. Each promoted cell keeps independent playback. Select and style the individual sprite/tile with its normal Aspect, Motion, and Prism syntax; the resource set itself is not an animated UI target.

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
