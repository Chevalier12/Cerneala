# IResourceProvider Interface

## Definition
Namespace: `Cerneala.UI.Resources`

Assembly/Project: `Cerneala`

Source: `UI/Resources/IResourceProvider.cs`

Looks up typed resources by `ResourceId<T>`.

```csharp
public interface IResourceProvider
```

## Examples

```csharp
using Cerneala.UI.Resources;

ResourceStore store = new();
ResourceId<string> title = new("Title");
store.SetResource(title, "Hello");

IResourceProvider provider = store;
Console.WriteLine(provider.GetResource(title));
// Hello

Console.WriteLine(provider.TryGetResource(new ResourceId<string>("Missing"), out string? missing));
// False

try
{
    provider.GetResource(new ResourceId<string>("Missing"));
}
catch (KeyNotFoundException ex)
{
    Console.WriteLine(ex.Message);
}
// Resource 'System.String:Missing' was not found.
```

## Remarks

`GetResource` is a default interface member. It calls `TryGetResource` and throws `KeyNotFoundException` with the message `Resource '<id>' was not found.` when the lookup fails, where `<id>` is `ResourceId<T>.ToString()`, for example `System.String:Missing`. A failed `TryGetResource` returns `false` and sets `resource` to `default`.

Cerneala implementations:

| Type | Key | Notes |
| --- | --- | --- |
| `ResourceStore` | The key string together with `T`. | `new ResourceId<int>("Title")` does not find a `string` stored as `"Title"`. A stored `null` is found for reference types. Keys are not validated. |
| `ResourceDictionary` | The key string only. | A value of another type under the same key makes `TryGetResource` return `false`. A `null` key throws `ArgumentNullException`, and an empty or whitespace key throws `ArgumentException`, even from `TryGetResource`. It does not declare its own `GetResource`, so the default member is used. |

Both implement `IObservableResourceProvider`, which adds `ResourceChanged`. Neither is thread-safe.

Where Cerneala uses a provider:

- `UIRoot.SetResourceProvider` installs the root provider. A window application installs `Application.Resources`, or the provider passed to the runtime when there is no application.
- `UIElement.TryFindResource` walks from the element up through its parents and checks each `UIElement.Resources` dictionary. Only when no dictionary has the key does it ask the root provider.
- `TextBlock` and text inputs use their own `ResourceProvider`, or else the root provider, to find fonts. They do not walk the element tree.
- Image lookup uses an explicitly given provider only; otherwise the element chain, then the root provider. A missing image is `null`, not an exception.
- Generated `.crn` code reads application resources through `GetResource`.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `TryGetResource<T>(ResourceId<T> id, out T resource)` | `bool` | Looks up `id`. Returns `true` and the resource when it is found. |
| `GetResource<T>(ResourceId<T> id)` | `T` | Returns the resource for `id`, or throws `KeyNotFoundException`. |

## Applies to

`ResourceStore`, `ResourceDictionary`, and `UIRoot.ResourceProvider`.

## See also

- `Cerneala.UI.Resources.ResourceId_T_`
- `Cerneala.UI.Resources.ResourceStore`
- `Cerneala.UI.Resources.ResourceDictionary`
- `Cerneala.UI.Resources.IObservableResourceProvider`
