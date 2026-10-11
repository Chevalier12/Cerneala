# IDpiProvider Interface

## Definition
Namespace: `Cerneala.UI.Platform`

Assembly/Project: `Cerneala`

Source: `UI/Platform/IDpiProvider.cs`

Reports the display scale and dots per inch of a platform.

```csharp
public interface IDpiProvider
```

## Examples

```csharp
using Cerneala.UI.Platform;

IPlatformServices services = new PlatformServices(Dpi: new FixedDpi());
Console.WriteLine(services.Dpi?.Scale);
// 2

sealed class FixedDpi : IDpiProvider
{
    public float Scale => 2;

    public float DpiX => 192;

    public float DpiY => 192;
}
```

## Remarks

This is an optional seam in `IPlatformServices`. No Cerneala code reads its members: layout, rendering, and input do not use it. `Detective` reports only whether a provider is present. The SDL platform does not supply one; it computes the window's coordinate scale itself.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Scale` | `float` | Gets the display scale factor. |
| `DpiX` | `float` | Gets the horizontal dots per inch. |
| `DpiY` | `float` | Gets the vertical dots per inch. |

## Applies to

`IPlatformServices.Dpi`.

## See also

- `Cerneala.UI.Platform.IPlatformServices`
- `Cerneala.UI.Platform.PlatformServices`
