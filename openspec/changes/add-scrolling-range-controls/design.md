## Context

Cerneala already has retained elements, typed properties, layout, retained rendering, hit testing, routed input, pointer capture, commands, styling, themes, and code-first templates. Section 16 builds on that stack by adding the controls needed for value ranges and scrolling: range primitives, thumbs/tracks, scrollbars, scroll viewers, sliders, progress bars, labels, radio buttons, tooltips, and popup roots.

These controls must be retained controls, not immediate-mode widgets. Their state should live in typed properties, their visual structure should be template-friendly, their input behavior should use existing routed input and pointer capture, and their layout/render effects should flow through existing invalidation metadata.

## Goals / Non-Goals

**Goals:**
- Add reusable range primitives: `RangeBase`, `Thumb`, `Track`, and `ScrollBar`.
- Add scroll viewer primitives and contracts: `ScrollViewer`, `ScrollContentPresenter`, `ScrollBarVisibility`, and `IScrollInfo`.
- Add user-facing controls: `Slider`, `ProgressBar`, `RadioButton`, `Label`, `ToolTip`, and `PopupRoot`.
- Keep all new controls backend-neutral and retained-tree friendly.
- Use typed properties for value, extent, viewport, offset, orientation, visibility, and selection-like state.
- Use existing pointer capture/routed input for drag and wheel behavior.
- Use existing templates and presenters for visual composition where useful.
- Add focused tests matching the roadmap file list and behavioral acceptance implied by scrolling/range controls.

**Non-Goals:**
- No item virtualization; that belongs to section 17.
- No full selection system or grouped radio-button manager beyond a minimal single-control checked state.
- No OS-level popups, windows, portals, or platform overlays.
- No animation, transitions, keyboard navigation polish, or accessibility tree work.
- No XAML/markup templates.

## Decisions

### RangeBase owns range coercion

`RangeBase` should define typed `Minimum`, `Maximum`, `Value`, `SmallChange`, and `LargeChange` properties. Coercion keeps `Value` inside `[Minimum, Maximum]` and keeps range endpoints deterministic when minimum or maximum changes.

Rationale: sliders, scrollbars, and progress bars all need the same value math. Putting it in one primitive avoids duplicated, drift-prone behavior.

Alternative considered: each control implements its own range validation. Rejected because the behavior would drift immediately.

### Thumb and Track use retained input, not polling

`Thumb` should be a retained draggable primitive. It should raise drag-start, drag-delta, and drag-completed behavior through typed/routed mechanisms or explicit events, using existing pointer capture. `Track` should translate between range values and thumb positions for horizontal/vertical orientation.

Rationale: dragging belongs in the retained input system. Polling mouse state inside render/layout would be brittle and frame-order dependent.

Alternative considered: implement drag in each `Slider`/`ScrollBar`. Rejected because it duplicates capture and delta handling.

### ScrollViewer separates viewport, extent, and offset

`ScrollContentPresenter` should measure content, compute extent/viewport data, and expose offset behavior through `IScrollInfo`. `ScrollViewer` should own scrollbars, visibility policy, wheel handling, and offset synchronization with the presenter.

Rationale: scrolling is layout state plus input state. Keeping extent/viewport/offset explicit makes tests deterministic and prepares for virtualization later without implementing it now.

Alternative considered: use a translated visual child with ad hoc fields on `ScrollViewer`. Rejected because it hides layout state and makes later virtualization harder.

### Controls compose through templates where practical

`ScrollBar`, `Slider`, `ProgressBar`, `RadioButton`, `Label`, and `ToolTip` should be normal retained controls that can be styled and templated. The MVP can provide direct behavior first, but visual parts should align with `ControlTemplate`, `ContentPresenter`, and existing panels.

Rationale: section 15 exists specifically so section 16 controls do not hard-code every visual forever.

Alternative considered: draw all controls directly in `OnRender`. Rejected as a short-term hack that fights the new template system.

### PopupRoot is retained, not platform-native

`PopupRoot` should be a retained overlay root within the current `UIRoot`/visual tree model. It should allow tooltip-like content to participate in layout/render/input without introducing OS windows.

Rationale: native popups are platform work and belong later. A retained overlay root is enough for tooltips and tests now.

Alternative considered: create platform windows or MonoGame-specific overlay handling. Rejected because controls must remain backend-neutral.

## Risks / Trade-offs

- [Risk] Scroll math can get off-by-one or NaN-prone. -> Centralize clamping/coercion and add tests for min/max/value/viewport/extent edge cases.
- [Risk] Dragging can leak pointer capture. -> Test drag start, delta, completion, cancellation, and disabled behavior.
- [Risk] ScrollViewer can over-invalidate layout every frame. -> Offset-only changes should invalidate arrange/render/hit-test as needed, not measure unless extent/viewport changes.
- [Risk] ToolTip/PopupRoot can become platform popup scope creep. -> Keep it retained and in-tree for this phase.
- [Risk] RadioButton can accidentally imply full selection/grouping. -> Keep MVP minimal and defer group selection behavior to the later items/selection phase unless tests require a tiny typed seam.
