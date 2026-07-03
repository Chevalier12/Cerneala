## Why

Cerneala now has retained controls, styling, code-first templates, input routing, and layout, but it still lacks the controls needed for real scrollable and value-driven UI. Section 16 of `ROADMAPv2.md` adds range primitives, scrolling primitives, and a small set of additional controls so applications can build usable panels without every project hand-rolling the same controls repeatedly.

## What Changes

- Add range primitives under `UI/Controls/Primitives`: `RangeBase`, `Thumb`, `Track`, and `ScrollBar`.
- Add scrolling controls and contracts: `ScrollViewer`, `ScrollContentPresenter`, `ScrollBarVisibility`, and `IScrollInfo`.
- Add first range/user-facing controls: `Slider` and `ProgressBar`.
- Add additional lightweight controls: `RadioButton`, `Label`, `ToolTip`, and `PopupRoot`.
- Ensure scrolling and dragging use retained layout, hit testing, routed input, pointer capture, typed properties, templates, and invalidation rather than backend-specific state.
- Add focused tests for range primitives, dragging, track math, scrollbars, scroll viewer behavior, sliders, progress bars, and tooltips.
- Update `ROADMAPv2.md` section 16 as files, tests, and behavior are completed.

## Capabilities

### New Capabilities
- `scrolling-range-controls`: Covers retained range primitives, scrollbars, scroll viewers, sliders, progress bars, radio buttons, labels, tooltips, popup roots, and their layout/input/template integration.

### Modified Capabilities

## Impact

- Adds new controls under `UI/Controls` and `UI/Controls/Primitives`.
- Adds tests under `tests/Cerneala.Tests/Controls` and `tests/Cerneala.Tests/Controls/Primitives`.
- Reuses existing retained element tree, typed properties, template system, styling, layout, rendering, hit testing, routed input, pointer capture, and command behavior.
- Does not add virtualization, selection collections, advanced text editing, platform popups, OS windows, or markup.
