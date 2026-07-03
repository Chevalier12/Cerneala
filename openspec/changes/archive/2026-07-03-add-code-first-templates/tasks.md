## 1. Template Model

- [x] 1.1 Create `UI/Controls/ControlTemplate.cs` as the non-generic template contract.
- [x] 1.2 Create `UI/Controls/ControlTemplate{TControl}.cs` for typed owner template factories.
- [x] 1.3 Create `UI/Controls/TemplateContext.cs` for typed owner and template service access.
- [x] 1.4 Create `UI/Controls/TemplateInstance.cs` to own generated template roots and detach bindings/children.
- [x] 1.5 Add template ownership helpers that attach generated roots through retained logical and visual child collections.
- [x] 1.6 Add `tests/Cerneala.Tests/Controls/ControlTemplateTests.cs`.

## 2. Control Integration

- [x] 2.1 Add a typed `Template` property to `Control` backed by the existing typed property system.
- [x] 2.2 Ensure applying the same template reuses the current `TemplateInstance`.
- [x] 2.3 Ensure changing `Control.Template` detaches old generated children and attaches new generated children once.
- [x] 2.4 Ensure template replacement invalidates layout, render, hit-test, and input visual work through existing retained invalidation.
- [x] 2.5 Ensure template-generated children participate in retained attach/detach lifecycle and stable element identity while retained.

## 3. Template Binding

- [x] 3.1 Create `UI/Controls/TemplateBinding{T}.cs`.
- [x] 3.2 Bind owner `UiProperty<T>` values to generated child `UiProperty<T>` values during template application.
- [x] 3.3 Update generated child values when the owner property changes.
- [x] 3.4 Detach template binding subscriptions when a `TemplateInstance` is detached.
- [x] 3.5 Reject mismatched source/target value types before template application.
- [x] 3.6 Add `tests/Cerneala.Tests/Controls/TemplateBindingTests.cs`.

## 4. Template Diagnostics

- [x] 4.1 Create `UI/Controls/TemplatePartAttribute.cs`.
- [x] 4.2 Expose declared template part names and expected part types for diagnostics/tests.
- [x] 4.3 Ensure missing diagnostic template parts do not block template application.

## 5. Content Templates And Presenter

- [x] 5.1 Create `UI/Controls/DataTemplate.cs`.
- [x] 5.2 Create `UI/Controls/DataTemplate{T}.cs`.
- [x] 5.3 Create `UI/Controls/ContentPresenter.cs`.
- [x] 5.4 Ensure `ContentPresenter` directly hosts `UIElement` content as retained child content.
- [x] 5.5 Ensure `ContentPresenter` uses a matching `DataTemplate` for non-element content.
- [x] 5.6 Ensure content replacement detaches stale presented children before attaching current children.
- [x] 5.7 Add `tests/Cerneala.Tests/Controls/ContentPresenterTests.cs`.
- [x] 5.8 Add `tests/Cerneala.Tests/Controls/DataTemplateTests.cs`.

## 6. Items Templates And Presenter

- [x] 6.1 Create `UI/Controls/ItemsPanelTemplate.cs`.
- [x] 6.2 Create `UI/Controls/ItemsPresenter.cs`.
- [x] 6.3 Ensure `ItemsPanelTemplate` creates a retained panel root for generated item children.
- [x] 6.4 Ensure `ItemsPresenter` materializes items through `DataTemplate` when provided.
- [x] 6.5 Ensure replacing items detaches stale item children and attaches current item children in retained order.
- [x] 6.6 Add `tests/Cerneala.Tests/Controls/ItemsPanelTemplateTests.cs`.

## 7. Retained System Integration

- [x] 7.1 Prove template-generated children participate in measure and arrange through existing layout traversal.
- [x] 7.2 Prove template-generated children render through existing retained rendering traversal.
- [x] 7.3 Prove template-generated children participate in hit testing and routed input through retained visual ancestry.
- [x] 7.4 Prove template-generated children can receive style and visual-state rules like normal retained elements.
- [x] 7.5 Preserve existing `Button`, `ContentControl`, and presenter behavior while introducing template-backed composition seams.

## 8. Boundaries And Roadmap

- [x] 8.1 Add or extend architecture boundary tests proving template APIs avoid MonoGame, Skia, HarfBuzz, `Texture2D`, and `SpriteBatch`.
- [x] 8.2 Update `ROADMAPv2.md` section 15 file checklist as files and tests are completed.
- [x] 8.3 Update `ROADMAPv2.md` section 15 acceptance checklist as behavior is completed.
- [x] 8.4 Verify `openspec validate add-code-first-templates --strict` passes.
- [x] 8.5 Verify `openspec validate --all --strict` passes.
- [x] 8.6 Verify `dotnet build Cerneala.slnx -warnaserror` passes.
- [x] 8.7 Verify `dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj` passes.
