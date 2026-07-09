# InvalidationRequest Class

## Definition
Namespace: `Cerneala.UI.Invalidation`

Assembly/Project: `Cerneala`

Source: `UI/Invalidation/InvalidationRequest.cs`

Describes a single invalidation request for a target `UIElement`, including the requested dirty flags, diagnostic reason, and optional property or resource context.

```csharp
public sealed class InvalidationRequest
```

Inheritance:
`object` -> `InvalidationRequest`

## Examples

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

UIElement element = new();

InvalidationRequest request = new(
    element,
    InvalidationFlags.Measure,
    "measure changed");

element.Invalidate(request);
```

The request can also describe a resource invalidation with explicit downstream effects:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;

UIElement element = new();

InvalidationRequest request = new(
    element,
    InvalidationFlags.Resource,
    "Resource changed",
    resourceEffects: InvalidationFlags.Render,
    affectsIntrinsicSize: false);
```

## Remarks

`InvalidationRequest` is the value object passed through the retained UI invalidation pipeline. `UIElement.Invalidate(InvalidationRequest)` requires the request target to match the element receiving the request. When the element is attached to a `UIRoot`, the root records the request in `InvalidationTrace`, updates root-level caches for semantic, render, and hit-test work, and then sends the request to `DirtyPropagation`.

`DirtyPropagation.GetEffectiveFlags` expands the requested flags before work is queued. Measure invalidation implies arrange and render work; arrange invalidation implies render work; text invalidation implies measure, arrange, and render work. Image invalidation depends on `AffectsIntrinsicSize`: intrinsic-size changes imply measure, arrange, and render work, while non-intrinsic image changes imply render work only.

When `Flags` includes `Resource`, the resource marker is removed from the effective flags and replaced by `ResourceEffects`; if `ResourceEffects` is `null`, render work is used. When `SourceProperty` has inherited-property metadata, inherited and subtree invalidation are added. When `SourceProperty` affects aspects, aspect work is added and render work is removed unless that property also affects render.

The constructor validates required inputs. `target` cannot be `null`, and `reason` cannot be `null`, empty, or whitespace.

## Constructors

| Name | Description |
| --- | --- |
| `InvalidationRequest(UIElement, InvalidationFlags, string, UiProperty?, InvalidationFlags?, bool)` | Initializes an invalidation request for a target element, requested flags, diagnostic reason, optional source property, optional resource effects, and intrinsic-size behavior. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Target` | `UIElement` | Gets the element that must receive the invalidation request. |
| `Flags` | `InvalidationFlags` | Gets the originally requested invalidation flags before effective propagation rules are applied. |
| `Reason` | `string` | Gets the non-empty diagnostic reason used by invalidation tracing and cache invalidation. |
| `SourceProperty` | `UiProperty?` | Gets the optional UI property that caused the invalidation. Property metadata can add inherited, subtree, aspect, or render behavior during propagation. |
| `ResourceEffects` | `InvalidationFlags?` | Gets the optional effective flags to use when `Flags` includes `Resource`; render work is used when this value is `null`. |
| `AffectsIntrinsicSize` | `bool` | Gets whether image or resource changes can affect intrinsic size. The default is `true`. |

## Exceptions

| Exception | Condition |
| --- | --- |
| `ArgumentNullException` | `target` is `null`. |
| `ArgumentException` | `reason` is `null`, empty, or whitespace. |

## Applies to

Cerneala retained UI invalidation requests, dirty propagation, layout scheduling, render scheduling, resource invalidation, and invalidation diagnostics.

## See also

- `Cerneala.UI.Invalidation.DirtyPropagation`
- `Cerneala.UI.Invalidation.InvalidationFlags`
- `Cerneala.UI.Elements.UIElement`
- `Cerneala.UI.Elements.UIRoot`
- `Cerneala.UI.Diagnostics.InvalidationTrace`
