## Why

Cerneala has accumulated retained UI services, host adapters, resources, accessibility, text input, and MonoGame integration in one project. The next portability step is to make platform services explicit and keep adapter-only dependencies isolated so future package splitting is a mechanical decision, not a refactor with dracu' stie cate fire trase prin core.

## What Changes

- Add platform-neutral service contracts for clipboard, cursor, file dialogs, DPI, and grouped platform services.
- Keep `ITextInputPlatform` and `IAccessibilityPlatform` as part of the platform boundary instead of duplicating them.
- Add service registration tests proving platform services can be composed without concrete adapters.
- Add architecture tests proving MonoGame dependencies remain adapter-scoped and that optional project/package split files are not falsely required.
- Update `ROADMAPv2.md` section 24 checkboxes as implementation lands.

## Capabilities

### New Capabilities
- `platform-boundaries-package-shape`: Covers platform service abstractions, adapter dependency boundaries, and package-shape readiness checks.

### Modified Capabilities
- `text-editing-ime`: Text input remains behind `ITextInputPlatform` and participates in platform services.
- `accessibility-semantics`: Accessibility platform integration remains behind `IAccessibilityPlatform` and participates in platform services.
- `game-loop-host-integration`: MonoGame host adapter remains isolated under `UI/Hosting/MonoGame`.

## Impact

- Affected code: `UI/Platform`, `UI/Hosting`, architecture tests, and `ROADMAPv2.md`.
- APIs: new platform service interfaces and a simple service aggregate.
- Dependencies: no new dependency and no project split in this phase; split csproj files remain optional future work.
