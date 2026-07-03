## ADDED Requirements

### Requirement: Accessibility platform participates in platform services
Cerneala SHALL expose `IAccessibilityPlatform` through the platform service aggregate without coupling accessibility semantics to native accessibility APIs.

#### Scenario: Accessibility service is optional platform member
- **WHEN** platform services are created with an accessibility platform
- **THEN** callers can retrieve that `IAccessibilityPlatform` through the aggregate
