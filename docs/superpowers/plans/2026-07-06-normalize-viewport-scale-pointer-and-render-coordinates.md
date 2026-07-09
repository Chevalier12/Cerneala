# Normalize Viewport Scale, Pointer, And Render Coordinates Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Prioritize throughput with subagents for independent inspection, RED test drafting, implementation patches, and verification, while preserving this plan's dependency order.

**Goal:** Define and enforce one runtime coordinate contract. Cerneala should use logical UI units for retained layout/hit testing, while MonoGame adapters convert physical pixels at the edge. This prevents half-scaled UI, wrong hit testing on HiDPI/backbuffer scaling, and hidden per-control coordinate hacks.

**Architecture:** Keep layout primitives and draw primitives backend-neutral. Do not add duplicate `Point`, `Rect`, or color types. Add small host/adapter helpers where needed. Core `UiHost.Update(InputFrame, ...)` should treat explicit `InputFrame` coordinates as already-normalized logical UI coordinates. MonoGame input and drawing adapters perform physical/logical conversion.

**Tech Stack:** C#/.NET 8, xUnit, existing `UI/Hosting`, `UI/Input/MonoGame`, `Drawing/MonoGame`, retained layout/render tests.

---

## File Structure

- Modify: `UI/Hosting/UiViewport.cs`
  - Add explicit factory/helper APIs for physical backbuffer size and scale.
- Create: `UI/Hosting/UiCoordinateMapper.cs`
  - Small backend-neutral pure helper for logical/physical conversion if useful.
- Modify: `UI/Input/PointerSnapshot.cs`
  - Add a non-mutating scale/map helper if useful for tests and adapter use.
- Modify: `UI/Input/InputFrame.cs`
  - Add a non-mutating pointer coordinate transform helper if useful.
- Modify: `UI/Input/MonoGame/MonoGameInputSource.cs`
  - Normalize physical mouse coordinates through an explicit scale provided by host/options.
- Modify: `UI/Hosting/MonoGame/MonoGameUiHost.cs`
  - Ensure MonoGame input scale and drawing scale are updated from the current `UiViewport`.
- Modify: `UI/Hosting/MonoGame/MonoGameUiHostOptions.cs`
  - Add explicit default scale/coordinate options only if required.
- Modify later in the next plan: `Drawing/MonoGame/MonoGameDrawingBackend.cs`
  - This plan may add the property/contract; backend implementation can be completed in the render backend hardening plan.
- Create: `tests/Cerneala.Tests/UI/Hosting/UiViewportScaleContractTests.cs`
- Create: `tests/Cerneala.Tests/Input/MonoGameInputCoordinateScaleTests.cs`
- Create: `tests/Cerneala.Tests/UI/Hosting/UiHostScaleHitTestContractTests.cs`

## Important Existing Behavior

- `UiViewport` already stores `Width`, `Height`, and `Scale`.
- `UIRoot.SetViewport(...)` stores scale and increments tree version.
- `UiHost.Update(InputFrame, viewport, elapsed)` dispatches explicit `InputFrame` directly.
- `MonoGameInputSource` reads raw `Mouse.GetState()` physical coordinates today.
- `MonoGameDrawingBackend` currently converts draw command coordinates directly to MonoGame rectangles/vectors.
- Text services have a scale concept in `TextRunStyle`, but viewport-scale semantics are not consistently locked at the runtime boundary.

Target behavior:

- `UiViewport.Width` and `UiViewport.Height` are logical UI units.
- `UiViewport.Scale` is physical pixels per logical UI unit.
- `UiViewport.FromPhysicalPixels(pixelWidth, pixelHeight, scale)` returns logical width/height and stores scale.
- Explicit test-created `InputFrame` instances are already logical and are not scaled again by `UiHost`.
- `MonoGameInputSource` converts physical mouse coordinates to logical coordinates using the active scale.
- `MonoGameUiHost.Update(...)` synchronizes the adapter scale before input is read.
- Scale changes invalidate measure/arrange/render/hit-test through the existing viewport path.

## Rules

- [ ] Do not introduce duplicate geometry primitives.
- [ ] Do not change core `LayoutPoint`, `LayoutRect`, `DrawPoint`, or `DrawRect` responsibilities.
- [ ] Do not make controls know about MonoGame pixels.
- [ ] Do not scale explicit `InputFrame` values inside `UiHost`.
- [ ] Do not implement native OS DPI provider behavior in this plan.
- [ ] Do not implement package split in this plan.

---

### Task 1: Add RED Viewport Scale Contract Tests

**Files:**
- Create: `tests/Cerneala.Tests/UI/Hosting/UiViewportScaleContractTests.cs`
- Create: `tests/Cerneala.Tests/UI/Hosting/UiHostScaleHitTestContractTests.cs`

- [ ] **Step 1: Add viewport helper tests**

Create tests:

```csharp
FromPhysicalPixelsDividesWidthAndHeightByScale()
FromPhysicalPixelsRejectsInvalidPixelSizeOrScale()
LogicalToPhysicalRoundsDeterministically()
PhysicalToLogicalPreservesFractionalLogicalCoordinates()
ViewportEqualityIncludesScale()
```

- [ ] **Step 2: Add host scale invalidation tests**

Create tests:

```csharp
ViewportScaleChangeInvalidatesMeasureArrangeRenderAndHitTest()
ViewportScaleChangeRebuildsHitTestBeforeNextInput()
ExplicitInputFrameCoordinatesAreNotScaledByUiHost()
UnchangedScaleSecondFrameDoesNoRetainedWork()
```

Use simple retained elements with known arranged bounds. The hit-test assertion should prove that logical pointer coordinates still hit the same retained element after a scale-only viewport change.

- [ ] **Step 3: Run targeted tests and verify RED**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~UiViewportScaleContractTests|FullyQualifiedName~UiHostScaleHitTestContractTests"
```

Expected: RED because the helper/API and explicit scale contract are not fully implemented.

- [ ] **Step 4: Commit RED tests**

```powershell
git add tests\Cerneala.Tests\UI\Hosting\UiViewportScaleContractTests.cs tests\Cerneala.Tests\UI\Hosting\UiHostScaleHitTestContractTests.cs
git commit -m "test: capture viewport scale coordinate contract"
```

---

### Task 2: Implement Backend-Neutral Coordinate Helpers

**Files:**
- Modify: `UI/Hosting/UiViewport.cs`
- Create: `UI/Hosting/UiCoordinateMapper.cs` if useful.
- Modify: `UI/Input/PointerSnapshot.cs` only if useful.
- Modify: `UI/Input/InputFrame.cs` only if useful.

- [ ] **Step 1: Add `UiViewport.FromPhysicalPixels(...)`**

Add a static factory:

```csharp
public static UiViewport FromPhysicalPixels(int pixelWidth, int pixelHeight, float scale)
```

Behavior:

- validate `pixelWidth >= 0`, `pixelHeight >= 0`, `scale > 0`;
- return `new UiViewport(pixelWidth / scale, pixelHeight / scale, scale)`;
- preserve finite validation already present in the constructor.

- [ ] **Step 2: Add small coordinate conversion helpers**

Keep helpers tiny and pure. Example API shape:

```csharp
public static float LogicalToPhysical(float logical, float scale);
public static float PhysicalToLogical(float physical, float scale);
public static int LogicalToPhysicalPixel(float logical, float scale);
```

Do not create new public geometry structs.

- [ ] **Step 3: Add pointer transform helper only if tests need it**

If adding to `InputFrame`, keep it non-mutating:

```csharp
public InputFrame TransformPointer(Func<float, float> mapX, Func<float, float> mapY)
```

Prefer simpler `PointerSnapshot.ScaleCoordinates(float divisor)` if enough.

---

### Task 3: Normalize MonoGame Pointer Input At The Adapter Edge

**Files:**
- Modify: `UI/Input/MonoGame/MonoGameInputSource.cs`
- Modify: `UI/Hosting/MonoGame/MonoGameUiHost.cs`
- Create: `tests/Cerneala.Tests/Input/MonoGameInputCoordinateScaleTests.cs`

- [ ] **Step 1: Add RED MonoGame input scale tests**

Create tests:

```csharp
MonoGameInputSourceExposesDefaultCoordinateScaleOfOne()
CoordinateScaleDividesMousePositionIntoLogicalCoordinates()
CoordinateScaleDoesNotAffectWheelDeltaOrButtons()
CoordinateScaleRejectsZeroNegativeOrNaN()
MonoGameUiHostUpdatesInputSourceScaleBeforeReadingFrame()
```

Use seams/test hooks that avoid real `Mouse.GetState()` where possible. If `MonoGameInputSource` has no injectable state reader, add a tiny internal state reader seam rather than broad redesign.

- [ ] **Step 2: Add `CoordinateScale` to `MonoGameInputSource`**

Behavior:

- default `1`;
- validation: finite and > 0;
- raw physical `MouseState.X/Y` become logical `x / CoordinateScale`, `y / CoordinateScale`;
- wheel values and button states remain unchanged.

- [ ] **Step 3: Wire host update**

Before reading input inside `MonoGameUiHost.Update(viewport, elapsedTime)`, set:

```text
InputSource.CoordinateScale = viewport.Scale
```

Do not scale explicit `Update(InputFrame, viewport, elapsedTime)` frames.

---

### Task 4: Prepare Drawing Scale Contract For Backend Hardening

**Files:**
- Modify: `Drawing/MonoGame/MonoGameDrawingBackend.cs`
- Modify: `UI/Hosting/MonoGame/MonoGameUiHost.cs`

- [ ] **Step 1: Add a backend coordinate scale property**

Add an adapter-level property such as:

```csharp
public float CoordinateScale { get; set; } = 1f;
```

Validation: finite and > 0.

- [ ] **Step 2: Host sets backend scale before drawing**

`MonoGameUiHost.Draw()` should set backend scale from the current host viewport before rendering.

- [ ] **Step 3: Do not implement all backend conversions here if the next plan owns them**

It is acceptable for the backend hardening plan to finish rectangle/vector/clip/text scale mapping, but this plan must establish the contract and tests for scale propagation.

---

### Task 5: Verify GREEN And Regressions

- [ ] **Step 1: Run targeted scale tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~UiViewportScaleContractTests|FullyQualifiedName~UiHostScaleHitTestContractTests|FullyQualifiedName~MonoGameInputCoordinateScaleTests"
```

Expected: GREEN.

- [ ] **Step 2: Run hosting/input retained tests**

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~UiHostViewportFrameContractTests|FullyQualifiedName~CorePreviewContractTests|FullyQualifiedName~AuthoringPreviewContractTests|FullyQualifiedName~MonoGameInputMapperTests"
```

Expected: GREEN.

- [ ] **Step 3: Run full suite**

```powershell
dotnet test Cerneala.slnx
```

Expected: GREEN.

- [ ] **Step 4: Commit implementation**

```powershell
git add UI\Hosting UI\Input tests\Cerneala.Tests
git commit -m "feat: normalize runtime viewport scale coordinates"
```
