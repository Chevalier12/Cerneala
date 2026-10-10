# Aspect System

> Code: `UI/Aspect`, `UI/Invalidation/AspectQueue.cs`, `UI/Detective/AspectTrace.cs`, `Cerneala.SourceGen` (Aspect emission) · Verified at commit `0cb32300` (2026-10-10)

Aspect is Cerneala's typed retained design system. There is one runtime resolver and cascade: `AspectEngine`.

Code-first packages, application resources, element-scoped packages, named/inline markup, and `ItemsControl.ItemContainerAspect` all become `AspectRuleSet`/`AspectDeclaration` inputs to that engine. Authoring origin is diagnostics metadata, not a second property-store precedence band.

## One runtime data flow

```text
C# AspectPackage -----------+
Application AspectPackage --+
Scoped AspectPackage --------+--> AspectProcessor --> AspectCatalog --> AspectEngine
Generated @default ----------+         |                    |              |
Generated named/inline ------+         |                    |              +--> AspectBase values
ElementAspect ---------------+         |                    +--> templates/tokens/behaviors
                                       +--> AspectQueue + lifecycle cleanup
```

For each queued element, `AspectProcessor` composes:

1. the root registry, including `DefaultAspectPackage`;
2. `AspectPackage` values from application resources;
3. packages from resource scopes, outermost to innermost. The walk starts at the element itself, so the element's own `Resources` are the innermost scope, and it stops before the root;
4. the element's `ElementAspect`.

The processor caches composition per element by root catalog reference, application `ResourceDictionary` reference and version, each scope's `ResourceDictionary` reference and version in order, and `ElementAspect` reference and version (`AspectProcessor.CatalogState.Matches`). Resource replacement invalidates the affected root/subtree and rebuilds only stale snapshots.

`AspectEngine` then performs one resolution pass:

1. filter by target type and slot;
2. evaluate every relevant condition once and capture its dependencies;
3. resolve the token/computed value of each matching declaration;
4. compare matching declarations by layer, source/scope order, specificity, and declaration order;
5. publish (`AspectEngine.ApplyResolved`): write each winner through `UiPropertyValueSource.AspectBase`, and clear the `AspectBase` value of properties that had a winner last time and have none now. Losing declarations are never written;
6. track dependencies for future `AspectQueue` invalidation.

When a published value carries an `AspectMotion`, the engine wraps the write in `root.Motion.BeginTransaction(...)`, so the value animates instead of jumping. State-driven motion (`AspectMotionSource.State`) uses `MotionPriority.Interactive`; other motion uses `Normal`. See [motion.md](motion.md).

## Frame integration

The scheduler runs Aspect in `FramePhase.Aspect`, after `CommandState` and before `Measure`. `UIRoot` wires the phase processor to `AspectProcessor.Process`, and the work list is `UIRoot.AspectQueue`. Condition dependencies (state, property, variant, data), resource changes and theme changes enqueue elements. See [invalidation-and-frame.md](invalidation-and-frame.md).

An idle frame does not scan the tree or rerun Aspect resolution: when the scheduler has no work, it returns before running any phase.

## Code-first packages

Create reusable rules with the common runtime model:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Media;

AspectPackage package = AspectPackage.Create("App.Controls")
    .Components(components =>
    {
        components.AddRule(new AspectRuleSet(
            "button.base",
            AspectLayer.App,
            new AspectTarget(typeof(Button)),
            [
                new AspectDeclaration(
                    Control.BackgroundProperty,
                    AspectValue<Brush?>.Literal(
                        new SolidColorBrush(new Color(20, 22, 27))))
            ],
            declarationOrder: 0));
    })
    .Build();

UIRoot root = new();
root.AspectRegistry.Register(package);
```

Put a package in `Application.Resources` or `UIElement.Resources` to make it application- or scope-visible. Inner scopes outrank outer scopes within the same layer. Scope metadata is catalog-owned; reusable rules and packages are never mutated while catalogs are built.

## Tokens, states, variants, and slots

Tokens are typed `AspectValue` inputs:

In an element's runtime token environment, precedence runs from framework built-in package defaults, through theme projection, to explicit root-registry packages, application-resource packages, and scoped packages from outermost to nearest (including the element's own resources). `ThemeTokenBridge` projects over the framework's built-in defaults, not over application or scoped overrides. The theme remains the fallback when no explicit override exists; if a projected theme key is missing, the built-in default remains available. Explicit `null` token values also override the theme. Framework defaults are identified by internal registration provenance, not package names or authoring origin. Removing an override restores the next visible source on the next Aspect pass, and template token bindings consume that same effective environment.

```csharp
AspectToken<Color> accent = AspectToken.Color("app.accent");

AspectPackage package = AspectPackage.Create("App.Tokens")
    .Tokens(tokens => tokens.Set(accent, new Color(77, 240, 255)))
    .Build();
```

Conditions can observe typed states, variants, UI properties, data, generated signals, and compound logic:

```csharp
AspectTarget hoverTarget = new(
    typeof(Button),
    conditions:
    [
        AspectCondition.Property(UIElement.IsMouseOverProperty).Is(true)
    ]);
```

Component templates register typed `AspectSlot` values rather than string selectors:

```csharp
context.RegisterSlot(ButtonSlots.Content, presenter);
```

A slot rule matches the generated part but evaluates the owning control's state and variants. Template replacement detaches the old slot context and engine state.

## ElementAspect

`ElementAspect` is the per-instance adapter for named/inline Aspect and live editing. It builds a local package and queues the same engine; it never writes a UI property directly.

```csharp
Button button = new();
ElementAspect aspect = new(
    [new ElementAspectValue(UIElement.OpacityProperty, 0.8f)]);

button.Aspect = aspect;
root.VisualChildren.Add(button);
root.ProcessFrame();

aspect.SetValue(UIElement.OpacityProperty, 0.6f);
root.ProcessFrame();
```

An `ElementAspect` may be shared. `SetValue` updates its declaration, increments its version, and invalidates every attached consumer. Detach removes engine output and sidecar lifetime; reattach resolves against the current ancestry.

`ItemsControl.ItemContainerAspect` assigns this same adapter to realized containers. There is no container-specific resolver.

## Markup lowering

The source generator validates `.crn` syntax and types at build time, then emits common runtime objects:

- unnamed `@default` and `@template` resources become `AspectPackage` rules/templates;
- named Aspect and inline `<Control.Aspect>` become `ElementAspect` declarations;
- `@when`/`@if` assignments become engine rules guarded by `AspectCondition.Signal(AspectConditionKey)`;
- resource/computed values remain `AspectValue` inputs until engine resolution.

```xml
<StackPanel>
  <StackPanel.Resources>
    <Aspect TargetType="Button">
      @default { Opacity = 0.70; }
      @when IsMouseOver { Opacity = 1.00; }
    </Aspect>

    <Aspect Name="Primary" TargetType="Button">
      @default { FontSize = 18; }
    </Aspect>
  </StackPanel.Resources>

  <Button Aspect="$Primary" Content="Save" />
  <Button>
    <Button.Aspect>
      @default { Opacity = 0.85; }
    </Button.Aspect>
  </Button>
</StackPanel>
```

Generated observations update `AspectConditionKey` and invalidate `AspectQueue`; they do not write conditional Aspect values into another source band.

## Sidecars are not another Aspect runtime

Motion, event handlers, presence, layout, scroll, drag, gestures, and bindings remain owned by their subsystems. The one bridge is `AspectMotion`: the engine starts a Motion transaction for a published value (see the resolution pass above), and the Motion system owns the animation from there.

Context-free generated observations can be contributed as an `AspectBehavior` on a package. `AspectProcessor` target-filters these behaviors, attaches each occurrence once before engine resolution, and disposes its lifetime on package replacement or element detach. Context-dependent Motion/event sidecars are emitted at the concrete application site where names and template context exist.

A sidecar may update a condition signal or invoke Motion. It cannot select winning Aspect declarations or publish style values directly.

## Templates

Packages can contribute component and content templates alongside rules:

```csharp
AspectPackage package = AspectPackage.Create("App.Templates")
    .Components(components => components.AddTemplate(
        new ComponentTemplateDefinition(
            "App.Button",
            typeof(Button),
            ButtonTemplates.Modern)))
    .Build();

button.ComponentTemplateKey = "App.Button";
```

Named component templates resolve from the same visible catalog. Direct `ComponentTemplate` wins over `ComponentTemplateKey`; later equal-type definitions win. Template bindings and token bindings are lifecycle-owned by `ComponentTemplateInstance`.

`UiPropertyValueSource.TemplateBinding` is below child Aspect values. Framework chrome that explicitly projects an owner palette/value above a part's own Aspect uses the template-owned `TemplateOwnerBinding` source. This is not an Aspect authoring-origin band.

## Property-store precedence

The full rules are in [property-system.md](property-system.md). From highest to lowest, the stored sources are (`UiPropertyStore.EffectiveOrder`):

```text
Local
Animation
MarkupConditional
MarkupBase
TemplateOwnerBinding
AspectVisualState
AspectBase
TemplateBinding
Inherited
```

When none of them holds a value, the store falls back to the framework default and then to the property's default. `Default` is that fallback, not a stored source.

Application, scoped, named, inline, and code-first origin does not appear in this list. Their internal winner is already determined by `AspectEngine` before publication through the canonical Aspect source.

## Diagnostics

`UIRoot.Detective.CaptureAspect(element)` and `UIRoot.Detective.TraceAspect(element, property)` report the exact resolution path. They wrap the internal `AspectEngine.GetDiagnostics` and the public `AspectTrace.Capture`. The report includes:

- package and markup document;
- code/default/named/inline `AspectAuthoringKind`;
- root/application/`scope[n]`/element scope;
- target, layer, source order, specificity, and declaration order;
- structural and condition rejection reasons;
- the exact condition results and dependencies used by matching;
- winning and rejected declarations plus token traces.

```csharp
AspectDiagnostics.Snapshot diagnostics = root.Detective.CaptureAspect(button);

AspectTraceSnapshot trace = root.Detective.TraceAspect(
    button,
    Control.BackgroundProperty);
```

Condition predicates are not reevaluated for diagnostics. `Apply` retains a compact evaluation snapshot; public trace objects are materialized lazily on the first diagnostics request.

## Invariants

- A state change queues the element for the next frame's Aspect phase, in both directions. `tests/Cerneala.Tests/UI/Aspect/AspectCheckedExpandedTests.cs:PropertyChangeQueuesStateRuleForNextFrameInBothDirections` (`control` is in `root.AspectQueue.Snapshot()`, then `Opacity` is `0.5` and back to `1`).
- A theme color change reruns Aspect without measure or arrange. `tests/Cerneala.Tests/UI/Rendering/RenderStressBudgetTests.cs:ThemeColorChangeDoesNotMeasureLargeTreeWhenOnlyRenderAspectChanges`.
- An explicit `null` token overrides the theme; removing it restores the theme value, also through template token bindings. `tests/Cerneala.Tests/UI/Aspect/ThemeTokenPrecedenceTests.cs:NullIsAnExplicitOverrideAndTemplateBindingsObserveIt`.
- One condition in one apply is evaluated once; diagnostics report origin, scope, rejected rules and dependencies. `tests/Cerneala.Tests/UI/Detective/ModernAspectTraceTests.cs:TraceReportsOriginScopeRejectedRulesConditionsAndDependencies` (`Counters.ConditionEvaluations == 1`).
- State-driven `AspectMotion` animates in both directions. `tests/Cerneala.Tests/UI/Aspect/AspectEngineTests.cs:EngineAnimatesStateAspectMotionInBothDirections`.
- An `ElementAspect` behavior attaches on attach and is disposed on detach and on `Aspect = null`. `tests/Cerneala.Tests/UI/Aspect/AspectAuditRegressionTests.cs:ElementAspectBehaviorFollowsAttachDetachAndReattachLifecycle`.
- Dependency sets, condition results and diagnostics token traces are read-only copies of their inputs. `AspectAuditRegressionTests.cs:PublicAspectCollectionsAreStableReadOnlySnapshots`.
- The removed parallel markup Aspect runtime and the authoring-specific value sources stay removed. `tests/Cerneala.Tests/Architecture/ModernAspectArchitectureTests.cs:ParallelMarkupAspectRuntimeAndAuthoringSpecificSourcesStayRemoved` (a name check; the only Aspect phase processor in `UIRoot` is `AspectProcessor.Process`).
- Netestat: type/slot filtering skips conditions. The code checks structure before conditions (`AspectEngine.ResolveCore`), but the only structure test uses a rule without conditions.
- Netestat: package-level `AspectBehavior` disposal on package replacement (only generated source text is tested).
- Netestat: a local-Aspect change (`ElementAspect.SetValue`) enqueues the consumers.

## Known limitations

- `AspectRegistry.Packages` is a live read-only view, not a snapshot. `AspectCatalog` wraps the lists it receives (`AsReadOnly`, `ReadOnlyDictionary`) without copying them.
- Aspect diagnostics are captured on every `AspectEngine` apply, even when no one reads them (see [detective.md](detective.md)).
