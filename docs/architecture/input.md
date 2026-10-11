# Input

> Code: `UI/Input`, `UI/Elements/UIElement.Events.cs`, `Cerneala.Platforms.Sdl3/Input/SdlInputSource.cs` · Verified at commit `0cb32300` (2026-10-10)

## Responsibility

The input system turns one platform `InputFrame` per update into hit tests,
routed events, focus changes, pointer capture and command execution on the
retained element tree. It routes over a derived tree (`UiInputTree`) that is
rebuilt only when the element tree or input-relevant state changes. It does not
read platform events itself and does not change layout or rendering directly;
handlers do that through normal property writes and invalidation.

## Components

| Type | File | Role | Owner |
|---|---|---|---|
| `IInputSource` | `UI/Input/IInputSource.cs` | `GetFrame()` returns the next `InputFrame`. Production implementer: `SdlInputSource` (`Cerneala.Platforms.Sdl3/Input/SdlInputSource.cs`, internal). | Platform window |
| `InputFrame` | `UI/Input/InputFrame.cs` | Immutable snapshot of pointer, keyboard and text input for one update. | One update |
| `ElementInputBridge` | `UI/Input/ElementInputBridge.cs` | Dispatches one `InputFrame` to the tree: pointer, keyboard, bindings, navigation, activation, text. | `UiHost` (`UiHost.InputBridge`) |
| `ElementInputCache` | `UI/Input/ElementInputCache.cs` | Holds the current `ElementInputRouteMap`; rebuilds it when needed; counts rebuilds (`RebuildCount`). | `UIRoot` (`UIRoot.InputCache`) |
| `ElementInputRouteBuilder` | `UI/Input/ElementInputRouteBuilder.cs` | Walks the visual tree and builds a route map. | `ElementInputCache` |
| `ElementInputRouteMap` | `UI/Input/ElementInputRouteMap.cs` | Element ↔ `UiElementId` lookup plus its `UiInputTree`. | `ElementInputCache` |
| `UiInputTree` | `UI/Input/UiInputTree.cs` | Parent links and handler registrations by `UiElementId`. `GetRouteToRoot` gives a route. | `ElementInputRouteMap` |
| `HitTestService` | `UI/Input/HitTestService.cs` | Finds the element under a point. | `ElementInputCache` |
| `RoutedEventRouter` | `UI/Input/RoutedEventRouter.cs` | Raises a routed event along a route (`Direct`, `Bubble`, `Tunnel`). | Static |
| `FocusManager` | `UI/Input/FocusManager.cs` | Keyboard focus and keyboard event dispatch to the focused element. | `ElementInputBridge`; the active one is set on `UIRoot.ActiveFocusManager` during dispatch |
| `PointerCaptureManager` | `UI/Input/PointerCaptureManager.cs` | Redirects pointer input to a captured element. | `ElementInputBridge` |
| `CommandRouter` | `UI/Input/CommandRouter.cs` | Routes `CanExecute` / `Execute` through command bindings. | Caller; for the `CommandState` phase, `UIRoot.CreatePhaseProcessors` creates one per frame pass (`UIRoot.cs:486`) |
| `ICommandStateSource` | `UI/Input/ICommandStateSource.cs` | Element whose state depends on a command (for example `ButtonBase`). Refreshed in the `CommandState` phase. | — |
| `KeyboardNavigationController`, `KeyboardActivationController`, `RetainedInputBindingProcessor`, `TextInputBridge` | `UI/Input/<Name>.cs` | Tab navigation, keyboard activation, key gestures, text input. | `ElementInputBridge` |

## Data Flow

1. **Platform to frame.** SDL events reach `SdlPlatformWindow`, which feeds the
   window's `SdlInputSource` (pointer move/leave, buttons, wheel, keys, text).
   `WindowApplicationRuntime.CollectPlatformInput`
   (`UI/Hosting/Windowing/WindowApplicationRuntime.cs:910-913`) calls
   `InputSource.GetFrame()`, or the preview input driver in preview mode.
   `GetFrame` consumes the accumulated state (`SdlInputSource.cs:16-24`).
2. **Host ordering.** `UiHost.UpdateCore` runs the scheduled frame pass first,
   then `ElementInputBridge.Dispatch` (`UiHost.cs:174-186`). Hit testing
   therefore sees layout committed by queued work of the same update.
3. **Dispatch** (`ElementInputBridge.cs:64-84`), in order:
   1. sets `UIRoot.ActiveFocusManager`;
   2. `UIRoot.InputCache.EnsureCurrent(root)`;
   3. hit test, then `PointerCaptureManager.OverrideTarget`;
   4. pointer: hover, wheel, press (left press focuses the target), release,
      click, drag, repeat buttons;
   5. keyboard events to the focused element (`FocusManager`);
   6. input bindings;
   7. keyboard navigation (Tab);
   8. keyboard activation;
   9. text input.
4. **Routing.** `RoutedEventRouter.Raise` (`RoutedEventRouter.cs:15-40`) gets the
   route from `UiInputTree.GetRouteToRoot(target)`: `Direct` uses only the
   target, `Bubble` goes target → root, `Tunnel` goes root → target. A handler
   runs when the event is not handled yet, or when it was registered with
   `handledEventsToo`. `RaisePair` raises the preview (tunnel) event first and
   copies its `Handled` to the bubble event.
5. **Commands.** `CommandRouter.CanExecute` and `Execute` raise the binding
   handlers root → target first and then target → root, and stop when a handler
   marks the arguments handled (`CommandRouter.cs:7-47`, `:65-92`).

Example. A `Button` is arranged at (0, 0, 40, 40) in a root. The input frame has
the left button pressed at (10, 10).

- `Dispatch` makes sure the route map is current. The hit test returns the
  button.
- The left press focuses the button: `bridge.FocusManager.FocusedElement` is the
  button, and `button.IsKeyboardFocused` is `true`.
- The preview and bubble mouse-down events run along the button's route.

## The Derived Route Tree

Routes do not walk `UIElement` parents at event time. They use the
`UiInputTree` inside the current `ElementInputRouteMap`.

**How it is built** (`ElementInputRouteBuilder.cs:6-75`):

- walks `VisualChildren`, plus the extra children of an internal
  `IInputSubtreeHost` (`SceneNode2D`, `RenderSurface2D`);
- an element that does not take part in input (`IsVisible` is `false`,
  `Visibility` is not `Visible`, or a presence exit is playing) is skipped with
  its whole subtree;
- a disabled element is not added and its handlers are not exported, but its
  children are walked and attach to the nearest added ancestor;
- each added element gets a `UiElementId`, its parent link and its handlers.

`BuildForCommandState` builds a separate map that includes disabled elements.
The `CommandState` phase builds it once per frame pass, on first use, and does
not store it in the cache (`UIRoot.cs:505-509`).

**When it is rebuilt.** `ElementInputCache.EnsureCurrent`
(`ElementInputCache.cs:35-46`) rebuilds when:

- the cache was marked dirty with `Invalidate(reason)`;
- the root is a different one; or
- `UIRoot.TreeVersion` changed (children inserted, moved or removed, viewport
  changed, presence exit finished).

`UIRoot.Invalidate` marks the cache dirty only for a `HitTest` invalidation from
`IsEnabled`, `IsVisible` or `Visibility`, or with the reason
`"Presence state changed"`, `"Input handler added"` or
`"Input handler removed"` (`UIRoot.cs:371-386`). Other hit-test invalidations,
for example a bounds change, keep the route map; hit testing reads current
bounds anyway. `RenderSurface2D` also invalidates the cache directly when its
scene presentation readiness changes.

## Hit Testing

`HitTestService.HitTestElement` (`HitTestService.cs:27-110`):

- skips an element, with its subtree, when a presence exit is playing, when it
  does not take part in hit testing (`IsHitTestVisible`, visibility), or when it
  is disabled;
- applies the element transform (skips it when the transform cannot be
  inverted) and its clip;
- visits children last to first, so the child drawn on top wins;
- an element outside its own bounds is not hit itself, but its children are
  still visited, unless a clip excludes the point;
- a `Panel` is hit directly only when it has input handlers, so an empty layout
  panel does not hide the sibling under it;
- the result must have an id in the route map.

## Lifecycle And Ownership

- One `ElementInputCache` per `UIRoot`, created in the root constructor.
- One `ElementInputBridge` per `UiHost`; it owns its `FocusManager`,
  `PointerCaptureManager` and the keyboard and text controllers.
- When a captured element is removed, the next route map rebuild releases the
  capture (`OverrideTarget` releases a capture whose element is not routable).

## Frame Integration

- Input dispatch is not a scheduler phase. It runs in `UiHost.UpdateCore`
  between the scheduled pass and the input pass. The scheduler never processes
  `FramePhase.Input`.
- The `HitTest` phase calls `ElementInputCache.EnsureCurrent`
  (`UIRoot.cs:501`), so a frame with hit-test work leaves a current route map.
- The `CommandState` phase calls `ICommandStateSource.RefreshCommandState` with
  `CommandRouter` and the command-state route map (`UIRoot.cs:491-495`,
  `:507-519`).
- `UIElement.RaiseEvent` (`UIElement.Events.cs:101-126`) uses the current route
  map when the element is attached. A detached element, or an event with the
  `Direct` strategy, runs only the element's own handlers.

## Invariants

Test paths are under `tests/Cerneala.Tests/`.

- An unchanged tree reuses the route map; a child insert or removal rebuilds it
  once. Test:
  `UI/Input/ElementInputCacheInvalidationTests.cs:InputDispatchReusesRouteMapWhenNothingChanged`,
  `UI/Input/HitTestCacheInvalidationTests.cs:VisualTreeMutationInvalidatesInputCache`,
  `UI/Input/HitTestCacheInvalidationTests.cs:RemovedCapturedElementIsReleasedWhenRouteMapRebuilds`.
- `Visibility`, a handler added and a handler removed rebuild the route map
  once. Test:
  `UI/Input/ElementInputCacheInvalidationTests.cs:HitTestInvalidationRebuildsRouteMapOnce`,
  `HandlerAddedAfterCacheBuildInvalidatesRouteMap`,
  `HandlerRemovedAfterCacheBuildInvalidatesRouteMap`. `IsEnabled`:
  `Input/DragDropControllerTests.cs:DragMoveUsesRetainedInputCacheWhenDirty`.
  A rebuild count for `IsVisible`: netestat.
- Hit testing skips invisible, collapsed and disabled elements. Test:
  `Input/HitTestServiceTests.cs:InvisibleCollapsedAndDisabledElementsAreSkipped`.
- The topmost child wins, and an empty layout panel does not hide a sibling.
  Test: `Input/HitTestServiceTests.cs:TopmostVisualChildWins`,
  `LayoutPanelWithoutInputHandlersDoesNotOccludeInteractiveSibling`.
- Tunnel events run root → target; a handled event stops ordinary handlers;
  `handledEventsToo` handlers still run. Test:
  `Input/RoutedEventRouterTests.cs:TunnelEventsInvokeRootThenTarget`,
  `Input/RoutedEventRouterTests.cs:HandledStopsRoute`,
  `Controls/WpfEventSurfaceTests.cs:HandledEventsTooRunsWithoutResumingOrdinaryHandlers`.
- Capture overrides the hit target. Test:
  `Input/PointerCaptureManagerTests.cs:CapturedElementOverridesHitTarget`.
- A left press focuses the button; Tab moves focus in visual order. Test:
  `Input/ElementInputBridgeTests.cs:PointerPressOnButtonFocusesButton`,
  `Input/KeyboardNavigationContractTests.cs:TabMovesToNextTabStopInVisualOrder`.
- Command routing goes root → target first and stops when handled. Test:
  `Input/CommandRouterTests.cs:CanExecuteUsesRetainedRouteOrder`,
  `Input/CommandRouterTests.cs:HandledPreviewExecuteSuppressesBubble`.
- On the first update, the press hits an element whose layout the same update
  committed. Test:
  `UI/Hosting/UiHostViewportFrameContractTests.cs:InitialFrameCommitsLayoutBeforePointerHitTestingWhenNeeded`.
- `UIElement.RaiseEvent` on an attached element follows the route map: netestat
  as a direct assertion (covered indirectly by
  `Controls/WpfEventSurfaceTests.cs:ControlEventBubblesThroughRetainedTree`).

## Diagnostic

`Detective.Capture` fills `DetectiveSnapshot.Input` from the root's input
cache. `Detective.CaptureInput(hitTarget, routedEvent)` describes one hit target
and routed event. `Detective.TraceRoutedEvent(target, routedEvent)` describes
the route of one routed event from a target. `ElementInputCache.RebuildCount`
and `LastInvalidationReason` are public.

## Known Limitations

- `StylusInputBridge`, `TouchInputBridge`, `GestureRecognizer`,
  `ManipulationProcessor` and `DragDropController` have no production caller;
  only tests construct them. The SDL input source produces pointer, keyboard,
  wheel and text input only.
- A disabled element's enabled descendants stay in the route map, but hit
  testing never reaches them because it stops at the disabled element.
