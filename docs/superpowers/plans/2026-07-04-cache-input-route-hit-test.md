# Cache Input Route Hit Test Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make input route and hit-test data root-owned, retained, and invalidation-driven, then remove direct `UI/Input` dependencies on concrete controls.

**Architecture:** Add an `ElementInputCache` owned by `UIRoot`; it stores the retained `ElementInputRouteMap`, rebuilds only when hit-test/input-route invalidation marks it dirty, and is consumed by mouse, touch, stylus, keyboard, and text input dispatch. `HitTestQueue` remains the scheduler-visible dirty work queue; the hit-test frame phase rebuilds the root cache once, while input dispatch can also call `EnsureCurrent(...)` so host input sees tree/layout changes before the rest of the frame. Control-specific button and thumb behavior moves behind small input-level interfaces implemented by controls.

**Tech Stack:** C#/.NET 8, xUnit, Cerneala retained UI runtime, existing `UIRoot`, `UIElement`, `HitTestQueue`, `ElementInputRouteBuilder`, `ElementInputRouteMap`, `HitTestService`, routed events, pointer/touch/stylus bridges, and command routing.

---

## File Structure

- Create: `UI/Input/ElementInputCache.cs`
  - Root-owned retained route map.
  - Tracks dirty state and rebuild count for diagnostics/tests.
  - Exposes `EnsureCurrent(...)`, `Rebuild(...)`, `Invalidate(...)`, `RouteMap`, and `HitTest(...)`.
- Create: `UI/Input/IInputPressable.cs`
  - Input-layer contract for pressed visual state; implemented by `ButtonBase`.
- Create: `UI/Input/IInputCommandSource.cs`
  - Input-layer command execution contract; implemented by `ButtonBase`.
- Create: `UI/Input/IPointerDragSource.cs`
  - Input-layer pointer drag contract; implemented by `Thumb`.
- Modify: `UI/Elements/UIRoot.cs`
  - Owns `ElementInputCache`.
  - Invalidates it when effective flags include `HitTest`.
  - Wires `FramePhase.HitTest` to rebuild the retained input cache.
- Modify: `UI/Elements/UIElement.cs`
  - Constructs `ElementHandlerStore` with its owner so handler changes can invalidate route data.
- Modify: `UI/Elements/ElementHandlerStore.cs`
  - Adds owner-aware route invalidation on handler add/remove.
  - Adds `RemoveHandler(...)`.
- Modify: `UI/Input/ElementInputBridge.cs`
  - Uses `root.InputCache` instead of `ElementInputRouteBuilder.Build(...)`.
  - Removes direct `ButtonBase`/`Thumb` references.
  - Uses `IInputCommandSource`, `IInputPressable`, and `IPointerDragSource`.
- Modify: `UI/Input/PressedStateTracker.cs`
  - Tracks `IInputPressable`, not `ButtonBase`.
- Modify: `UI/Input/HitTestService.cs`
  - Keeps existing direct route-map overload.
  - Updates `HitTest(UIRoot, ...)` convenience overload to use `root.InputCache`.
- Modify: `UI/Input/TouchInputBridge.cs`
  - Uses `root.InputCache` instead of building a route map per dispatch.
- Modify: `UI/Input/StylusInputBridge.cs`
  - Uses `root.InputCache` instead of building a route map per dispatch.
- Modify: `UI/Controls/Primitives/ButtonBase.cs`
  - Implements `IInputPressable` and `IInputCommandSource`.
- Modify: `UI/Controls/Primitives/Thumb.cs`
  - Implements `IPointerDragSource`.
- Create: `tests/Cerneala.Tests/UI/Input/ElementInputCacheInvalidationTests.cs`
  - RED/GREEN coverage for retained cache reuse and dirty rebuilds.
- Create: `tests/Cerneala.Tests/UI/Input/HitTestCacheInvalidationTests.cs`
  - RED/GREEN coverage for tree, layout, visibility, enabled, handler, and capture-related route/hit-test invalidation.
- Create: `tests/Cerneala.Tests/UI/Input/InputControlBoundaryTests.cs`
  - Architecture boundary test proving `UI/Input` no longer references `UI/Controls`.
- Modify: existing tests under `tests/Cerneala.Tests/Input/*` only where API changes require using `root.InputCache.RouteMap`.
- Modify: `AUDIT_FIX_PLAN.md`
  - Link this detailed plan now.
  - Mark Plan 4 checklist only after implementation and verification.
- Modify: `ROADMAPv2_AUDIT.md`
  - Add implementation note only after full verification passes.

## Important Existing Behavior

Current `ElementInputBridge.Dispatch(...)` rebuilds the route map every input dispatch:

```csharp
ElementInputRouteMap routeMap = routeBuilder.Build(root);
HitTestResult? hitTarget = hitTestService.HitTest(root, routeMap, inputFrame.Pointer.X, inputFrame.Pointer.Y);
```

`TouchInputBridge` and `StylusInputBridge` do the same thing. `HitTestService.HitTest(UIRoot, ...)` also creates its own route map internally.

Current `HitTestQueue` is only queue/accounting:

```csharp
public sealed class HitTestQueue
{
    public IReadOnlyList<UIElement> Snapshot()
    {
        ElementQueueOrder.RemoveElementsOutsideRoot(root, elements, order);
        return ElementQueueOrder.Sort(root, order.Where(elements.Contains));
    }
}
```

The scheduler processes `FramePhase.HitTest`, but there is no processor wired by `UIRoot`, so the phase clears dirty flags without rebuilding retained input data.

Current `UI/Input` depends on controls directly:

```csharp
using Cerneala.UI.Controls.Primitives;

private static ButtonBase? FindButtonBase(UIElement element) { ... }
private static Thumb? FindThumb(UIElement element) { ... }
```

`PressedStateTracker` also stores `ButtonBase?`. The target is an input-layer interface boundary:

```csharp
public interface IInputPressable
{
    bool IsPressed { get; set; }
}

public interface IInputCommandSource
{
    bool ExecuteCommand(CommandRouter router, ElementInputRouteMap routeMap);
}

public interface IPointerDragSource
{
    bool BeginPointerDrag(PointerCaptureManager captureManager, ElementInputRouteMap routeMap, MouseButtonEventArgs args);
    bool UpdatePointerDrag(MouseEventArgs args);
    bool CompletePointerDrag(PointerCaptureManager captureManager, ElementInputRouteMap routeMap, MouseButtonEventArgs args);
}
```

---

### Task 1: Add RED Tests For Retained Input Cache

**Files:**
- Create: `tests/Cerneala.Tests/UI/Input/ElementInputCacheInvalidationTests.cs`
- Create: `tests/Cerneala.Tests/UI/Input/HitTestCacheInvalidationTests.cs`

- [ ] **Step 1: Create retained cache reuse tests**

Create `tests/Cerneala.Tests/UI/Input/ElementInputCacheInvalidationTests.cs`:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.UI.Input;

public sealed class ElementInputCacheInvalidationTests
{
    [Fact]
    public void InputDispatchReusesRouteMapWhenNothingChanged()
    {
        UIRoot root = RootWithChild(out UIElement child);
        ElementInputBridge bridge = new();

        bridge.Dispatch(root, PointerFrame(10, 10));
        int rebuildsAfterFirstDispatch = root.InputCache.RebuildCount;
        ElementInputRouteMap firstMap = root.InputCache.RouteMap;

        bridge.Dispatch(root, PointerFrame(11, 10));

        Assert.Same(firstMap, root.InputCache.RouteMap);
        Assert.Equal(rebuildsAfterFirstDispatch, root.InputCache.RebuildCount);
        Assert.True(child.IsPointerOver);
    }

    [Fact]
    public void HitTestInvalidationRebuildsRouteMapOnce()
    {
        UIRoot root = RootWithChild(out UIElement child);
        ElementInputBridge bridge = new();
        bridge.Dispatch(root, PointerFrame(10, 10));
        int rebuildsAfterFirstDispatch = root.InputCache.RebuildCount;

        child.Visibility = Visibility.Collapsed;
        bridge.Dispatch(root, PointerFrame(10, 10));

        Assert.Equal(rebuildsAfterFirstDispatch + 1, root.InputCache.RebuildCount);
        Assert.False(root.InputCache.RouteMap.TryGetId(child, out _));
    }

    [Fact]
    public void HandlerAddedAfterCacheBuildInvalidatesRouteMap()
    {
        UIRoot root = RootWithChild(out UIElement child);
        ElementInputBridge bridge = new();
        bridge.Dispatch(root, PointerFrame(10, 10));
        int rebuildsAfterFirstDispatch = root.InputCache.RebuildCount;
        bool called = false;

        child.Handlers.AddHandler(InputEvents.MouseDownEvent, (_, _) => called = true);
        bridge.Dispatch(root, PointerFrame(10, 10, pressed: true));

        Assert.True(called);
        Assert.Equal(rebuildsAfterFirstDispatch + 1, root.InputCache.RebuildCount);
    }

    [Fact]
    public void HitTestPhaseRebuildsDirtyInputCacheBeforeNoWorkFrames()
    {
        UIRoot root = RootWithChild(out UIElement child);
        root.InputCache.EnsureCurrent(root);
        int rebuildsAfterInitialBuild = root.InputCache.RebuildCount;

        child.Invalidate(Cerneala.UI.Invalidation.InvalidationFlags.HitTest, "route changed");
        Cerneala.UI.Invalidation.FrameStats stats = root.ProcessFrame();

        Assert.Equal(rebuildsAfterInitialBuild + 1, root.InputCache.RebuildCount);
        Assert.True(stats.HitTestElements > 0);
        Assert.False(root.Scheduler.HasWork);
    }

    private static UIRoot RootWithChild(out UIElement child)
    {
        UIRoot root = new(100, 100);
        child = Arranged(0, 0, 40, 40);
        root.VisualChildren.Add(child);
        root.ProcessFrame();
        return root;
    }

    private static UIElement Arranged(float x, float y, float width, float height)
    {
        UIElement element = new();
        element.Arrange(new ArrangeContext(new LayoutRect(x, y, width, height)));
        return element;
    }

    private static InputFrame PointerFrame(float x, float y, bool pressed = false)
    {
        PointerSnapshot previous = PointerSnapshot.Empty.WithPosition(x, y);
        PointerSnapshot current = PointerSnapshot.Empty.WithPosition(x, y);
        if (pressed)
        {
            current = current.WithButton(InputMouseButton.Left, true);
        }

        return new InputFrame(previous, current, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
    }
}
```

- [ ] **Step 2: Create hit-test invalidation trigger tests**

Create `tests/Cerneala.Tests/UI/Input/HitTestCacheInvalidationTests.cs`:

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Input;
using Cerneala.UI.Layout;

namespace Cerneala.Tests.UI.Input;

public sealed class HitTestCacheInvalidationTests
{
    [Fact]
    public void VisualTreeMutationInvalidatesInputCache()
    {
        UIRoot root = new(100, 100);
        UIElement first = Arranged(0, 0, 40, 40);
        root.VisualChildren.Add(first);
        root.ProcessFrame();
        root.InputCache.EnsureCurrent(root);
        int rebuildsAfterInitialBuild = root.InputCache.RebuildCount;

        UIElement second = Arranged(40, 0, 40, 40);
        root.VisualChildren.Add(second);
        root.InputCache.EnsureCurrent(root);

        Assert.Equal(rebuildsAfterInitialBuild + 1, root.InputCache.RebuildCount);
        Assert.True(root.InputCache.RouteMap.TryGetId(second, out _));
    }

    [Fact]
    public void LayoutBoundsChangeInvalidatesHitTestResult()
    {
        UIRoot root = new(100, 100);
        UIElement child = Arranged(0, 0, 20, 20);
        root.VisualChildren.Add(child);
        root.ProcessFrame();
        root.InputCache.EnsureCurrent(root);

        child.Arrange(new ArrangeContext(new LayoutRect(50, 0, 20, 20)));
        child.Invalidate(Cerneala.UI.Invalidation.InvalidationFlags.HitTest, "manual bounds change");

        HitTestResult? oldPoint = root.InputCache.HitTest(root, 10, 10);
        HitTestResult? newPoint = root.InputCache.HitTest(root, 55, 10);

        Assert.NotSame(child, oldPoint?.Element);
        Assert.Same(child, newPoint!.Element);
    }

    [Fact]
    public void DisabledAndInvisibleElementsAreRemovedFromRetainedRouteMap()
    {
        UIRoot root = new(100, 100);
        UIElement disabled = Arranged(0, 0, 40, 40);
        UIElement invisible = Arranged(40, 0, 40, 40);
        root.VisualChildren.Add(disabled);
        root.VisualChildren.Add(invisible);
        root.ProcessFrame();
        root.InputCache.EnsureCurrent(root);

        disabled.IsEnabled = false;
        invisible.IsVisible = false;
        root.InputCache.EnsureCurrent(root);

        Assert.False(root.InputCache.RouteMap.TryGetId(disabled, out _));
        Assert.False(root.InputCache.RouteMap.TryGetId(invisible, out _));
    }

    [Fact]
    public void RemovedCapturedElementIsReleasedWhenRouteMapRebuilds()
    {
        UIRoot root = new(100, 100);
        UIElement captured = Arranged(0, 0, 40, 40);
        UIElement fallback = Arranged(50, 0, 40, 40);
        root.VisualChildren.Add(captured);
        root.VisualChildren.Add(fallback);
        root.ProcessFrame();
        ElementInputBridge bridge = new();
        root.InputCache.EnsureCurrent(root);
        bridge.PointerCaptureManager.Capture(captured, root.InputCache.RouteMap);

        root.VisualChildren.Remove(captured);
        bridge.Dispatch(root, PointerFrame(60, 10));

        Assert.False(bridge.PointerCaptureManager.HasCapture);
    }

    private static UIElement Arranged(float x, float y, float width, float height)
    {
        UIElement element = new();
        element.Arrange(new ArrangeContext(new LayoutRect(x, y, width, height)));
        return element;
    }

    private static InputFrame PointerFrame(float x, float y)
    {
        PointerSnapshot previous = PointerSnapshot.Empty.WithPosition(x, y);
        PointerSnapshot current = PointerSnapshot.Empty.WithPosition(x, y);
        return new InputFrame(previous, current, KeyboardSnapshot.Empty, KeyboardSnapshot.Empty, []);
    }
}
```

- [ ] **Step 3: Run cache RED tests**

Run:

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~ElementInputCacheInvalidationTests|FullyQualifiedName~HitTestCacheInvalidationTests"
```

Expected: build fails because `UIRoot.InputCache` and `ElementInputCache` do not exist. This is the valid RED for retained input cache ownership.

- [ ] **Step 4: Commit RED tests**

```powershell
git add tests\Cerneala.Tests\UI\Input\ElementInputCacheInvalidationTests.cs tests\Cerneala.Tests\UI\Input\HitTestCacheInvalidationTests.cs
git commit -m "test: capture retained input cache gaps"
```

---

### Task 2: Add Root-Owned Retained Input Cache

**Files:**
- Create: `UI/Input/ElementInputCache.cs`
- Modify: `UI/Elements/UIRoot.cs`
- Modify: `UI/Input/HitTestService.cs`

- [ ] **Step 1: Create `ElementInputCache`**

Create `UI/Input/ElementInputCache.cs`:

```csharp
using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public sealed class ElementInputCache
{
    private readonly ElementInputRouteBuilder routeBuilder;
    private readonly HitTestService hitTestService;
    private bool isDirty = true;

    public ElementInputCache(ElementInputRouteBuilder? routeBuilder = null, HitTestService? hitTestService = null)
    {
        this.routeBuilder = routeBuilder ?? new ElementInputRouteBuilder();
        this.hitTestService = hitTestService ?? new HitTestService();
        RouteMap = new ElementInputRouteMap();
        LastInvalidationReason = "Initial input cache";
    }

    public ElementInputRouteMap RouteMap { get; private set; }

    public bool IsDirty => isDirty;

    public int RebuildCount { get; private set; }

    public string LastInvalidationReason { get; private set; }

    public void Invalidate(string reason)
    {
        LastInvalidationReason = string.IsNullOrWhiteSpace(reason)
            ? "Input route changed"
            : reason;
        isDirty = true;
    }

    public ElementInputRouteMap EnsureCurrent(UIRoot root)
    {
        ArgumentNullException.ThrowIfNull(root);
        if (isDirty)
        {
            Rebuild(root);
        }

        return RouteMap;
    }

    public ElementInputRouteMap Rebuild(UIRoot root)
    {
        ArgumentNullException.ThrowIfNull(root);
        RouteMap = routeBuilder.Build(root);
        RebuildCount++;
        isDirty = false;
        return RouteMap;
    }

    public HitTestResult? HitTest(UIRoot root, float x, float y, HitTestFilter? filter = null)
    {
        ArgumentNullException.ThrowIfNull(root);
        ElementInputRouteMap routeMap = EnsureCurrent(root);
        return hitTestService.HitTest(root, routeMap, x, y, filter);
    }
}
```

- [ ] **Step 2: Add `InputCache` to `UIRoot`**

In `UI/Elements/UIRoot.cs`, add the property near the queues/processors:

```csharp
public ElementInputCache InputCache { get; }
```

In the constructor, after `HitTestQueue = new HitTestQueue(this);`, add:

```csharp
InputCache = new ElementInputCache();
```

Add `using Cerneala.UI.Input;` if it is not already present.

- [ ] **Step 3: Invalidate input cache from retained invalidation**

In `UI/Elements/UIRoot.cs`, update `Invalidate(InvalidationRequest request)`:

```csharp
public override void Invalidate(InvalidationRequest request)
{
    ArgumentNullException.ThrowIfNull(request);
    Trace.RecordRequest(request);
    InvalidationFlags effective = DirtyPropagation.Default.GetEffectiveFlags(request);
    if (effective.HasFlag(InvalidationFlags.Render))
    {
        RetainedRenderCache.InvalidateRoot();
    }

    if (effective.HasFlag(InvalidationFlags.HitTest))
    {
        InputCache.Invalidate(request.Reason);
    }

    DirtyPropagation.Default.Propagate(request, this, LayoutQueue, StyleQueue, RenderQueue, HitTestQueue, Trace);
}
```

- [ ] **Step 4: Rebuild input cache during hit-test phase**

In `UI/Elements/UIRoot.cs`, update `CreatePhaseProcessors()`:

```csharp
return new FramePhaseProcessors
{
    Style = StyleProcessor.Process,
    Measure = layoutProcessors.Measure,
    Arrange = layoutProcessors.Arrange,
    RenderCache = RenderQueueProcessor.Process,
    HitTest = _ => InputCache.EnsureCurrent(this)
};
```

This intentionally rebuilds once per dirty frame; after the first hit-test element, `EnsureCurrent(...)` is a no-op.

- [ ] **Step 5: Update `HitTestService` root overload**

In `UI/Input/HitTestService.cs`, replace the root overload with:

```csharp
public HitTestResult? HitTest(UIRoot root, float x, float y, HitTestFilter? filter = null)
{
    ArgumentNullException.ThrowIfNull(root);
    return root.InputCache.HitTest(root, x, y, filter);
}
```

Keep the existing `HitTest(UIElement root, ElementInputRouteMap routeMap, ...)` overload unchanged.

- [ ] **Step 6: Run retained cache tests**

Run:

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~ElementInputCacheInvalidationTests|FullyQualifiedName~HitTestCacheInvalidationTests|FullyQualifiedName~HitTestServiceTests|FullyQualifiedName~HitTestQueueTests"
```

Expected: cache tests still fail where bridges and handler changes still rebuild stale maps or miss invalidation. Existing hit-test service/queue tests should keep passing.

- [ ] **Step 7: Commit root cache infrastructure**

```powershell
git add UI\Input\ElementInputCache.cs UI\Elements\UIRoot.cs UI\Input\HitTestService.cs
git commit -m "feat: add root-owned input cache"
```

---

### Task 3: Make Input Bridges Consume The Retained Cache

**Files:**
- Modify: `UI/Input/ElementInputBridge.cs`
- Modify: `UI/Input/TouchInputBridge.cs`
- Modify: `UI/Input/StylusInputBridge.cs`
- Modify: existing tests under `tests/Cerneala.Tests/Input/*` only if they construct route maps manually for capture setup.

- [ ] **Step 1: Update mouse bridge route acquisition**

In `UI/Input/ElementInputBridge.cs`, remove the `routeBuilder` field and stop calling `routeBuilder.Build(root)`.

Change the constructor signature from:

```csharp
public ElementInputBridge(
    ElementInputRouteBuilder? routeBuilder = null,
    HitTestService? hitTestService = null,
    PointerCaptureManager? pointerCaptureManager = null,
    HoverTracker? hoverTracker = null,
    PressedStateTracker? pressedStateTracker = null,
    ClickTracker? clickTracker = null,
    CommandRouter? commandRouter = null,
    FocusManager? focusManager = null,
    TextInputBridge? textInputBridge = null)
```

to:

```csharp
public ElementInputBridge(
    HitTestService? hitTestService = null,
    PointerCaptureManager? pointerCaptureManager = null,
    HoverTracker? hoverTracker = null,
    PressedStateTracker? pressedStateTracker = null,
    ClickTracker? clickTracker = null,
    CommandRouter? commandRouter = null,
    FocusManager? focusManager = null,
    TextInputBridge? textInputBridge = null)
```

Update `Dispatch(...)`:

```csharp
public void Dispatch(UIRoot root, InputFrame inputFrame)
{
    ArgumentNullException.ThrowIfNull(root);
    ArgumentNullException.ThrowIfNull(inputFrame);

    ElementInputRouteMap routeMap = root.InputCache.EnsureCurrent(root);
    HitTestResult? hitTarget = hitTestService.HitTest(root, routeMap, inputFrame.Pointer.X, inputFrame.Pointer.Y);
    HitTestResult? pointerTarget = pointerCaptureManager.OverrideTarget(hitTarget, routeMap, inputFrame.Pointer.X, inputFrame.Pointer.Y);

    DispatchPointer(inputFrame, routeMap, hitTarget, pointerTarget);
    focusManager.DispatchKeyboard(inputFrame, routeMap);
    textInputBridge.Dispatch(inputFrame.TextInputEvents, focusManager, routeMap);
}
```

- [ ] **Step 2: Update touch bridge route acquisition**

In `UI/Input/TouchInputBridge.cs`, remove `routeBuilder` and constructor injection for it. Use:

```csharp
public TouchInputBridge(HitTestService? hitTestService = null)
{
    this.hitTestService = hitTestService ?? new HitTestService();
}
```

Update `Dispatch(...)`:

```csharp
public void Dispatch(UIRoot root, TouchInputFrame frame)
{
    ArgumentNullException.ThrowIfNull(root);
    ArgumentNullException.ThrowIfNull(frame);

    ElementInputRouteMap routeMap = root.InputCache.EnsureCurrent(root);
    foreach (TouchInputPoint point in frame.Points)
    {
        DispatchPoint(root, routeMap, point);
    }
}
```

- [ ] **Step 3: Update stylus bridge route acquisition**

In `UI/Input/StylusInputBridge.cs`, remove `routeBuilder` and constructor injection for it. Use:

```csharp
public StylusInputBridge(HitTestService? hitTestService = null)
{
    this.hitTestService = hitTestService ?? new HitTestService();
}
```

Update `Dispatch(...)`:

```csharp
public void Dispatch(UIRoot root, StylusInputFrame frame)
{
    ArgumentNullException.ThrowIfNull(root);
    ArgumentNullException.ThrowIfNull(frame);

    ElementInputRouteMap routeMap = root.InputCache.EnsureCurrent(root);
    foreach (StylusInputPoint point in frame.Points)
    {
        HitTestResult? target = hitTestService.HitTest(root, routeMap, point.X, point.Y);
        if (target is null)
        {
            continue;
        }

        DispatchPoint(routeMap, target, point);
    }
}
```

- [ ] **Step 4: Update capture setup tests**

Where tests currently call:

```csharp
ElementInputRouteMap routeMap = new ElementInputRouteBuilder().Build(root);
bridge.PointerCaptureManager.Capture(button, routeMap);
```

replace with:

```csharp
ElementInputRouteMap routeMap = root.InputCache.EnsureCurrent(root);
bridge.PointerCaptureManager.Capture(button, routeMap);
```

Do this only in tests that need capture setup; keep `ElementInputRouteBuilderTests` unchanged because they test the builder directly.

- [ ] **Step 5: Run bridge cache tests**

Run:

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~ElementInputCacheInvalidationTests|FullyQualifiedName~HitTestCacheInvalidationTests|FullyQualifiedName~ElementInputBridgeTests|FullyQualifiedName~Touch|FullyQualifiedName~Stylus"
```

Expected: cache reuse tests pass except handler invalidation may still fail until Task 4.

- [ ] **Step 6: Commit retained cache consumption**

```powershell
git add UI\Input\ElementInputBridge.cs UI\Input\TouchInputBridge.cs UI\Input\StylusInputBridge.cs tests\Cerneala.Tests
git commit -m "fix: route input through retained cache"
```

---

### Task 4: Invalidate Route Cache On Handler Changes

**Files:**
- Modify: `UI/Elements/UIElement.cs`
- Modify: `UI/Elements/ElementHandlerStore.cs`
- Modify: `tests/Cerneala.Tests/UI/Input/ElementInputCacheInvalidationTests.cs`
- Modify: existing handler-store tests if constructor changes require it.

- [ ] **Step 1: Make `ElementHandlerStore` owner-aware**

In `UI/Elements/ElementHandlerStore.cs`, replace the class start with:

```csharp
public sealed class ElementHandlerStore
{
    private readonly UIElement owner;
    private readonly Dictionary<RoutedEvent, List<RoutedEventHandler>> handlers = [];

    internal ElementHandlerStore(UIElement owner)
    {
        this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }
```

- [ ] **Step 2: Update `UIElement` construction**

In `UI/Elements/UIElement.cs`, replace:

```csharp
public ElementHandlerStore Handlers { get; } = new();
```

with:

```csharp
public ElementHandlerStore Handlers { get; }
```

In the `UIElement()` constructor, after child collections are created, add:

```csharp
Handlers = new ElementHandlerStore(this);
```

- [ ] **Step 3: Invalidate route cache on add/remove**

In `UI/Elements/ElementHandlerStore.cs`, update `AddHandler(...)`:

```csharp
public void AddHandler(RoutedEvent routedEvent, RoutedEventHandler handler)
{
    ArgumentNullException.ThrowIfNull(routedEvent);
    ArgumentNullException.ThrowIfNull(handler);

    if (!handlers.TryGetValue(routedEvent, out List<RoutedEventHandler>? registeredHandlers))
    {
        registeredHandlers = [];
        handlers.Add(routedEvent, registeredHandlers);
    }

    registeredHandlers.Add(handler);
    InvalidateRoute("Input handler added");
}
```

Add `RemoveHandler(...)`:

```csharp
public bool RemoveHandler(RoutedEvent routedEvent, RoutedEventHandler handler)
{
    ArgumentNullException.ThrowIfNull(routedEvent);
    ArgumentNullException.ThrowIfNull(handler);

    if (!handlers.TryGetValue(routedEvent, out List<RoutedEventHandler>? registeredHandlers))
    {
        return false;
    }

    bool removed = registeredHandlers.Remove(handler);
    if (!removed)
    {
        return false;
    }

    if (registeredHandlers.Count == 0)
    {
        handlers.Remove(routedEvent);
    }

    InvalidateRoute("Input handler removed");
    return true;
}
```

Add helper:

```csharp
private void InvalidateRoute(string reason)
{
    if (owner.Root is null)
    {
        return;
    }

    owner.Invalidate(Cerneala.UI.Invalidation.InvalidationFlags.HitTest, reason);
}
```

- [ ] **Step 4: Add remove-handler cache test**

Append to `tests/Cerneala.Tests/UI/Input/ElementInputCacheInvalidationTests.cs`:

```csharp
[Fact]
public void HandlerRemovedAfterCacheBuildInvalidatesRouteMap()
{
    UIRoot root = RootWithChild(out UIElement child);
    ElementInputBridge bridge = new();
    int calls = 0;
    RoutedEventHandler handler = (_, _) => calls++;
    child.Handlers.AddHandler(InputEvents.MouseDownEvent, handler);
    bridge.Dispatch(root, PointerFrame(10, 10, pressed: true));
    int rebuildsAfterFirstDispatch = root.InputCache.RebuildCount;

    Assert.True(child.Handlers.RemoveHandler(InputEvents.MouseDownEvent, handler));
    bridge.Dispatch(root, PointerFrame(10, 10, pressed: true));

    Assert.Equal(1, calls);
    Assert.Equal(rebuildsAfterFirstDispatch + 1, root.InputCache.RebuildCount);
}
```

- [ ] **Step 5: Run handler invalidation tests**

Run:

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~ElementInputCacheInvalidationTests|FullyQualifiedName~ElementHandlerStoreTests|FullyQualifiedName~ElementInputRouteBuilderTests"
```

Expected: all filtered tests pass.

- [ ] **Step 6: Commit handler route invalidation**

```powershell
git add UI\Elements\UIElement.cs UI\Elements\ElementHandlerStore.cs tests\Cerneala.Tests\UI\Input\ElementInputCacheInvalidationTests.cs tests\Cerneala.Tests\UI\Elements\ElementHandlerStoreTests.cs
git commit -m "fix: invalidate input cache for handler changes"
```

---

### Task 5: Remove Control Dependencies From `UI/Input`

**Files:**
- Create: `UI/Input/IInputPressable.cs`
- Create: `UI/Input/IInputCommandSource.cs`
- Create: `UI/Input/IPointerDragSource.cs`
- Modify: `UI/Input/PressedStateTracker.cs`
- Modify: `UI/Input/ElementInputBridge.cs`
- Modify: `UI/Controls/Primitives/ButtonBase.cs`
- Modify: `UI/Controls/Primitives/Thumb.cs`
- Create: `tests/Cerneala.Tests/UI/Input/InputControlBoundaryTests.cs`

- [ ] **Step 1: Add boundary RED test**

Create `tests/Cerneala.Tests/UI/Input/InputControlBoundaryTests.cs`:

```csharp
namespace Cerneala.Tests.UI.Input;

public sealed class InputControlBoundaryTests
{
    [Fact]
    public void UiInputDoesNotReferenceControls()
    {
        string inputRoot = FindRepositoryPath("UI", "Input");
        string monoGameInputRoot = Path.Combine(inputRoot, "MonoGame");
        string[] forbiddenTerms =
        [
            "Cerneala.UI.Controls",
            "ButtonBase",
            "Thumb"
        ];

        foreach (string file in Directory.EnumerateFiles(inputRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (file.StartsWith(monoGameInputRoot, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string text = File.ReadAllText(file);
            foreach (string forbiddenTerm in forbiddenTerms)
            {
                Assert.DoesNotContain(forbiddenTerm, text, StringComparison.Ordinal);
            }
        }
    }

    private static string FindRepositoryPath(params string[] segments)
    {
        string repositoryRoot = FindRepositoryRoot();
        string candidate = Path.Combine(new[] { repositoryRoot }.Concat(segments).ToArray());

        if (Directory.Exists(candidate) || File.Exists(candidate))
        {
            return candidate;
        }

        throw new DirectoryNotFoundException($"Could not find repository path: {Path.Combine(segments)}");
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Cerneala.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }
}
```

- [ ] **Step 2: Run boundary RED test**

Run:

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~InputControlBoundaryTests"
```

Expected: fails because `ElementInputBridge.cs` and `PressedStateTracker.cs` reference `ButtonBase`/`Thumb`.

- [ ] **Step 3: Add input-level interfaces**

Create `UI/Input/IInputPressable.cs`:

```csharp
namespace Cerneala.UI.Input;

public interface IInputPressable
{
    bool IsPressed { get; set; }
}
```

Create `UI/Input/IInputCommandSource.cs`:

```csharp
namespace Cerneala.UI.Input;

public interface IInputCommandSource
{
    bool ExecuteCommand(CommandRouter router, ElementInputRouteMap routeMap);
}
```

Create `UI/Input/IPointerDragSource.cs`:

```csharp
namespace Cerneala.UI.Input;

public interface IPointerDragSource
{
    bool BeginPointerDrag(PointerCaptureManager captureManager, ElementInputRouteMap routeMap, MouseButtonEventArgs args);

    bool UpdatePointerDrag(MouseEventArgs args);

    bool CompletePointerDrag(PointerCaptureManager captureManager, ElementInputRouteMap routeMap, MouseButtonEventArgs args);
}
```

- [ ] **Step 4: Implement interfaces in controls**

In `UI/Controls/Primitives/ButtonBase.cs`, change the class declaration:

```csharp
public class ButtonBase : Control, IInputPressable, IInputCommandSource
```

The existing `IsPressed` property and `ExecuteCommand(...)` method satisfy the interfaces.

In `UI/Controls/Primitives/Thumb.cs`, change the class declaration:

```csharp
public class Thumb : Control, IPointerDragSource
```

Add explicit forwarding methods:

```csharp
public bool BeginPointerDrag(PointerCaptureManager captureManager, ElementInputRouteMap routeMap, MouseButtonEventArgs args)
{
    return BeginDrag(captureManager, routeMap, args);
}

public bool UpdatePointerDrag(MouseEventArgs args)
{
    return UpdateDrag(args);
}

public bool CompletePointerDrag(PointerCaptureManager captureManager, ElementInputRouteMap routeMap, MouseButtonEventArgs args)
{
    return CompleteDrag(captureManager, routeMap, args);
}
```

- [ ] **Step 5: Update `PressedStateTracker`**

Replace `UI/Input/PressedStateTracker.cs` with:

```csharp
using Cerneala.UI.Elements;

namespace Cerneala.UI.Input;

public sealed class PressedStateTracker
{
    public IInputPressable? PressedElement { get; private set; }

    public void Press(UIElement? target)
    {
        if (target is not IInputPressable pressable)
        {
            Cancel();
            return;
        }

        if (ReferenceEquals(PressedElement, pressable))
        {
            return;
        }

        Cancel();
        PressedElement = pressable;
        pressable.IsPressed = true;
    }

    public void Release()
    {
        Cancel();
    }

    public void Cancel()
    {
        if (PressedElement is null)
        {
            return;
        }

        PressedElement.IsPressed = false;
        PressedElement = null;
    }
}
```

- [ ] **Step 6: Update `ElementInputBridge` command and drag lookup**

In `UI/Input/ElementInputBridge.cs`, remove:

```csharp
using Cerneala.UI.Controls.Primitives;
```

Replace command execution helpers with:

```csharp
private void ExecuteButtonCommandOnClick(
    ElementInputRouteMap routeMap,
    HitTestResult? routedTarget,
    HitTestResult? clickTarget,
    InputMouseButton button,
    int clickCount)
{
    if (button != InputMouseButton.Left ||
        clickCount <= 0 ||
        routedTarget is null ||
        clickTarget is null)
    {
        return;
    }

    UIElement? commandElement = FindAncestor<IInputCommandSource>(clickTarget.Element);
    if (commandElement is not IInputCommandSource commandSource ||
        (!ReferenceEquals(routedTarget.Element, clickTarget.Element) &&
        !ReferenceEquals(routedTarget.Element, commandElement)))
    {
        return;
    }

    commandSource.ExecuteCommand(commandRouter, routeMap);
}
```

Replace thumb helpers with interface-based helpers:

```csharp
private void BeginThumbDrag(ElementInputRouteMap routeMap, HitTestResult? target, InputMouseButton button)
{
    if (button != InputMouseButton.Left || target is null)
    {
        return;
    }

    if (FindAncestor<IPointerDragSource>(target.Element) is not IPointerDragSource dragSource)
    {
        return;
    }

    int x = (int)MathF.Round(target.X);
    int y = (int)MathF.Round(target.Y);
    dragSource.BeginPointerDrag(pointerCaptureManager, routeMap, new MouseButtonEventArgs(InputEvents.MouseDownEvent, target.ElementId, button, x, y, 1));
}

private static void UpdateThumbDrag(HitTestResult? target)
{
    if (target is null)
    {
        return;
    }

    if (FindAncestor<IPointerDragSource>(target.Element) is not IPointerDragSource dragSource)
    {
        return;
    }

    int x = (int)MathF.Round(target.X);
    int y = (int)MathF.Round(target.Y);
    dragSource.UpdatePointerDrag(new MouseEventArgs(InputEvents.MouseMoveEvent, target.ElementId, x, y));
}

private void CompleteThumbDrag(ElementInputRouteMap routeMap, HitTestResult? target, InputMouseButton button, int clickCount)
{
    if (button != InputMouseButton.Left || target is null)
    {
        return;
    }

    if (FindAncestor<IPointerDragSource>(target.Element) is not IPointerDragSource dragSource)
    {
        return;
    }

    int x = (int)MathF.Round(target.X);
    int y = (int)MathF.Round(target.Y);
    dragSource.CompletePointerDrag(pointerCaptureManager, routeMap, new MouseButtonEventArgs(InputEvents.MouseUpEvent, target.ElementId, button, x, y, clickCount));
}
```

Add the shared ancestor helper in the same class:

```csharp
private static UIElement? FindAncestor<TContract>(UIElement element)
{
    for (UIElement? current = element; current is not null; current = current.VisualParent)
    {
        if (current is TContract)
        {
            return current;
        }
    }

    return null;
}
```

- [ ] **Step 7: Run input boundary and behavior tests**

Run:

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~InputControlBoundaryTests|FullyQualifiedName~ElementInputBridgeTests|FullyQualifiedName~Button|FullyQualifiedName~Thumb|FullyQualifiedName~Scroll"
```

Expected: all filtered tests pass; the boundary test finds no `Cerneala.UI.Controls`, `ButtonBase`, or `Thumb` in `UI/Input`.

- [ ] **Step 8: Commit boundary cleanup**

```powershell
git add UI\Input\IInputPressable.cs UI\Input\IInputCommandSource.cs UI\Input\IPointerDragSource.cs UI\Input\PressedStateTracker.cs UI\Input\ElementInputBridge.cs UI\Controls\Primitives\ButtonBase.cs UI\Controls\Primitives\Thumb.cs tests\Cerneala.Tests\UI\Input\InputControlBoundaryTests.cs
git commit -m "fix: decouple input bridge from controls"
```

---

### Task 6: Verify Input Cache Integration

**Files:**
- Modify tests only if old tests intentionally expected per-dispatch route rebuilding.

- [ ] **Step 1: Run input, invalidation, hosting, and control tests**

Run:

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~Input|FullyQualifiedName~HitTest|FullyQualifiedName~Invalidation|FullyQualifiedName~Hosting|FullyQualifiedName~Controls"
```

Expected: all filtered tests pass.

- [ ] **Step 2: Search for stale route-build paths and control coupling**

Run:

```powershell
rg -n "new ElementInputRouteBuilder\\(\\)\\.Build|routeBuilder\\.Build|Cerneala\\.UI\\.Controls|ButtonBase|Thumb|PressedElement" UI\Input tests\Cerneala.Tests
```

Expected:

- `ElementInputRouteBuilderTests` may still call `new ElementInputRouteBuilder().Build(...)`.
- Production input bridges should not call `routeBuilder.Build(...)`.
- `UI/Input` should not reference `Cerneala.UI.Controls`, `ButtonBase`, or `Thumb`.
- `ButtonBase`/`Thumb` references may remain in control files and control tests.
- `PressedElement` may remain as `IInputPressable?`, not `ButtonBase?`.

- [ ] **Step 3: Run route/hit-test cache smoke through `UiHost`**

Run:

```powershell
dotnet test tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "FullyQualifiedName~HostUpdateDispatchesInputBeforeScheduler|FullyQualifiedName~UiHostLateTreeMutationTests|FullyQualifiedName~ElementInputCacheInvalidationTests|FullyQualifiedName~HitTestCacheInvalidationTests"
```

Expected: all filtered tests pass. This proves host input still dispatches before the main scheduler pass while using the retained input cache.

- [ ] **Step 4: Commit any focused test migrations**

If Step 1 or Step 2 reveals tests that intentionally expected route rebuilds every dispatch, update those tests to retained-cache expectations and commit:

```powershell
git add tests\Cerneala.Tests
git commit -m "test: update input cache expectations"
```

If no files changed, do not create an empty commit.

---

### Task 7: Update Audit Documentation

**Files:**
- Modify: `AUDIT_FIX_PLAN.md`
- Modify: `ROADMAPv2_AUDIT.md`
- Modify: `docs/superpowers/plans/2026-07-04-cache-input-route-hit-test.md`

- [ ] **Step 1: Update `AUDIT_FIX_PLAN.md` Plan 4 checklist**

Under `### Plan 4: cache-input-route-hit-test`, add this link if it is not already present:

```markdown
Detailed plan: `docs/superpowers/plans/2026-07-04-cache-input-route-hit-test.md`
```

After implementation and focused verification pass, change these Plan 4 items from `[ ]` to `[x]`:

```markdown
- [x] Add root-owned retained input route / hit-test cache.
- [x] Rebuild route/hit-test data only when dirty.
- [x] Invalidate cache for tree changes, layout bounds changes, visibility, enabled state, handlers, and relevant capture changes.
- [x] Make `ElementInputBridge.Dispatch(...)` consume retained cache instead of rebuilding route maps every frame.
- [x] Move button command execution out of `ElementInputBridge`.
- [x] Move thumb drag behavior behind handlers or an input-level interface.
- [x] Add `tests/Cerneala.Tests/UI/Input/ElementInputCacheInvalidationTests.cs`.
- [x] Add `tests/Cerneala.Tests/UI/Input/HitTestCacheInvalidationTests.cs`.
- [x] Add `tests/Cerneala.Tests/UI/Input/InputControlBoundaryTests.cs`.
```

- [ ] **Step 2: Add completion note to `ROADMAPv2_AUDIT.md`**

Only after `dotnet test Cerneala.slnx` passes, add this note under `## Must Fix` > `### 5. Input route/hit-test caching is not retained`, after the required changes list:

```markdown
Implementation note: fixed by `cache-input-route-hit-test`; `UIRoot` now owns a retained `ElementInputCache`, route/hit-test data rebuilds only when hit-test/input-route invalidation marks it dirty, mouse/touch/stylus dispatch consume the retained route map, handler changes invalidate the cache, and `UI/Input` no longer depends directly on concrete controls.
```

- [ ] **Step 3: Run markdown reference check**

Run:

```powershell
rg -n "cache-input-route-hit-test|ElementInputCache|HitTestCacheInvalidationTests|InputControlBoundaryTests" AUDIT_FIX_PLAN.md ROADMAPv2_AUDIT.md docs\superpowers\plans\2026-07-04-cache-input-route-hit-test.md
```

Expected: all three docs reference the completed plan and the new retained input cache pieces.

- [ ] **Step 4: Commit docs**

```powershell
git add AUDIT_FIX_PLAN.md ROADMAPv2_AUDIT.md docs\superpowers\plans\2026-07-04-cache-input-route-hit-test.md
git commit -m "docs: record retained input cache completion"
```

---

### Task 8: Full Verification

**Files:**
- No production edits unless verification reveals a missed migration.

- [ ] **Step 1: Run full test suite**

Run:

```powershell
dotnet test Cerneala.slnx
```

Expected: all tests pass.

- [ ] **Step 2: Verify OpenSpec references did not return**

Run:

```powershell
rg -n "OpenSpec|openspec|opsx" ROADMAPv2.md ROADMAPv2_AUDIT.md tests UI
```

Expected: no matches.

- [ ] **Step 3: Verify input boundary directly**

Run:

```powershell
rg -n "Cerneala\.UI\.Controls|ButtonBase|Thumb" UI\Input
```

Expected: no matches in `UI\Input` outside comments if none are introduced. Prefer zero matches.

- [ ] **Step 4: Inspect git status and diff**

Run:

```powershell
git status --short
git diff --stat
```

Expected: no uncommitted files after final commits.

- [ ] **Step 5: Final commit if verification finds a missed fix**

If Step 1, Step 2, or Step 3 required code/test fixes:

```powershell
git add <fixed-paths>
git commit -m "fix: complete retained input cache"
```

If no files changed, do not create an empty commit.

---

## Self-Review

### Spec Coverage

- Add root-owned retained input route / hit-test cache: Task 2 creates `ElementInputCache` and adds `UIRoot.InputCache`.
- Rebuild route/hit-test data only when dirty: Tasks 1-3 test and implement `RebuildCount`, `IsDirty`, `EnsureCurrent(...)`, and hit-test phase rebuild.
- Invalidate cache for tree changes, layout bounds, visibility, enabled state, handlers, and capture-related changes: tree/layout/visibility/enabled already raise `HitTest`; Task 4 adds handler invalidation; Task 1 includes capture stale-route coverage.
- Make `ElementInputBridge.Dispatch(...)` consume retained cache: Task 3.
- Move button command execution out of `ElementInputBridge`: Task 5 replaces concrete `ButtonBase` lookup with `IInputCommandSource`.
- Move thumb drag behavior behind handlers or input-level interface: Task 5 adds `IPointerDragSource`.
- Add required tests: Tasks 1 and 5 create the three audit-required test files.

### Placeholder Scan

This plan contains no placeholder implementation steps and no vague test instructions without concrete test code. Conditional commits are limited to test migration or verification fixes and explicitly say not to create empty commits.

### Type Consistency

- `ElementInputCache.EnsureCurrent(UIRoot)` returns `ElementInputRouteMap`, matching bridge usage.
- `ElementInputCache.HitTest(...)` delegates to the existing `HitTestService.HitTest(UIElement, ElementInputRouteMap, ...)` overload.
- `UIRoot.InputCache` is available before `ElementInputBridge`, `TouchInputBridge`, `StylusInputBridge`, and tests use it.
- `IInputPressable`, `IInputCommandSource`, and `IPointerDragSource` live in `UI/Input`, so controls may implement them without making `UI/Input` depend on `UI/Controls`.
- `PressedStateTracker.PressedElement` changes from `ButtonBase?` to `IInputPressable?`, matching the boundary cleanup.
