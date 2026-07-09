# RenderDependency Struct

## Definition
Namespace: `Cerneala.UI.Rendering`

Assembly/Project: `Cerneala`

Source: `UI/Rendering/RenderDependency.cs`

Represents the value-based dependency stamp used by retained rendering to decide whether an element's local render cache is stale.

```csharp
public readonly record struct RenderDependency(
    int TextVersion = 0,
    string TextLayoutIdentity = "",
    int ImageVersion = 0,
    string ResourceIdentity = "",
    long ResourceVersion = 0,
    int CustomVersion = 0)
```

Inheritance:
`ValueType` -> `RenderDependency`

## Examples

Create a dependency value for cached drawing that depends on text layout and a shared resource version:

```csharp
using Cerneala.UI.Rendering;

RenderDependency dependencies = RenderDependency.None
    .WithTextLayoutIdentity("text:label/42")
    .WithResourceIdentity("resource:logo")
    .WithResourceVersion(7);
```

Compare dependency values to determine whether the dependency stamp changed:

```csharp
RenderDependency first = RenderDependency.None
    .WithTextVersion(1)
    .WithImageVersion(2);

RenderDependency second = RenderDependency.None
    .WithTextVersion(1)
    .WithImageVersion(2);

bool unchanged = first == second;
```

## Remarks

`RenderDependency` is a readonly record struct, so equality compares the dependency components by value. `ElementRenderCache.IsStale` compares the cached `RenderDependency` with the current element `RenderDependencies`; a different dependency value makes the cache stale.

The `None` value is the default dependency stamp. The `With...` methods return modified copies, allowing callers to compose dependency state without mutating the original value.

`TextLayoutIdentity` and `ResourceIdentity` are string identity components. The corresponding `With...Identity` methods normalize `null` to `string.Empty`. Version components are stored as supplied; `ResourceVersion` is a `long`, which supports resource tracker versions larger than `int.MaxValue`.

## Constructors

| Name | Description |
| --- | --- |
| `RenderDependency(int TextVersion = 0, string TextLayoutIdentity = "", int ImageVersion = 0, string ResourceIdentity = "", long ResourceVersion = 0, int CustomVersion = 0)` | Initializes a dependency stamp with optional text, image, resource, and custom components. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `None` | `RenderDependency` | Gets the default dependency stamp with all numeric versions set to `0` and identity strings set to empty strings. |
| `TextVersion` | `int` | Gets the text version component. |
| `TextLayoutIdentity` | `string` | Gets the text layout identity component. |
| `ImageVersion` | `int` | Gets the image version component. |
| `ResourceIdentity` | `string` | Gets the resource identity component. |
| `ResourceVersion` | `long` | Gets the resource version component. |
| `CustomVersion` | `int` | Gets the custom version component. |

## Methods

| Name | Returns | Description |
| --- | --- | --- |
| `WithTextVersion(int version)` | `RenderDependency` | Returns a copy with `TextVersion` set to `version`. |
| `WithTextLayoutIdentity(string identity)` | `RenderDependency` | Returns a copy with `TextLayoutIdentity` set to `identity`, or `string.Empty` when `identity` is `null`. |
| `WithImageVersion(int version)` | `RenderDependency` | Returns a copy with `ImageVersion` set to `version`. |
| `WithResourceIdentity(string identity)` | `RenderDependency` | Returns a copy with `ResourceIdentity` set to `identity`, or `string.Empty` when `identity` is `null`. |
| `WithResourceVersion(long version)` | `RenderDependency` | Returns a copy with `ResourceVersion` set to `version`. |
| `WithCustomVersion(int version)` | `RenderDependency` | Returns a copy with `CustomVersion` set to `version`. |

## Applies to

Retained rendering cache invalidation in the `Cerneala` UI runtime.

## See also

- `ElementRenderCache`
- `IRenderableElement`
- `UIElement.RenderDependencies`
