## 1. Project Setup

- [x] 1.1 Create `Cerneala.SourceGen/Cerneala.SourceGen.csproj`.
- [x] 1.2 Create `tests/Cerneala.Tests.SourceGen/Cerneala.Tests.SourceGen.csproj`.
- [x] 1.3 Add both projects to `Cerneala.slnx`.

## 2. Generator

- [x] 2.1 Create `Cerneala.SourceGen/UiMarkupGenerator.cs`.
- [x] 2.2 Read `.cui.xml` additional files and derive deterministic generated class names.
- [x] 2.3 Generate code-first factories for supported controls and properties.
- [x] 2.4 Report diagnostics for malformed XML, unsupported elements, and unsupported properties.

## 3. Tests

- [x] 3.1 Add `tests/Cerneala.Tests.SourceGen/UiMarkupGeneratorTests.cs`.
- [x] 3.2 Prove supported markup emits generated source that compiles against Cerneala runtime APIs.
- [x] 3.3 Prove generated code uses public typed properties and does not call runtime markup parser/factory.
- [x] 3.4 Prove malformed or unsupported markup reports diagnostics.

## 4. Verification And Roadmap

- [x] 4.1 Run source generator tests.
- [x] 4.2 Run full test suite.
- [x] 4.3 Validate OpenSpec strictly.
- [x] 4.4 Update `ROADMAPv2.md` section 25 and implementation order source generation checkboxes.
