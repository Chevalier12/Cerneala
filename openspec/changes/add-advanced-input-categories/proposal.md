## Why

Cerneala already declares touch, stylus, manipulation, and drag/drop routed event metadata, but there is no behavior behind those categories yet. Section 26 turns that metadata into backend-neutral input primitives so platform adapters can feed advanced input without coupling UI core to MonoGame, OS APIs, or designer-only assumptions.

## What Changes

- Add backend-neutral touch and stylus bridges that dispatch retained routed events from explicit snapshots.
- Add a small gesture recognizer and manipulation processor for deterministic tap/drag/pinch-style state transitions.
- Add drag/drop primitives with `DataTransfer` and a `DragDropController`.
- Add cursor primitives with `Cursor` and `CursorService`.
- Add retained ink primitives: `Stroke`, `StrokeCollection`, and an `InkCanvas` control that records stylus/touch input.
- Add tests for each advanced input category and update `ROADMAPv2.md` section 26 as items complete.

## Capabilities

### New Capabilities

- `advanced-input-categories`: Touch, stylus, gesture, manipulation, drag/drop, cursor, and ink APIs above retained input routing.

### Modified Capabilities

- `retained-input-bridge`: Existing metadata-only touch/stylus/manipulation/drag/drop routed events gain backend-neutral dispatch paths and typed event args.

## Impact

- Adds `UI/Input/TouchInputBridge.cs`, `StylusInputBridge.cs`, `GestureRecognizer.cs`, `ManipulationProcessor.cs`, `DragDropController.cs`, `DataTransfer.cs`, `Cursor.cs`, and `CursorService.cs`.
- Adds `UI/Controls/InkCanvas.cs`, `UI/Ink/Stroke.cs`, and `UI/Ink/StrokeCollection.cs`.
- Adds focused tests under `tests/Cerneala.Tests/Input` and `tests/Cerneala.Tests/Controls`.
- No new platform-specific dependencies.
