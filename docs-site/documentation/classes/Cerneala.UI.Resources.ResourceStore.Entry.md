# ResourceStore.Entry Class

## Definition
Namespace: `Cerneala.UI.Resources`

Assembly/Project: `Cerneala`

Source: `UI/Resources/ResourceStore.cs`

Stores one resource value together with its version inside `ResourceStore`.

```csharp
private sealed record Entry(object? Value, long Version)
```

Inheritance:
`object` -> `Entry`

## Examples

`Entry` is private to `ResourceStore`. Public code stores and retrieves resources through the store:

```csharp
using Cerneala.UI.Resources;

ResourceStore store = new();
ResourceId<string> title = new("title");

store.SetResource(title, "Main");
string value = store.GetResource(title);
long version = store.GetVersion(title);
```

## Remarks

`Entry` is the internal dictionary value used by `ResourceStore`. It carries the stored resource object and the version number assigned when the value was written.

The version is incremented by `ResourceStore.SetResource` when a resource value changes.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Value` | `object?` | Gets the stored resource value. |
| `Version` | `long` | Gets the version associated with the stored value. |

## Applies To

Cerneala UI resource storage internals.

## See Also

- `Cerneala.UI.Resources.ResourceStore`
- `Cerneala.UI.Resources.ResourceChangedEventArgs`
