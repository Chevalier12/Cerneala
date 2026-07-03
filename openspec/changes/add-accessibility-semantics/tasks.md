## 1. Core Semantics

- [x] 1.1 Add `SemanticsRole`, `SemanticsProperty`, `SemanticsNode`, and `SemanticsTree` under `UI/Accessibility`.
- [x] 1.2 Add `AccessibleName` helper and explicit accessible-name property support.
- [x] 1.3 Add `SemanticsProvider` for deterministic retained visual tree traversal.

## 2. Control Semantics

- [x] 2.1 Add `AutomationPeer`, `ButtonAutomationPeer`, `TextBoxAutomationPeer`, and `ItemsControlAutomationPeer`.
- [x] 2.2 Add semantics coverage for button, textbox, and items controls.
- [x] 2.3 Add `IAccessibilityPlatform` adapter boundary without native OS dependencies.

## 3. Tests and Roadmap

- [x] 3.1 Add focused tests for `SemanticsTree` and `SemanticsProvider`.
- [x] 3.2 Add focused tests for button and textbox semantics.
- [x] 3.3 Add or update boundary/roadmap tests proving accessibility remains backend-neutral and roadmap section 21 is complete.
- [x] 3.4 Mark `ROADMAPv2.md` section 21 files, tests, and implementation order item complete.
- [x] 3.5 Run `openspec validate add-accessibility-semantics --strict` and `dotnet test`.
