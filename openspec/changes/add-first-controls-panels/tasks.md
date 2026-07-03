## 1. Control Base

- [x] 1.1 Add `UI/Controls/Control.cs` with background, foreground, border color, border thickness, padding, font family, and font size typed properties.
- [x] 1.2 Ensure control visual properties map to render/input visual invalidation and metric properties map to measure/render invalidation.
- [x] 1.3 Add `UI/Controls/VisualState.cs` with minimal hover, pressed, focused, disabled state names.
- [x] 1.4 Add `tests/Cerneala.Tests/Controls/ControlTests.cs` for property defaults, invalidation behavior, and backend-neutral dependencies.

## 2. Content and Decorator Controls

- [x] 2.1 Add `UI/Controls/ContentControl.cs` with retained `Content` ownership for `UIElement` content.
- [x] 2.2 Add `UI/Controls/Decorator.cs` with one retained child and child replacement behavior.
- [x] 2.3 Add layout behavior for content/decorator controls so child measure and arrange include padding/border semantics where applicable.
- [x] 2.4 Add `tests/Cerneala.Tests/Controls/ContentControlTests.cs` for logical/visual ownership, replacement, and invalidation.
- [x] 2.5 Add `tests/Cerneala.Tests/Controls/DecoratorTests.cs` for child ownership, layout, and replacement.

## 3. Controls-Facing Panels

- [x] 3.1 Add `UI/Controls/Panel.cs` as the controls-facing panel type that reuses existing retained panel layout behavior.
- [x] 3.2 Add `UI/Controls/Canvas.cs` as the controls-facing canvas type and preserve existing canvas coordinate behavior.
- [x] 3.3 Add `UI/Controls/StackPanel.cs` as the controls-facing stack panel type and preserve orientation behavior.
- [x] 3.4 Add `tests/Cerneala.Tests/Controls/PanelTests.cs` for visual child layout behavior.
- [x] 3.5 Add `tests/Cerneala.Tests/Controls/CanvasTests.cs` for controls-facing canvas layout parity.
- [x] 3.6 Add `tests/Cerneala.Tests/Controls/StackPanelTests.cs` for controls-facing stack panel layout parity and no unnecessary re-measure.

## 4. Visual Content Controls

- [x] 4.1 Add `UI/Controls/Border.cs` with background fill, border stroke, padding/border measure, inner child arrange, and rectangle drawing commands.
- [x] 4.2 Add minimal backend-neutral text measuring/rendering seam needed by `TextBlock` without implementing full roadmap section 11 text services.
- [x] 4.3 Add `UI/Controls/TextBlock.cs` with `Text` property, text measure, text render, text metric invalidation, and foreground render invalidation.
- [x] 4.4 Add `UI/Controls/Image.cs` using `IDrawImage` source, render invalidation, and intrinsic-size measure behavior where supported by the draw image abstraction.
- [x] 4.5 Add `tests/Cerneala.Tests/Controls/BorderTests.cs` for layout and rectangle drawing commands.
- [x] 4.6 Add `tests/Cerneala.Tests/Controls/TextBlockTests.cs` for text measure/render/invalidation boundaries.
- [x] 4.7 Add `tests/Cerneala.Tests/Controls/ImageTests.cs` for source rendering and source invalidation.

## 5. Button Control

- [x] 5.1 Keep `UI/Controls/Primitives/ButtonBase.cs` as the primitive command/pressed-state base and add missing tests in `tests/Cerneala.Tests/Controls/Primitives/ButtonBaseTests.cs`.
- [x] 5.2 Add `UI/Controls/Button.cs` as a concrete retained control built on `ButtonBase`.
- [x] 5.3 Implement button content measurement, arrangement, direct rendering, and visual state rendering for hover, pressed, focused, disabled, and command state.
- [x] 5.4 Verify button click uses existing retained input and command routing for direct and routed commands.
- [x] 5.5 Add `tests/Cerneala.Tests/Controls/ButtonTests.cs` for root attachment, layout, rendering, hit testing, hover, press, click, focus, and command execution.

## 6. Scope Guardrails and Roadmap

- [x] 6.1 Do not implement `ToggleButton`, `CheckBox`, `ControlTemplate`, or `TemplatePart`; leave their `ROADMAPv2.md` entries unchecked or marked deferred exactly as roadmap wording requires.
- [x] 6.2 Update `ROADMAPv2.md` section 10 checkboxes for completed OpenSpec artifacts, implementation files, tests, and acceptance checklist items.
- [x] 6.3 Ensure controls remain backend-neutral by adding or extending architecture tests if no existing test covers `UI/Controls` dependency boundaries.

## 7. Verification

- [x] 7.1 Run `dotnet build Cerneala.slnx -warnaserror`.
- [x] 7.2 Run `dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj`.
- [x] 7.3 Run focused controls/layout/render/input/command tests for section 10.
- [x] 7.4 Run `openspec validate add-first-controls-panels --strict`.
- [x] 7.5 Run `openspec validate --all --strict`.
