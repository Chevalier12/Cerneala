## 1. Styling Model

- [x] 1.1 Create `UI/Styling/Style.cs` for named/anonymous style definitions and rule ownership.
- [x] 1.2 Create `UI/Styling/Setter.cs` as the non-generic base/diagnostic contract for style setters.
- [x] 1.3 Create `UI/Styling/Setter{T}.cs` for typed `UiProperty<T>` setter values.
- [x] 1.4 Ensure setters reject mismatched values before application.
- [x] 1.5 Add `tests/Cerneala.Tests/UI/Styling/StyleTests.cs`.
- [x] 1.6 Add `tests/Cerneala.Tests/UI/Styling/SetterTests.cs`.

## 2. Selectors And Rules

- [x] 2.1 Create `UI/Styling/StyleSelector.cs` for explicit type/predicate selector behavior.
- [x] 2.2 Create `UI/Styling/PseudoClass.cs` for hover, pressed, focus, disabled, and selected pseudo states.
- [x] 2.3 Create `UI/Styling/VisualStateRule.cs` for pseudo-class-driven rule matching.
- [x] 2.4 Create `UI/Styling/StyleRule.cs` to combine selector, visual-state condition, and setters.
- [x] 2.5 Create `UI/Styling/StyleSheet.cs` as an ordered rule collection.
- [x] 2.6 Ensure later matching rules deterministically replace earlier style values for the same source/property.
- [x] 2.7 Add `tests/Cerneala.Tests/UI/Styling/StyleRuleTests.cs`.
- [x] 2.8 Add `tests/Cerneala.Tests/UI/Styling/PseudoClassTests.cs`.

## 3. Style Application And Precedence

- [x] 3.1 Create `UI/Styling/StyleApplicator.cs`.
- [x] 3.2 Apply base style setters through `UiPropertyValueSource.StyleBase`.
- [x] 3.3 Apply visual-state setters through `UiPropertyValueSource.StyleVisualState`.
- [x] 3.4 Clear stale style-owned property values when a rule no longer matches.
- [x] 3.5 Prove local values override style values through existing property precedence.
- [x] 3.6 Ensure applying unchanged style values does not raise duplicate property changes.
- [x] 3.7 Add `tests/Cerneala.Tests/UI/Styling/StyleApplicatorTests.cs`.

## 4. Style Invalidation

- [x] 4.1 Create `UI/Styling/StyleInvalidation.cs`.
- [x] 4.2 Recompute visual-state styles when hover, pressed, focus, disabled, or selected state changes.
- [x] 4.3 Ensure style changes use typed property metadata to request measure/render/hit-test/style invalidation.
- [x] 4.4 Ensure render-only style changes do not force measure work.
- [x] 4.5 Ensure pseudo-state changes do not cause repeated retained work when matching state is unchanged.
- [x] 4.6 Add `tests/Cerneala.Tests/UI/Styling/StyleInvalidationTests.cs`.

## 5. Themes

- [x] 5.1 Create `UI/Styling/Theme.cs`.
- [x] 5.2 Create `UI/Styling/ThemeKey{T}.cs`.
- [x] 5.3 Create `UI/Styling/ThemeProvider.cs`.
- [x] 5.4 Create `UI/Styling/ThemeResource.cs`.
- [x] 5.5 Create `UI/Styling/ThemePalette.cs`.
- [x] 5.6 Create `UI/Styling/DefaultTheme.cs`.
- [x] 5.7 Resolve `ThemeResource` values through explicit `ThemeProvider` during style application.
- [x] 5.8 Ensure theme replacement recomputes affected styled values.
- [x] 5.9 Ensure theme replacement invalidates only affected retained work.
- [x] 5.10 Add `tests/Cerneala.Tests/UI/Styling/ThemeTests.cs`.

## 6. Diagnostics And Boundaries

- [x] 6.1 Create `UI/Styling/StyleDiagnostics.cs`.
- [x] 6.2 Report matched rules for a styled element.
- [x] 6.3 Report applied property values and their effective value source.
- [x] 6.4 Report stale style values cleared by an applicator pass.
- [x] 6.5 Add architecture boundary tests proving `UI/Styling` has no MonoGame, Skia, HarfBuzz, `Texture2D`, or `SpriteBatch` references.

## 7. Integration Coverage

- [x] 7.1 Add integration tests proving `Button` visual properties can be styled without local-value overwrite.
- [x] 7.2 Add integration tests proving hover/pressed/focus/disabled pseudo classes can drive retained render changes.
- [x] 7.3 Add integration tests proving theme-backed setters update styled retained controls after theme replacement.
- [x] 7.4 Add tests proving style application remains deterministic when multiple rules match.
- [x] 7.5 Verify `dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj` passes.

## 8. Roadmap And Validation

- [x] 8.1 Update `ROADMAPv2.md` section 14 file checklist as files and tests are completed.
- [x] 8.2 Update `ROADMAPv2.md` section 14 acceptance checklist as behavior is completed.
- [x] 8.3 Verify `openspec validate add-styling-theme-engine --strict` passes.
- [x] 8.4 Verify `openspec validate --all --strict` passes.
- [x] 8.5 Verify `dotnet build Cerneala.slnx -warnaserror` passes.
- [x] 8.6 Verify `dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj` passes.
