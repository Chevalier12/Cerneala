# ResourceStore.ResourceKey Class

## Definition
Namespace: `Cerneala.UI.Resources`

Assembly/Project: `Cerneala`

Source: `UI/Resources/ResourceStore.cs`

Represents the internal typed dictionary key used by `ResourceStore`.

```csharp
private readonly record struct ResourceKey(Type Type, string Key)
```

Inheritance:
`ValueType` -> `ResourceKey`

## Examples

`ResourceKey` is private to `ResourceStore`. Public code supplies typed resource ids:

```csharp
using Cerneala.UI.Resources;

ResourceStore store = new();
ResourceId<int> count = new("count");

store.SetResource(count, 42);
bool found = store.TryGetResource(count, out int value);
```

## Remarks

`ResourceKey` combines a resource `Type` with a string key so resources with the same key but different generic types can be stored separately.

The static `From<T>` method converts a public `ResourceId<T>` into the internal key by using `typeof(T)` and `id.Key`.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Type` | `Type` | Gets the resource type component. |
| `Key` | `string` | Gets the resource key component. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `From<T>(ResourceId<T> id)` | `ResourceKey` | Creates an internal key from a typed resource id. |

## Applies To

Cerneala UI resource storage internals.

## See Also

- `Cerneala.UI.Resources.ResourceStore`
- `Cerneala.UI.Resources.ResourceId<T>`
