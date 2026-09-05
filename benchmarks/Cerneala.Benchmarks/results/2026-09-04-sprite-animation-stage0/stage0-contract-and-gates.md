# Sprite animation stage 0: inventory and frozen contract

Date: 2026-09-04

## RoslynIndexer inventory

The index was valid before the inventory (3,741 documents, 99,424 symbols, 404,915 references). The production callers/consumers are:

| Contract | Owners and consumers |
| --- | --- |
| `FrameTime` | `UiHost` obtains the elapsed delta, passes it to `TimeSensitiveRenderInvalidator` and input, and records it in `UiFrame`. `RenderSurface2D` stores the delta and supplies it to `RenderSurface2DFrame`. `MonoGameRenderSurface2DSession` also constructs a frame with the supplied delta. Text input/caret and repeat-button paths consume the same delta independently. |
| `UpdateRenderTime` | The shared contract is `ITimeSensitiveRenderElement`; `TimeSensitiveRenderInvalidator` traverses the UI tree. Implementations are `RenderSurface2D`, `TextBox`, `PasswordBox`, `TextInputCore` and `TextInputViewport`. Sprite animation remains aggregated by `RenderSurface2D`; it does not add one time-sensitive UI node or timer per sprite. |
| `RedrawMode` | `RenderSurface2D` owns the property. `Continuous` currently invalidates each active frame; `OnDemand` is used by the drawing playground and invalidates on content changes. `Tetrisish` explicitly uses `Continuous`. |
| `Sprite2D.SourceRect` | `Sprite2D.Record` forwards it to `DrawImageGeometry`/`RenderSurface2DFrame`; sprite bounds use the same resolved source. The command is consumed by both drawing backends. `TileInstance2D` has its own optional source override, and promoted recording resolves it against `TileDefinition2D.SourceRect`. |
| flip | `Sprite2D.Flip` is `RenderSurface2DSpriteFlip` and is mapped once by `RenderSurface2DFrame`. Tiles use `TileFlip2D`, resolved by `TileMap2D`, then mapped to `DrawImageFlip`. Both backends consume only the final command flip. |

## API decision

Animation extends `Sprite2D`; there is no `AnimatedSprite2D`.

This keeps one image-resolution, bounds, Prism and draw-command path. A dedicated node would either duplicate the sealed sprite implementation or force inheritance solely to recover the same recording path. Static sprites remain valid: without both `Animations` and a resolvable `AnimationState`, existing `SourceRect` and `Flip` are the complete presentation contract.

The public definition model is:

- `SpriteAnimationFrame(DrawRect sourceRect, TimeSpan duration, RenderSurface2DSpriteFlip flip = None)`, immutable, with read-only `SourceRect`, `Duration` and `Flip`;
- `SpriteAnimationClip(string name, IEnumerable<SpriteAnimationFrame> frames, bool isLooping = true)`, immutable/versioned, with non-empty unique name, at least one frame and derived total `Duration`;
- `SpriteAnimationSet(IEnumerable<SpriteAnimationClip> clips)`, immutable/versioned, with unique ordinal clip names;
- `SpriteAnimationStateChangeMode.Restart` and `.Resume`;
- `Animations`, `AnimationState`, `AnimationPlaybackRate`, `IsAnimationPaused`, `AnimationStateChangeMode` and `RestartAnimation()` on `Sprite2D` and promoted `TileInstance2D`.

Per-frame duration is the only timing truth. `FramesPerSecond` is excluded. Ping-pong is excluded from v1. A frame source rectangle must have finite coordinates and strictly positive finite width/height. Duration must be positive. Playback rate must be finite and non-negative; zero behaves as paused without changing `IsAnimationPaused`, while a negative value is rejected.

## Markup contract

Markup uses a resource and immutable nested declarations. The source generator constructs the immutable objects; it does not expose mutable `Frames` or `Clips` collections.

```xml
<RenderSurface2D DataType="Game.WorldState"
                 xmlns:resources="clr-namespace:Cerneala.UI.Resources;assembly=Cerneala">
  <RenderSurface2D.Resources>
    <resources:ImageResource Name="HeroAtlas" Source="Assets/hero.png" />
    <SpriteAnimationSet Name="HeroAnimations">
      <SpriteAnimationClip Name="Idle" IsLooping="true">
        <SpriteAnimationFrame SourceX="0" SourceY="0"
                              SourceWidth="16" SourceHeight="16"
                              Duration="240ms" />
      </SpriteAnimationClip>
      <SpriteAnimationClip Name="Walk" IsLooping="true">
        <SpriteAnimationFrame SourceX="0" SourceY="16"
                              SourceWidth="16" SourceHeight="16"
                              Duration="90ms" Flip="Horizontal" />
      </SpriteAnimationClip>
      <SpriteAnimationClip Name="Attack" IsLooping="false">
        <SpriteAnimationFrame SourceX="0" SourceY="32"
                              SourceWidth="16" SourceHeight="16"
                              Duration="60ms" />
      </SpriteAnimationClip>
    </SpriteAnimationSet>
  </RenderSurface2D.Resources>
  <RenderSurface2D.Scene>
    <Scene2D>
      <Sprite2D SourceResourceId="$HeroAtlas"
                Animations="$HeroAnimations"
                AnimationState="$DataContext.AnimationState:OneWay"
                AnimationPlaybackRate="1"
                AnimationStateChangeMode="Resume">
        <Sprite2D.Aspect>
          <Aspect>
            @when $DataContext.IsAttacking
            {
              @set { AnimationState = "Attack"; Tint = "#FFFF8080"; }
            }
            @on Loaded
            {
              @animate with Tween(100ms)
              {
                @to { TranslateX = 8; Opacity = 0.75; AnimationPlaybackRate = 1.25; }
              }
            }
          </Aspect>
        </Sprite2D.Aspect>
        @prism
        {
          @layer HeroContent { Opacity = 1; @filter Blur { Radius = 1; } }
        }
      </Sprite2D>
    </Scene2D>
  </RenderSurface2D.Scene>
</RenderSurface2D>
```

`SourceX`, `SourceY`, `SourceWidth` and `SourceHeight` are generator attributes for the real `DrawRect` constructor argument; they are not fictional runtime properties and are legal only on `SpriteAnimationFrame` markup. `Duration` uses the existing Cerneala duration grammar. A promoted `TileInstance2D` uses the same `Animations` resource and state properties.

## Time, state and lifecycle policy

- `RenderSurface2DFrame.FrameTime` is an elapsed delta, not an absolute timestamp. Every attached instance accumulates scaled elapsed ticks; the pure sampler receives `(clip, accumulated elapsed, playback rate)` and uses integer tick arithmetic.
- Intervals are left-closed/right-open. `t=0` selects frame 0; an exact frame end selects the next frame. Exact loop duration wraps to frame 0. A non-loop clip clamps to its final frame and becomes completed at its total duration.
- Large positive jumps reduce modulo total duration for loops and clamp for finite clips. Sampling does not iterate once per skipped cycle. Regressive/negative elapsed is rejected. Tick overflow is saturated before sampling.
- `Restart` resets the entered state to frame 0. `Resume` saves elapsed per clip name and restores it when that state is re-entered. `RestartAnimation()` resets only the current state.
- Changing `Source`, `SourceResourceId`, `DataContext`, visibility or culling does not reset progress. A binding that changes `Animations` or `AnimationState` follows those properties' normal replacement/state-change rules. Replacing `Animations` resets all saved progress because definitions/versions changed.
- Detach stops updates and preserves elapsed state; reattach resumes. A hidden or culled attached node continues to advance but emits no draw command. Pause, rate zero and completed non-loop clips stop requesting presentation changes.
- In `OnDemand`, elapsed time is accumulated each host tick but the surface invalidates only when an effective source rectangle/flip changes. `Continuous` keeps its existing every-frame invalidation contract even for static or completed content.

## Ownership matrix

| Property/channel | Aspect | Motion | Frame sampler |
| --- | --- | --- | --- |
| `Animations`, `AnimationState`, pause, state-change mode | discrete set/binding | rejected as non-animatable | reads resolved values |
| `AnimationPlaybackRate` | set/binding | animatable numeric channel | consumes the effective sampled rate; Motion is not a second frame sampler |
| `SourceRect` | static fallback/base | rejected while no dedicated ownership contract exists | active frame owns effective source rectangle without mutating the base property |
| `Flip` | base flip | rejected as discrete/non-interpolable | effective flip is base XOR frame flip, independently for horizontal and vertical |
| tint, opacity, transforms | supported | supported through existing mixers/properties | never owns them |
| Prism | attachable to sprite/tile | parameters retain their existing ownership | encloses only the current frame draw and uses its transformed destination bounds; collider/picking geometry is unchanged |

The source generator must diagnose Motion targeting `Animations`, clip/frame collections, `AnimationState`, pause, state-change mode, `SourceRect` or `Flip`. It must also diagnose duplicate clip names, a statically missing state, invalid source rectangles and non-positive durations at the offending source span.

## RED gate

The permanent tests are tagged `SpriteAnimationStage=0` in the core and SourceGen projects. They freeze boundary sampling, large jumps, loop/non-loop, pause/rates, invalidation, attach/detach/reattach, atlas and DataContext preservation, restart/resume, shared definitions, Aspect/Motion/Prism ownership and promoted-tile reuse. At this checkpoint they must compile and fail only because the animation types/properties/markup capability do not exist.

Observed RED results:

- core: 7/7 tests fail at the explicit guard `RED: approved sprite-animation capability is absent: SpriteAnimationFrame`; the project and fixtures compile;
- SourceGen: 5/5 tests fail because `SpriteAnimationSet` is unsupported, animation properties are unsupported, or the reserved located validation diagnostic `CERNEALAUI016` is absent; the existing name-scope validator additionally recognizes the deliberate duplicate `Idle` name;
- results are archived as `stage0-core-red.trx` and `stage0-sourcegen-red.trx` beside this file.
