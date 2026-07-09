# ResourceDependencyTracker.ResourceDependency Record

## Definition
Namespace: `Cerneala.UI.Resources`

Assembly/Project: `Cerneala`

Source: [`UI/Resources/ResourceDependencyTracker.cs`](../../UI/Resources/ResourceDependencyTracker.cs)

Stores the element, resource key, and invalidation metadata for one tracked resource dependency.

```csharp
private sealed record ResourceDependency(
    UIElement Owner,
    ResourceKey Key,
    InvalidationFlags Effects,
    bool AffectsIntrinsicSize);
```

Inheritance:
`object` -> `ResourceDependencyTracker.ResourceDependency`

Declaring type:
`ResourceDependencyTracker`

## Examples
`ResourceDependency` is private to `ResourceDependencyTracker`; callers create it indirectly by recording a dependency for a `UIElement`.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Resources;

ResourceDependencyTracker tracker = new();
UIElement owner = new();
ResourceId<ImageResource> logo = new("logo");

tracker.RecordDependency(
    owner,
    logo,
    InvalidationFlags.Measure | InvalidationFlags.Render,
    affectsIntrinsicSize: true);
```

## Remarks
`ResourceDependency` is the value stored in `ResourceDependencyTracker`'s per-resource dependency dictionary. `RecordDependency` creates one record for the owner and resource key being tracked, replacing any previous dependency for the same owner under that resource.

When `NotifyResourceChanged` receives a matching `ResourceChangedEventArgs`, the tracker reads the record's `Owner`, `Effects`, and `AffectsIntrinsicSize` values to create a public `ResourceDependencyChange`. Detached owners are removed before changes are produced, so records for elements no longer attached to a root do not emit invalidation work.

The `Key` value is used to group dependencies by resource type and resource id key. The record does not subscribe to providers, update versions, or invalidate elements by itself; that behavior belongs to `ResourceDependencyTracker`.

## Constructors
| Name | Description |
| --- | --- |
| `ResourceDependency(UIElement owner, ResourceKey key, InvalidationFlags effects, bool affectsIntrinsicSize)` | Initializes a dependency record with the owner, normalized resource key, invalidation effects, and intrinsic-size flag. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `Owner` | `UIElement` | Gets the element that recorded the dependency. |
| `Key` | `ResourceKey` | Gets the normalized resource type and resource id key tracked by this dependency. |
| `Effects` | `InvalidationFlags` | Gets the invalidation effects to report when the resource changes. |
| `AffectsIntrinsicSize` | `bool` | Gets whether the resource change affects the owner's intrinsic size. |

## Applies to
Cerneala UI resource dependency tracking for retained rendering and layout invalidation.

## See also
- [`ResourceDependencyTracker`](Cerneala.UI.Resources.ResourceDependencyTracker.md)
- [`ResourceDependencyChange`](../../UI/Resources/ResourceDependencyTracker.cs)
- [`ResourceId<T>`](Cerneala.UI.Resources.ResourceIdT.md)
- [`InvalidationFlags`](../../UI/Invalidation/InvalidationFlags.cs)
