# DirtyPropagation Class

## Definition
Namespace: `Cerneala.UI.Invalidation`

Assembly/Project: `Cerneala`

Source: `UI/Invalidation/DirtyPropagation.cs`

Expands invalidation requests into effective dirty flags and queues the affected elements for retained UI frame processing.

```csharp
public sealed class DirtyPropagation
```

Inheritance:
`object` -> `DirtyPropagation`

## Examples

```csharp
using Cerneala.UI.Diagnostics;
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

UIRoot root = new();
UIElement child = new();
root.VisualChildren.Add(child);

InvalidationRequest request = new(child, InvalidationFlags.Measure, "measure changed");

DirtyPropagation.Default.Propagate(
    request,
    root,
    root.LayoutQueue,
    root.InheritedPropertyQueue,
    root.AspectQueue,
    root.RenderQueue,
    root.HitTestQueue,
    root.Trace);

bool childNeedsMeasure = child.DirtyState.Has(InvalidationFlags.Measure);
bool renderQueued = root.RenderQueue.Count > 0;
```

## Remarks

`DirtyPropagation` is the shared policy object used by `UIRoot.Invalidate` to convert an `InvalidationRequest` into the concrete dirty state and queue entries needed by the retained frame scheduler.

`GetEffectiveFlags` normalizes request flags before work is queued. Measure invalidation also implies arrange and render work; arrange invalidation implies render work; text invalidation implies measure, arrange, and render work. Image invalidation is render-only when `InvalidationRequest.AffectsIntrinsicSize` is `false`, and measure/arrange/render when intrinsic size can change. Resource invalidation is replaced by `InvalidationRequest.ResourceEffects` when supplied, or render work when no explicit effects are supplied.

Property metadata can also expand the request. A source property with `UiPropertyOptions.Inherits` adds inherited-property and subtree propagation. A source property with `UiPropertyOptions.AffectsAspect` adds aspect work; if that property does not also affect render, render is removed from the effective flags.

`Propagate` marks the target element with the effective flags except for the `Subtree` modifier. Measure invalidation also marks visual ancestors with measure and arrange work until a layout boundary is reached. When the original request includes `Subtree`, the same non-`Subtree` flags are applied to visual descendants.

The queueing step enqueues only the work categories handled by this class: measure, arrange, inherited properties, aspects, render, and hit testing. Semantic invalidation can still be part of the dirty flags, but `UIRoot` handles semantic cache invalidation outside `DirtyPropagation`.

All public methods validate required reference parameters and throw `ArgumentNullException` for `null` inputs.

## Constructors

| Name | Description |
| --- | --- |
| `DirtyPropagation()` | Initializes a dirty propagation policy instance. |

## Properties

| Name | Description |
| --- | --- |
| `Default` | Gets the shared `DirtyPropagation` instance used by the retained UI invalidation path. |

## Methods

| Name | Description |
| --- | --- |
| `GetEffectiveFlags(InvalidationRequest)` | Returns the request flags after applying property metadata, layout/render implications, image/resource behavior, and input visual behavior. |
| `Propagate(InvalidationRequest, UIRoot, LayoutQueue, InheritedPropertyQueue, AspectQueue, RenderQueue, HitTestQueue, InvalidationTrace)` | Marks affected elements dirty, records propagation in the trace, and enqueues affected elements in the supplied frame queues. |

## Propagation Rules

| Input condition | Effective behavior |
| --- | --- |
| `Measure` | Adds `Arrange` and `Render`; propagates measure/arrange to visual ancestors until a layout boundary. |
| `Arrange` | Adds `Render`. |
| `Text` | Adds `Measure`, `Arrange`, and `Render`. |
| `Image` with `AffectsIntrinsicSize == true` | Adds `Measure`, `Arrange`, and `Render`. |
| `Image` with `AffectsIntrinsicSize == false` | Adds `Render`. |
| `Resource` | Removes `Resource` and adds `ResourceEffects`, or `Render` when `ResourceEffects` is `null`. |
| `InputVisual` | Adds `Render`. |
| Source property has `UiPropertyOptions.Inherits` | Adds `Inherited` and `Subtree`. |
| Source property has `UiPropertyOptions.AffectsAspect` | Adds `Aspect`; removes `Render` unless the property also has `AffectsRender`. |
| Original request has `Subtree` | Applies the effective non-`Subtree` flags to visual descendants. |

## Applies to

Cerneala retained UI invalidation, layout scheduling, render scheduling, inherited property propagation, aspect processing, and hit-test cache updates.

## See also

- `Cerneala.UI.Invalidation.InvalidationRequest`
- `Cerneala.UI.Invalidation.InvalidationFlags`
- `Cerneala.UI.Elements.UIRoot`
- `Cerneala.UI.Elements.UIElement`
- `Cerneala.UI.Diagnostics.InvalidationTrace`
