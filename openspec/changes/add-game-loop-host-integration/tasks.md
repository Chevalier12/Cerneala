## 1. Core Hosting Primitives

- [x] 1.1 Create `UI/Hosting/UiViewport.cs` as a value object for width, height, and scale with validation and equality.
- [x] 1.2 Create `UI/Hosting/UiFrame.cs` to carry elapsed time, viewport, input frame, and `FrameStats`.
- [x] 1.3 Create `UI/Hosting/IUiClock.cs` for deterministic frame timing.
- [x] 1.4 Create `UI/Hosting/UiHostOptions.cs` for root, viewport, input source, backend bridge, and host configuration.
- [x] 1.5 Create `UI/Hosting/IUiBackend.cs` as a backend-neutral host bridge over input and drawing services.

## 2. Retained Host Runtime

- [x] 2.1 Create `UI/Hosting/UiHost.cs` and wire it to a retained `UIRoot`.
- [x] 2.2 Implement root assignment and missing-root validation in `UiHost`.
- [x] 2.3 Implement `UiHost.Update(...)` to read one `InputFrame` from `IInputSource` when no explicit frame is supplied.
- [x] 2.4 Implement `UiHost.Update(...)` overload or option that accepts an explicit `InputFrame`.
- [x] 2.5 Implement first-frame retained invalidation so the first update performs measure, arrange, and render-cache work.
- [x] 2.6 Implement viewport change detection and apply changes through the retained root.
- [x] 2.7 Ensure viewport changes schedule retained arrange/render work.
- [x] 2.8 Ensure unchanged later updates report no retained measure, arrange, render-cache, or hit-test work.
- [x] 2.9 Store the last processed `UiFrame` with viewport, input frame, elapsed time, and frame stats.
- [x] 2.10 Implement `UiHost.Draw(IDrawingBackend backend)` to submit retained root commands through the root retained renderer.
- [x] 2.11 Ensure `UiHost.Draw(...)` does not process scheduler phase work.
- [x] 2.12 Ensure draw and update failures throw clear argument or invalid-operation errors.

## 3. MonoGame Host Adapter

- [x] 3.1 Create `UI/Hosting/MonoGame/MonoGameUiHostOptions.cs`.
- [x] 3.2 Create `UI/Hosting/MonoGame/MonoGameContentServices.cs` for minimal image/font service glue.
- [x] 3.3 Create `UI/Hosting/MonoGame/MonoGameUiHost.cs` that composes `MonoGameInputSource`, `MonoGameDrawingBackend`, and core `UiHost`.
- [x] 3.4 Add a MonoGame text-input queue path that forwards text input to `MonoGameInputSource`.
- [x] 3.5 Keep all `Microsoft.Xna.Framework` references out of core `UI/Hosting` files.

## 4. Playground Integration

- [x] 4.1 Update `Playground/Cerneala.Playground/Game1.cs` to create a retained `UIRoot`.
- [x] 4.2 Update `Game1.cs` to create and own a `MonoGameUiHost`.
- [x] 4.3 Update `Game1.Update(...)` to call host update after existing exit handling.
- [x] 4.4 Update `Game1.Draw(...)` to call host draw inside the MonoGame drawing pass.
- [x] 4.5 Remove the playground-only immediate command demo path when the retained host demo replaces it.

## 5. Hosting Tests

- [x] 5.1 Add `tests/Cerneala.Tests/UI/Hosting/FakeUiClock.cs`.
- [x] 5.2 Add `tests/Cerneala.Tests/UI/Hosting/FakeDrawingBackend.cs`.
- [x] 5.3 Add `tests/Cerneala.Tests/UI/Hosting/FakeInputSource.cs`.
- [x] 5.4 Add `tests/Cerneala.Tests/UI/Hosting/UiViewportTests.cs`.
- [x] 5.5 Add `tests/Cerneala.Tests/UI/Hosting/UiHostTests.cs` for root setup, input frame capture, missing dependency errors, and last frame diagnostics.
- [x] 5.6 Add `tests/Cerneala.Tests/UI/Hosting/UiHostFrameContractTests.cs` for first-frame work, unchanged-frame no-work behavior, viewport invalidation, and draw/update separation.
- [x] 5.7 Add `tests/Cerneala.Tests/UI/Hosting/MonoGameUiHostBoundaryTests.cs` to prove core hosting has no MonoGame references and adapter files stay isolated.

## 6. Roadmap And Validation

- [x] 6.1 Update `ROADMAPv2.md` section 7 checkboxes immediately as each file and frame contract is completed.
- [x] 6.2 Run focused hosting tests.
- [x] 6.3 Run `dotnet test`.
- [x] 6.4 Run `openspec validate add-game-loop-host-integration --strict`.
- [x] 6.5 Run `openspec validate --all --strict`.
