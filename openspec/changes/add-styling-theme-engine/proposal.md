## Why

Cerneala now has retained controls, typed properties, input visual state, resources, and a playground. The next blocker is a modern styling/theme layer so controls can share visual rules without hard-coded rendering logic or WPF-era string/reflection machinery.

## What Changes

- Add a typed styling system under `UI/Styling` with styles, rules, selectors, setters, style sheets, applicator, invalidation, diagnostics, and pseudo-class/visual-state support.
- Add theme primitives under `UI/Styling` with typed keys, theme values/resources, default palette/theme, and explicit theme provider behavior.
- Apply styles through the existing `UiProperty` value precedence, using style base values and style visual-state values without reflection in the hot path.
- Connect retained input state such as hover, pressed, focus, disabled, and selected-style pseudo states to style rule matching.
- Ensure style and theme changes invalidate only the retained work affected by the changed typed properties or resource dependencies.
- Add focused tests for style composition, typed setters, applicator behavior, invalidation, themes, and pseudo classes.

## Capabilities

### New Capabilities
- `styling-theme-engine`: Typed styling, selectors, visual-state rules, theme resources, diagnostics, invalidation behavior, and local/style precedence integration.

### Modified Capabilities

## Impact

- Adds `UI/Styling/*` public APIs.
- Adds tests under `tests/Cerneala.Tests/UI/Styling`.
- Uses existing `UiProperty`, `UiValueSource`, retained invalidation, input visual state, and resource services.
- Keeps core styling backend-neutral with no MonoGame, Skia, HarfBuzz, `Texture2D`, or `SpriteBatch` references.
