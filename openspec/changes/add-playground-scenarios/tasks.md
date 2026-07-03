## 1. Sample Infrastructure

- [x] 1.1 Create `Playground/Cerneala.Playground/Samples/RetainedButtonSample.cs`.
- [x] 1.2 Create `Playground/Cerneala.Playground/Samples/LayoutSample.cs`.
- [x] 1.3 Create `Playground/Cerneala.Playground/Samples/TextSample.cs`.
- [x] 1.4 Ensure each sample builds retained controls instead of immediate drawing elements.

## 2. Sample Selector

- [x] 2.1 Create `Playground/Cerneala.Playground/Samples/SampleSelector.cs`.
- [x] 2.2 Ensure selector exposes button, layout, and text samples.
- [x] 2.3 Ensure selector command/click behavior switches the active retained sample and invalidates layout/render.
- [x] 2.4 Add focused tests for sample selector sample list and switching behavior.

## 3. Invalidation Stats Overlay

- [x] 3.1 Create `Playground/Cerneala.Playground/Samples/InvalidationStatsOverlay.cs`.
- [x] 3.2 Ensure overlay maps `UiFrame`/frame stats into retained text content.
- [x] 3.3 Ensure no-op frames can show zero measured elements, zero arranged elements, and zero regenerated local render caches.
- [x] 3.4 Add focused tests for diagnostics formatting or state mapping.

## 4. Game1 Integration

- [x] 4.1 Update `Playground/Cerneala.Playground/Game1.cs` to remove the local custom immediate-style demo element.
- [x] 4.2 Wire `SampleSelector` into the retained `UIRoot` through `MonoGameUiHost`.
- [x] 4.3 Ensure `Game1.Update(...)` continues forwarding viewport and elapsed time to `MonoGameUiHost.Update(...)`.
- [x] 4.4 Ensure `Game1.Draw(...)` continues calling `MonoGameUiHost.Draw(...)` so cached commands draw every frame.
- [x] 4.5 Preserve text input queueing through `MonoGameUiHost.QueueTextInput(...)`.

## 5. Playground Verification

- [x] 5.1 Add tests proving the button sample contains retained `StackPanel`, `TextBlock`, `Button`, and `Border`.
- [x] 5.2 Add tests proving layout and text samples use retained controls.
- [x] 5.3 Add tests or boundary checks proving playground sample UI avoids local immediate-only demo elements.
- [x] 5.4 Verify unchanged host frames still report no retained regeneration where the test seam exists.

## 6. Roadmap And Validation

- [x] 6.1 Update `ROADMAPv2.md` section 13 checklist as files, tests, and acceptance items are completed.
- [x] 6.2 Verify `openspec validate add-playground-scenarios --strict` passes.
- [x] 6.3 Verify `dotnet build Cerneala.slnx -warnaserror` passes.
- [x] 6.4 Verify `dotnet test tests/Cerneala.Tests/Cerneala.Tests.csproj` passes.
