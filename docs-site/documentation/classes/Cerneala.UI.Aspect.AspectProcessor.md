# AspectProcessor Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectProcessor.cs`

Coordinates root-level aspect processing for `UIElement` instances by building the current aspect catalog, synchronizing token defaults, and delegating application and cleanup to an `AspectEngine`.

```csharp
public sealed class AspectProcessor
```

Inheritance:
`object` -> `AspectProcessor`

## Examples

Use the canonical processor exposed by a `UIRoot`:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;

UIRoot root = new();
Button button = new();

root.LogicalChildren.Add(button);
root.AspectProcessor.Process(button);

AspectDiagnostics.Snapshot diagnostics =
    root.AspectProcessor.Engine.GetDiagnostics(button);
```

Clear aspect state when manually coordinating element cleanup:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;

UIRoot root = new();
Button button = new();

root.LogicalChildren.Add(button);
root.AspectProcessor.Process(button);
root.AspectProcessor.Clear(button);
```

## Remarks

The processor is root-owned. `Process`, `Clear`, and environment synchronization verify `UIRoot.Relay` before reading or mutating retained Aspect state.

`AspectProcessor` is created by `UIRoot` and exposed through `UIRoot.AspectProcessor`. The root also wires `AspectProcessor.Process` into the aspect phase of its `UiFrameScheduler`, so normal frame processing uses this class rather than calling `AspectEngine` directly.

For each element, the processor composes one catalog from the root registry, packages stored in the application resource provider, packages stored in ancestor element resource dictionaries from outer to inner scope, and the element's `ElementAspect`. The composition is cached by root-catalog identity, resource-dictionary identity/version, ancestry, and element-aspect identity/version. Stable inputs reuse the same catalog snapshot.

Before applying declarations, the processor synchronizes matching `AspectBehavior` sidecars from the composed catalog. Each visible behavior occurrence attaches once; unchanged occurrences retain their lifetimes, while removed or replaced occurrences are disposed. This lifecycle path owns generated Motion/event/observation sidecars without introducing another Aspect matcher or cascade.

Cascade comparison uses the rule layer first, then the composed source/scope order, target specificity, and declaration order. This gives application packages precedence over root packages in the same layer, inner scopes precedence over outer scopes, and the runtime-layer `ElementAspect` precedence over reusable package rules. Markup/local UI property sources still participate independently in `UiPropertyStore` precedence.

Each composed catalog has an element-owned stable `AspectEnvironment`. When its catalog or active `Theme` changes, the processor rebuilds effective token values from the composed token defaults, overlays values projected by `ThemeTokenBridge`, and publishes them through that stable environment. Scoped token overrides therefore affect descendants without leaking into siblings.

The processor also synchronizes component and content template definitions from each composed catalog. A control's named component template is resolved from all visible scopes, with the closest assignable owner type winning and later definitions winning equal-specificity ties. Resource replacement invalidates the affected subtree, replaces template instances, and disposes old template roots. A `ContentPresenter` refreshes its content-template registry when its composed catalog version changes. Template-created slot elements are processed with their registered slot path, the owner component's variants and states, and the slot element's data context with the owner component recorded as data owner.

The engine receives the target element, current catalog, synchronized environment, root theme provider, and the element's `AspectVariantSet` when the element is a `Control`; non-control elements are processed with `AspectVariantSet.Empty`. It also receives an `AspectDataContext` built from the element's current `DataContext`, so `AspectCondition.Data` works on the root frame-processing path. The root property-mutation observer compares effective property changes with the engine's captured property dependencies and queues Aspect work even when the property metadata does not include `AffectsAspect`. Mutations performed by the engine during its own apply/clear operation are suppressed from this feedback path.

`Clear` disposes synchronized behavior lifetimes, delegates to `AspectEngine.Clear`, and discards the element's catalog/environment caches. Element lifecycle cleanup calls this during detach, which removes previously applied aspect-base values and tracked diagnostics/dependencies; reattach resolves the current ancestry and local aspect afresh.

The `Engine` property exposes the owned `AspectEngine` for diagnostics, dependency inspection, and low-level aspect operations. Mutating the engine directly can bypass the root-level catalog and token-default synchronization performed by `Process`.

## Constructors

| Name | Description |
| --- | --- |
| `AspectProcessor(UIRoot root)` | Initializes a processor bound to the specified root. Throws `ArgumentNullException` when `root` is `null`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Engine` | `AspectEngine` | Gets the processor-owned engine used to apply, clear, and inspect aspect state. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Process(UIElement element)` | `void` | Composes visible root/application/scoped/local Aspect sources, applies them through the engine, refreshes content/template context, and applies a control template when needed. Throws `ArgumentNullException` when `element` is `null`. |
| `Clear(UIElement element)` | `void` | Disposes package behavior lifetimes and clears aspect-base values and tracked aspect state for `element`. |

## Applies to

Cerneala UI root-level aspect processing for elements attached to a `UIRoot`.

## See also

- `AspectEngine`
- `AspectCatalog`
- `AspectBehavior`
- `AspectRegistry`
- `AspectEnvironment`
- `UIRoot`
- `Control.AspectVariants`
