## ADDED Requirements

### Requirement: Playground exposes retained samples
Cerneala SHALL provide playground samples that construct retained control trees using the retained UI APIs.

#### Scenario: Button sample creates retained controls
- **WHEN** the retained button sample is created
- **THEN** it contains a retained `StackPanel` with `TextBlock`, `Button`, and `Border` content

#### Scenario: Layout sample demonstrates retained panels
- **WHEN** the layout sample is created
- **THEN** it uses retained panel controls to demonstrate layout without immediate drawing code

#### Scenario: Text sample demonstrates retained text
- **WHEN** the text sample is created
- **THEN** it renders text through retained `TextBlock` and text services

### Requirement: Sample selector switches retained samples
Cerneala SHALL provide a sample selector that swaps the active retained sample in the playground.

#### Scenario: Selector exposes available samples
- **WHEN** the sample selector is initialized
- **THEN** it exposes button, layout, and text samples

#### Scenario: Selector changes active sample by command
- **WHEN** a selector command is executed
- **THEN** the active retained sample changes and retained layout/render invalidation is requested

### Requirement: Invalidation stats overlay reports retained work
Cerneala SHALL provide a playground overlay that displays retained frame work diagnostics.

#### Scenario: Overlay shows no-op frame counters
- **WHEN** a frame has no retained changes
- **THEN** the overlay reports zero measured elements, zero arranged elements, and zero regenerated local render caches

#### Scenario: Overlay participates in retained rendering
- **WHEN** the overlay is visible
- **THEN** it is represented as retained UI content rather than immediate drawing outside the host

### Requirement: Playground proves frame draw and retained reuse
Cerneala SHALL draw every frame while avoiding retained regeneration on unchanged frames.

#### Scenario: Draw occurs every frame
- **WHEN** the MonoGame draw loop runs
- **THEN** `MonoGameDrawingBackend.Render(cachedCommands)` is reached through `MonoGameUiHost.Draw`

#### Scenario: Unchanged update avoids retained regeneration
- **WHEN** two playground updates occur without input, viewport, or retained state changes
- **THEN** the later frame reports zero measure, arrange, and local render-cache rebuild work

### Requirement: Playground scenarios are tested
Cerneala SHALL include focused tests for retained sample construction and selector behavior where practical.

#### Scenario: Required playground tests exist
- **WHEN** this implementation phase is complete
- **THEN** tests exist for sample construction, sample selector switching, and diagnostics formatting or state mapping

#### Scenario: Full tests pass
- **WHEN** this implementation phase is complete
- **THEN** `dotnet test` passes
