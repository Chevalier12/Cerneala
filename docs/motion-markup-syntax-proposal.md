# Motion Markup Language Reference

This document defines the implemented Cerneala Motion markup language. The
historical filename is retained so existing plan links do not break. Syntax in
the main sections is accepted and statically lowered by `Cerneala.SourceGen`;
the Deferred Surface section is explicitly not accepted.

The language includes inline and named `Aspect` behavior,
`Tween`/`Spring` resources, `@when`, `@if`, `@on`, immediate `@set`, and parallel
property starts inside `@animate` with optional `@from` and required `@to`. Values support typed
literals, `current`, the existing reactive sources, conditional expressions,
unqualified properties, and statically resolved Motion targets. The supported start
options are `retarget`, `holdOnComplete`, and `debugName`.

Motion assignments accept these target forms:

```xml
Opacity = 1;
$self.Opacity = 1;
$owner.Opacity = 1;
$Name.Opacity = 1;
$self.parts.$PART_Chrome.Opacity = 1;
$owner.parts.$PART_Chrome.Opacity = 1;
$Name.parts.$PART_Chrome.Opacity = 1;
```

An unqualified property and `$self.Property` both target the element to which the
Aspect is applied. `$owner` is available only while emitting a control template and
targets that template's owner. The `parts` segment explicitly enters the resolved
control template's part map; it is not a general-purpose child lookup. Named targets,
owners, parts, and properties are all validated and emitted statically by the generator,
without reflection.

Event subscriptions are emitted as direct `+=`/`-=` operations.
Each applied Aspect owns an independent lifecycle session: detach/dispose
unsubscribes events, cancels its grouped handles, and clears its Motion property
bindings. `MotionClip`, composition directives, keyframes, presence/layout,
scroll/input controllers, and named execution handles described below use the
same generated lifecycle session.

The language is constrained to markup and generator work over the current Motion runtime. Generator-owned validation, event wiring, resource construction, and lifetime bookkeeping are compile-time responsibilities. New animation semantics or runtime capabilities are not silently assumed. Anything that cannot be lowered to the current runtime is called out as deferred rather than presented as available behavior.

`@from` and `@to` are first-class language primitives. This keeps animation endpoints readable and lets the generator validate property pairs and their types at build time.

## Resources

`Tween` and `Spring` resources are generator-known spec declarations, not one polymorphic `MotionSpec` CLR instance shared across every property type. At each use, the generator specializes the declaration to the inferred `MotionSpec<T>` required by that property. This is what allows one `$Responsive` declaration to serve both `float` properties and a `Transform` layout correction without inventing an untyped property-animation adapter.

```xml
<Window.Resources>
    <Tween Name="QuickOut"
           Duration="180ms"
           Easing="EaseOut" />

    <Spring Name="Responsive"
            Stiffness="520"
            Damping="38"
            Mass="1" />

    <MotionClip Name="ClickPulse"
                TargetType="Border">
        @sequence
        {
            @animate with Tween(90ms, EaseOut)
            {
                @from { Scale = current; }
                @to   { Scale = 0.97; }
            }

            @animate with $Responsive
            {
                @to { Scale = 1; }
            }
        }
    </MotionClip>

    <Aspect Name="NavigationHover"
            TargetType="Border">

        @when IsMouseOver
        {
            @if IsMouseOver
            {
                @animate with $QuickOut
                {
                    @from
                    {
                        $HoverText.Opacity = 0;
                        $Indicator.ScaleX = 0;
                        $Label.TranslateX = 0;
                    }

                    @to
                    {
                        $HoverText.Opacity = 1;
                        $Indicator.ScaleX = 1;
                        $Label.TranslateX = 6 with $Responsive;
                    }
                }
            }
            @if !IsMouseOver
            {
                @animate with $QuickOut
                {
                    @to
                    {
                        $HoverText.Opacity = 0;
                        $Indicator.ScaleX = 0;
                        $Label.TranslateX = 0 with $Responsive;
                    }
                }
            }
        }

        @on Click
        {
            @run $ClickPulse;
        }
    </Aspect>
</Window.Resources>
```

Reusable behavior is attached through an Aspect:

```xml
<Border Aspect="$NavigationHover">
    ...
</Border>
```

`MotionClip` is not attached directly. It is a trigger-free generated animation recipe invoked by an Aspect through `@run`; there is no runtime `MotionClip` class.

## `@from` and `@to`

`@from` is optional. When it is omitted, Motion starts from the current visual value:

```xml
@animate with $QuickOut
{
    @to
    {
        Opacity = 1;
        TranslateY = 0;
    }
}
```

The current value can also be requested explicitly:

```xml
@from
{
    Opacity = current;
    TranslateY = current;
}
```

The generator enforces these rules:

- A property declared in `@from` must also appear in `@to`.
- The source and destination value types must match the property type.
- Every animated property must have a compatible interpolator.
- `@to` is required for a normal animation.
- Values in `@from` describe animation state; they are not applied as separate visible changes.

## Inline Aspect

```xml
<Border>
    <Border.Aspect>
        <Aspect>
            @when IsMouseOver
            {
                @animate with Tween(160ms, EaseOut)
                {
                    @from
                    {
                        Opacity = current;
                        Scale = current;
                    }

                    @to
                    {
                        Opacity = IsMouseOver ? 1 : 0.72;
                        Scale = IsMouseOver ? 1.04 : 1;
                    }
                }
            }
        </Aspect>
    </Border.Aspect>
</Border>
```

## Events

The general event form is:

```xml
@on EventName
{
    ...
}
```

`@on` acts only as an activation trigger for the enclosed declarative behavior. It does not introduce an implicit event-arguments symbol into the Motion language.

```xml
@on Click
{
    @animate with $Responsive
    {
        @from
        {
            Scale = current;
            Opacity = current;
        }

        @to
        {
            Scale = 1.05;
            Opacity = 1;
        }
    }
}
```

## Parallel and Sequential Composition

`@set` is an immediate execution leaf for discrete state changes that belong to a
Motion sequence, such as status text or an enum phase. It accepts one or more
assignable typed properties and completes synchronously, so the next child of an
`@sequence` starts in the same activation. It does not accept `current` or a Motion
spec because it performs no interpolation:

```xml
@sequence
{
    @set
    {
        $Status.Text = "ARMING";
        $Phase.Opacity = 1;
    }

    @animate with Tween(180ms, EaseOut)
    {
        @to { $Core.Scale = 1; }
    }
}
```

Properties in one `@animate` block run in parallel:

```xml
@animate
{
    @to
    {
        Opacity = 1 with $QuickOut;
        Scale = 1 with $Responsive;
        TranslateY = 0 with $Responsive;
    }
}
```

Explicit composition supports nested sequential and parallel groups:

```xml
@sequence
{
    @animate with $QuickOut
    {
        @from { Opacity = 0; }
        @to   { Opacity = 1; }
    }

    @parallel
    {
        @animate with $Responsive
        {
            @from { Scale = 0.8; }
            @to   { Scale = 1; }
        }

        @animate with Tween(300ms, EaseOut)
        {
            @from { $Ring.Opacity = 0; }
            @to   { $Ring.Opacity = 1; }
        }
    }
}
```

## Keyframes

`@keyframes` defines a deterministic timeline. Its children are ordinary `@animate` blocks positioned on explicit ranges of that shared timeline.

```xml
@keyframes duration 700ms
{
    @animate 0%..35% with EaseOut
    {
        @from
        {
            Opacity = 0;
        }

        @to
        {
            Opacity = 1;
        }
    }

    @animate 0%..55% with EaseOut
    {
        @from
        {
            Scale = 0.8;
        }

        @to
        {
            Scale = 1.12;
        }
    }

    @animate 55%..100% with EaseInOut
    {
        @from
        {
            Scale = 1.12;
        }

        @to
        {
            Scale = 1;
        }
    }
}
```

The example reads as three explicit timeline segments:

- `Opacity` animates between `0%` and `35%`.
- `Scale` animates from `0%` to `55%`.
- `Scale` returns to its final value from `55%` to `100%`.
- The complete timeline lasts `700ms`; segment durations are derived from their ranges.

Every child remains a normal `@animate`: its properties run in parallel, and its `@from` and `@to` blocks retain their normal meaning. `@keyframes` only controls when each animation occupies the shared timeline.

Unlike `@parallel`, keyframe segments can begin later or overlap partially:

```xml
@keyframes duration 900ms
{
    @animate 0%..40% with EaseOut
    {
        @from { Opacity = 0; }
        @to   { Opacity = 1; }
    }

    @animate 20%..70% with EaseInOut
    {
        @from { TranslateY = 24; }
        @to   { TranslateY = 0; }
    }

    @animate 65%..100% with EaseOut
    {
        @from { Scale = 0.96; }
        @to   { Scale = 1; }
    }
}
```

`@sequence` remains completion-driven: one child begins after the previous child completes. `@keyframes` has fixed offsets on one finite, automatically advancing timeline. The generator lowers each targeted property to a typed `KeyframesSpec<T>` and groups the resulting handles.

The current `KeyframesSpec<T>` sampler has no seek, reverse, or externally driven progress API. Therefore `progress`, scrubbing, and manual or scroll-driven keyframe timelines are deliberately unsupported. They require runtime work first.

`@keyframes` can participate in the same composition tree as ordinary animations:

```xml
@on Click
{
    @sequence
    {
        @animate with $Responsive
        {
            @to { Scale = 0.96; }
        }

        @keyframes duration 700ms
        {
            @animate 0%..55% with EaseOut
            {
                @from { Scale = 0.96; }
                @to   { Scale = 1.12; }
            }

            @animate 55%..100% with EaseInOut
            {
                @from { Scale = 1.12; }
                @to   { Scale = 1; }
            }
        }
    }
}
```

The generator enforces these rules:

- `@keyframes` requires a positive duration.
- Only ranged `@animate` blocks may appear directly inside `@keyframes`.
- Nested `@keyframes`, `@sequence`, and `@parallel` blocks are not allowed inside a keyframe timeline.
- Every range must be ordered, non-empty, and contained within `0%..100%`.
- Ranges may overlap when they animate different properties.
- Overlapping ranges that target the same element property are rejected; ranges may share an exact boundary.
- Gaps are allowed, and a property retains its last sampled value through a gap.
- Each ranged `@animate` must be deterministic in time. Tween easing is supported; Spring and Decay are rejected because the current `KeyframesSpec<T>` is a finite offset/value sampler rather than a container for nested specs.

## Motion Options

```xml
@animate
{
    retarget = PreserveProgress;
    holdOnComplete = true;
    debugName = "Tour/NavigationHover";

    @to
    {
        Scale = 1;
        Opacity = 1;
    }
}
```

These declarations lower to `MotionPropertyStartOptions`: `RetargetMode`, `HoldOnComplete`, and `DebugName`. `RetargetMode` currently accepts only `Restart` and `PreserveProgress`.

`MotionChannel` and `MotionPriority` types exist, but channels are not wired into property animation start options and `MotionPriority` currently has only `Normal`. `conflict`, per-animation reduced-motion policy, and a generic `cancel` start option do not exist in the runtime and are unsupported.

## Reusable Motion Clips

`MotionClip` stores a reusable, trigger-free animation recipe. It is invoked explicitly from an Aspect with `@run`; declaring it in resources does not start it or attach behavior to a control.

```xml
<Window.Resources>
    <MotionClip Name="EntranceClip"
                TargetType="Control">
        @animate with $Responsive
        {
            @from
            {
                Opacity = 0;
                TranslateY = 24;
                Scale = 0.96;
            }

            @to
            {
                Opacity = 1 with Tween(240ms, EaseOut);
                TranslateY = 0;
                Scale = 1;
            }
        }
    </MotionClip>

    <Aspect Name="Entrance"
            TargetType="Control">
        @on Loaded
        {
            @run $EntranceClip;
        }
    </Aspect>
</Window.Resources>
```

```xml
<Grid Aspect="$Entrance" />
```

A `MotionClip` must contain exactly one top-level body. That body must be one root execution node: `@set`, `@animate`, `@sequence`, `@parallel`, `@keyframes`, or the restricted `@stagger` form defined later. Two sibling execution bodies are invalid; authors must express their relationship explicitly through a single composition body. A clip cannot contain activation directives such as `@when` or `@on`, cannot invoke another clip with `@run`, and cannot be assigned directly to a control. `TargetType` gives the generator enough information to validate owner properties and targets at build time.

This is invalid because the clip has two top-level bodies:

```xml
<MotionClip Name="InvalidClip" TargetType="Control">
    @animate with $QuickOut
    {
        @to { Opacity = 1; }
    }

    @animate with $Responsive
    {
        @to { Scale = 1; }
    }
</MotionClip>
```

The relationship must be explicit inside one body:

```xml
<MotionClip Name="ValidClip" TargetType="Control">
    @parallel
    {
        @animate with $QuickOut
        {
            @to { Opacity = 1; }
        }

        @animate with $Responsive
        {
            @to { Scale = 1; }
        }
    }
</MotionClip>
```

Each `@run` creates an independent execution with its own handles and cancellation state. The resource remains immutable and owns no event subscriptions. `@run` is an execution leaf and may participate in `@sequence` and `@parallel` composition inside an Aspect.

## Language Model

```text
<Aspect>
    @when expression           react to observable state
    @if expression             conditionally activate a declarative branch
    @on EventName              react to an event
    @run $MotionClip           invoke a reusable animation recipe
    @animate                    execute an inline visual transition
    @from                      declare initial values
    @to                        declare destination values
    @keyframes duration        position ranged animations on a shared timeline
    @parallel                  compose concurrent motion
    @sequence                  compose sequential motion
</Aspect>

<MotionClip>
    exactly one of: @animate | @sequence | @parallel | @keyframes | @stagger
</MotionClip>
```

Aspect owns reusable behavior, activation, state observation, events, and lifecycle. Motion directives describe the visual work performed by that behavior. `MotionClip` exists only for reusable animation recipes that need to be invoked from one or more Aspects; it does not duplicate Aspect attachment or lifecycle semantics.

---

## Extended Motion Surface

This implemented surface builds on the model above instead of introducing a second animation language:

- Aspect owns activation, observation, input, presence, layout, and lifecycle.
- `MotionClip` owns only a reusable, trigger-free execution graph.
- Every composition directive has one explicit child relationship.
- There is no implicit `$event` object.
- CSS-like Motion expressions do not accept binding-mode suffixes.

### Repeat and Ping-Pong

The runtime implements repeat and ping-pong as typed spec wrappers around `TweenSpec<T>`, not as execution nodes. Markup therefore expresses them through `with`; they cannot wrap a sequence, parallel group, Spring, Decay, or an entire `MotionClip`.

```xml
@animate with Repeat(Tween(180ms, EaseOut), 3)
{
    @from { Opacity = 0.35; }
    @to   { Opacity = 1; }
}

@animate with PingPong(Tween(900ms, EaseInOut), 4)
{
    @from { TranslateY = -6; }
    @to   { TranslateY = 6; }
}
```

`Repeat(..., count)` performs exactly `count` tween cycles. `Repeat(..., forever)` maps to the runtime's null repeat count; under reduced motion it completes immediately at the destination. `PingPong(..., cycles)` requires a positive finite cycle count, and each cycle is one traversal.

The completed value follows the sampler: an even cycle count finishes at `@from`, while an odd cycle count finishes at `@to`.

### Stagger

The runtime's `MotionStagger` only calculates `offset * index`. It does not enumerate collections, schedule arbitrary execution graphs, or implement ordering modes. The initial markup form therefore supports a forward snapshot of targets and one Tween-based `@animate`; the generator applies `TweenSpec<T>.WithDelay(stagger.GetDelay(index))` to each item. The unqualified target inside the body is the current collection item.

```xml
@stagger target $NavigationItems
         each 45ms
{
    @animate with $QuickOut
    {
        @from
        {
            Opacity = 0;
            TranslateX = -10;
        }

        @to
        {
            Opacity = 1;
            TranslateX = 0;
        }
    }
}
```

The target collection is snapshotted when execution starts; mutations do not rewrite an in-flight stagger. Reverse and center-out ordering, Spring staggering, and staggering arbitrary sequences require capabilities beyond the current runtime and are deferred.

### Keyframe Steps and Holds

Stepped interpolation remains an easing choice on a normal ranged animation:

```xml
@keyframes duration 800ms
{
    @animate 0%..70% with Step(6, JumpEnd)
    {
        @from { TranslateX = 0; }
        @to   { TranslateX = 240; }
    }

    @animate 70%..100% hold
    {
        @from { Opacity = 1; }
        @to   { Opacity = 0; }
    }
}
```

`Step(count, JumpStart|JumpEnd|JumpBoth|JumpNone)` maps directly to `StepEasing` and divides the range into discrete samples. `hold` maps to `MotionKeyframe<T>.Hold`: it keeps the segment's `@from` value until the final boundary, where `@to` is applied. Both forms remain deterministic; neither introduces another kind of keyframe body.

`hold` affects only sampling inside that range. It does not decide whether the final property value remains after the animation completes. Persistence after completion is controlled separately by `holdOnComplete` on the property animation.

### Presence

Presence is an Aspect-owned declaration because it configures how the retained-tree coordinator handles attachment and removal. The current runtime exposes `PresenceOptions.FadeAndScale`: authors choose enter and exit specs, while the coordinator owns fixed opacity and presence-scale endpoints.

```xml
<Aspect Name="DialogPresence" TargetType="Control">
    @presence
    {
        enter = $QuickOut;
        exit = Tween(140ms, EaseIn);
        excludeInputWhileExiting = true;
    }
</Aspect>
```

Attachment animates `PresenceOpacity` from `0` to `1` and `PresenceScale` from `0.95` to `1`. Removal keeps the element available to rendering until opacity reaches `0`, while scale targets `0.95`, and then detaches it. `excludeInputWhileExiting` maps to the existing runtime option.

The generated Presence option must be assigned before the element is attached. Assigning Presence to an element that is already attached does not retroactively start the current runtime's entrance path.

The condition that inserts or removes the element belongs to normal template or `@if` structure. Presence does not observe an `IsOpen` expression itself. Custom `@enter`/`@exit` execution bodies, custom endpoints, and `initial = skip|animate` are not supported by the current runtime.

### Layout Motion

Layout Motion belongs to Aspect because it observes geometry before and after normal layout work. The current runtime requires a `LayoutMotionId` and one transform correction spec.

```xml
<Aspect Name="AnimatedLayout" TargetType="Control">
    @layout id $DataContext.LayoutId
            with $Responsive;
</Aspect>
```

The coordinator captures the old and new bounds, creates one inverse transform containing both translation and scale correction, then animates that correction to identity. Authors do not manually animate `X`, `Y`, `Width`, or `Height`.

The current runtime has no `position`, `size`, or `positionAndSize` modes, no coordinate-space option, no crossfade, and no programmable layout-transition sequence. A stable ID lets the same retained element preserve its previous snapshot when it is reparented; it does not create a shared-element transition between two different controls.

### Scroll Timelines

`@scroll` is an Aspect-owned linear mapping over the current `ScrollTimeline` API. The source must be an attached `ScrollViewer`. Vertical or horizontal progress is normalized and clamped over the viewer's entire scrollable extent.

```xml
@scroll source $Scroller
        axis vertical
{
    Opacity = 0..1;
    TranslateY = 48..0;
}
```

Each assignment lowers to `ScrollTimelineProgress.Map(from, to)` and a property binding. The runtime currently supports only `float` output properties and linear mapping. Layout-affecting properties require an explicit `allowLayout = true` opt-in. The generated Aspect owns timeline updates and bindings for its attachment lifetime.

Pixel ranges, easing, arbitrary input subranges, scroll-driven keyframes, and non-float values are not supported by the current runtime. They are deferred rather than smuggled into markup as imaginary features.

### Drag and Gesture Controllers

Input-driven Motion uses the existing semantic controllers rather than exposing a `$event` object to markup. Generated code may consume routed pointer arguments internally, but those arguments do not become Motion-language variables.

```xml
@drag with $Responsive;
```

```xml
@gesture press with $Responsive;
```

`@drag` targets `TranslateX` and `TranslateY` on the Aspect element, tracks pointer velocity, projects both axes by the runtime's fixed velocity factor on release, and uses the supplied `MotionSpec<float>` to settle. Pointer-capture loss returns both axes to their drag-start values with that spec.

`@gesture press` maps to the current `GestureMotionController`: press animates `Scale` to `0.97` and release animates it back to `1` using the supplied spec.

Separate source and target elements, axis selection, bounds, resistance, snapping, Decay release, pinch, and rotation gestures do not exist in the current controllers. They are unsupported.

### Execution Handles

Handles are explicit Aspect-instance slots. This is generator-owned bookkeeping over `MotionHandle` and `MotionGroupHandle`, allowing one trigger to cancel work started by another trigger without introducing global names.

```xml
<Aspect Name="SearchMotion" TargetType="Control">
    @handle SearchPulse;

    @on SearchStarted
    {
        @run $Searching as SearchPulse;
    }

    @on SearchCancelled
    {
        @cancel SearchPulse;
    }
</Aspect>
```

`@on` is resolved statically against the Aspect's `TargetType` and its base types. The example above is grammatically valid, but it compiles only if `Control` exposes `SearchStarted` and `SearchCancelled` as events. If those custom events are declared by a more specific control, the Aspect must name that type instead, for example `TargetType="SearchControl"`. The generator emits a build-time diagnostic when an event cannot be found or when the resolved member is not an event; it never discovers or injects code into similarly named methods. Event subscription and unsubscription are generated directly, without reflection.

`@run ... as Handle` first cancels the execution already stored in that slot, then stores the new one. `@cancel` cancels a composed clip through `MotionGroupHandle.Cancel()`, which in turn cancels child motions with their default `KeepCurrent` behavior. Handles cannot escape their Aspect instance and are cancelled when that Aspect detaches.

The runtime's leaf `MotionHandle` supports `KeepCurrent`, `Revert`, and `Complete` cancellation and has a `Complete()` method. `MotionGroupHandle`, which represents parallel and sequential composition, supports only parameterless cancellation and has no public completion operation. Therefore a generic `@complete` directive or selectable cancellation behavior for arbitrary `MotionClip` handles would require a unified runtime handle contract and is unsupported.

### Advanced Execution Options

Only options that reach the current property-animation APIs belong on `@animate`:

```xml
@animate with $Responsive
{
    retarget = PreserveProgress;
    holdOnComplete = true;
    debugName = "Navigation/Selection";

    @to
    {
        TranslateX = 0;
        Opacity = 1;
    }
}
```

`retarget` accepts `Restart` or `PreserveProgress`. `holdOnComplete` decides whether the animation value source remains after natural completion; otherwise the property returns to its non-animation value source. `debugName` feeds Motion diagnostics.

The markup default for `holdOnComplete` is `true`, matching `MotionAnimationBuilder<T>.With(...)`. Authors may set it to `false` when the animated value should be released after completion.

Spec-owned options stay on their resources or inline constructors:

```xml
<Tween Name="DelayedReveal"
       Duration="240ms"
       Delay="80ms"
       Easing="EaseOut"
       FillMode="Both" />

<Spring Name="Responsive"
        Stiffness="520"
        Damping="38"
        Mass="1"
        RestSpeed="0.01"
        RestDelta="0.01"
        VelocityMode="Preserve" />
```

`FillMode` is a Tween option with the runtime values `None`, `Backwards`, `Forwards`, and `Both`; it is not the property persistence switch. The current Tween sampler consults `Backwards` and `Both` while waiting through its delay. Post-completion persistence still belongs to `holdOnComplete`.

`VelocityMode` is a Spring option with `Preserve` and `Reset` values. The enum exists on `SpringSpec<T>`, but the current `MotionValue<T>` retarget path recreates samplers rather than calling `SpringSpec<T>`'s sampler retarget method, so markup must not claim that this option currently preserves velocity across property-animation retargets.

Decay markup is deferred in the current grammar. `<Decay>` resources and inline
`Decay(...)` constructors are rejected because every available property execution
requires an `@to` endpoint, while `DecaySpec<T>` ignores that endpoint. A future
contract must start from the current visual value plus a typed velocity without a
decorative destination. The following shape is therefore a design candidate, not
accepted markup:

```xml
<Decay Name="FloatFling"
       ValueType="float"
       InitialVelocity="1200"
       Deceleration="0.998"
       Min="0"
       Max="480"
       Bounce="$Responsive" />
```

If that candidate is implemented later, `Min` and `Max` must be supplied together
when `Bounce` is used, Decay must require a vector-capable mixer, and bounds must
require a comparable value type.

`MotionClearBehavior` exists only when explicitly clearing a property binding and accepts `RestoreBase` or `HoldCurrent`. The markup language has no generic `@clear` directive.

`MotionChannel` exists but is not consumed by `MotionPropertyStartOptions`. `MotionPriority` is present in start options but currently has only the `Normal` value. There is no runtime `conflict` option or per-animation reduced-motion override. Those names are intentionally absent rather than exposed as decorative options with no effect.

### Parameterized Motion Clips

Parameters keep reusable clips reusable without turning them into behavior or giving them activation rules.
Generic parameter types use XML-safe square brackets in markup; `MotionSpec[float]`
is resolved statically to the CLR type `MotionSpec<float>` by the generator.

```xml
<MotionClip Name="SlideIn" TargetType="Control">
    @parameter Distance: float = 24;
    @parameter Entrance: MotionSpec[float] = $QuickOut;

    @animate with Entrance
    {
        @from
        {
            Opacity = 0;
            TranslateY = Distance;
        }

        @to
        {
            Opacity = 1;
            TranslateY = 0;
        }
    }
</MotionClip>
```

```xml
@on Loaded
{
    @run $SlideIn(
        Distance = 40,
        Entrance = $Responsive
    );
}
```

Parameters are typed, immutable for one execution, and may have compile-time defaults. Arguments are validated by the generator. Parameters may influence values, targets, specs, counts, ranges, and options, but they cannot contain activation behavior or change the rule that a `MotionClip` has exactly one top-level body.

### Extended Composition Model

The execution grammar is:

```text
execution-body :=
      @set
    | @animate
    | @sequence      { execution-body+ }
    | @parallel      { execution-body+ }
    | @keyframes     { ranged-animate+ }
    | @stagger       { tween-animate }
    | @run $MotionClip

motion-spec :=
      Tween(...)
    | Spring(...)
    | Keyframes(...)
    | Decay(...)
    | Repeat(Tween(...), count|forever)
    | PingPong(Tween(...), cycles)
```

`@run` is an Aspect-only execution leaf. It is excluded from `MotionClip` bodies, preventing recursive clip graphs and keeping clip validation finite.

Aspect-only lifecycle and input directives are:

```text
@when        @if          @on
@presence    @layout      @scroll
@drag        @gesture     @handle
@cancel
```

`MotionClip` continues to allow exactly one top-level execution body. `@stagger` expands what that body can describe within the runtime-backed restriction above; it does not weaken the single-body rule or blur the boundary between animation recipes and Aspect-owned behavior.

## Static Lowering, Ownership, and Diagnostics

Motion markup is resolved at build time against the Aspect `TargetType`, named
named elements, properties, events, resource scopes, and `MotionSpec<T>` types. Generated
code uses typed property identifiers and direct event `+=`/`-=` operations. It
does not use reflection, `dynamic`, per-frame string lookup, or tick-created
closures.

Each applied Aspect owns one generated Motion session. Attach activates its
observations, direct event subscriptions, scroll bindings, and input
controllers. Detach or disposal unsubscribes them, cancels active executions
and named handle slots, clears owned animation bindings, and releases retained
owners. Reattach creates one fresh subscription/controller set. `@run ... as`
restarts a slot by canceling its previous execution before storing the new one;
`@cancel` is idempotent when the slot is empty.

| Diagnostic | Category |
| --- | --- |
| `CERNEALAUI020` | Directive syntax and grammar |
| `CERNEALAUI021` | Target, named element, or property resolution |
| `CERNEALAUI022` | Event resolution and concrete `TargetType` suggestions |
| `CERNEALAUI023` | Property, value, resource, and spec typing |
| `CERNEALAUI024` | Composition shape |
| `CERNEALAUI025` | Lifecycle-only directive placement or ownership |
| `CERNEALAUI026` | Runtime capability deliberately not supported |

Diagnostics select the exact directive, target, event, property, or resource
token in the `.cui.xml` source. Generated member names are deterministic. The
shared factory does not force `#line` mappings across interleaved XML nodes,
because a false C# mapping would make generated-code debugging worse.

## Grammar and Future Tooling Contract

`Cerneala.SourceGen/MotionMarkupLanguage.cs` is the machine-readable directive
table. The parser consumes that table when classifying Motion directives, and
`MachineReadableMotionDirectiveTableIsConsumedByContextDiagnostics` exercises
every entry. Tooling must consume this table or a generated derivative rather
than maintain a second handwritten keyword list.

Future editor tooling is not implemented by the generator. Its contract is:

- completion filters directives by Aspect, trigger, execution, and clip context;
- hover reports resolved property, event, target, parameter, and `MotionSpec<T>` types;
- go-to-definition resolves named Aspects, MotionClips, specs, handles, parameters, and `$Name` targets;
- rename updates statically resolved resource, handle, parameter, and named-element references;
- quick fixes use diagnostic IDs and exact spans, including concrete `TargetType` suggestions;
- generated-code preview shows deterministic C# without changing runtime semantics.

## Deferred Surface

The following names are designs or runtime work, not accepted Motion markup:
Decay resources/constructors; `@else`; `$event`; Motion transactions; direct
`Motion` resources; generic `@complete`/`@clear`; selectable group cancellation;
keyframe seek, reverse, scrubbing, or scroll drive; stagger ordering beyond the
forward snapshot form; custom Presence endpoints/bodies; layout modes,
crossfade, and shared-element transitions; non-float or eased scroll mappings;
and drag/gesture bounds, snapping, resistance, Decay release, pinch, or rotation.
