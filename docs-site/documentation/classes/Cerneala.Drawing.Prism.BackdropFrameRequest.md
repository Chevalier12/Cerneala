# BackdropFrameRequest Struct

## Definition
Namespace: `Cerneala.Drawing.Prism`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/BackdropFrameRequest.cs`

Describes the physical frame and analyzed Prism requirement for one backdrop acquisition.

```csharp
public readonly record struct BackdropFrameRequest
```

## Examples

```csharp
using Cerneala.Drawing.Prism;

static void InspectRequest(in BackdropFrameRequest request)
{
    Console.WriteLine(
        $"{request.PixelWidth}x{request.PixelHeight} " +
        $"for {request.BackdropRequirement.ScopeCount} scopes");
}
```

## Remarks

`UiHost` creates one request only when `PrismFrameAnalysis` reports a visible, non-empty backdrop requirement. The physical dimensions are derived from the current logical viewport and pixel scale. All compatible consumers in that drawing frame share the lease returned for the request.

## Constructors

| Name | Description |
| --- | --- |
| `BackdropFrameRequest(int, int, float, PrismBackdropRequirement)` | Creates a validated acquisition request. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `PixelWidth` | `int` | Gets the requested physical pixel width. |
| `PixelHeight` | `int` | Gets the requested physical pixel height. |
| `PixelScale` | `float` | Gets the physical pixels per logical unit. |
| `BackdropRequirement` | `PrismBackdropRequirement` | Gets the single frame analysis result that selected the backdrop consumers. |

## Exceptions

| Exception | Condition |
| --- | --- |
| `ArgumentOutOfRangeException` | A dimension is not positive or `PixelScale` is not finite and positive. |
| `ArgumentNullException` | `BackdropRequirement` is `null`. |

## Applies to

Cerneala backdrop-aware host drawing.

## See also

- `Cerneala.Drawing.Prism.IBackdropFrameSource`
- `Cerneala.Drawing.Prism.Graph.PrismBackdropRequirement`
