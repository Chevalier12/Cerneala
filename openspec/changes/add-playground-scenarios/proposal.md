## Why

Cerneala has the retained UI foundation, controls, text, resources, and MonoGame host integration, but the playground still shows a single custom demo element. The MVP needs a real retained playground scenario that proves controls, input, commands, layout, text, and render-cache reuse inside an actual MonoGame loop.

## What Changes

- Add retained playground samples for button interaction, layout composition, and text rendering.
- Add a sample selector that swaps retained sample trees through `MonoGameUiHost`.
- Add an invalidation stats overlay that reports retained measure, arrange, and render-cache work.
- Update `Game1` to build the sample selector and call retained host update/draw every frame.
- Preserve the retained behavior contract: unchanged frames still draw every frame but do not regenerate layout or local render caches.

## Capabilities

### New Capabilities
- `playground-scenarios`: Retained playground sample set, sample selector, and invalidation diagnostics overlay for proving the MVP in a real MonoGame loop.

### Modified Capabilities
- `game-loop-host-integration`: Playground host integration shall wire `Game1` to retained sample selection and diagnostics instead of a single custom immediate-style demo element.

## Impact

- Affected code: `Playground/Cerneala.Playground/Samples/*`, `Playground/Cerneala.Playground/Game1.cs`, and playground-focused tests.
- Affected systems: retained controls, retained layout/rendering, command routing, input bridge, MonoGame host update/draw.
- No core retained architecture changes are expected unless the playground exposes a real integration bug.
