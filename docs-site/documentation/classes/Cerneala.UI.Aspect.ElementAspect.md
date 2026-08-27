# ElementAspect Class

## Definition
Namespace: `Cerneala.UI.Aspect`  
Assembly/Project: `Cerneala`  
Source: `UI/Aspect/ElementAspect.cs`

Per-element source of Aspect declarations resolved by the root-owned `AspectEngine`, with incremental editing support.

```csharp
public sealed class ElementAspect
```

## Examples
```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;

UIRoot root = new();
Button button = new();
var aspect = new ElementAspect([
    new ElementAspectValue(UIElement.OpacityProperty, 0.8f)
]);

root.VisualChildren.Add(button);
button.Aspect = aspect;
root.ProcessFrame();

aspect.SetValue(UIElement.OpacityProperty, 0.6f);
root.ProcessFrame();
```

## Remarks
Assignments are copied and exposed through a read-only list, and a property may appear only once. The default constructor targets `UIElement`; the named overload records an optional resource name and an explicit target type. Attaching an aspect to an incompatible element throws before it can participate in resolution.

`ElementAspect` does not write UI properties directly. It projects its assignments into an `AspectPackage` consumed with the root, application, and scoped packages by `AspectProcessor`. Winning values are published through the canonical `AspectBase` source. Replacing or clearing `UIElement.Aspect` invalidates the element's Aspect work; detach clears engine output, and reattach resolves the current aspect again.

`SetValue` adds or replaces one assignment, increments the local definition version, and queues Aspect invalidation for every attached consumer. Detached consumers retain dirty Aspect state for their next attachment. The engine performs the actual property comparison and invalidates only properties whose winning value changed. Setting an equal value is a no-op and schedules no frame work.

`IsConditional` records that generated conditional behavior accompanies the default declarations. It does not create a second property applicator.

The full generated-code constructor accepts immutable `ElementAspectCondition` entries, an optional behavior factory, and optional `AspectOrigin`. The factory may attach observations, event handlers, or Motion sessions and returns their lifetime. `UIElement` creates that lifetime when the aspect becomes effective, preserves lifecycle detach/reattach behavior, and disposes it when the aspect is replaced or cleared. Conditional declarations still win through `AspectEngine`; the behavior only changes `AspectConditionKey` state. Generated named and inline Aspects preserve their document and authoring kind without changing `Name` semantics or cascade.

## Constructors
| Name | Description |
| --- | --- |
| `ElementAspect(IReadOnlyList<ElementAspectValue>, bool)` | Creates an aspect; conditional mode defaults to `false`. |
| `ElementAspect(string?, Type, IReadOnlyList<ElementAspectValue>, bool)` | Creates an optionally named aspect for an explicit `UIElement`-derived target type. |
| `ElementAspect(string?, Type, IReadOnlyList<ElementAspectValue>, IReadOnlyList<ElementAspectCondition>, Func<UIElement, IDisposable?>?, bool, AspectOrigin?)` | Creates an aspect with engine-resolved conditions, an optional lifecycle behavior factory, and optional diagnostic origin metadata. |

## Properties
| Name | Description |
| --- | --- |
| `Name` | Optional diagnostic/resource name; blank input is normalized to `null`. |
| `TargetType` | Element type accepted by this aspect. |
| `Origin` | Immutable code/markup authoring metadata reported by diagnostics. |
| `DefaultValues` | Read-only snapshot of current default property assignments. |
| `IsConditional` | Whether condition processing is required. |
| `Conditions` | Immutable conditional declaration groups. |
| `ConditionKeys` | Condition keys in the same order as `Conditions`. |

## Methods
| Name | Description |
| --- | --- |
| `SetValue(UiProperty, object?)` | Adds or replaces one assignment, queues engine invalidation for consumers, and returns `true`; returns `false` when the value is unchanged. |

## Exceptions
| Member | Exception | Condition |
| --- | --- | --- |
| `SetValue` | `ArgumentNullException` | `property` is `null`. |
| `SetValue` | `ArgumentException` | `value` is invalid for the UI property. |
| `ElementAspect(string?, Type, ...)` | `ArgumentException` | `targetType` does not derive from `UIElement`. |

An element rejects the aspect with `InvalidOperationException` when its runtime type does not satisfy `TargetType` or an assignment targets an incompatible UI property.

## Applies to
Per-element Aspect resolution, generated named/inline markup, item-container aspects, and live Aspect editing.
