# ResourceDependencyTracker Class

## Definition
Namespace: `Cerneala.UI.Resources`

Assembly/Project: `Cerneala`

Source: `UI/Resources/ResourceDependencyTracker.cs`

Tracks which UI elements depend on resources and reports invalidation work when resources change.

```csharp
public sealed class ResourceDependencyTracker
```

Inheritance:
`object` -> `ResourceDependencyTracker`

## Examples

Track a provider and record that an element depends on a resource:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Resources;

ResourceDependencyTracker tracker = new();
tracker.Track(provider);

ResourceId<ImageResource> logo = new("logo");
UIElement owner = GetElement();

tracker.RecordDependency(owner, logo, InvalidationFlags.Render);

long version = tracker.GetDependencyVersion(owner);
```

Handle a direct resource change notification:

```csharp
using Cerneala.UI.Resources;

ResourceChangedEventArgs args = new(
    typeof(ImageResource),
    "logo",
    oldValue: null,
    newValue: null,
    version: 2);

IReadOnlyList<ResourceDependencyChange> changes = tracker.NotifyResourceChanged(args);
```

## Remarks

`ResourceDependencyTracker` stores dependencies by resource type and key. Each dependency records the owning `UIElement`, the invalidation effects to apply, and whether the change affects intrinsic size.

`Track` subscribes once to an `IObservableResourceProvider` and forwards provider `ResourceChanged` events to `NotifyResourceChanged`. When a resource changes, the tracker updates that resource version, removes detached owners from the affected dependency set, increments dependency versions for remaining owners, and returns `ResourceDependencyChange` records.

Owners are compared by reference identity. `RemoveOwner` removes one owner from all dependency sets and clears its stored dependency version.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Track(IObservableResourceProvider provider)` | `void` | Subscribes to resource change notifications from a provider if it is not already tracked. |
| `RecordDependency<T>(UIElement owner, ResourceId<T> id)` | `void` | Records a render dependency for an owner and resource id. |
| `RecordDependency<T>(UIElement owner, ResourceId<T> id, InvalidationFlags effects, bool affectsIntrinsicSize = true)` | `void` | Records a dependency with explicit invalidation effects and intrinsic-size behavior. |
| `GetDependencyVersion(UIElement owner)` | `long` | Gets the dependency version for an owner, or `0` when none is recorded. |
| `GetResourceVersion<T>(ResourceId<T> id)` | `long` | Gets the last known version for a resource, or `0` when it has not changed. |
| `GetDependents<T>(ResourceId<T> id)` | `IReadOnlyCollection<UIElement>` | Gets the current dependent elements for a resource id. |
| `RemoveOwner(UIElement owner)` | `void` | Removes an owner from every tracked dependency set. |
| `NotifyResourceChanged(ResourceChangedEventArgs args)` | `IReadOnlyList<ResourceDependencyChange>` | Applies a resource change notification and returns dependency changes for attached owners. |

## Applies To

Cerneala UI resource invalidation and retained rendering integration.

## See Also

- `Cerneala.UI.Resources.ResourceChangedEventArgs`
- `Cerneala.UI.Resources.ResourceDependencyChange`
- `Cerneala.UI.Resources.IObservableResourceProvider`
- `Cerneala.UI.Invalidation.InvalidationFlags`
