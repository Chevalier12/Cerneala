# ResourceChangedEventArgs Class

## Definition
Namespace: `Cerneala.UI.Resources`

Assembly/Project: `Cerneala`

Source: `UI/Resources/ResourceChangedEventArgs.cs`

Provides event data for resource changes raised by observable resource providers.

```csharp
public sealed class ResourceChangedEventArgs : EventArgs
```

Inheritance:
`Object` -> `EventArgs` -> `ResourceChangedEventArgs`

## Examples

The following example listens for a `ResourceStore` change and filters the event to one typed resource identifier.

```csharp
using Cerneala.UI.Resources;

ResourceId<string> titleResource = new("Title");
ResourceStore store = new();

store.ResourceChanged += (_, args) =>
{
    if (args.Matches(titleResource))
    {
        Console.WriteLine($"Resource '{args.Key}' changed to '{args.NewValue}'.");
    }
};

store.SetResource(titleResource, "Hello");
```

## Remarks

`ResourceChangedEventArgs` identifies a changed resource by its exact CLR resource type and resource key. `OldValue` and `NewValue` carry the previous and new values as nullable `object` instances because resources can be any type and can also be set to `null`.

`ResourceStore` creates this event data when `SetResource<T>` stores a value that differs from the previous value. The store passes `typeof(T)`, the resource identifier key, the previous value, the new value, and the incremented resource version.

Use `Matches<T>(ResourceId<T>)` when a handler needs to check whether the change belongs to a specific typed resource identifier. Matching uses exact type equality with `typeof(T)` and ordinal string equality on `Key`.

## Constructors

| Name | Description |
| --- | --- |
| `ResourceChangedEventArgs(Type resourceType, string key, object? oldValue, object? newValue, long version)` | Initializes event data for a resource change. Throws when `resourceType` is `null`, `key` is null, empty, or whitespace, or `version` is less than or equal to zero. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ResourceType` | `Type` | Gets the exact CLR type used to identify the changed resource. |
| `Key` | `string` | Gets the resource key. |
| `OldValue` | `object?` | Gets the previous resource value. |
| `NewValue` | `object?` | Gets the new resource value. |
| `Version` | `long` | Gets the positive version associated with the resource change. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Matches<T>(ResourceId<T> id)` | `bool` | Returns `true` when `ResourceType` equals `typeof(T)` and `Key` equals `id.Key`; otherwise, returns `false`. |

## Applies to

`Cerneala.UI.Resources` resource providers and resource dependency tracking in the `Cerneala` project.

## See also

- `IObservableResourceProvider`
- `ResourceStore`
- `ResourceId<T>`
- `ResourceDependencyTracker`
