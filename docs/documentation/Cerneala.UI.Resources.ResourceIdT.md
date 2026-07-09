# ResourceId<T> Class

## Definition
Namespace: `Cerneala.UI.Resources`

Assembly/Project: `Cerneala`

Source: `UI/Resources/ResourceId{T}.cs`

Identifies a typed UI resource by key.

```csharp
public readonly record struct ResourceId<T>
```

Inheritance:
`ValueType` -> `ResourceId<T>`

## Examples

Create a typed resource id:

```csharp
using Cerneala.UI.Resources;

ResourceId<ImageResource> logo = new("logo");

string key = logo.Key;
Type resourceType = logo.ResourceType;
string display = logo.ToString();
```

## Remarks

`ResourceId<T>` pairs a non-empty string key with the resource type represented by `T`. The constructor throws `ArgumentException` when `key` is `null`, empty, or whitespace.

`ResourceType` returns `typeof(T)`. `ToString` formats the id as the full type name followed by the key.

## Constructors

| Signature | Description |
| --- | --- |
| `ResourceId(string key)` | Initializes a typed resource id with a non-empty key. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Key` | `string` | Gets the resource key. |
| `ResourceType` | `Type` | Gets the resource type represented by `T`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `ToString()` | `string` | Returns a string containing the full resource type name and key. |

## Applies To

Cerneala UI resource lookup and dependency tracking APIs.

## See Also

- `Cerneala.UI.Resources.ResourceStore`
- `Cerneala.UI.Resources.ResourceDependencyTracker`
- `Cerneala.UI.Resources.IResourceProvider`
