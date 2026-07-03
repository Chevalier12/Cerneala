## ADDED Requirements

### Requirement: TextBlock uses retained text services
Cerneala SHALL measure and render `TextBlock` through the `UI.Text` service layer instead of controls-local approximate text measurement.

#### Scenario: TextBlock measures through text services
- **WHEN** a `TextBlock` is measured
- **THEN** it requests measurement from `UI.Text.TextMeasurer` using its text, font family, font size, wrapping policy, and available layout width

#### Scenario: TextBlock renders through text renderer
- **WHEN** a `TextBlock` is render-dirty
- **THEN** it records text drawing commands through `UI.Text.TextRenderer`

#### Scenario: Text content invalidates text metrics and render
- **WHEN** `TextBlock.Text` changes
- **THEN** text layout cache identity changes and retained measure and render invalidation are requested

#### Scenario: Text color invalidates render only
- **WHEN** `TextBlock.Foreground` changes without changing text metrics inputs
- **THEN** retained render invalidation is requested without invalidating cached text measurement

#### Scenario: Font changes invalidate measurement and render
- **WHEN** `TextBlock.FontFamily` or `TextBlock.FontSize` changes
- **THEN** retained measure and render invalidation are requested and text measurement is recomputed
