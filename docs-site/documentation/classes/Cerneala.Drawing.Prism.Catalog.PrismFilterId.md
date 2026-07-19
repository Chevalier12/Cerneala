# PrismFilterId Enum

## Definition
Namespace: `Cerneala.Drawing.Prism.Catalog`

Assembly/Project: `Cerneala`

Source: `Cerneala.SourceGen/Prism/Catalog/prism-catalog.json` (generated)

Identifies a built-in Prism filter by its stable catalog identifier.

```csharp
public enum PrismFilterId
```

## Remarks

The numeric values are generated from the validated Prism catalog and remain stable across generated output. Use the enum rather than copying its integer value.

## Values

| Name | Stable ID | Description |
| --- | ---: | --- |
| `BrightnessContrast` | `1` | Color and adjustment filter. |
| `Levels` | `2` | Color and adjustment filter. |
| `Curves` | `3` | Color and adjustment filter. |
| `Exposure` | `4` | Color and adjustment filter. |
| `Vibrance` | `5` | Color and adjustment filter. |
| `HueSaturation` | `6` | Color and adjustment filter. |
| `ColorBalance` | `7` | Color and adjustment filter. |
| `BlackWhite` | `8` | Color and adjustment filter. |
| `PhotoFilter` | `9` | Color and adjustment filter. |
| `ChannelMixer` | `10` | Color and adjustment filter. |
| `ColorLookup` | `11` | Color and adjustment filter. |
| `Invert` | `12` | Color and adjustment filter. |
| `Posterize` | `13` | Color and adjustment filter. |
| `Threshold` | `14` | Color and adjustment filter. |
| `GradientMap` | `15` | Color and adjustment filter. |
| `SelectiveColor` | `16` | Color and adjustment filter. |
| `Average` | `17` | Blur and sharpen filter. |
| `Blur` | `18` | Blur and sharpen filter. |
| `BlurMore` | `19` | Blur and sharpen filter. |
| `BoxBlur` | `20` | Blur and sharpen filter. |
| `GaussianBlur` | `21` | Blur and sharpen filter. |
| `LensBlur` | `22` | Blur and sharpen filter. |
| `MotionBlur` | `23` | Blur and sharpen filter. |
| `RadialBlur` | `24` | Blur and sharpen filter. |
| `ShapeBlur` | `25` | Blur and sharpen filter. |
| `SmartBlur` | `26` | Blur and sharpen filter. |
| `SurfaceBlur` | `27` | Blur and sharpen filter. |
| `FieldBlur` | `28` | Blur and sharpen filter. |
| `IrisBlur` | `29` | Blur and sharpen filter. |
| `TiltShift` | `30` | Blur and sharpen filter. |
| `PathBlur` | `31` | Blur and sharpen filter. |
| `SpinBlur` | `32` | Blur and sharpen filter. |
| `Sharpen` | `33` | Blur and sharpen filter. |
| `SharpenMore` | `34` | Blur and sharpen filter. |
| `SharpenEdges` | `35` | Blur and sharpen filter. |
| `UnsharpMask` | `36` | Blur and sharpen filter. |
| `SmartSharpen` | `37` | Blur and sharpen filter. |
| `HighPass` | `38` | Blur and sharpen filter. |
| `Transform` | `39` | Distort, geometry, and morphology filter. |
| `AdaptiveWideAngle` | `40` | Distort, geometry, and morphology filter. |
| `LensCorrection` | `41` | Distort, geometry, and morphology filter. |
| `DiffuseGlow` | `42` | Distort, geometry, and morphology filter. |
| `Displace` | `43` | Distort, geometry, and morphology filter. |
| `Glass` | `44` | Distort, geometry, and morphology filter. |
| `OceanRipple` | `45` | Distort, geometry, and morphology filter. |
| `Pinch` | `46` | Distort, geometry, and morphology filter. |
| `PolarCoordinates` | `47` | Distort, geometry, and morphology filter. |
| `Ripple` | `48` | Distort, geometry, and morphology filter. |
| `Shear` | `49` | Distort, geometry, and morphology filter. |
| `Spherize` | `50` | Distort, geometry, and morphology filter. |
| `Twirl` | `51` | Distort, geometry, and morphology filter. |
| `Wave` | `52` | Distort, geometry, and morphology filter. |
| `ZigZag` | `53` | Distort, geometry, and morphology filter. |
| `Liquify` | `54` | Distort, geometry, and morphology filter. |
| `Maximum` | `55` | Distort, geometry, and morphology filter. |
| `Minimum` | `56` | Distort, geometry, and morphology filter. |
| `Offset` | `57` | Distort, geometry, and morphology filter. |
| `AddNoise` | `58` | Noise, pixelate, render, and video filter. |
| `Despeckle` | `59` | Noise, pixelate, render, and video filter. |
| `DustScratches` | `60` | Noise, pixelate, render, and video filter. |
| `Median` | `61` | Noise, pixelate, render, and video filter. |
| `ReduceNoise` | `62` | Noise, pixelate, render, and video filter. |
| `ColorHalftone` | `63` | Noise, pixelate, render, and video filter. |
| `Crystallize` | `64` | Noise, pixelate, render, and video filter. |
| `Facet` | `65` | Noise, pixelate, render, and video filter. |
| `Fragment` | `66` | Noise, pixelate, render, and video filter. |
| `Mezzotint` | `67` | Noise, pixelate, render, and video filter. |
| `Mosaic` | `68` | Noise, pixelate, render, and video filter. |
| `Pointillize` | `69` | Noise, pixelate, render, and video filter. |
| `Clouds` | `70` | Noise, pixelate, render, and video filter. |
| `DifferenceClouds` | `71` | Noise, pixelate, render, and video filter. |
| `Fibers` | `72` | Noise, pixelate, render, and video filter. |
| `LensFlare` | `73` | Noise, pixelate, render, and video filter. |
| `LightingEffects` | `74` | Noise, pixelate, render, and video filter. |
| `Deinterlace` | `75` | Noise, pixelate, render, and video filter. |
| `NtscColors` | `76` | Noise, pixelate, render, and video filter. |
| `ColoredPencil` | `77` | Artistic, brush, sketch, stylize, and texture filter. |
| `Cutout` | `78` | Artistic, brush, sketch, stylize, and texture filter. |
| `DryBrush` | `79` | Artistic, brush, sketch, stylize, and texture filter. |
| `FilmGrain` | `80` | Artistic, brush, sketch, stylize, and texture filter. |
| `Fresco` | `81` | Artistic, brush, sketch, stylize, and texture filter. |
| `NeonGlow` | `82` | Artistic, brush, sketch, stylize, and texture filter. |
| `PaintDaubs` | `83` | Artistic, brush, sketch, stylize, and texture filter. |
| `PaletteKnife` | `84` | Artistic, brush, sketch, stylize, and texture filter. |
| `PlasticWrap` | `85` | Artistic, brush, sketch, stylize, and texture filter. |
| `PosterEdges` | `86` | Artistic, brush, sketch, stylize, and texture filter. |
| `RoughPastels` | `87` | Artistic, brush, sketch, stylize, and texture filter. |
| `SmudgeStick` | `88` | Artistic, brush, sketch, stylize, and texture filter. |
| `Sponge` | `89` | Artistic, brush, sketch, stylize, and texture filter. |
| `Underpainting` | `90` | Artistic, brush, sketch, stylize, and texture filter. |
| `Watercolor` | `91` | Artistic, brush, sketch, stylize, and texture filter. |
| `AccentedEdges` | `92` | Artistic, brush, sketch, stylize, and texture filter. |
| `AngledStrokes` | `93` | Artistic, brush, sketch, stylize, and texture filter. |
| `Crosshatch` | `94` | Artistic, brush, sketch, stylize, and texture filter. |
| `DarkStrokes` | `95` | Artistic, brush, sketch, stylize, and texture filter. |
| `InkOutlines` | `96` | Artistic, brush, sketch, stylize, and texture filter. |
| `Spatter` | `97` | Artistic, brush, sketch, stylize, and texture filter. |
| `SprayedStrokes` | `98` | Artistic, brush, sketch, stylize, and texture filter. |
| `SumiE` | `99` | Artistic, brush, sketch, stylize, and texture filter. |
| `BasRelief` | `100` | Artistic, brush, sketch, stylize, and texture filter. |
| `ChalkCharcoal` | `101` | Artistic, brush, sketch, stylize, and texture filter. |
| `Charcoal` | `102` | Artistic, brush, sketch, stylize, and texture filter. |
| `Chrome` | `103` | Artistic, brush, sketch, stylize, and texture filter. |
| `ConteCrayon` | `104` | Artistic, brush, sketch, stylize, and texture filter. |
| `GraphicPen` | `105` | Artistic, brush, sketch, stylize, and texture filter. |
| `HalftonePattern` | `106` | Artistic, brush, sketch, stylize, and texture filter. |
| `NotePaper` | `107` | Artistic, brush, sketch, stylize, and texture filter. |
| `Photocopy` | `108` | Artistic, brush, sketch, stylize, and texture filter. |
| `Plaster` | `109` | Artistic, brush, sketch, stylize, and texture filter. |
| `Reticulation` | `110` | Artistic, brush, sketch, stylize, and texture filter. |
| `Stamp` | `111` | Artistic, brush, sketch, stylize, and texture filter. |
| `TornEdges` | `112` | Artistic, brush, sketch, stylize, and texture filter. |
| `WaterPaper` | `113` | Artistic, brush, sketch, stylize, and texture filter. |
| `Diffuse` | `114` | Artistic, brush, sketch, stylize, and texture filter. |
| `Emboss` | `115` | Artistic, brush, sketch, stylize, and texture filter. |
| `Extrude` | `116` | Artistic, brush, sketch, stylize, and texture filter. |
| `FindEdges` | `117` | Artistic, brush, sketch, stylize, and texture filter. |
| `GlowingEdges` | `118` | Artistic, brush, sketch, stylize, and texture filter. |
| `Solarize` | `119` | Artistic, brush, sketch, stylize, and texture filter. |
| `Tiles` | `120` | Artistic, brush, sketch, stylize, and texture filter. |
| `TraceContour` | `121` | Artistic, brush, sketch, stylize, and texture filter. |
| `Wind` | `122` | Artistic, brush, sketch, stylize, and texture filter. |
| `Craquelure` | `123` | Artistic, brush, sketch, stylize, and texture filter. |
| `Grain` | `124` | Artistic, brush, sketch, stylize, and texture filter. |
| `MosaicTiles` | `125` | Artistic, brush, sketch, stylize, and texture filter. |
| `Patchwork` | `126` | Artistic, brush, sketch, stylize, and texture filter. |
| `StainedGlass` | `127` | Artistic, brush, sketch, stylize, and texture filter. |
| `Texturizer` | `128` | Artistic, brush, sketch, stylize, and texture filter. |
| `OilPaint` | `129` | Artistic, brush, sketch, stylize, and texture filter. |
| `CustomConvolution` | `130` | General and Cerneala-native filter. |
| `ColorMatrix` | `131` | General and Cerneala-native filter. |
| `Color` | `132` | General and Cerneala-native filter. |
| `ChromaticAberration` | `133` | General and Cerneala-native filter. |
| `Scanlines` | `134` | General and Cerneala-native filter. |

## Applies to

Cerneala Prism definitions, generated markup, runtime state, and drawing backends.
