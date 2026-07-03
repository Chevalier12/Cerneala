## 1. Platform Contracts

- [x] 1.1 Add `UI/Platform/IPlatformServices.cs` aggregate.
- [x] 1.2 Add `UI/Platform/IClipboard.cs`.
- [x] 1.3 Add `UI/Platform/ICursorService.cs`.
- [x] 1.4 Add `UI/Platform/IFileDialogService.cs`.
- [x] 1.5 Add or confirm `UI/Platform/ITextInputPlatform.cs` as platform member.
- [x] 1.6 Add `UI/Platform/IDpiProvider.cs`.
- [x] 1.7 Include `IAccessibilityPlatform` in platform services.

## 2. Boundaries and Package Shape

- [x] 2.1 Verify `UI/Hosting/MonoGame/` remains the only MonoGame UI host adapter folder.
- [x] 2.2 Keep optional package split project files deferred unless real split files are created.
- [x] 2.3 Add platform service registration/composition tests.
- [x] 2.4 Add platform and MonoGame dependency boundary tests.

## 3. Verification and Roadmap

- [x] 3.1 Run OpenSpec validation and full project tests.
- [x] 3.2 Update `ROADMAPv2.md` section 24 checkboxes for implemented files, tests, deferred split files, and acceptance criteria.
