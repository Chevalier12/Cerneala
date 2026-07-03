## Context

Cerneala already has typed properties, explicit value-source precedence, retained invalidation, input visual state, resource services, and retained controls. Section 14 of `ROADMAPv2.md` adds styling and themes after those foundations so controls can share visual rules without hard-coded visuals, string paths, or reflection-heavy WPF-style machinery.

The existing `UiObject.SetValue(..., UiPropertyValueSource.StyleBase)` and `UiObject.SetValue(..., UiPropertyValueSource.StyleVisualState)` APIs are the core integration point. Styling should feed those sources and let the property store, metadata validation/coercion, and retained invalidation pipeline do the rest.

## Goals / Non-Goals

**Goals:**
- Add typed style objects, rules, selectors, style sheets, setters, applicator, invalidation, diagnostics, themes, and pseudo-class/visual-state support under `UI/Styling`.
- Keep hot-path property application typed and deterministic.
- Apply base style values through `StyleBase` and visual-state style values through `StyleVisualState`.
- Let local values override styles through existing property precedence.
- Connect pseudo classes to existing retained state: pointer over, pressed, keyboard focus, focus within, disabled, and selected where supported.
- Propagate style and theme changes through retained invalidation and resource dependency seams.
- Keep styling backend-neutral.

**Non-Goals:**
- No XAML parser, markup language, string property paths, or trigger clone.
- No templates or visual tree generation; that belongs to section 15.
- No animations; animation precedence already exists but animation behavior is out of scope.
- No full CSS cascade, inheritance model, or selector language beyond typed selectors and pseudo classes needed by retained controls.
- No global hidden theme singleton.

## Decisions

### Typed setters own hot-path property application

`Setter<T>` stores a `UiProperty<T>` and a typed value provider. `StyleApplicator` applies it by calling `SetValue(property, value, UiPropertyValueSource.StyleBase)` or `SetValue(property, value, UiPropertyValueSource.StyleVisualState)`.

Rationale: `UiProperty` already owns validation, coercion, equality, invalidation options, and precedence. Duplicating that in styling would create a second state model and turn into a bug factory.

Alternative considered: store property names and resolve by reflection when applying a style. Rejected because the roadmap explicitly requires typed properties without reflection in the hot path.

### Style rules are explicit selector + setter groups

`StyleRule` combines a typed `StyleSelector`, optional pseudo-class/visual-state condition, and setters. `StyleSheet` is an ordered collection of rules. Ordering is deterministic: later matching rules can replace earlier style values for the same property within the same source.

Rationale: this is enough for default themes and first control styling while staying easy to reason about.

Alternative considered: implement CSS specificity. Rejected as YAGNI for the current retained control set.

### Visual state rules use `StyleVisualState`

Pseudo-class and visual-state matches apply through `UiPropertyValueSource.StyleVisualState`; base rules apply through `StyleBase`. When pseudo-state no longer matches, the applicator clears only the style visual-state values it previously owned.

Rationale: the property store already gives visual-state style values higher priority than base style values but lower priority than animation/local values.

Alternative considered: mutate local properties on hover/pressed/focus. Rejected because it would break user local overrides.

### Theme values are explicit resources

`Theme`, `ThemeKey<T>`, `ThemeProvider`, `ThemeResource`, `DefaultTheme`, and `ThemePalette` provide typed lookup of theme values. Theme lookup is injected or attached explicitly; no static global theme is required.

Rationale: resource services already require explicit providers and replacement notifications. Themes should follow the same rule so tests and hosts stay deterministic.

Alternative considered: static current theme. Rejected because it creates hidden global state and makes retained invalidation harder to audit.

### Invalidation is metadata-driven

Style changes do not invent their own invalidation flags. Applying or clearing style values must go through `UiObject.SetValue` / `ClearValue` so `UiPropertyOptions` drive measure, arrange, render, hit-test, style, and input-visual invalidation.

Rationale: this keeps one invalidation contract and prevents over-invalidating every frame.

Alternative considered: queue subtree render invalidation for any style change. Rejected because the acceptance criteria require invalidating only affected work.

## Risks / Trade-offs

- [Risk] Style ownership per property/source can be subtle when rules stop matching. → Track which properties were applied by the current applicator pass and clear stale `StyleBase`/`StyleVisualState` values deterministically.
- [Risk] Pseudo-class changes may over-apply all rules too often. → Start with focused recomputation for the target element and add diagnostics counters before optimizing broader cascades.
- [Risk] Theme resources can create stale render caches if dependencies are not recorded. → Route theme resource resolution through explicit dependency tracking and tests for theme replacement invalidation.
- [Risk] `selected` pseudo class has no existing core selected property. → Model `PseudoClass.Selected` as supported by controls that expose selected state later; do not add selection behavior in this change unless a minimal typed state seam is needed.
- [Risk] Without templates, styled controls still use current render implementations. → This change styles existing properties only; richer structural visuals are deferred to templates.
