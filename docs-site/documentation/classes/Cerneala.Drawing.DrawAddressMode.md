# DrawAddressMode Enum

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawImageOptions.cs`

Selects how texture coordinates outside the image range are addressed.

```csharp
public enum DrawAddressMode
```

## Examples

```csharp
DrawImageOptions options = new(addressMode: DrawAddressMode.Wrap);
drawing.DrawImage(image, destination, options);
```

## Remarks

Addressing is platform-neutral. The MonoGame backend maps each value to a cached sampler state and combines it with the selected `DrawSamplingMode`.

## Values

| Name | Description |
| --- | --- |
| `Clamp` | Clamps texture coordinates to the image edge. |
| `Wrap` | Repeats the image when texture coordinates leave the normalized range. |

## Applies To

Advanced image, image-quad, nine-slice, textured-mesh, and sprite-batch drawing.

## See Also

- `DrawSamplingMode`
- `DrawImageOptions`
