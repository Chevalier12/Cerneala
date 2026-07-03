## ADDED Requirements

### Requirement: Text services expose backend-neutral text style
Cerneala SHALL provide backend-neutral text style types that describe retained text without referencing concrete drawing backends.

#### Scenario: Text run style converts to drawing text run
- **WHEN** a text service renders a styled text value
- **THEN** it can create a `DrawTextRun` using resolved drawing font data without exposing Skia, HarfBuzz, MonoGame, or host-specific types to controls

#### Scenario: Text style validates metric inputs
- **WHEN** a text style is created or updated
- **THEN** font family, font size, wrapping, trimming, and scale inputs are validated before they are used for measurement or rendering

### Requirement: Font resolver provides explicit font lookup
Cerneala SHALL provide `FontResolver` that resolves retained font requests through explicit font services or deterministic fallback behavior.

#### Scenario: Font resolver returns draw font
- **WHEN** text services request a font for a font family and size
- **THEN** the resolver returns an `IDrawFont` suitable for creating drawing text runs

#### Scenario: Font lookup is not global
- **WHEN** a text control measures or renders text
- **THEN** font lookup is performed through explicit text service dependencies rather than hidden global state

### Requirement: Text measurer computes deterministic desired size
Cerneala SHALL provide `TextMeasurer` that computes text desired size from text content, style, available layout width, wrapping policy, and scale.

#### Scenario: Empty text measures to zero width
- **WHEN** empty text is measured
- **THEN** the result has deterministic zero text width and a valid line height for the resolved style

#### Scenario: Font changes affect measurement
- **WHEN** font family or font size changes
- **THEN** measurement is recomputed and the resulting measurement cache identity changes

#### Scenario: Wrapping width affects measurement
- **WHEN** wrapping is enabled and the available wrapping width changes
- **THEN** measurement is recomputed using the new wrapping width

### Requirement: Text layout cache reuses unchanged metrics
Cerneala SHALL provide `TextLayoutCache` that reuses text measurement results for unchanged metrics-affecting inputs.

#### Scenario: Unchanged text layout hits cache
- **WHEN** text content, resolved font identity, font size, wrapping mode, wrapping width, trimming mode, and scale are unchanged
- **THEN** repeated measurement reuses the cached text layout result

#### Scenario: Color does not affect metrics cache
- **WHEN** only text foreground color changes
- **THEN** the text layout cache identity remains unchanged

#### Scenario: Text content invalidates metrics cache
- **WHEN** text content changes
- **THEN** the text layout cache identity changes and measurement is recomputed

### Requirement: Text renderer records retained drawing commands
Cerneala SHALL provide `TextRenderer` that records text drawing commands through `DrawingContext`.

#### Scenario: Text renderer draws through drawing context
- **WHEN** retained text is rendered
- **THEN** `TextRenderer` emits text commands using `DrawingContext.DrawText`

#### Scenario: Text renderer uses cached layout
- **WHEN** text layout for the requested input is already cached
- **THEN** rendering reuses the cached layout result instead of recomputing metrics

#### Scenario: Text renderer remains backend-neutral
- **WHEN** text rendering code is compiled
- **THEN** `UI/Text` does not reference MonoGame, Skia, HarfBuzz, `Texture2D`, or `SpriteBatch`

### Requirement: MVP line breaking is explicit
Cerneala SHALL provide explicit text wrapping, trimming, and line-breaking policy types for retained text layout.

#### Scenario: No-wrap text produces one line
- **WHEN** text wrapping is disabled
- **THEN** line breaking returns a single retained text line for the input text

#### Scenario: Wrapped text respects available width
- **WHEN** text wrapping is enabled and text exceeds the available wrapping width
- **THEN** line breaking produces deterministic wrapped lines that fit the available width according to MVP measurement rules

#### Scenario: Deferred text features are not exposed as implemented
- **WHEN** bidi, text selection, or editing controller behavior is requested
- **THEN** the MVP text services do not report those features as implemented

### Requirement: Text services are tested
Cerneala SHALL include focused tests for retained text services.

#### Scenario: Required text service tests exist
- **WHEN** this implementation phase is complete
- **THEN** tests exist for font resolution, text measurement, text layout caching, text rendering, wrapping, and TextBlock invalidation behavior

#### Scenario: Full tests pass
- **WHEN** this implementation phase is complete
- **THEN** `dotnet test` passes
