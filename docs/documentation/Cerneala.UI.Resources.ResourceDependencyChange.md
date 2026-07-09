# ResourceDependencyChange Class

## Definition
Namespace: `Cerneala.UI.Resources`

Assembly/Project: `Cerneala`

Source: `UI/Resources/ResourceDependencyTracker.cs`

Describes the invalidation work associated with one attached `UIElement` after a tracked resource changes.

```csharp
public sealed record ResourceDependencyChange(
    UIElement Owner,
    InvalidationFlags Effects,
    bool AffectsIntrinsicSize)
```

Inheritance:
`object` -> `ResourceDependencyChange`

## Examples

Process dependency changes returned by `ResourceDependencyTracker.NotifyResourceChanged`:

```csharp
using Cerneala.UI.Invalidation;
using Cerneala.UI.Resources;

ResourceChangedEventArgs args = new(
    typeof(ImageResource),
    "Logo",
    oldValue: null,
    newValue: null,
    version: 2);

foreach (ResourceDependencyChange change in tracker.NotifyResourceChanged(args))
{
    change.Owner.Invalidate(new InvalidationRequest(
        change.Owner,
        InvalidationFlags.Resource,
        "Resource changed",
        resourceEffects: change.Effects,
        affectsIntrinsicSize: change.AffectsIntrinsicSize));
}
```

## Remarks

`ResourceDependencyChange` is returned by `ResourceDependencyTracker.NotifyResourceChanged` for each still-attached owner that depends on the changed resource. Detached owners are cleaned up by the tracker and do not produce change records.

The `Owner` identifies the element to invalidate. `Effects` carries the original invalidation flags recorded for the resource dependency, such as `Measure`, `Arrange`, or `Render`. `AffectsIntrinsicSize` tells the invalidation pipeline whether the resource change can alter intrinsic size.

`UIRoot` consumes these records by incrementing layout and render versions when required, then invalidating the owner with an `InvalidationRequest` whose `ResourceEffects` and `AffectsIntrinsicSize` values come from the change record.

## Constructors

| Name | Description |
| --- | --- |
| `ResourceDependencyChange(UIElement Owner, InvalidationFlags Effects, bool AffectsIntrinsicSize)` | Initializes a resource dependency change for an owner, the invalidation effects to apply, and intrinsic-size behavior. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Owner` | `UIElement` | Gets the attached element whose tracked resource dependency changed. |
| `Effects` | `InvalidationFlags` | Gets the invalidation effects recorded for the dependency. |
| `AffectsIntrinsicSize` | `bool` | Gets whether the resource change can affect intrinsic size. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Deconstruct(out UIElement Owner, out InvalidationFlags Effects, out bool AffectsIntrinsicSize)` | Deconstructs the record into its owner, invalidation effects, and intrinsic-size flag. |

## Applies to

Cerneala UI resource dependency tracking and root-level resource invalidation.

## See also

- `Cerneala.UI.Resources.ResourceDependencyTracker`
- `Cerneala.UI.Resources.ResourceChangedEventArgs`
- `Cerneala.UI.Invalidation.InvalidationRequest`
- `Cerneala.UI.Invalidation.InvalidationFlags`
- `Cerneala.UI.Elements.UIRoot`
