# Retained Rendering

> Code: `UI/Rendering`, render members of `UI/Elements/UIElement.cs`, `UI/Hosting/UiHost.cs` (commit and draw) · Verified at commit `0cb32300` (2026-10-10)

## Responsibility

Retained rendering keeps, for every element, the drawing commands that its
`Render` method recorded (the local command list), and builds one root command
list from those local lists during the update. An element's `Render` runs again
only when its render state changed. Drawing a frame only hands the committed
root list to the backend: it never calls `Render`, runs layout or changes the
tree. Recording and the backend contract belong to [Drawing](drawing.md).

## Components

| Type | File | Role | Owner |
|---|---|---|---|
| `RetainedRenderer` | `UI/Rendering/RetainedRenderer.cs` | `Commit` builds the root list when it is invalid; `Render` returns the committed list or throws; `Submit` checks the frame context and calls the backend. | `UIRoot` (`UIRoot.RetainedRenderer`, `UIRoot.cs:97`) |
| `RetainedRenderCache` | `UI/Rendering/RetainedRenderCache.cs` | One `ElementRenderCache` per element (strong references), the root list `RootCommands`, `IsRootValid`, `Version` (incremented by `MarkRootBuilt`), and the image leases of the root list. | `UIRoot` (`UIRoot.RetainedRenderCache`, `UIRoot.cs:95`) |
| `ElementRenderCache` | `UI/Rendering/ElementRenderCache.cs` | One element's local `Commands` and what they were built from: `RenderVersion`, `Dependencies`, `ContentBounds`; plus its image leases. | `RetainedRenderCache` |
| `RenderQueueProcessor` | `UI/Rendering/RenderQueueProcessor.cs` | Processor of the `RenderCache` phase. | `UIRoot` (`UIRoot.cs:96`) |
| `DrawCommandListBuilder` | `UI/Rendering/DrawCommandListBuilder.cs` | Walks the visual tree and writes the root list. | `RetainedRenderer` |
| `RenderContext` | `UI/Rendering/RenderContext.cs` | What `Render` receives: `Element`, `DrawingContext`, `Bounds`, `Layer`, `Counters`. | One `Render` call |
| `RenderDependency` | `UI/Rendering/RenderDependency.cs` | Value with text, image, resource and custom versions; `UIElement.RenderDependencies`. A different value makes the local list stale. | One element |
| `RenderCounters` | `UI/Rendering/RenderCounters.cs` | `CacheHits`, `CacheMisses`, `LocalRebuilds`, `ComposedElements`, `EmittedCommands`. | `UIRoot` (`UIRoot.cs:94`) |
| `DrawCommandListPool` | `UI/Rendering/DrawCommandListPool.cs` | Pool of lists with `Rent` and `Return`. No production code uses it. | — |

## Data Flow

1. **A render change is recorded.** A property with `AffectsRender` or
   `AffectsInputVisual` changes (`UIElement.cs:1250-1262`):
   - for a scope property (`RenderTransform`, `RenderTransformOrigin`,
     `Opacity`, `TranslateX`, `TranslateY`, `Scale`, `ScaleX`, `ScaleY`,
     `Rotation`, `SkewX`, `SkewY`, `ClipToBounds`; `UIElement.cs:1466-1480`)
     the element increments `RenderScopeVersion` and notes a scope-only change;
   - for any other property it increments `RenderVersion` and notes a content
     change.

   The `Render` flag then makes `UIRoot.Invalidate` mark the root list invalid
   and puts the element in the `RenderQueue`. Other sources of render work:
   - a new arranged rectangle increments `RenderVersion`, marks the root list
     invalid and enqueues the element without the `Render` flag
     (`UIElement.SetArrangedBounds`, `UIElement.cs:990-1005`);
   - `SetRenderDependencies` increments `RenderVersion` and invalidates
     `Render` (`UIElement.cs:1076-1086`);
   - a new layout correction transform, presence visual or Prism attachment is
     a scope-only change (`UIElement.cs:1040-1042`, `:1071-1073`, `:677-682`).
2. **`RenderCache` phase.** `RenderQueueProcessor.Process`
   (`RenderQueueProcessor.cs:17-36`):
   - after a scope-only change it marks the root list invalid and stops when
     the local list is not stale. `Render` does not run;
   - otherwise it calls `ElementRenderCache.Ensure`, forced when the element
     still has the `Render` flag. When `Ensure` rebuilt the list, the root list
     is marked invalid.
3. **Local rebuild.** `Ensure` (`ElementRenderCache.cs:75-117`) returns early
   when not forced and not stale (`CacheHits` + 1). Otherwise it clears the
   list, calls `element.Render` with a new `DrawingContext` over the list (only
   when the element is visible), retains the images the commands use, and
   records `RenderVersion`, `Dependencies` and `ContentBounds`.
4. **Commit.** At the end of every `UiHost` update,
   `RetainedRenderer.Commit` (`UiHost.cs:220`) calls
   `DrawCommandListBuilder.Build` only when the root list is invalid.
5. **Root build.** `Build` (`DrawCommandListBuilder.cs:17-43`) clears the root
   list and walks the whole visual tree in preorder. An element that does not
   take part in rendering, or whose opacity multiplied by its ancestors'
   opacity is 0 or less, is skipped with its subtree. For each other element
   it writes, in this order (`:45-190`):
   - `PushTransform` when the element transform is not the identity, and
     `PushClip` when the element clips;
   - `BeginPrism` when a Prism instance is attached;
   - its local commands, moved to its position and multiplied by the combined
     opacity of the element and its ancestors;
   - its visual children, then its children that are still playing an exit
     animation;
   - `EndPrism`, `PopClip`, `PopTransform`, for the scopes it opened.

   Then the root list retains its images and `MarkRootBuilt` sets
   `IsRootValid` and increments `Version`. When the build throws, the root list
   is cleared and its images are released.
6. **Draw.** `UiHost.Draw` calls `RetainedRenderer.Render`, which throws
   `"Root command list is not committed. ..."` when the root list is invalid,
   and then `Submit` (see [Drawing](drawing.md#data-flow)).

Example. A `UIRoot` holds two elements, `first` and `second`. Frame 1 runs
`Render` on both and builds the root list. Then the code runs
`first.Invalidate(InvalidationFlags.Render, "test");`.

- In the next frame the `RenderCache` phase processes only `first`: its
  `Render` runs once more. `second.Render` does not run.
- The root list is invalid, so `Commit` builds it again from `first`'s new
  local list and `second`'s cached local list.
- If `first.Opacity = 0.5f;` runs instead, the change is scope-only: no
  `Render` runs, and only the root list is built again with the new opacity.

## Moved Elements

When an element moves but keeps its size, its old local list can be reused.
During the root build, `GetLocalCommands` (`DrawCommandListBuilder.cs:617-653`)
reuses the cached list with an offset when the list is valid, the element has
no `Render` flag, the dependencies are equal, and the width and height equal
`ContentBounds`. Each command is then moved by the difference between the new
and the cached position. `LayoutManager.Arrange` removes such descendants from
the `RenderQueue` (`UI/Layout/LayoutManager.cs:78-117`). In every other case
`GetValidCommands` returns the list and throws when it is stale.

## Lifecycle And Ownership

- `RetainedRenderer`, `RetainedRenderCache`, `RenderQueueProcessor` and
  `RenderCounters` are created by the `UIRoot` constructor
  (`UIRoot.cs:94-97`) and live as long as the root.
- `RetainedRenderCache` holds element caches by strong reference, so an
  abandoned element cannot take its image acquisitions away before they are
  released (`RetainedRenderCache.cs:9-11`).
- **Detach.** `UIElement.DetachFromRoot` calls
  `RetainedRenderCache.ReleaseElement` (`UIElement.cs:732`), which marks the
  root list invalid and disposes the element's cache and its image leases.
  The committed root list still holds its own leases, so an image stays alive
  until the next commit builds the root list without it. Example: an `Image`
  is removed with `root.VisualChildren.Remove(control);`. Right after the
  removal the image's dispose count is 0; after the next commit it is 1 and
  `RootCommands` is empty.
- **Release.** `UIRoot.ReleaseDrawingResources` (`UIRoot.cs:251-262`) releases
  every element cache, render surface resources and the root list, but keeps
  the shared image cache.
- **Dispose.** `RetainedRenderCache.Dispose` disposes every element cache and
  the root list, continues after a failure, and throws one
  `AggregateException` at the end (`RetainedRenderCache.cs:68-82`).

## Frame Integration

- Phase `RenderCache`, after `Arrange` and before `HitTest`; queue
  `RenderQueue`. The scheduler clears the `Render` flag after the processor
  returns, and only when the element was not enqueued again
  (`UiFrameScheduler.cs:462-489`).
- `RetainedRenderer.Commit` runs outside the scheduler, at the end of each
  `UiHost` update; the time is reported as retained commit time.
- `FrameStats.RenderedElements` counts elements processed by the phase.

## Invariants

Test paths are under `tests/Cerneala.Tests/`.

- An unchanged element reuses its local list: `Render` does not run again.
  Test: `UI/Rendering/ElementRenderCacheTests.cs:UnchangedElementCacheIsReused`,
  `UI/Rendering/ResourceRenderDependencyTests.cs:UnchangedResourceDependencyAllowsCacheReuse`.
- A render change on one element runs only that element's `Render`. Test:
  `UI/Rendering/RetainedRendererTests.cs:ChildRenderChangeDoesNotRebuildUnrelatedSiblingLocalCommands`,
  `UI/Rendering/RenderQueueProcessorTests.cs:QueuedRenderWorkRebuildsElementCacheDuringFrame`.
- An `Opacity` change does not run `Render` again; the next root list carries
  the new opacity. Test:
  `UI/Rendering/RenderLayerMotionTests.cs:RenderOnlyMotionInvalidatesRootWithoutRebuildingLocalCommands`.
  The same for the other scope properties: netestat (tests check only dirty
  and queue state, `UI/Elements/UIElementMotionPropertyTests.cs`). A
  scope-only change on an element whose local list is stale still runs
  `Render`. Test:
  `UI/Rendering/RenderQueueProcessorTests.cs:ScopeOnlyInvalidationDoesNotSkipStaleLayoutCache`.
- A moved element of the same size reuses its local list at the new position.
  Test:
  `UI/Layout/LayoutManagerTests.cs:ScrollOffsetDoesNotRebuildContentLocalRenderCache`
  (scroll by 48: `Render` count unchanged, the rectangle is at Y = -48).
- A child's commands carry its ancestors' opacity multiplied in. Test:
  `Drawing/Prism/PrismRetainedCommandContractTests.cs:BuilderNestsParentAndChildScopesAndPreservesRenderState`
  (parent and child opacity 0.5: alpha 128 and 64). The skip at combined
  opacity 0: netestat.
- A failing `Render` keeps the element dirty and queued. Test:
  `UI/Rendering/RenderQueueProcessorTests.cs:FailedRenderProcessingKeepsDirtyFlagsAndQueuedWork`.
- A removed image is released at the next commit, not at removal. Test:
  `UI/Resources/ImageResourceLeaseTests.cs:DetachingAnImageKeepsCommittedCommandsAliveUntilTheNextCommit`.
  That the element's cache entry is removed: netestat.
  `UIRoot.ReleaseDrawingResources` releasing the render caches: netestat
  (its direct tests check only 3D surface targets).
- A different resource identity or resource version makes the local list
  stale. Test:
  `UI/Rendering/ResourceRenderDependencyTests.cs:ResourceIdentityParticipatesInCacheStaleness`,
  `UI/Rendering/ResourceRenderDependencyTests.cs:DifferentResourceReplacementInvalidatesCacheEvenWhenResourceVersionsMatch`,
  `UI/Rendering/RenderStressBudgetTests.cs:ResourceChangeInvalidatesOnlyRegisteredDependentsWithinBudget`.
- Draw does not run `Render` and does not build the root list. Drawing an
  uncommitted root list throws and does not call the backend. Test:
  `UI/Hosting/UiHostFrameContractTests.cs:DrawSubmitsCachedCommandsWithoutReRendering`,
  `UI/Hosting/UiHostFrameContractTests.cs:DrawDoesNotRegenerateRenderCacheAfterPostUpdateInvalidation`,
  `UI/Rendering/RetainedRendererDrawPurityTests.cs:RenderThrowsWhenRootCommandListIsNotCommitted`,
  `UI/Rendering/RetainedRendererDrawPurityTests.cs:SubmitUsesCommittedCommandListWithoutCopying`.
- The root list is built only when it is invalid. Test:
  `UI/Rendering/RetainedRenderCacheTests.cs:RootCacheVersionChangesWhenRootCommandsAreBuilt`,
  `Drawing/Prism/PrismRetainedCommandContractTests.cs:PrismParameterChangeReusesStructuralAndElementCommands`,
  `Drawing/Prism/PrismRetainedCommandContractTests.cs:ThousandsOfAnimatedParameterCommitsReuseRetainedCommands`
  (a Prism parameter change keeps the list, its `Version` and the cache
  `Version`).
- Parent and child transforms compose in order, and an element transform wraps
  the transforms its `Render` recorded. A clip wraps the element's subtree.
  Test:
  `UI/Rendering/RetainedTransformContractTests.cs:ParentAndChildTransformsComposeWithoutLosingRotationOrSkew`,
  `UI/Rendering/RetainedTransformContractTests.cs:ElementTransformWrapsRatherThanReordersDrawingLocalTransform`,
  `UI/Rendering/DrawCommandListBuilderTests.cs:ClipCommandsWrapVisibleSubtree`.
- Disposing the retained cache releases every image once, also when one
  release throws. Test:
  `UI/Rendering/RenderCacheImageLifetimeTests.cs:RetainedCacheDisposalReleasesLocalAndRootCommands`,
  `UI/Rendering/RenderCacheImageLifetimeTests.cs:RetainedCacheDoesNotLoseItsReleaseResponsibilityWhenAnElementIsAbandoned`,
  `UI/Rendering/RenderCacheImageLifetimeTests.cs:RetainedCacheReleasesEveryImageEvenWhenOneReleaseThrows`.

## Diagnostic

`RenderCounters` on the root counts local cache hits, misses and rebuilds,
composed elements and emitted commands. `FrameStats.RenderedElements` counts
the elements of the `RenderCache` phase, and the root's `InvalidationTrace`
records that phase when tracing is enabled. `UiHost` reports the commit time in
the frame's diagnostics timing (`UiHost.cs:219-237`).

## Known Limitations

- Any invalid root list is rebuilt from scratch: `Build` clears it and walks
  the whole visual tree (`DrawCommandListBuilder.cs:24`, `:137-147`). Local
  caches avoid `Render` calls, not the walk. The cost of the walk is
  unmeasured; no test asserts traversal work.
- `DrawCommandListPool` has no production caller; only
  `tests/Cerneala.Tests/UI/Rendering/DrawCommandListPoolTests.cs` uses it.
