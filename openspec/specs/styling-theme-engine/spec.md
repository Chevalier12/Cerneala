# styling-theme-engine Specification

## Purpose
TBD - created by archiving change add-styling-theme-engine. Update Purpose after archive.
## Requirements
### Requirement: Typed styles group retained property setters
Cerneala SHALL provide typed style objects and setters that target registered `UiProperty<T>` values without reflection in the style application hot path.

#### Scenario: Style contains typed setters
- **WHEN** a style is created for retained UI values
- **THEN** it can hold `Setter<T>` entries that reference `UiProperty<T>` and typed values

#### Scenario: Setter rejects mismatched property value types
- **WHEN** a setter is created with a value that is not assignable to the target property type
- **THEN** creation fails before style application

#### Scenario: Applying setter uses property validation
- **WHEN** a style setter is applied to a retained element
- **THEN** the target property's validation, coercion, equality, and invalidation metadata are honored

### Requirement: Style rules select retained elements explicitly
Cerneala SHALL provide style selectors, rules, and style sheets that match retained elements deterministically.

#### Scenario: Type selector matches derived element
- **WHEN** a style selector targets a retained element type
- **THEN** it matches instances of that type and derived types according to the selector contract

#### Scenario: Style sheet preserves deterministic order
- **WHEN** multiple matching rules set the same property at the same style source
- **THEN** the later rule in the style sheet determines the applied style value

#### Scenario: Non-matching rule does not apply setters
- **WHEN** a style rule selector does not match an element
- **THEN** none of that rule's setters are applied to the element

### Requirement: Style applicator uses existing value precedence
Cerneala SHALL apply style values through existing `UiPropertyValueSource` precedence rather than a parallel styling store.

#### Scenario: Base style uses style base source
- **WHEN** a base style rule applies to an element
- **THEN** its setters are stored through `UiPropertyValueSource.StyleBase`

#### Scenario: Visual state style uses visual state source
- **WHEN** a visual-state rule applies to an element
- **THEN** its setters are stored through `UiPropertyValueSource.StyleVisualState`

#### Scenario: Local values override style values
- **WHEN** an element has a local value and a matching style sets the same property
- **THEN** the effective property value remains the local value

#### Scenario: Clearing removed style rules restores lower precedence value
- **WHEN** a previously applied style rule no longer matches an element
- **THEN** the applicator clears only the style-owned value source so the next lower precedence value becomes effective

### Requirement: Pseudo classes represent retained visual state
Cerneala SHALL expose pseudo-class values for retained visual states used by styling rules.

#### Scenario: Hover pseudo class follows pointer over
- **WHEN** an element has pointer-over state
- **THEN** `PseudoClass.Hover` can match that element for visual-state styling

#### Scenario: Pressed pseudo class follows press state
- **WHEN** a pressable retained element is pressed
- **THEN** `PseudoClass.Pressed` can match that element for visual-state styling

#### Scenario: Focus pseudo class follows keyboard focus
- **WHEN** an element has keyboard focus or focus within according to the pseudo-class rule
- **THEN** focus pseudo classes can match that element for visual-state styling

#### Scenario: Disabled pseudo class follows enabled state
- **WHEN** an element is disabled
- **THEN** `PseudoClass.Disabled` can match that element for visual-state styling

### Requirement: Visual state changes update style values
Cerneala SHALL update visual-state style values when retained input or control state changes affect pseudo-class matching.

#### Scenario: Hover change reapplies visual state style
- **WHEN** retained input changes pointer-over state on an element
- **THEN** style visual-state values for that element are recomputed

#### Scenario: Pressed change enqueues render invalidation through property metadata
- **WHEN** retained input changes a pressed style match that affects a render property
- **THEN** render invalidation is requested through the target property's metadata

#### Scenario: Reapplying unchanged visual state does not create duplicate work
- **WHEN** pseudo-class matching remains unchanged
- **THEN** style application does not repeatedly change effective property values or enqueue redundant retained work

### Requirement: Themes provide typed visual resources
Cerneala SHALL provide explicit typed theme primitives for reusable visual values.

#### Scenario: Theme resolves typed value
- **WHEN** a theme contains a value for a `ThemeKey<T>`
- **THEN** resolving that key returns a value of type `T`

#### Scenario: Missing theme value fails clearly
- **WHEN** a required theme key is not available from the current theme provider
- **THEN** lookup fails with a clear diagnostic error

#### Scenario: Default theme provides first control palette
- **WHEN** the default theme is requested
- **THEN** it provides a palette and baseline values usable by existing first controls

### Requirement: Theme resources integrate with retained invalidation
Cerneala SHALL let style setters depend on theme resources and update retained elements when those resources change.

#### Scenario: Theme resource setter resolves before applying
- **WHEN** a setter uses a `ThemeResource`
- **THEN** the applicator resolves it through an explicit `ThemeProvider` before setting the target property

#### Scenario: Theme replacement updates styled elements
- **WHEN** the active theme changes
- **THEN** elements using theme-backed style values recompute affected style values

#### Scenario: Theme change invalidates affected work only
- **WHEN** a theme-backed style value changes one render-affecting property
- **THEN** retained render invalidation is requested without forcing unrelated measure work

### Requirement: Style diagnostics expose applied styling state
Cerneala SHALL provide diagnostics for inspecting style matches and applied style values.

#### Scenario: Diagnostics list matching rules
- **WHEN** diagnostics inspect a styled element
- **THEN** they can report which style rules matched that element

#### Scenario: Diagnostics identify effective style source
- **WHEN** diagnostics inspect a styled property
- **THEN** they can report whether the value came from style base, style visual state, local value, or another existing property source

#### Scenario: Diagnostics include stale clear operations
- **WHEN** a style pass clears a previously applied style value
- **THEN** diagnostics can identify the property and source that were cleared

### Requirement: Styling remains backend-neutral
Cerneala SHALL keep styling and theme APIs independent of concrete rendering and platform adapters.

#### Scenario: Styling avoids backend references
- **WHEN** `UI/Styling` is compiled
- **THEN** it does not reference MonoGame, Skia, HarfBuzz, `Texture2D`, or `SpriteBatch`

#### Scenario: Styling uses retained abstractions
- **WHEN** styling affects visual output
- **THEN** it does so through typed properties and retained invalidation rather than direct drawing backend calls

### Requirement: Styling and themes are tested
Cerneala SHALL include focused tests for styling, typed setters, selector matching, applicator behavior, invalidation, themes, pseudo classes, and diagnostics.

#### Scenario: Required styling tests exist
- **WHEN** this implementation phase is complete
- **THEN** tests exist under `tests/Cerneala.Tests/UI/Styling` for styles, rules, setters, applicator behavior, invalidation, themes, and pseudo classes

#### Scenario: Full tests pass
- **WHEN** this implementation phase is complete
- **THEN** `dotnet test` passes

