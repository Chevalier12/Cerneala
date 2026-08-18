# Plan: migration `Foreground` to type `Brush`

## Summary

We change `Control.Foreground` from `Color` to `Brush?` without exposing a fake contract in which only `SolidColorBrush` works. Unlike the background and border, the foreground reaches the rasterization of the text and requires the masking of the glyphs with compound brushes.

## Proposed final contract
```csharp
public static readonly UiProperty<Brush?> ForegroundProperty;
public Brush? Foreground { get; set; }
```
- The default value is a concrete `SolidColorBrush(Color.Black)`, because the inherited text must remain visible.
- The property remains inherited and affects rendering.
- `Foreground="Tomato"` explicitly produces `SolidColorBrush`.
- We do not keep an alias `ForegroundColor` and we do not add default conversion from `Color`.

## Technical problem

Today `Foreground` is consumed as `Color` by `TextBlock`, `Button`, `CheckBox`, `ContentPresenter`, `TextBoxBase`, `TextAspect`, `TextRenderer`, `DrawTextRun` and the Skia/MonoGame backend. A gradient or image brush should be applied over the coverage of the glyphs, not over the full text rectangle.

## Phase 1: command pattern for text

1. We extend the text command to transport `IDrawBrush`, keeping the color overload only if it is internally API compatible.
2. The separation remains clear: shaping produces glyphs and metrics, brushing produces the final color.
3. The cache key of the text separates the mask of the glyphs from the brush, so that changing the brush does not needlessly reraster the shape of the text.
4. Caches and resources remain isolated per `GraphicsDevice`.

## Phase 2: rasterization and composition

1. Skia produces an alpha mask or color-independent overlay texture.
2. `SolidColorBrush` colors the mask through the existing fast path.
3. Linear and radial gradient are sampled in the coordinates of the text layout.
4. Image/drawing/visual brush use the same stretch, viewport, viewbox and tile mode rules as the other surfaces.
5. The opacity of the brush and the opacity of the element are composed only once.

## Phase 3: controls and inheritance

1. We migrate `Control.ForegroundProperty` and all bindings/templates to `Brush?`.
2. `TextBlock`, `Button`, `CheckBox`, `Label`, `ContentPresenter`, `TextBoxBase` and derived controls send the complete brush to `TextRenderer`.
3. `CaretColor` and `SelectionBackground` remain `Color` at this stage.
4. We check the inherited propagation and the invalidation of the subtrees.

## Phase 4: aspects, themes and motion

1. Foreground tokens become `AspectToken<Brush?>`.
2. The theme derives solid brushes from existing semantic colors.
3. `BrushMixer` animates structurally compatible solid brushes and gradients.
4. Image/drawing/visual brush snap to destination or require animation of internal properties, not interpolation between objects.

## Phase 5: markup
1. The color shorthand, resources and property elements have the same runtime/sourcegen semantics.
2. Accepted examples:
```xml
<TextBlock Foreground="Tomato" Text="Salut" />
```

```xml
<TextBlock Text="Gradient">
  <TextBlock.Foreground>
    <LinearGradientBrush ... />
  </TextBlock.Foreground>
</TextBlock>
```
3. Resources that are not `Brush` produce type diagnosis.

## Testing and acceptance

- API tests for `UiProperty<Brush?>`, default and inheritance;
- shaping tests that demonstrate that the brush does not change the metrics;
- pixel tests for solid text, linear gradient, radial gradient and image brush;
- bidi, wrapping, trimming, selection and clipping tests;
- cache tests for reusing the glyph mask;
- tests DPI, resize, device reset and windows with different devices;
- motion tests for solid and compatible gradient;
- build and complete suite without warnings or errors;
- synchronized API documentation and migration guide.

## Risks

- coloring the text directly in the current texture can multiply the cache explosively;
- the coordinates of the brush must be established for each line and run bidi;
- subpixel antialiasing may be incompatible with a simple alpha mask;
- visual brush can introduce cycles through content that includes the source text.

## Non-objectives

- `CaretColor` or `SelectionBackground` migration;
- arbitrary animation between two image/visual brushes;
- changing the shaping, bidi or line breaking algorithms;
- binary compatibility with `UiProperty<Color>`.