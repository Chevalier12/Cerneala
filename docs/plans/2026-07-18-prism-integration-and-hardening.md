# Prism — integration and hardening

## Purpose

Close the coverage matrix, sample Prism in a real Cerneala view, and
establishes limits, performance and lifecycle resistance through measurements.

**Dependencies:** all other Prism plans from
`2026-07-18-prism-plan-index.md`.

## Stage 0 — completeness audit

- [x] Generate the final report catalog → syntax → binder → runtime → graph →
  kernel → Motion → diagnostics → test → documentation.
- [x] Compare the report with all filters, styles, blend modes, masks, backdrop,
  color profiles and properties approved in the two design documents.
- [x] Remove dead entries, duplicates and "maybe someday" added APIs;
  don't fill gaps with silent fallback.
- [x] Run the API diff against the foundation baseline and justify each
  public symbol through a script from the proposal.

### Gate stage 0

- [x] The report has zero gaps, zero divergent defaults and zero public API
  without current consumer.

## Stage 1 — operational diagnostics

- [x] Complete diagnostics for parse/binding, capabilities, fallback,
  graph build, surface budgets, shader load and backdrop acquisition.
- [x] Exposes an internal diagnostic view with active compositions, passes,
  captures, surfaces, peak, allocations, fallback and Motion active, without
  references that extend the lifetime of elements.
- [x] Dump deterministic graph and redact GPU identifiers
  unstable, so snapshots are useful in CI.
- [x] Add tests that check that disabled diagnostics have overhead
  minimum and zero allocations per frame after warmup.

### Gate stage 1

- [x] For any important failure path there is a precise diagnosis and a
  test and diagnostics do not introduce memory leak or hidden work.

## Stage 2 — dogfooding in Presentation

- [x] Apply Prism via Cerneala markup to a natural element from
  `CernealaPresentation/SolarSystemChapterView.crn`, preferable to the card of
  planet and its background, without custom control `OnRender` and without the change
  orbits/selection logic.
- [x] Defines the reusable composition as `PrismComposition` and exercises
  minimum layer, group, style/filter, mask, Motion on layer also called backdrop.
- [x] Adds a deterministic automation state that fixes the planet,
  animation, viewport and time before capture.
- [x] Extends the existing report from
  `CernealaPresentation/PresentationWindow.Automation.cs` with Prism counters
  useful, no test API in production view.
- [x] Capture desktop and small viewport exclusively via API
  `IWindowPlatform.RenderPng`; visually and automatically check the lack of overlap,
  accidental clipping and illegible text.

### Gate stage 2
- [x] The actual example uses only markup and public Prism APIs, and whatever
  discovered defect is fixed in the framework and covered by the regression test.

## Stage 3 — lifecycle and memory

- [x] Run a minimum of 10,000 attach/detach/reattach cycles for instances
  Prism with Motion, bindings, filters, styles, masks and backdrop.
- [x] Automate SolarSystem ↔ Diagnostics repeat navigation and check
  zero Motion, passes, leases and active surfaces after hide/detach.
- [x] Test hide/unhide, `Collapsed`, resource replacement, template
  recycling, root replacement, resize, device reset and backdrop source
  replacement.
- [x] Uses `WeakReference`, pool counters and memory snapshots
  to separate managed leaks from unreleased GPU resources.
- [x] Fix any leak in the invariant owner and add a RED test first;
  does not introduce special cleanup in SolarSystem. (It was not necessary: stress,
  `WeakReference`s and GPU counters detected no leaks.)

### Gate stage 3

- [x] After GC/device cleanup, the number of elements, instances, Motion handles,
  leases and retained GPU resources returns to baseline.

## Stage 4 — performance and budgets

- [x] Build benchmarks for static, animated parameter, many
  layers, filter chains, styles, nested groups and split backdrop.
- [x] Measures CPU build/submit, GPU frame time, passes, captures, allocations,
  peak live surfaces, hit/miss retained and GPU memory at resolutions
  representative.
- [x] Confirm zero managed allocations after warmup for Prism static and missing
  rebuild `ElementRenderCache` to non-structural animations.
- [x] Confirms that the second identical frame produces hit retained, zero capture and
  zero effect passes covered, and every pixel-affecting input produces a miss.
- [x] Only now sets default values for surface budget and limits
  transient/retained cache; document data and behavior when
  the limit is exceeded.
- [x] Do not add adaptive quality, async compute or third-party plugin API;
  record opportunities separately only if the data requires them.

### Gate stage 4

- [x] Budgets have reproducible benchmark and deterministic failure behavior, again
  the dogfood scene stays on target with no hidden degradation.

## Stage 5 — documentation and compatibility

- [x] Update the proposal and the TDD with the final measured decisions, keeping
  clearly separate the implemented contract and the postponed ideas.
- [x] Use the skill `writing-api-documentation` for all pages in
  `docs-site/documentation/classes/` touched and sync manifest.
- [x] Add concise guides for Photoshop model, default source,
  layer/group/mask/clipping, Motion paths, backdrop and diagnostics; the data
  catalog remain generated.
- [x] Check non-Prism backends: inner content renders normally without
  exception, change of layout/input or obligation of a backdrop provider.
- [x] Runs the API compatibility check and explicitly marks anything
  breaking change required in the release documentation.

### Gate stage 5

- [x] The documentation describes exactly the implemented behavior and all APIs
  public have the correct manifest page and entry.

## Stage 6 — final suite

- [x] Run full reindex and
  `dotnet run --no-build --project .\Tools\RoslynRepoIndexer\src\RoslynRepoIndexer.Cli\RoslynRepoIndexer.Cli.csproj -- doctor`.
- [x] Running
  `dotnet test .\tests\Cerneala.Tests.SourceGen\Cerneala.Tests.SourceGen.csproj`.
- [x] Running
  `dotnet test .\tests\Cerneala.Tests\Cerneala.Tests.csproj`.
- [x] Run `dotnet test .\Cerneala.slnx` from checkout/clean build, incl
  recompiling shaders.
- [x] Run Presentation automation and all golden captures via API on
  set viewports.
- [x] Run benchmarks and final stress tests, save baselines
  reproducible and check limits.
- [x] Run `git diff --check`, API report and catalog report.

## The definition of done

- [x] Prism is demonstrated end-to-end in real markup, without code-behind
  rendering or workaround at view level.
- [x] Full catalog, conformance, lifecycle, memory, performance,
  cache retained, compatibility and documentation are green.
- [x] All gates in the index and in the nine planes are checked.