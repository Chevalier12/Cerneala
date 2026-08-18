# Prism — cache retained GPU cross-frame

**Status:** completed

## Purpose

Save and reuse between frames captures, intermediate results and
final Prism compositions when each pixel-affecting input is unchanged.
Cache is mandatory for retained, backend-owned, GPU-only architecture and
explicitly limited as memory.

**Dependencies:** `2026-07-18-prism-retained-composition-graph.md`,
`2026-07-18-prism-monogame-compositor.md`,
`2026-07-18-prism-color-blend-and-styles.md`,
`2026-07-18-prism-filter-catalog.md` and
`2026-07-18-prism-backdrop-hosting.md`.

## Stage 0 — RED contracts and baseline cache-off

- [x] Add RED tests in `tests/Cerneala.Tests/Drawing/MonoGame/Prism/Cache/`
  which compares each cache-on result with a fresh cache-off execution.
- [x] Tests fix the contract: the second identical static frame produces a hit
  final, skips capture and effect passes covered, but draws cached output.
- [x] Add RED miss matrix for content, structure, parameters, Motion,
  resources, lower UI, backdrop `ContentVersion`, bounds, pixel scale, profile,
  format, capability set and shader package changed.
- [x] Add RED tests for intermediate hit, two controls with the same
  definition, hash collision, budget exceeded, forgotten pin entry, exception, detach,
  Hidden/Collapsed, replacement and device reset.
- [x] Record baseline without cache: captures, passes, CPU submit, GPU
  time, allocations and peak transient surfaces for standard scenes.

### Gate stage 0

- [x] The tests fail exclusively because the versioning/retained cache is missing, and
  the cache-off oracle is deterministic and verified by existing goldens.

## Stage 1 — incremental retained versioning

- [x] Add the smallest aggregated visual version to the retained layer
  holds render invalidation; do not compute the key traversing the subtree per frame.
- [x] Increment the version on any render-affecting change: property,
  Motion, local command, child, image/text/resource content or Presence.
- [x] Propagate generation minimum to parent Prism scope without invalidation of
  measure/arrange or hit testing.
- [x] Keep `PrismStructuralVersion` and `PrismValueVersion` separately; o
  writing the same value is no-op and produces no miss.
- [x] Request monotone versions for images, masks, LUTs, patterns and resources
  auxiliary; a resource without a stable version makes the node uncacheable.
- [x] For backdrop, compose `ContentVersion` with UI node versions
  lower in paint order, without references to the owner.

### Gate stage 1

- [x] Each pixel-affecting mutation tested changes exactly the required stamp,
  layout/input mutations with no visual effect don't change it unnecessarily, and analysis
  only one pass remains.
## Stage 2 — full keys and cacheability

- [x] Add backend-neutral `PrismDependencyStamp` and
  `PrismRetainedCacheKey`, consisting only of identifiers/versions/values
  immutable fingerprints.
- [x] Includes in key: verifiable structural hash, stable node id, versions
  Prism, owner/source/resource identities plus versions, lower UI, raster bounds,
  pixel scale, transform relevant, color profiles, format, sampling, capability
  set and shader package version.
- [x] Make `PrismCacheOwnerToken` unique and unused for the duration of the backend;
  the digital version of the content is never compared without identity.
- [x] Don't accept a hit just because the hash matches; verify identity
  structural and the entire dependency stamp.
- [x] Generates the determinism and dependencies of each operation from the catalog;
  default time, unseeded randomness, or unversioned resource disallow cache.
- [x] Enable cache for capture, expensive intermediate nodes and final result;
  the optimizer decides eligibility, the executor doesn't guess.
- [x] Add unit tests for equality, deterministic fingerprint, collisions,
  almost identical keys and lack of UI strings/references.

### Gate stage 2

- [x] Catalog matrix reports cacheability for each operation, and
  no known pixel-affecting dependencies are missing from key/stamp.

## Stage 3 — the owner of the retained surfaces

- [x] Add `Drawing/MonoGame/Prism/Surfaces/PrismRetainedSurfaceCache`,
  separated by `PrismSurfacePool`; only the retained cache retains content.
- [x] Defines transient → retained atomic promotion after success and return
  retained → pool/dispose to eviction, without double ownership.
- [x] Keeps pin-forget entries as long as they are used in draw and prohibits
  eviction/dispose before the release of the last lease.
- [x] Implements deterministic LRU under byte budget and maximum-entry budget;
  eviction prefers the oldest non-forgotten entries.
- [x] Use a common accountant: hard cap for all Prism surfaces and
  soft head for retained; evacuate retained before denying memory
  transient required for a correct frame.
- [x] Keep cache on MonoGame backend UI/render thread; don't add
  locks, task graph, async compute or generic abstraction of GPU fences.
- [x] Handle failed allocation via `PrismFallbackPolicy`: render fresh/bypass
  sure, diagnostic and zero partial entry.

### Gate stage 3

- [x] Ownership of each surface is unique and observable, byte accounting
  is exact, and exceptions leave zero orphaned leases.

## Stage 4 — lookup, pruning and promotion

- [x] First check the final result key; on hit, jump commands
interior of the scope and all passes covered, then compose the surface.
- [x] On final miss, search only intermediate nodes marked cacheable and
  prunes the subgraph covered by the hit without changing the Photoshop order.
- [x] On complete miss, execute normal graph and promote results only
  successfully completed and useful according to the optimizer's plan.
- [x] Does not retain the backdrop texture provided by the host; retain only results
  processed owned by Cerneala and validated by `ContentVersion`.
- [x] For control-based nodes, include the owner token and forbid
  sharing cross-owner; allows sharing only for external inputs with the same
  full identity/versioning. The markup name does not participate in the key.
- [x] Add final/intermediate/miss hit differential tests for alpha, masks,
  clipping, groups, blend modes, styles, filters, nested Prism and backdrop.

### Gate stage 4

- [x] Cache-on output is identical to cache-off within the stated tolerance, and
  diagnostics confirm that passes/captures are skipped exactly as far as the hit covers.

## Stage 5 — invalidation and lifecycle

- [x] Invalidates affected entries at composition/resource replacement,
  viewport/pixel-scale/output-profile/shader-package change and device reset/loss.
- [x] On detach/dispose, invalidate owner generation and remove any index
  auxiliary without scanning the entire cache or keeping the owner alive.
- [x] Transports the invalidation by a numeric `PrismCacheOwnerToken` and a
  backend-neutral queue consumed on submit; The UI does not directly release the GPU.
- [x] At `Hidden`/`Collapsed`, do zero lookup and zero promotion; marks
  associated inputs immediately evictable and cancel Motion through the common lifecycle.
- [x] When reattach/unhide, allow hit only if the dependency stamp is complete
  remains valid; otherwise recalculate without old pixels.
- [x] Test 10,000 attach/detach, navigation, hide/unhide, replacement,
  resize and device reset with `WeakReference`, bytes and lease counters.
- [x] Checks that no entry contains `UIElement`, binding, delegates,
  Motion handle or backdrop source lease.

### Gate stage 5

- [x] After drain/GC/reset, cache respects budget and keeps no items,
  instances, expired resources or inaccessible surfaces.

## Stage 6 — diagnostics, budgets and performance

- [x] Expose counters for final/intermediate hit, miss reason, promotion,
  eviction reason, bytes/entries, pinned entries, saved captures and saved passes.
- [x] Include dependency-stamp diff in development diagnostics without serializing
  GPU resources or allocate per frame when diagnostics are turned off.
- [x] Benchmark static control, static backdrop, game backdrop animated,
  Motion parameter, changed resource, many common instances and small budget.
- [x] Set default values of byte/entry budgets only after measurements
  on the reference hardware and retain configurable options.
- [x] Confirm `0 B` managed after warmup on static hit and prove win
  net versus lookup/fingerprint cost.
- [x] Add an internal cache-off mode for conformance and diagnostics, not ca
  dialect markup or property per layer.

### Gate stage 6

- [x] Cache has metered gain in stable scenes, bounded cost in scenes
  dynamics and no invented numerical threshold without a baseline.

## Stage 7 — API docs and verification

- [x] Upgrade with the `writing-api-documentation` skill
  `PrismRendererOptions` and any budget/diagnostics public API; synchronize
  `docs-site/documentation/manifest.json`.
- [x] Update the TDD/proposal only with details confirmed by
  implementation and benchmark without changing the grammar.
- [x] Run mandatory reindexing after every C# batch/project.
- [x] Running
  `dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj --filter "PrismRetainedCache|PrismDependencyStamp|PrismBackdrop"`.
- [x] Run all Prism goldens in cache-on and cache-off mode, then
  `dotnet test .\Cerneala.slnx`.
- [x] Run cache stress/benchmarks and `git diff --check`.

## The definition of done

- [x] A static Prism effectively reuses GPU output between frames and
  it jumps covered work, not just the CPU structure.
- [x] Any pixel-affecting changes invalidate properly; the cache-on output is
  identical to cache-off, no old pixels or sharing between incompatible controls.
- [x] The cache is GPU-only, budgeted, leak-free, lifecycle/device safe
  reset and fully observable through diagnostics.