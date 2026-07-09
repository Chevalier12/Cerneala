# ResourceDependencyTracker.ReferenceEqualityComparer Class

## Definition
Namespace: `Cerneala.UI.Resources`

Assembly/Project: `Cerneala`

Source: `UI/Resources/ResourceDependencyTracker.cs`

Compares `UIElement` instances by reference identity for dependency tracking dictionaries.

```csharp
private sealed class ReferenceEqualityComparer : IEqualityComparer<UIElement>
```

Inheritance:
`object` -> `ReferenceEqualityComparer`

Implements:
`IEqualityComparer<UIElement>`

## Examples

`ReferenceEqualityComparer` is private to `ResourceDependencyTracker`. Public code uses the tracker APIs instead:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Resources;

ResourceDependencyTracker tracker = new();
UIElement owner = GetElement();

tracker.RecordDependency(owner, new ResourceId<ImageResource>("logo"));
long version = tracker.GetDependencyVersion(owner);
```

## Remarks

The comparer returns equality only when two `UIElement` references are the same object. It computes hash codes with `RuntimeHelpers.GetHashCode`, which is based on object identity rather than overridden equality behavior.

`ResourceDependencyTracker` uses this comparer for owner dictionaries so dependency ownership is tied to a specific element instance.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Instance` | `ReferenceEqualityComparer` | Gets the singleton comparer instance used by the tracker. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Equals(UIElement? x, UIElement? y)` | `bool` | Returns `true` only when `x` and `y` are the same object reference. |
| `GetHashCode(UIElement obj)` | `int` | Returns an identity-based hash code for `obj`. |

## Applies To

Cerneala UI resource dependency tracking internals.

## See Also

- `Cerneala.UI.Resources.ResourceDependencyTracker`
- `Cerneala.UI.Elements.UIElement`
