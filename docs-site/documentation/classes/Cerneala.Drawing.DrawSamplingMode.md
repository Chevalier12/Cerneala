# DrawSamplingMode Enum

## Definition

Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawImageOptions.cs`

Selects the texture filtering used by advanced image and textured-mesh commands.

```csharp
public enum DrawSamplingMode
```

## Examples

```csharp
DrawImageOptions options = new(sampling: DrawSamplingMode.Point);
drawing.DrawImage(image, destination, options);
```

## Remarks

`Point` preserves hard texel boundaries. `Linear` blends neighboring texels and is the default. The backend reuses cached sampler states rather than creating a state for each command.

## Values

| Name | Description |
| --- | --- |
| `Point` | Samples the nearest texel. |
| `Linear` | Linearly filters neighboring texels. |

## Applies To

Advanced image, image-quad, nine-slice, textured-mesh, and sprite-batch drawing.

## See Also

- `DrawAddressMode`
- `DrawImageOptions`
