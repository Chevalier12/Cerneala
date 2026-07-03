## ADDED Requirements

### Requirement: Retained rendering tracks text layout dependencies
Cerneala SHALL include text layout dependency identity in retained render cache staleness checks for text-rendering elements.

#### Scenario: Unchanged text dependency reuses render cache
- **WHEN** a text-rendering element has unchanged render version and unchanged text layout dependency identity
- **THEN** its retained local render command cache can be reused

#### Scenario: Text metrics dependency invalidates render cache
- **WHEN** text content, resolved font identity, font size, wrapping width, wrapping mode, trimming mode, or scale changes
- **THEN** the text-rendering element's local render command cache is considered stale

#### Scenario: Foreground-only change avoids metrics invalidation
- **WHEN** only text foreground color changes
- **THEN** retained rendering invalidates visible render output without changing the text layout dependency identity
