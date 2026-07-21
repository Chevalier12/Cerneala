# BackdropFrameRequest Struct

## Definition
Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/BackdropFrameRequest.cs`

Describes the physical frame requested for one backdrop acquisition.

```csharp
public readonly record struct BackdropFrameRequest
```

## Examples

```csharp
using Cerneala.Drawing.Prism;

BackdropFrameRequest request = new(1920, 1080, 1.5f);

Console.WriteLine(
    $"{request.PixelWidth}x{request.PixelHeight} at {request.PixelScale}x");
```

## Remarks

`UiHost` creates a request only when the current drawing submission needs backdrop input. The physical dimensions are derived from the logical viewport and pixel scale. All compatible consumers in that drawing frame share the lease returned for the request.

Graph analysis remains an internal framework concern. Backdrop providers receive only the dimensions and scale needed to supply a compatible frame.

## Constructors

| Name | Description |
| --- | --- |
| `BackdropFrameRequest(int pixelWidth, int pixelHeight, float pixelScale)` | Creates a validated acquisition request. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `PixelWidth` | `int` | Gets the requested physical pixel width. |
| `PixelHeight` | `int` | Gets the requested physical pixel height. |
| `PixelScale` | `float` | Gets the physical pixels per logical unit. |

## Exceptions

| Exception | Condition |
| --- | --- |
| `ArgumentOutOfRangeException` | A dimension is not positive or `PixelScale` is not finite and positive. |

## Applies to

Cerneala backdrop-aware host drawing.

## See also

- `Cerneala.Drawing.Prism.IBackdropFrameSource`
