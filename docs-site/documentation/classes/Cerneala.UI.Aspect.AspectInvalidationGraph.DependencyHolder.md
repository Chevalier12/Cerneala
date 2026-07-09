# DependencyHolder Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectInvalidationGraph.cs`

Stores the `AspectDependencySet` value used by `AspectInvalidationGraph` entries.

```csharp
private sealed class DependencyHolder
```

Containing type:
`AspectInvalidationGraph`

Inheritance:
`object` -> `DependencyHolder`

## Examples

`DependencyHolder` is private to `AspectInvalidationGraph`; callers observe it through the graph's public tracking API.

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectInvalidationGraph graph = new();
Button button = new();
AspectDependencySet dependencies = new(catalogVersion: 2, environmentVersion: 5);

graph.Track(button, dependencies);

bool tracked = graph.TryGetDependencies(button, out AspectDependencySet stored);
bool sameSet = ReferenceEquals(dependencies, stored);
```

## Remarks

`DependencyHolder` is a private nested implementation detail used as the value type for `ConditionalWeakTable<UIElement, DependencyHolder>` inside `AspectInvalidationGraph`. It wraps an `AspectDependencySet` so the graph can associate dependency data with a `UIElement` without making the element stay alive only because it is tracked.

The constructor stores the supplied dependency set directly in the read-only `Dependencies` property. The holder does not copy, compare, validate, or mutate the dependency set. `AspectInvalidationGraph.Track` creates a new holder whenever an element is tracked, and `TryGetDependencies` returns the holder's stored dependency set when the element has an entry.

## Constructors

| Name | Description |
| --- | --- |
| `DependencyHolder(AspectDependencySet dependencies)` | Initializes the holder with the dependency set to expose through `Dependencies`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Dependencies` | `AspectDependencySet` | Gets the dependency set stored for the tracked element. |

## Applies to

Cerneala UI aspect dependency tracking for `UIElement` instances.

## See also

- `AspectInvalidationGraph`
- `AspectDependencySet`
- `AspectEngine`
