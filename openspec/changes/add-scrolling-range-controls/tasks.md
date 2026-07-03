## 1. Range Primitives

- [x] 1.1 Create `UI/Controls/Primitives/RangeBase.cs`.
- [x] 1.2 Add typed `Minimum`, `Maximum`, `Value`, `SmallChange`, and `LargeChange` properties.
- [x] 1.3 Coerce `Value` when it is set outside `[Minimum, Maximum]`.
- [x] 1.4 Coerce `Value` when `Minimum` or `Maximum` changes.
- [x] 1.5 Ensure range property changes use retained invalidation metadata.
- [x] 1.6 Add `tests/Cerneala.Tests/Controls/Primitives/RangeBaseTests.cs`.

## 2. Drag And Track Primitives

- [x] 2.1 Create `UI/Controls/Primitives/Thumb.cs`.
- [x] 2.2 Implement retained drag start, drag delta, and drag completion using existing pointer capture/input behavior.
- [x] 2.3 Create `UI/Controls/Primitives/Track.cs`.
- [x] 2.4 Map range values to thumb arrange position for horizontal and vertical orientation.
- [x] 2.5 Convert thumb drag deltas back into range value changes.
- [x] 2.6 Add decrease/increase region behavior for deterministic small or large range changes.
- [x] 2.7 Add `tests/Cerneala.Tests/Controls/Primitives/ThumbTests.cs`.
- [x] 2.8 Add `tests/Cerneala.Tests/Controls/Primitives/TrackTests.cs`.

## 3. ScrollBar

- [x] 3.1 Create `UI/Controls/Primitives/ScrollBar.cs`.
- [x] 3.2 Compose scrollbar behavior from `RangeBase`, `Track`, and `Thumb`.
- [x] 3.3 Support horizontal and vertical scrollbar orientation.
- [x] 3.4 Ensure scrollbar value follows thumb drag and clamps to range.
- [x] 3.5 Ensure scrollbar can be templated while retaining generated track/thumb children across frames.
- [x] 3.6 Add `tests/Cerneala.Tests/Controls/ScrollBarTests.cs`.

## 4. ScrollViewer And Scroll Presenter

- [x] 4.1 Create `UI/Controls/ScrollBarVisibility.cs`.
- [x] 4.2 Create `UI/Controls/IScrollInfo.cs`.
- [x] 4.3 Create `UI/Controls/ScrollContentPresenter.cs`.
- [x] 4.4 Create `UI/Controls/ScrollViewer.cs`.
- [x] 4.5 Measure content extent and viewport size deterministically.
- [x] 4.6 Clamp horizontal and vertical offsets to scrollable range.
- [x] 4.7 Apply offset during arrange/render/hit-test without unnecessary measure invalidation.
- [x] 4.8 Handle retained mouse wheel input for vertical scrolling.
- [x] 4.9 Implement disabled, hidden, visible, and auto scrollbar visibility policy.
- [x] 4.10 Add `tests/Cerneala.Tests/Controls/ScrollViewerTests.cs`.

## 5. Slider And ProgressBar

- [x] 5.1 Create `UI/Controls/Slider.cs`.
- [x] 5.2 Implement slider value updates through retained track/thumb behavior.
- [x] 5.3 Support horizontal and vertical slider orientation.
- [x] 5.4 Create `UI/Controls/ProgressBar.cs`.
- [x] 5.5 Render progress ratio through retained rendering without input behavior.
- [x] 5.6 Add `tests/Cerneala.Tests/Controls/SliderTests.cs`.
- [x] 5.7 Add `tests/Cerneala.Tests/Controls/ProgressBarTests.cs`.

## 6. Lightweight Controls And Popups

- [x] 6.1 Create `UI/Controls/RadioButton.cs`.
- [x] 6.2 Add typed checked state and retained click behavior for `RadioButton`.
- [x] 6.3 Create `UI/Controls/Label.cs`.
- [x] 6.4 Ensure `Label` hosts retained content through existing content/presenter behavior.
- [x] 6.5 Create `UI/Controls/PopupRoot.cs`.
- [x] 6.6 Create `UI/Controls/ToolTip.cs`.
- [x] 6.7 Ensure tooltip content is hosted through retained popup root behavior, not platform popup APIs.
- [x] 6.8 Add `tests/Cerneala.Tests/Controls/ToolTipTests.cs`.

## 7. Integration And Boundaries

- [x] 7.1 Prove new controls participate in retained layout, rendering, hit testing, and routed input.
- [x] 7.2 Prove range/scrolling controls can use styles and templates without rebuilding generated children every frame.
- [x] 7.3 Extend architecture boundary tests proving section 16 controls avoid MonoGame, Skia, HarfBuzz, `Texture2D`, and `SpriteBatch`.
- [x] 7.4 Preserve existing controls, templates, layout, input, and styling tests.

## 8. Roadmap And Validation

- [x] 8.1 Update `ROADMAPv2.md` section 16 file checklist as files and tests are completed.
- [x] 8.2 Update `ROADMAPv2.md` section 16 behavioral completion notes if acceptance details are added during implementation.
- [x] 8.3 Verify `openspec validate add-scrolling-range-controls --strict` passes.
- [x] 8.4 Verify `openspec validate --all --strict` passes.
- [x] 8.5 Verify `dotnet build Cerneala.slnx -warnaserror` passes.
- [x] 8.6 Verify `dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj` passes.
