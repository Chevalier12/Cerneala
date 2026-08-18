# Plan: migration of `Background` and `BorderBrush` to type `Brush`

## Summary

We change `Control.Background` and `Control.BorderBrush` from `Color` to `Brush?`, so that the controls can use solid colors, gradients, images, drawings and visual brushes. The plan depends on the infrastructure in `2026-07-11-brush-types-and-rendering.md`.

These are intentional breaking changes. The values ​​`Color` must be explicitly wrapped in `SolidColorBrush`; we do not add default conversions and do not keep color aliases.

## Final contracts
```csharp
public static readonly UiProperty<Brush?> BackgroundProperty;
public Brush? Background { get; set; }

public static readonly UiProperty<Brush?> BorderBrushProperty;
public Brush? BorderBrush { get; set; }
```
- Both properties have the default value `null`.
- `null` means that the surface or outline is not drawn.
- `BorderThickness` remains separate; zero thickness suppresses the outline.
- `Background="Tomato"` and `BorderBrush="Tomato"` explicitly produce `new SolidColorBrush(Color.Tomato)`.
- Resource references must resolve to `Brush`.
- Property-element shapes accept compound brushes.
- `Foreground` remains `Color` in this plan and has a separate plan.

## Phase 1: `Control` contract

1. We change `BackgroundProperty` and `BorderBrushProperty` to `UiProperty<Brush?>`.
2. We change the CLR properties to `Brush?` with default `null`.
3. We keep rendering invalidation and visual input.
4. We eliminate assumptions based on `Color.A`.
5. We add API tests for the type, default and absence of the `BorderColor` alias.

## Phase 2: controls and rendering

1. We adapt `Border`, `Button`, `CheckBox`, `ListBoxItem`, `ScrollBar`, `Thumb`, `Track`, `ProgressBar` and `TextBoxBase`.
2. The order remains background -> border -> content.
3. The brushes are sent intact via `DrawingContext` and `DrawCommand` to the backend.
4. `SolidColorBrush` uses the existing fast track; compound brushes use the textured pipeline.
5. We test fill and stroke with solid, linear gradient, radial gradient and image brush.

## Phase 3: aspects, themes and motion

1. `ButtonTokens.Background`, `HoverBackground`, `PressedBackground` and `BorderBrush` become `AspectToken<Brush?>`.
2. We add semantic tokens `DefaultAspectTokens.Brush.Background`, `Surface` and `Border`.
3. The colors in `ThemePalette` remain semantic colors for clear color and brush derivation; control properties receive tokens `Brush`.
4. `AnimatablePropertyRegistry` uses `BrushMixer` for both properties.
5. Solid brushes and gradients with compatible stops are interpolated; image/drawing/visual brushes are not interpolated as integer values.

## Phase 4: markup and generator

1. Runtime schema and source generator detect both properties as `Brush?`.
2. We support color shorthand, resources and property elements:
```xml
<Border Background="Tomato" BorderBrush="#FF334455" />
```

```xml
<Border Background="$Surface" BorderBrush="$Outline" />
```

```xml
<Border>
  <Border.Background>
    <LinearGradientBrush ... />
  </Border.Background>
  <Border.BorderBrush>
    <ImageBrush ... />
  </Border.BorderBrush>
</Border>
```
3. Incompatible resources and incomplete brushes produce diagnostics, not hidden conversions.

## Phase 5: migration and documentation
```csharp
// vechi
control.Background = Color.White;
control.BorderBrush = Color.Red;

// nou
control.Background = new SolidColorBrush(Color.White);
control.BorderBrush = new SolidColorBrush(Color.Red);
```

1. We update the samples, tests, API pages and the documentation manifest.
2. We document `null` as semantic transparency.
3. We do not introduce aliases or default operators for `Color`.

## Acceptance

- complete build without warnings or errors;
- all existing tests migrated;
- API tests and invalidation for both properties;
- markup tests for shorthand, resources and property elements;
- rendering tests for solid, linear, radial and image brush on fill and border;
- motion tests for solid and compatible gradient;
- synchronized documentation and manifest.

## Non-objectives

- `Foreground` migration;
- changing `SelectionBackground`, `CaretColor` or the clear color of the window;
- implicit conversions `Color` -> `Brush`;
- a new layout or clipping system.