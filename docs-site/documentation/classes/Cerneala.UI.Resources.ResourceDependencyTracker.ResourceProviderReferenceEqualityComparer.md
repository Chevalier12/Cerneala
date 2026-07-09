# ResourceDependencyTracker.ResourceProviderReferenceEqualityComparer Class

## Definition
Namespace: `Cerneala.UI.Resources`

Assembly/Project: `Cerneala`

Source: [`UI/Resources/ResourceDependencyTracker.cs`](../../UI/Resources/ResourceDependencyTracker.cs)

Compares observable resource providers by reference identity for provider tracking inside `ResourceDependencyTracker`.

```csharp
private sealed class ResourceProviderReferenceEqualityComparer : IEqualityComparer<IObservableResourceProvider>
```

Inheritance:
`object` -> `ResourceDependencyTracker.ResourceProviderReferenceEqualityComparer`

Declaring type:
`ResourceDependencyTracker`

Implements:
`IEqualityComparer<IObservableResourceProvider>`

## Examples

`ResourceProviderReferenceEqualityComparer` is private to `ResourceDependencyTracker`; callers use `Track` instead of creating the comparer directly. Tracking the same provider instance twice subscribes only once to `ResourceChanged`.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Resources;

ResourceStore store = new();
ResourceDependencyTracker tracker = new();

tracker.Track(store);
tracker.Track(store);

UIRoot root = new();
UIElement owner = new();
ResourceId<string> greeting = new("Greeting");

root.VisualChildren.Add(owner);
tracker.RecordDependency(owner, greeting, InvalidationFlags.Render);

store.SetResource(greeting, "Hello");

long ownerVersion = tracker.GetDependencyVersion(owner);
// ownerVersion is 1 because the provider was tracked once by object identity.
```

## Remarks

`ResourceProviderReferenceEqualityComparer` backs the tracker's internal `HashSet<IObservableResourceProvider>`. It treats two providers as equal only when they are the same object reference, regardless of any equality members the provider type may override.

The comparer keeps `ResourceDependencyTracker.Track` idempotent for the same provider instance. When `Track` sees an already tracked provider, it returns before adding another `ResourceChanged` subscription. Different provider instances remain distinct even if their own `Equals` implementations would report equality.

`GetHashCode` uses `RuntimeHelpers.GetHashCode`, so the hash code follows object identity rather than provider-defined value equality.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Instance` | `ResourceProviderReferenceEqualityComparer` | Gets the singleton comparer instance used by the tracker's provider set. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Equals(IObservableResourceProvider? x, IObservableResourceProvider? y)` | `bool` | Returns `true` only when `x` and `y` are the same object reference. |
| `GetHashCode(IObservableResourceProvider obj)` | `int` | Returns an identity-based hash code for `obj`. |

## Applies To

Cerneala UI resource dependency tracking internals.

## See Also

- [`ResourceDependencyTracker`](Cerneala.UI.Resources.ResourceDependencyTracker.md)
- `Cerneala.UI.Resources.IObservableResourceProvider`
- `Cerneala.UI.Resources.ResourceChangedEventArgs`
