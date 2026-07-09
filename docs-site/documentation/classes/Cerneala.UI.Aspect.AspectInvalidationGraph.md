# AspectInvalidationGraph Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectInvalidationGraph.cs`

Tracks the latest aspect dependency set associated with each `UIElement`.

```csharp
public sealed class AspectInvalidationGraph
```

Inheritance:
`object` -> `AspectInvalidationGraph`

## Examples

Track dependencies for an element and read them back:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectInvalidationGraph graph = new();
Button button = new();
AspectDependencySet dependencies = new(
    states: [AspectState.Hover],
    catalogVersion: 4,
    environmentVersion: 12);

graph.Track(button, dependencies);

if (graph.TryGetDependencies(button, out AspectDependencySet tracked))
{
    int catalogVersion = tracked.CatalogVersion;
}
```

Remove an element from tracking:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectInvalidationGraph graph = new();
Button button = new();

graph.Track(button, new AspectDependencySet());
graph.Untrack(button);

bool hasDependencies = graph.TryGetDependencies(button, out _);
```

## Remarks

`AspectInvalidationGraph` is a small per-element dependency store used by `AspectEngine`. After `AspectEngine.Apply` resolves and applies aspect values to a `UIElement`, the engine stores the resolved `AspectDependencySet` in this graph. `AspectEngine.GetDependencies` then reads the tracked set through `TryGetDependencies`.

Tracked entries are keyed by `UIElement` in a `ConditionalWeakTable`, so the graph does not keep an element alive solely because dependencies were tracked for it. Calling `Track` replaces any previous dependency set for the same element by removing the old entry and adding a new holder.

`TryGetDependencies` returns `false` and outputs a new empty `AspectDependencySet` when no entry is tracked. `Track` throws `ArgumentNullException` when `element` or `dependencySet` is `null`; `TryGetDependencies` throws when `element` is `null`. `Untrack` removes the element when present and otherwise has no effect.

## Constructors

| Name | Description |
| --- | --- |
| `AspectInvalidationGraph()` | Initializes an empty graph. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Track(UIElement element, AspectDependencySet dependencySet)` | `void` | Associates `dependencySet` with `element`, replacing any previously tracked set. Throws `ArgumentNullException` when either argument is `null`. |
| `TryGetDependencies(UIElement element, out AspectDependencySet dependencySet)` | `bool` | Gets the dependency set tracked for `element`. Returns `false` and outputs an empty set when the element is not tracked. Throws `ArgumentNullException` when `element` is `null`. |
| `Untrack(UIElement element)` | `void` | Removes the tracked dependency set for `element` when present. |

## Applies to

Cerneala UI aspect dependency tracking for `UIElement` instances.

## See also

- `AspectEngine`
- `AspectDependencySet`
- `ResolvedAspect`
- `UIElement`
