# Plan: ScrollViewer, ScrollBar and Track Template Parts

> Date: 2026-07-13
> Status: completed
> Dependency: `docs/plans/2026-07-13-repeat-button.md`
> Goal: replacing hardcoded composition with functional template parts and adding scrollbar directional buttons

## 1. Summary

The current implementation directly builds `ScrollContentPresenter`, two `ScrollBar`, one `Track` and one `Thumb`. When a `ComponentTemplate` is applied, the fallbacks can be removed from the tree, but the controls continue to synchronize the old private instances. The template looks new, and the logic speaks to the furniture from the former apartment.

The plan introduces explicit party contracts, default functional templates and a common lifecycle for resolving/unsubscribing parties. `ScrollViewer` will operate on the presenter and scrollbars of the active template. `ScrollBar` will operate on the track and the two active `RepeatButton`s. `Track` will operate on the active thumb.

The default template of `ScrollBar` will display directional buttons at the ends. These produce small changes. Clicking on the area of ​​the track before or after the thumb remains a big change.

## 2. Composition contract

Target structure:
```text
ScrollViewer
  PART_ScrollContentPresenter : ScrollContentPresenter
  PART_HorizontalScrollBar    : ScrollBar
  PART_VerticalScrollBar      : ScrollBar

ScrollBar
  PART_DecreaseButton         : RepeatButton
  PART_Track                  : Track
  PART_IncreaseButton         : RepeatButton

Track
  PART_Thumb                  : Thumb
```
Semantics of parts:

- `PART_ScrollContentPresenter`, `PART_HorizontalScrollBar` and `PART_VerticalScrollBar` are mandatory for a functional `ScrollViewer`.
- `PART_Track` is mandatory for a functional `ScrollBar`.
- `PART_DecreaseButton` and `PART_IncreaseButton` are optional for minimalist templates, but the default template provides them.
- `PART_Thumb` is mandatory for a dredging `Track`.
- Arrows are not thumbs. There is only one thumb, that is the mobile handle.

## 3. Objectives

- [x] `ScrollViewer`, `ScrollBar` and `Track` declare `[TemplatePart]` for the above contracts.
- [x] All logic uses the parts of the active template, not fallback instances left in private fields.
- [x] Changing or removing the template unsubscribes the old parts.
- [x] Invalid templates fail early with messages calling the missing part or the wrong type.
- [x] The default template `ScrollViewer` uses a real root layout and keeps scrollbar convergence `Auto`.
- [x] The default template `ScrollBar` works vertically and horizontally.
- [x] The directional buttons use `SmallChange` and repeat as long as they are pressed.
- [x] The click on the track uses `LargeChange`.
- [x] The thumb drag continues to update the value and offset of the viewer.
- [x] `ScrollEventType` correctly reflects the source of the change.
- [x] public APIs `Presenter`, `HorizontalScrollBar`, `VerticalScrollBar`, `Track` and `Thumb` expose the active parts.
- [x] The idle layout does not leave measure/arrange work remaining.

## 4. Non-objectives
- [x] We do not introduce inertial scrolling or overscroll.
- [x] We do not introduce overlay scrollbars or animated auto-hide.
- [x] We are not changing the existing policy `Disabled`, `Auto`, `Hidden`, `Visible`.
- [x] We do not rewrite `IScrollInfo` or virtualization.
- [x] We do not add public WPF commands like `LineUpCommand` if the buttons can be linked simply and testably to `ScrollBar`.
- [x] We are not making a general system of template triggers in this change.
- [x] We don't move the scrolling logic to the aspect system just because we can make the shit more abstract.

## 5. The proposed architecture

### 5.1 Common Lifecycle for Parts

It extends `Control` with a protected hook called after each application or removal of templates and with a typed resolver for parts. The exact form may vary, but the contract must allow:
```csharp
protected virtual void OnTemplateApplied(ComponentTemplateInstance? instance);
protected TElement GetRequiredTemplatePart<TElement>(string name)
    where TElement : UIElement;
protected TElement? GetOptionalTemplatePart<TElement>(string name)
    where TElement : UIElement;
```
Rules:

- the derived control is first unsubscribed from the previously memorized parties;
- solve and validate the parts of the new court;
- only then connect the events and synchronize the state;
- if the solution fails, the new court is detached/disposed of and the control does not remain half-applied;
- `ApplyTemplate()` remains idempotent for the same court.

### 5.2 Default ScrollViewer template

The default template uses a `Grid` with two rows and two columns:
```text
*    | Auto
-----+-----
Auto | corner
```
Placement:

- presenter in row 0, column 0;
- vertical scrollbar in row 0, column 1;
- horizontal scrollbar in row 1, column 0;
- the corner can remain empty in MVP.

`ScrollViewer.MeasureCore` applies the template and measures the root in up to three passes. After each pass, it reads the extent/viewport from the active presenter, recalculates the visibility of the parts and remeasures only if the state has changed. `ArrangeCore` repeats the same convergence for the final viewport. Thus, the template establishes the layout, and the owner establishes the scrolling policy.

### 5.3 ScrollBar default template

The default template uses a small, adjustable internal panel that distributes:
```text
DecreaseButton | Track flexibil | IncreaseButton
```
For vertical, the order is top, center, bottom. For horizontal it is left, center, right. The panel receives `Orientation` through template binding and does not require recreating the template when the orientation changes.

The buttons contain simple and clear directional glyphs. The glyph is for presentation only; the semantics comes from the fact that the parts are decreased/increased.

### 5.4 Track default template

The default template uses an internal root that arranges `PART_Thumb` according to the geometry calculated by `Track`. Custom templates can replace the presentation, but must provide a valid `Thumb` and respect the axis/orientation.

The geometry of the value, the length of the thumb and the pointer-value conversion remain in one place in `Track`; the formulas in the templates and in the control are not copied.

## 6. Estimated files
Possible new files:

- `UI/Controls/ScrollViewerTemplates.cs`;
- `UI/Controls/Primitives/ScrollBarTemplates.cs`;
- `UI/Controls/Primitives/ScrollBarLayoutPanel.cs`;
- `UI/Controls/Primitives/TrackTemplates.cs`;
- `UI/Controls/Primitives/TrackLayoutPanel.cs`;
- `UI/Controls/Primitives/TrackValueChangedEventArgs.cs`, only if necessary for the reason of the change;
- dedicated tests for part lifecycle and template swap.

Probably changed files:

- `UI/Controls/Control.cs`;
- `UI/Controls/ScrollViewer.cs`;
- `UI/Controls/Primitives/ScrollBar.cs`;
- `UI/Controls/Primitives/Track.cs`;
- `UI/Controls/Primitives/ScrollEventArgs.cs` only if the existing contract needs to be clarified;
- `tests/Cerneala.Tests/Controls/ComponentTemplateLifecycleTests.cs`;
- `tests/Cerneala.Tests/Controls/ScrollViewerTests.cs`;
- `tests/Cerneala.Tests/Controls/ScrollBarTests.cs`;
- `tests/Cerneala.Tests/Controls/Primitives/TrackTests.cs`;
- the corresponding API pages from `docs-site/documentation/classes/`;
- `docs-site/documentation/manifest.json` if new public types appear.

## 7. Implementation stages

### Stage 0 - Baseline and characterization contracts

- [x] Complete the `RepeatButton` plan and confirm that his tests are green.
- [x] Generates `FileTree.md` and updates the RoslynIndexer index.
- [x] Run the existing tests for `ScrollViewer`, `ScrollBar`, `Track`, `Thumb`, template lifecycle and layout scheduler.
- [x] Add characterization tests for wheel, drag, track click, visibility policy and convergence `Auto`.
- [x] Add characterization tests for the idle frame with no work remaining.
- [x] Add a test that demonstrates the current defect: a track provided by templates does not control `ScrollBar`.
- [x] Add a test that demonstrates the current bug: the parts of a `ScrollViewer` template do not become the active parts.
- [x] Add a test that demonstrates the current bug: a template thumb is not the source of the active drag.

**Gate Stage 0**

- [x] The existing baseline is green.
- [x] The three template defects are reproduced by red tests.
- [x] The public API has not changed yet.

### Stage 1 - Lifecycle of the parties in Control

- [x] Add the typed resolvers for mandatory and optional parts.
- [x] Add the protected hook for applied/removed templates.
- [x] Calls the hook also when `ComponentTemplate` becomes `null`.
- [x] Defines the exact order: detach old subscriptions, dispose old instance, attach new instance, validate parts, publish instance.
- [x] If the hook of the new instance crashes, return to a coherent state without partial root attached.
- [x] Keep `ApplyTemplate()` idempotent.
- [x] Add tests for repeated apply, template swap, null template, missing part and wrong type.
- [x] Add tests that check the lack of handlers left on the old parts.
- [x] Check `CheckBox` and migrate it to the helper only if it reduces code without regression.

**Gate stage 1**

- [x] Lifecycle is covered independently of scrolling.
- [x] An old part can no longer change the owner after template swap.
- [x] The errors indicate the name of the part and the expected type.
- [x] All existing templated controls remain green.

### Stage 2 - Track and PART_Thumb

- [x] Declare `[TemplatePart("PART_Thumb", typeof(Thumb))]` on `Track`.
- [x] Enter the default template and register `PART_Thumb` through `RequirePart`.
- [x] Replaces the readonly field used as fallback with reference to the active thumb.
- [x] Connect `DragDelta` only to the active thumb.
- [x] Disconnect `DragDelta` from the old thumb to template swap/null/detach.
- [x] Keeps the current formulas for range, ratio, viewport and thumb length.
- [x] Move the default layout to a dedicated root/panel without duplicating the formulas.
- [x] Keep the click before/after thumb as `LargeDecrement`/`LargeIncrement`.
- [x] Enter an internal reason for the change if `ValueChanged` simply cannot inform `ScrollBar` correctly.
- [x] Keep `Thumb` as public property returning the active part.

Tests stage 2:

- [x] The default thumb is positioned identically to the baseline.
- [x] The proportional thumb uses `ViewportSize`.
- [x] The custom thumb drag changes the value.
- [x] The old thumb drag no longer changes the value after template swap.
- [x] Track click before the thumb produces large decrement.
- [x] Track click after thumb produces large increment.
- [x] Horizontal and vertical orientation use the correct axis.
- [x] Zero range and track shorter than the thumb minimum remain stable.

**Gate stage 2**

- [x] `Track` no longer syncs an invisible thumb.
- [x] Geometry is not duplicated between control and root templates.
- [x] The old and new tests for drag/layout are green.

### Stage 3 - ScrollBar, PART_Track and the directional buttons

- [x] Declares parts `PART_Track`, `PART_DecreaseButton` and `PART_IncreaseButton`.
- [x] Creates the default orientable template with `RepeatButton` at the ends.
- [x] Solve `PART_Track` as a mandatory part.
- [x] Resolve the two buttons as optional parts to allow scrollbars without arrows.
- [x] Synchronizes `Minimum`, `Maximum`, `Value`, `SmallChange`, `LargeChange`, `ViewportSize` and `Orientation` to the active track.
- [x] Synchronizes track changes back to `ScrollBar.Value`.
- [x] Link decreased to `Track.DecreaseSmall()` and increased to `Track.IncreaseSmall()`.
- [x] Does not execute the small change both through `Command` and through the handler; choose only one internal road.
- [x] Raise `Scroll` with `SmallDecrement` or `SmallIncrement` for darts.
- [x] Raise `LargeDecrement` or `LargeIncrement` for clicks on the track.
- [x] Raise `ThumbTrack` for love only, not for any change in value.
- [x] Decide and test if `EndScroll` rises to release; don't add it just as decoration if there is no consumer.
- [x] Keep `Track` as public property returning the active part.
- [x] Disconnect all events of old parts to template swap.

Tests stage 3:

- [x] Vertical scrollbar displays the up/down buttons and the track between them.
- [x] Horizontal scrollbar displays left/right buttons.
- [x] The change `Orientation` rearranges the template without manual recreation.
- [x] The initial click on decrease changes the value with `SmallChange`.
- [x] Pressed youth repeats the change according to `RepeatButton`.
- [x] The value stops at `Minimum`/`Maximum` without additional false events.
- [x] A template without buttons remains draggable and page-scrollable.
- [x] A custom track becomes the real source of value.
- [x] The old track no longer changes the scrollbar after swap.
- [x] `ScrollEventType` corresponds to each interaction.

**Gate stage 3**

- [x] The default scrollbar has functional arrows.
- [x] Minimalist templates without arrows are allowed.
- [x] There is no synchronization with detached tracks.
- [x] Events no longer label the page click as `ThumbTrack`.

### Stage 4 - ScrollViewer and active parts

- [x] Declare the three mandatory parts of `ScrollViewer`.
- [x] Create the default template with `Grid`, presenter and two scrollbars.
- [x] Sets the default template via the same value source used by existing themed controls.
- [x] Eliminates the hardcoded construction and ownership of the three children in the constructor.
- [x] Solve and memorize the active parts when applying the template.
- [x] Link `Content` to the active presenter and update it when the content changes.
- [x] Binds changes of the offset of the active presenter to the synchronization of the scrollbars.
- [x] Binds the active scrollbar values ​​back to the presenter.
- [x] Keep the existing public properties, but make them return the active parts.
- [x] Defines the behavior of accessing the properties before the measure: the constructor must apply the default template or the getter must call `ApplyTemplate()`.
- [x] Do not return `null` from existing non-null public properties.
- [x] Disconnect the old presenter and scrollbars from template swap.

**Gate Stage 4**

- [x] Viewer no longer has hardcoded copies in parallel with the template.
- [x] Content, offset and values ​​are synchronized only with the active parts.
- [x] The existing public API remains compatible at the level of nullability and use.

### Stage 5 - Convergence of the layout through the root template

- [x] Extracts the calculation `ShowsScrollBar`, `ReservesSpace` and `ToVisibility` without duplication between measure and arrange.
- [x] It measures the root of the template, not the three parts as hardcoded brothers of the owner.
- [x] After each measure, recalculates the need for bars from the extent and viewport of the active presenter.
- [x] Repeat at most three passes and keep the fallback conservative when the state oscillates.
- [x] In arrange, reevaluate against the final size and rearrange the root only when the visibility has changed.
- [x] Keeps the semantics of `Hidden`: active scrolling, reserved space, visually hidden.
- [x] Keep the semantics of `Disabled`: scrolling off, zero offset, collapsed bar.
- [x] Remove the `ConsumeOwnedScrollBarLayoutWork` workarounds only after the tests prove that the root template does not leave work late. (It was not completely eliminated: visibility invalidation still programs the ancestors, and the remaining consumption is limited to the hierarchy of active parts and to measure/arrange already executed synchronously.)
- [x] If workarounds remain necessary, document the cause and limit them to active parts.
- [x] Check unbounded measure, increasing/decreasing content and the interaction between bars `Auto`.

Tests stage 5:

- [x] One `Auto` bar can force the appearance of the other.
- [x] The bars disappear when the content shrinks.
- [x] Unbounded measure produces correct desired size.
- [x] Arrange smaller than measure reevaluate the bars.
- [x] `Hidden` reserves space through the template layout.
- [x] `Visible` remains visible without overflow.
- [x] The following unmodified frame reports zero measure/arrange/render work.
- [x] A custom template with the same parts keeps scrolling functional.

**Gate Stage 5**

- [x] The layout of the template replaces the hardcoded geometry without regressions.
- [x] Convergence is limited and deterministic.
- [x] There are no permanent invalidations or outstanding work on the idle frame.

### Stage 6 - Template swap, errors and robustness

- [x] Change the `Track` template during life and check the new thumb.
- [x] Change the template `ScrollBar` and check the new track/buttons.
- [x] Change the `ScrollViewer` template and check the new presenter/bars.
- [x] Check null templates and return to the default template according to the established policy.
- [x] Check for missing, duplicate or wrong type parts.
- [x] Checks the detachment of the entire viewer during drag or repeat.
- [x] Checks the content change during a scroll repetition.
- [x] Check that the old parts do not retain the owner through handlers.
- [x] Checks the reattachment of the same viewer to another root.
- [x] Add tests with weak references only if the direct lifecycle tests cannot demonstrate the absence of retention. (It was not necessary: the direct tests exercise the old parts after swap/detach and demonstrate that the owner is no longer modified.)

**Gate stage 6**

- [x] Template swap does not leave statuses or subscriptions stale.
- [x] Author errors are clear and appear when applying the template.
- [x] Detach cancels drag and repeat without further activations.

### Stage 7 - Markup, documentation and final verification

- [x] Add source generator tests for the name `PART_*` in markup templates.
- [x] Checks the validation of the party types declared by `[TemplatePart]`.
- [x] Add a Playground sample that shows vertical and horizontal scrollbar with arrows.
- [x] Visually check the glyphs, hit targets and orientation at different scales.
- [x] Update the API docs for `Control`, `ScrollViewer`, `ScrollBar`, `Track` and any new public type using the `writing-api-documentation` skill.
- [x] Corrects in the same change the old sections that state that `ScrollBar` or `ScrollViewer` do not declare events.
- [x] Updates `docs-site/documentation/manifest.json` for new or renamed pages. (It wasn't necessary: no public types, new API pages, or renamed pages appeared.)
- [x] Reindexes after each code or project change.
- [x] Run the targeted tests for controls, template lifecycle, markup and layout scheduler.
- [x] Runs `dotnet test Cerneala.slnx`.
- [x] Check the API public diff and compare it with the contract of this plan.

**Gate stage 7**

- [x] The whole suite is green.
- [x] Playground demonstrates arrows, drag, page click and wheel.
- [x] The documentation describes the active parts and the actual lifecycle.
- [x] There are no invisible private courts that continue to receive synchronization.

## 8. Recommended order

- [x] Ends `RepeatButton`.
- [x] Completes the common lifecycle of the templates.
- [x] Migrate `Track`.
- [x] Migrate `ScrollBar` and add arrows.
- [x] Migrate `ScrollViewer`.
- [x] Only then delete the old fallbacks and workarounds.

Order is important. If we start with `ScrollViewer` and let `ScrollBar` synchronize the phantom track, we get a beautiful interface that moves the dick.

## 9. The definition of ready

The implementation is ready when the default template displays and operates the directional buttons, the track and the thumb; all controls use the active parts of the template; changing the template completely moves the logic and subscriptions to the new parts; visibility and convergence policies remain correct; and an idle frame does not save layout work. `PART_*` must be functional contracts, not labels stuck on empty boxes.