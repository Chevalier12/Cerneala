## Why

The retained UI foundation now has typed state, tree ownership, invalidation, layout, rendering, host integration, input, and commands, but it still lacks usable controls that an app can compose. This change creates the first retained control set so `UIRoot` can host real UI instead of raw elements and low-level panels.

## What Changes

- Add `Control` as the base retained control with common visual and text-related typed properties.
- Add retained content primitives: `ContentControl`, `Decorator`, `Border`, `TextBlock`, `Image`, and `Button`.
- Expose controls-facing panel aliases/wrappers for `Panel`, `Canvas`, and `StackPanel` over the existing layout panel behavior.
- Keep `ButtonBase` as the primitive base and add the concrete `Button` control on top of it.
- Add minimal visual state naming for hover, pressed, focused, and disabled states.
- Render controls through existing `DrawingContext`/`DrawCommandList` paths and participate in retained render cache invalidation.
- Keep templates, toggle/check controls, and full text-service caching out of this change unless required by the MVP acceptance tests.

## Capabilities

### New Capabilities

- `first-controls-panels`: First retained control set, common control properties, controls-facing panels, direct rendering controls, and MVP button/text/border/image behavior.

### Modified Capabilities

- `layout-system`: Controls-facing panel types participate in the existing retained layout behavior.
- `retained-rendering-cache`: Control render hooks emit drawing commands into retained render caches.
- `retained-input-bridge`: Button controls use retained hover, pressed, focus, click, and command behavior.
- `command-router-actions`: Concrete buttons execute direct and routed commands through existing command routing.
- `retained-ui-mvp-foundation`: MVP roadmap progress includes first controls and panels.

## Impact

- Affected production code:
  - `UI/Controls/Control.cs`
  - `UI/Controls/ContentControl.cs`
  - `UI/Controls/Decorator.cs`
  - `UI/Controls/Border.cs`
  - `UI/Controls/Panel.cs`
  - `UI/Controls/Canvas.cs`
  - `UI/Controls/StackPanel.cs`
  - `UI/Controls/TextBlock.cs`
  - `UI/Controls/Image.cs`
  - `UI/Controls/Button.cs`
  - `UI/Controls/VisualState.cs`
  - `UI/Controls/Primitives/ButtonBase.cs`
- Affected tests:
  - `tests/Cerneala.Tests/Controls/ControlTests.cs`
  - `tests/Cerneala.Tests/Controls/ContentControlTests.cs`
  - `tests/Cerneala.Tests/Controls/DecoratorTests.cs`
  - `tests/Cerneala.Tests/Controls/BorderTests.cs`
  - `tests/Cerneala.Tests/Controls/PanelTests.cs`
  - `tests/Cerneala.Tests/Controls/CanvasTests.cs`
  - `tests/Cerneala.Tests/Controls/StackPanelTests.cs`
  - `tests/Cerneala.Tests/Controls/TextBlockTests.cs`
  - `tests/Cerneala.Tests/Controls/ImageTests.cs`
  - `tests/Cerneala.Tests/Controls/Primitives/ButtonBaseTests.cs`
  - `tests/Cerneala.Tests/Controls/ButtonTests.cs`
- Affected planning:
  - `ROADMAPv2.md`
  - `openspec/specs/first-controls-panels/spec.md`
  - `openspec/specs/layout-system/spec.md`
  - `openspec/specs/retained-rendering-cache/spec.md`
  - `openspec/specs/retained-input-bridge/spec.md`
  - `openspec/specs/command-router-actions/spec.md`
  - `openspec/specs/retained-ui-mvp-foundation/spec.md`
