# UiViewport Struct

## Definition
Namespace: `Cerneala.UI.Hosting`

Assembly/Project: `Cerneala` (`net8.0`)

Source: [`UI/Hosting/UiViewport.cs`](../../UI/Hosting/UiViewport.cs)

Represents the logical size and scale used by a hosted Cerneala UI frame.

```csharp
public readonly record struct UiViewport
```

Inheritance:
`Object` -> `ValueType` -> `UiViewport`

Implements:
`IEquatable<UiViewport>`

## Examples

Create a viewport from logical dimensions:

```csharp
using Cerneala.UI.Hosting;

UiViewport viewport = new(width: 1280, height: 720);
UiHost host = new(new UiHostOptions
{
    Viewport = viewport
});
```

Create a viewport from physical pixel dimensions when the platform scale is known:

```csharp
using Cerneala.UI.Hosting;

UiViewport viewport = UiViewport.FromPhysicalPixels(
    pixelWidth: 1920,
    pixelHeight: 1080,
    scale: 1.5f);
```

## Remarks

`UiViewport` stores the viewport `Width` and `Height` in logical units together with a positive `Scale`. `FromPhysicalPixels` converts physical pixel dimensions to logical dimensions by dividing each dimension by `scale`.

The constructor accepts zero for `Width` and `Height`, but rejects negative, `NaN`, and infinite values. `Scale` must be finite and greater than zero. `FromPhysicalPixels` also rejects negative pixel dimensions before converting them.

`UiHost` uses `UiViewport` to apply the current viewport to the retained `UIRoot`. When a host update receives a different viewport, the root viewport is updated and the tree is invalidated for measure, arrange, render, hit testing, and subtree work.

Because this type is a `readonly record struct`, values are immutable after construction and use value-based equality.

## Constructors

| Name | Description |
| --- | --- |
| `UiViewport(float width, float height, float scale = 1)` | Initializes a viewport from logical dimensions and scale. Throws `ArgumentOutOfRangeException` when `width` or `height` is negative or not finite, or when `scale` is not finite or is less than or equal to zero. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Width` | `float` | Gets the logical viewport width. |
| `Height` | `float` | Gets the logical viewport height. |
| `Scale` | `float` | Gets the positive scale used to map logical units to physical pixels. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `FromPhysicalPixels(int pixelWidth, int pixelHeight, float scale)` | `UiViewport` | Creates a viewport by converting non-negative physical pixel dimensions into logical dimensions using `scale`. Throws `ArgumentOutOfRangeException` when a pixel dimension is negative or when `scale` is not finite or is less than or equal to zero. |

## Applies To

Cerneala UI hosting APIs in the `Cerneala.UI.Hosting` namespace.

## See Also

- [`UiCoordinateMapper`](../../UI/Hosting/UiCoordinateMapper.cs)
- [`UiHost`](../../UI/Hosting/UiHost.cs)
- [`UiHostOptions`](../../UI/Hosting/UiHostOptions.cs)
