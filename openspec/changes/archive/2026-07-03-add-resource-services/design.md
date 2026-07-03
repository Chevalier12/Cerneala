## Context

Cerneala has retained controls, retained layout/render caches, and backend-neutral text services. `Image` currently accepts direct `IDrawImage` handles, while text font resolution uses explicit `IFontSource` or fallback behavior. That is good for early MVP work, but it does not provide stable resource identity, replacement notification, or dependency tracking for controls that need to respond when a font or image is swapped.

This change introduces a typed resource layer that is explicit and observable enough for retained invalidation, without recreating WPF resource dictionaries as core machinery. Resource services remain above drawing handles and below controls/text services.

## Goals / Non-Goals

**Goals:**
- Add typed resource ids and explicit provider/store APIs under `UI.Resources`.
- Add observable resource replacement events and dependency tracking.
- Add font and image resource wrappers that resolve to `IDrawFont` and `IDrawImage`.
- Add image loading abstraction plus MonoGame adapter that does not leak `Texture2D` into controls.
- Connect resource replacement to retained measure/render invalidation for image and font consumers.
- Update `ROADMAPv2.md` section 12 as tasks complete.

**Non-Goals:**
- Do not implement WPF-style resource dictionaries, implicit lookup, theme lookup, merged dictionaries, or dynamic resource expressions.
- Do not make resources global singletons.
- Do not add a general asset pipeline, async loading, lifetime eviction, or streaming.
- Do not change `IDrawImage` or `IDrawFont` into UI resource concepts; they remain drawing handles.

## Decisions

### Use typed `ResourceId<T>` values

Resources are addressed by `ResourceId<T>` where `T` is the resource contract type, such as `FontResource` or `ImageResource`.

Rationale: typed ids prevent accidental image/font mixups while still keeping resource lookup explicit and cheap.

Alternative considered: plain string keys. Rejected because they push type errors to runtime and make invalidation dependencies less clear.

### Keep provider/store explicit

`IResourceProvider` exposes typed lookup. `ResourceStore` owns resource values and raises resource replacement notifications. Controls and services receive providers explicitly through host/services or direct injection.

Rationale: this keeps resource access testable and avoids hidden global lookup.

Alternative considered: static global resource registry. Rejected because it would violate the roadmap acceptance criterion and make retained invalidation unpredictable.

### Track resource dependencies separately from drawing handles

`ResourceDependencyTracker` records which retained element or service output depends on which resource ids and exposes a resource dependency version for retained render dependencies.

Rationale: direct `IDrawImage`/`IDrawFont` equality cannot tell us whether an explicit resource was replaced or whether layout/render should be invalidated.

Alternative considered: compare resolved drawing handles only. Rejected because backends can reuse wrapper identities or replace content behind the same logical resource.

### Resource-backed image replacement can affect measure or render

Image consumers distinguish fixed-size render-only usage from intrinsic-size layout usage. Replacing an image resource invalidates render for fixed-size consumers and measure+render for intrinsic-size consumers.

Rationale: this matches retained layout semantics and avoids remeasuring when the control's desired size is independent from the image resource.

Alternative considered: always invalidate measure. Rejected because it does unnecessary retained layout work.

### Font resources feed text services

`FontResolver` can resolve a font resource through explicit resource services. Replacing a font resource invalidates text measurement and render for dependent text controls.

Rationale: text metrics depend on resolved font identity, and text layout cache keys must change when font resources change.

Alternative considered: treat font resource replacement as render-only. Rejected because font metrics can change.

### Keep MonoGame image loading in adapter namespace

`UI.Resources.MonoGame.MonoGameImageLoader` can load backend images and return `IDrawImage` or `MonoGameImage`, but `UI.Resources` core and `UI.Controls` must not reference `Texture2D`.

Rationale: controls stay backend-neutral while the host can still provide practical MonoGame resource loading.

Alternative considered: put MonoGame loading directly in `ImageResource`. Rejected because it leaks platform details into core resource services.

## Risks / Trade-offs

- [Risk] Resource invalidation can become stale if consumers forget to register dependencies. -> Mitigation: dependency tracker tests must prove image/font replacement invalidates dependent controls.
- [Risk] Resource services can drift toward a WPF dictionary clone. -> Mitigation: keep MVP APIs typed, explicit, and provider-based only.
- [Risk] Font resource identity and text layout cache identity can diverge. -> Mitigation: font resource replacement must update text layout dependency identity and text measurement tests.
- [Risk] MonoGame loader tests may require graphics device setup. -> Mitigation: core resource tests use fakes; MonoGame adapter tests can remain construction/boundary-focused unless a stable test seam exists.

## Migration Plan

1. Add `UI.Resources` typed ids, provider/store, events, dependency tracker, font/image resource wrappers, and image loader interface.
2. Add MonoGame image loader adapter under `UI.Resources.MonoGame`.
3. Integrate image resource replacement with `Image` invalidation.
4. Integrate font resource replacement with `FontResolver`/`TextBlock` invalidation.
5. Extend retained render dependencies for resource identity/version changes.
6. Add focused tests and update `ROADMAPv2.md`.

Rollback is limited to removing resource-backed properties/services and reverting controls to direct drawing handles.

## Open Questions

- Should resource dependency tracking live on `UIRoot`/host services immediately, or start as an injectable service owned by controls/tests?
- Should image loading be synchronous only for MVP, with async loading deferred to a later resource pipeline phase?
