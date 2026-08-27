# Aspect System

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
3. packages from ancestor resource scopes, outermost to innermost;
4. the element's `ElementAspect`.

The processor caches composition by root catalog identity, resource-dictionary identity/version, ancestry, and `ElementAspect` identity/version. Resource replacement invalidates the affected root/subtree and rebuilds only stale snapshots.

`AspectEngine` then performs one resolution pass:

1. filter by target type and slot;
2. evaluate every relevant condition exactly once and capture its dependencies;
3. compare matching declarations by layer, source/scope order, specificity, and declaration order;
4. resolve token/computed values;
5. publish winners through `UiPropertyValueSource.AspectBase` and clear losers that no longer apply;
6. track dependencies for future `AspectQueue` invalidation.

An idle frame does not scan the tree or rerun Aspect resolution.

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

Motion, event handlers, presence, layout, scroll, drag, gestures, and bindings remain owned by their subsystems.

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

From highest to lowest, the concrete store order is:

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
Default
```

Application, scoped, named, inline, and code-first origin does not appear in this list. Their internal winner is already determined by `AspectEngine` before publication through the canonical Aspect source.

## Diagnostics

`AspectEngine.GetDiagnostics` and `AspectTrace.Capture` report the exact resolution path:

- package and markup document;
- code/default/named/inline `AspectAuthoringKind`;
- root/application/`scope[n]`/element scope;
- target, layer, source order, specificity, and declaration order;
- structural and condition rejection reasons;
- the exact condition results and dependencies used by matching;
- winning and rejected declarations plus token traces.

```csharp
AspectDiagnostics.Snapshot diagnostics =
    root.AspectProcessor.Engine.GetDiagnostics(button);

AspectTraceSnapshot trace = AspectTrace.Capture(
    button,
    Control.BackgroundProperty,
    diagnostics);
```

Condition predicates are not reevaluated for diagnostics. `Apply` retains a compact evaluation snapshot; public trace objects are materialized lazily on the first diagnostics request.

## Lifecycle invariants

- Type/slot filtering happens before conditions.
- Each condition node is evaluated once per relevant resolution.
- Catalog/package/registry collections are immutable snapshots.
- Package/document/scope origin is catalog-owned and stable.
- Resource, token, state, variant, data, and local-Aspect changes enqueue retained work.
- Package and element sidecars detach and dispose deterministically.
- Motion, input, event routing, and binding ownership remain outside `AspectEngine`.
- `UIRoot` does not discover markup Aspect executors or run a second matcher.
