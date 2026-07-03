## ADDED Requirements

### Requirement: Text input platform participates in platform services
Cerneala SHALL expose `ITextInputPlatform` through the platform service aggregate without coupling text editing to native text APIs.

#### Scenario: Text input service is optional platform member
- **WHEN** platform services are created with a text input platform
- **THEN** callers can retrieve that `ITextInputPlatform` through the aggregate
