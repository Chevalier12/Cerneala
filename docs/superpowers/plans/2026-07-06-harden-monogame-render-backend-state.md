# Harden MonoGame Render Backend State Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Prioritize throughput with subagents for independent inspection, RED test drafting, implementation patches, and verification, while preserving this plan's dependency order.

**Goal:** Make `MonoGameDrawingBackend` safe and predictable as a real runtime adapter: scaled draw mapping, balanced clipping, scissor restoration, text texture cache behavior, image validation, and idempotent disposal. The retained UI core can be correct and still feel broken if the backend leaks render state or maps coordinates inconsistently.

**Architecture:** Keep `UI/Drawing` as command recording and backend rendering only. Do not turn `DrawCommandList` into a scene graph. Extract tiny pure helper classes only where they make backend behavior testable without requiring a live graphics device. MonoGame-specific code stays under `UI/Drawing/MonoGame` and `UI/Hosting/MonoGame`.

**Tech Stack:** C#/.NET 8, xUnit, existing `DrawCommandList`, `DrawingContext`, `MonoGameDrawingBackend`, MonoGame adapter folders.

---

## File Structure

- Modify: `UI/Drawing/MonoGame/MonoGameDrawingBackend.cs`
  - Apply `CoordinateScale` consistently to rectangles, positions, thicknesses, and clips.
  - Restore scissor state reliably after render.
  - Make `Dispose()` idempotent.
- Create: `UI/Drawing/MonoGame/MonoGameDrawMapper.cs`
  - Pure helper for scaled rectangle/vector/thickness/scissor mapping.
- Create: `UI/Drawing/MonoGame/MonoGameClipStack.cs`
  - Pure or mostly pure helper for nested clip intersection/restoration if useful.
- Modify: `UI/Hosting/MonoGame/MonoGameUiHost.cs`
  - Ensure draw lifecycle sets scale and does not expose backend state leaks.
- Create: `tests/Cerneala.Tests/Drawing/MonoGame/MonoGameDrawMapperTests.cs`
- Create: `tests/Cerneala.Tests/Drawing/MonoGame/MonoGameClipStackTests.cs`
- Create: `tests/Cerneala.Tests/Drawing/MonoGame/MonoGameDrawingBackendStateTests.cs`

## Important Existing Behavior

- `DrawCommandKind` supports rectangles, ellipses, lines, text, images, push clip, and pop clip.
- `MonoGameDrawingBackend.Render(...)` iterates a `DrawCommandList` and dispatches commands.
- `MonoGameDrawingBackend.PushClip(...)` uses `GraphicsDevice.ScissorRectangle` and a stack.
- `MonoGameUiHost.Draw()` owns `SpriteBatch.Begin(...ScissorRasterizerState)` and `SpriteBatch.End()`.
- Text textures are cached by text run and color; `Dispose()` disposes cached text textures.
- The previous plan should have established adapter coordinate scale propagation.

Target behavior:

- Logical draw commands map to physical MonoGame rectangles/vectors using the active scale.
- Clip rectangles map through the same scale and intersect with current scissor bounds.
- Scissor rectangle is restored after balanced clips and after render submission finishes.
- `PopClip` underflow is benign but diagnostic/test-covered.
- Text texture cache reuses identical text/color/font runs and disposes textures once.
- Backend rejects non-MonoGame images with clear errors, as it does today.

## Rules

- [ ] Do not add new draw command kinds.
- [ ] Do not add gradients, shadows, path rendering, render targets, or effects.
- [ ] Do not make controls reference MonoGame types.
- [ ] Do not make `DrawingContext` own backend state.
- [ ] Do not use live graphics device tests unless the repo already has reliable test infrastructure for them.
- [ ] Prefer pure helper tests for scale/clip algorithms.

---

### Task 1: Add RED Draw Mapping And Clip Tests

**Files:**
- Create: `tests/Cerneala.Tests/Drawing/MonoGame/MonoGameDrawMapperTests.cs`
- Create: `tests/Cerneala.Tests/Drawing/MonoGame/MonoGameClipStackTests.cs`

- [ ] **Step 1: Add scaled draw mapper tests**

Create tests:

```csharp
ScaledRectangleMapsLogicalBoundsToPhysicalPixels()
ScaledVectorMapsLogicalPointToPhysicalVector()
ScaledThicknessRoundsToAtLeastOnePhysicalPixel()
ScaleOnePreservesExistingMapping()
InvalidScaleIsRejected()
```

- [ ] **Step 2: Add clip stack tests**

Create tests:

```csharp
InitialClipUsesViewportBounds()
PushClipIntersectsWithPreviousClip()
NestedPopRestoresPreviousClip()
EmptyIntersectionProducesEmptyClip()
PopUnderflowLeavesClipUnchanged()
BalancedRenderLeavesClipStackEmpty()
```

- [ ] **Step 3: Run targeted tests and verify RED**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~MonoGameDrawMapperTests|FullyQualifiedName~MonoGameClipStackTests"
```

Expected: RED because helper classes do not exist yet.

- [ ] **Step 4: Commit RED tests**

```powershell
git add tests\Cerneala.Tests\Drawing\MonoGame\MonoGameDrawMapperTests.cs tests\Cerneala.Tests\Drawing\MonoGame\MonoGameClipStackTests.cs
git commit -m "test: capture monogame draw mapping and clip contracts"
```

---

### Task 2: Extract Tiny Backend Helpers

**Files:**
- Create: `UI/Drawing/MonoGame/MonoGameDrawMapper.cs`
- Create: `UI/Drawing/MonoGame/MonoGameClipStack.cs`
- Modify: `UI/Drawing/MonoGame/MonoGameDrawingBackend.cs`

- [ ] **Step 1: Implement `MonoGameDrawMapper`**

The helper should map existing draw primitives to MonoGame values using scale:

- `DrawRect` -> `Microsoft.Xna.Framework.Rectangle`
- `DrawPoint` -> `Microsoft.Xna.Framework.Vector2`
- `float thickness` -> positive physical pixel thickness

Keep it internal if public API is not needed.

- [ ] **Step 2: Implement `MonoGameClipStack`**

The helper should:

- track previous clip rectangles;
- compute intersections deterministically;
- tolerate underflow;
- expose stack depth for tests if needed.

- [ ] **Step 3: Replace duplicated backend mapping code**

Use helpers from `MonoGameDrawingBackend` instead of local `ToRectangle`, `ToVector2`, and `Intersect` logic where practical.

Do not rewrite rendering algorithms unless tests require it.

---

### Task 3: Harden Backend State Restoration

**Files:**
- Modify: `UI/Drawing/MonoGame/MonoGameDrawingBackend.cs`
- Create: `tests/Cerneala.Tests/Drawing/MonoGame/MonoGameDrawingBackendStateTests.cs`

- [ ] **Step 1: Add RED backend state tests that do not need a real GPU where possible**

Create tests:

```csharp
RenderWithBalancedClipsEndsWithEmptyClipStack()
RenderWithExtraPopDoesNotThrow()
CoordinateScaleAppliesToBackendMapper()
DisposeIsIdempotent()
DisposeClearsTextTextureCacheDiagnostics()
```

If direct backend construction requires `GraphicsDevice`, test via helper diagnostics or internal seams instead of fragile headless GPU setup.

- [ ] **Step 2: Restore scissor after render**

Ensure that a render submission cannot leave the graphics device scissor rectangle stuck in a nested clip. Use `try/finally` around render if necessary.

- [ ] **Step 3: Clear clip stack after render completion**

A malformed command list should not poison future draw calls. Keep behavior deterministic and test-covered.

- [ ] **Step 4: Make dispose idempotent**

Multiple `Dispose()` calls should not double-dispose textures or throw.

---

### Task 4: Keep Text And Image Runtime Behavior Honest

**Files:**
- Modify: `UI/Drawing/MonoGame/MonoGameDrawingBackend.cs`
- Modify tests only as required.

- [ ] **Step 1: Add diagnostics for text texture cache count if needed**

A small internal/test-visible count is acceptable. Do not expose public cache internals as framework API.

- [ ] **Step 2: Preserve text cache key correctness**

Cache key should include text, font identity, effective size, and color. If coordinate scale changes how text is rasterized, ensure the key changes accordingly.

- [ ] **Step 3: Keep image validation strict**

`DrawImage` should still reject non-`MonoGameImage` images with a clear exception in the MonoGame backend.

---

### Task 5: Verify GREEN And Regressions

- [ ] **Step 1: Run targeted MonoGame drawing tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~MonoGameDrawMapperTests|FullyQualifiedName~MonoGameClipStackTests|FullyQualifiedName~MonoGameDrawingBackendStateTests"
```

Expected: GREEN.

- [ ] **Step 2: Run drawing/rendering boundary tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~DrawingContextTests|FullyQualifiedName~DrawCommandListTests|FullyQualifiedName~AdvancedDrawCommandTests|FullyQualifiedName~RetainedRendererDrawPurityTests|FullyQualifiedName~ArchitectureBoundaryTests"
```

Expected: GREEN.

- [ ] **Step 3: Run full suite**

```powershell
dotnet test Cerneala.slnx
```

Expected: GREEN.

- [ ] **Step 4: Commit implementation**

```powershell
git add UI\Drawing\MonoGame UI\Hosting\MonoGame tests\Cerneala.Tests
git commit -m "fix: harden monogame drawing backend state"
```
