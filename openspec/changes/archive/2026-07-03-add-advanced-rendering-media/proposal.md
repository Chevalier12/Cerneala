## Why

Cerneala already has retained controls, layout, styling, resources, text, images, and rendering caches, but the drawing surface is still limited to rectangles, text, images, and clips. Advanced retained controls need backend-neutral media descriptors and shape controls so richer visuals can be expressed without leaking MonoGame, Skia, HarfBuzz, `SpriteBatch`, or `Texture2D` into UI code.

## What Changes

- Add a backend-neutral advanced media capability covering brushes, pens, geometries, transforms, opacity layers, shadow metadata, and image source identity.
- Extend drawing commands only for primitives that this phase can route through tests and backend adapter coverage.
- Add retained shape controls (`Shape`, `Rectangle`, `Ellipse`, `Path`) that render through media abstractions and retained drawing commands.
- Add focused tests for advanced draw commands, brushes, geometries, transforms, shape controls, and image sources.
- Update `ROADMAPv2.md` checkboxes as files and acceptance criteria are completed.

## Capabilities

### New Capabilities
- `advanced-rendering-media`: Covers backend-neutral media descriptors, advanced drawing commands, shape controls, and adapter/test coverage for the retained rendering surface.

### Modified Capabilities
- `retained-rendering-cache`: Retained rendering must continue to compose advanced shape commands through `DrawCommandList` without backend-specific dependencies.
- `first-controls-panels`: Controls gain shape controls while preserving the existing backend-neutral controls contract.
- `resource-services`: Resource-backed brush, image, or media identities must remain explicit and versionable instead of becoming hidden backend objects.

## Impact

- Affected code: `UI/Drawing`, new `UI/Media`, new `UI/Controls/Shapes`, retained rendering integration where needed, and tests under `tests/Cerneala.Tests`.
- APIs: new public media and shape APIs plus small drawing command additions that have tests and backend adapter behavior.
- Dependencies: no new production dependency is expected; MonoGame-specific rendering remains isolated under the existing MonoGame drawing backend.
