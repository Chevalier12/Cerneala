# ResourceDependencyTracker.ResourceKey Class

## Definition
Namespace: `Cerneala.UI.Resources`

Assembly/Project: `Cerneala`

Source: `UI/Resources/ResourceDependencyTracker.cs`

Represents the internal typed key used by `ResourceDependencyTracker` to group resource dependencies.

```csharp
private readonly record struct ResourceKey(Type Type, string Key)
```

Inheritance:
`ValueType` -> `ResourceKey`

## Examples

`ResourceKey` is private to `ResourceDependencyTracker`. Public code records dependencies with `ResourceId<T>` instead:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Resources;

ResourceDependencyTracker tracker = new();
UIElement owner = GetElement();

tracker.RecordDependency(owner, new ResourceId<ImageResource>("logo"));
```

## Remarks

`ResourceKey` stores the resource `Type` and string key used as dictionary keys inside `ResourceDependencyTracker`.

The static `From<T>` method converts a public `ResourceId<T>` into the internal key by pairing `typeof(T)` with `id.Key`.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Type` | `Type` | Gets the resource type component of the internal key. |
| `Key` | `string` | Gets the resource string key component. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `From<T>(ResourceId<T> id)` | `ResourceKey` | Creates an internal key from a typed public resource id. |

## Applies To

Cerneala UI resource dependency tracking internals.

## See Also

- `Cerneala.UI.Resources.ResourceDependencyTracker`
- `Cerneala.UI.Resources.ResourceId<T>`
