# ResourceStore Class

## Definition
Namespace: `Cerneala.UI.Resources`

Assembly/Project: `Cerneala`

Source: `UI/Resources/ResourceStore.cs`

Stores typed resources by `ResourceId<T>` and publishes change notifications when stored values change.

```csharp
public sealed class ResourceStore : IObservableResourceProvider
```

Inheritance:
`Object` -> `ResourceStore`

Implements:
`IObservableResourceProvider`, `IResourceProvider`

## Examples

```csharp
using Cerneala.UI.Resources;

ResourceStore store = new();
ResourceId<string> titleId = new("title");

store.ResourceChanged += (sender, args) =>
{
    if (args.Matches(titleId))
    {
        Console.WriteLine($"Resource {args.Key} changed to version {args.Version}.");
    }
};

store.SetResource(titleId, "Hello");

if (store.TryGetResource(titleId, out string title))
{
    Console.WriteLine(title);
}
```

## Remarks

`ResourceStore` is an in-memory resource provider. Each entry is keyed by both the resource type `T` and the string key from `ResourceId<T>`, so two resource IDs with the same key but different generic resource types are separate entries.

Calling `SetResource<T>` stores the value and increments that entry's version. The first stored value receives version `1`; later changed values increment from the previous version. If an existing value compares equal to the new value by `EqualityComparer<T>.Default`, the store leaves the entry unchanged and does not raise `ResourceChanged`.

`ResourceChanged` is raised after a new or changed value is stored. The event args include the resource type, key, old value, new value, and new positive version.

`TryGetResource<T>` returns `true` only when the store contains a matching entry whose value is compatible with `T`, including a stored `null` for nullable/reference-type resources. `GetResource<T>` returns the stored value or throws `KeyNotFoundException` when the resource is not found. `GetVersion<T>` returns the stored entry version, or `0` when the resource is missing.

The class does not expose synchronization; coordinate access externally when sharing a store across threads.

## Constructors

| Name | Description |
| --- | --- |
| `ResourceStore()` | Initializes an empty resource store. |

## Methods

| Name | Description |
| --- | --- |
| `SetResource<T>(ResourceId<T> id, T resource)` | Stores or replaces the resource value for `id`, increments the entry version, and raises `ResourceChanged` when the value actually changes. |
| `TryGetResource<T>(ResourceId<T> id, out T resource)` | Attempts to retrieve the resource value for `id`. Returns `true` when a compatible value is present. |
| `GetResource<T>(ResourceId<T> id)` | Retrieves the resource value for `id`, or throws `KeyNotFoundException` when it is missing. |
| `GetVersion<T>(ResourceId<T> id)` | Returns the current version for `id`, or `0` when no resource is stored for that ID. |

## Events

| Name | Event Type | Description |
| --- | --- | --- |
| `ResourceChanged` | `EventHandler<ResourceChangedEventArgs>?` | Raised after `SetResource<T>` stores a new value that differs from the previous value. |

## Applies to

`Cerneala.UI.Resources` in the `Cerneala` project.

## See also

- `IObservableResourceProvider`
- `IResourceProvider`
- `ResourceId<T>`
- `ResourceChangedEventArgs`
