## 1. Markup Model And Diagnostics

- [x] 1.1 Create `UI/Markup/MarkupDiagnostic.cs` with severity, code, message, and optional line/column data.
- [x] 1.2 Create `UI/Markup/MarkupLoadOptions.cs` for strict/recovery loading behavior.
- [x] 1.3 Create `UI/Markup/UiMarkupDocument.cs` and node/value model for root, attributes, text, children, and diagnostics.
- [x] 1.4 Create `UI/Markup/ContentPropertyAttribute.cs` and `UI/Markup/DesignTimeOnlyAttribute.cs`.

## 2. Reader And Writer

- [x] 2.1 Create `UI/Markup/UiMarkupReader.cs` that parses the supported XML subset into `UiMarkupDocument`.
- [x] 2.2 Create `UI/Markup/UiMarkupWriter.cs` that serializes markup documents deterministically.
- [x] 2.3 Add `tests/Cerneala.Tests/UI/Markup/UiMarkupReaderTests.cs`.
- [x] 2.4 Add `tests/Cerneala.Tests/UI/Markup/UiMarkupWriterTests.cs`.
- [x] 2.5 Add `tests/Cerneala.Tests/UI/Markup/MarkupDiagnosticTests.cs`.

## 3. Schema And Factory

- [x] 3.1 Create `UI/Markup/UiMarkupTypeRegistry.cs` for explicit element, property, content, child, and converter registrations.
- [x] 3.2 Create `UI/Markup/UiMarkupSchema.cs` with default registrations for stable retained controls needed by tests.
- [x] 3.3 Create `UI/Markup/UiFactory.cs` that turns markup documents into retained `UIElement` trees through the registry.
- [x] 3.4 Create `UI/Markup/GeneratedUiFactory.cs` runtime seam for precompiled factories.
- [x] 3.5 Add `tests/Cerneala.Tests/UI/Markup/UiFactoryTests.cs`.

## 4. Integration Contracts

- [x] 4.1 Prove markup-created property assignment uses typed validation/coercion instead of bypassing `UiObject`.
- [x] 4.2 Prove markup-created/generated trees use the retained invalidation and render-cache path.
- [x] 4.3 Keep `Cerneala.SourceGen/UiMarkupGenerator.cs` and source-generator tests explicitly deferred unless a real source-generation project is introduced.

## 5. Verification And Roadmap

- [x] 5.1 Run focused markup tests.
- [x] 5.2 Run full test suite.
- [x] 5.3 Validate OpenSpec strictly.
- [x] 5.4 Update `ROADMAPv2.md` section 25 checkboxes for completed runtime files, tests, spec, acceptance items, and deferred source-generator items.
