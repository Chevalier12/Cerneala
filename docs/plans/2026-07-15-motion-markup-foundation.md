# Plan: foundation for Motion markup

> Date: 2026-07-15
> Status: completed
> Dependency: none
> Purpose: we deliver the first usable vertical with Tween/Spring specs, inline/named Aspect, `@when`, `@if`, `@on`, `@animate`, `@from` and `@to`, completely source-generated and with correct lifecycle.

## 1. Baseline and the current problem

`UiMarkupDirectiveParser.cs` now parses `@when`, `@if`, `@default` and `@template` in an internal AST. `UiMarkupReactiveEmitter` solves typed sources and `GeneratedMarkup.AttachConditions` connects a controller to `IElementLifecycleBehavior`. `UiMarkupGenerator` already knows resources `Aspect`, but they contain assignments/templates, not Motion behavior.

Motion exhibits `MotionAnimationBuilder<T>`, `MotionPropertyBinding<T>` and typical specs. The public builder defaults to `HoldOnComplete=true`, but does not expose all `MotionPropertyStartOptions`; the markup must not bypass this lack by accessing internal members.

## 2. The proposed architecture

- We extend the existing parser through a separate Motion AST, not through C# string concatenation.
- The recommended separation is `UiMarkupMotionParser`, `UiMarkupMotionResolver` and `UiMarkupMotionEmitter` as parts of the generator; names are estimates, responsibilities are not.
- We add a bridge runtime generated in `UI/Markup/GeneratedMarkupMotion.cs`: a behavior per element/Aspect creates a session when attach, detaches events/observations and cancels executions when detach.
- The resources `Tween` and `Spring` are generator-known declarations. On each use, the resolver constructs `MotionSpec<T>` for the property type; an untyped runtime spec does not appear.
- Named Aspects are expanded and validated at each application site. Compatibility uses assignability (`elementType` derives from `TargetType`), not name equality.
- `$Name.Property` is resolved statically against the elements called visible at the application site; an application that cannot satisfy all targets receives a diagnosis.

## 3. Non-objectives

- No `MotionClip`, composition, keyframes, handles, Presence, Layout, Scroll, Drag or Gesture in this vertical.
- No `$event`, reflection, method interception, arbitrary C# expressions or runtime parsing.
- Without changing the existing semantics for assignments and templates from `Aspect`.

## 4. Estimated files

- `Cerneala.SourceGen/UiMarkupDirectiveParser.cs`
- new partials under `Cerneala.SourceGen/` for AST, solving and issuing Motion
- `Cerneala.SourceGen/UiMarkupGenerator.cs`
- `UI/Markup/GeneratedMarkupMotion.cs`
- `UI/Motion/MotionAnimationBuilder.cs` only if a public overload is required for `MotionPropertyStartOptions`
- `tests/Cerneala.Tests.SourceGen/UiMarkupGeneratorMotionTests.cs`
- `tests/Cerneala.Tests/UI/Markup/GeneratedMarkupMotionTests.cs`
- the affected API pages from `docs-site/documentation/classes/`

## 5. Implementation stages

### Stage 0 - RED and minimum public contract

- [x] Add sourcegen RED tests for resources `Tween`/`Spring`, Aspect named and inline, a `@animate` with `@from`/`@to`, `current`, default spec and spec per property.
- [x] Add RED diagnostics for missing `@to`, nonexistent property, incompatible type, absent mixer, `@from` without pair in `@to`, unknown resource and Motion directive in illegal context.
- [x] Add RED tests for `retarget`, `holdOnComplete` and `debugName`; confirm through an API test that the generator does not need internal members.
- [x] Establishes the minimum public overload of `MotionAnimationBuilder<T>` or the public bridge `GeneratedMarkup` that transmits `MotionPropertyStartOptions`, without duplicating the logic from `MotionPropertyBinding<T>`.
- [x] Update API docs for any public member entered and run public API diff review.
- [x] Reindex the solution after C# changes.

**Gate Stage 0**

- [x] The new tests fail for the expected behavioral reasons, and the chosen contract can represent all three real options.
- [x] No generic AST runtime model or untyped spec was introduced.

### Stage 1 - Grammar and AST Motion

- [x] Extends `DirectiveCursor` to recognize Motion directives only in allowed Aspect bodies, keeping existing `@when`/`@if` compatible.
- [x] Explicitly models `@animate`, `@from`, `@to`, targeted assignments, `with`, options and source locations.
- [x] Parse Motion values ​​through a limited grammar: typeable literals, `current`, reactive sources already supported, resource references and conditional expression used by the proposal; it doesn't directly output arbitrary text like C#.
- [x] Parse durations `ms`/`s`, easing names and inline constructors `Tween(...)`/`Spring(...)` with diagnostics for the wrong token.
- [x] Keep XML controls forbidden in execution bodies and keep non-Motion directives unchanged.
- [x] Add recovery tests for braces, semicolons, quotes and unknown directives.
- [x] Reindex the solution.

**Gate stage 1**

- [x] AST separates syntax, semantic type and emitted code; no test depends on fragile matching on raw bodies.
- [x] All parser tests and old tests `@when`/`@if` are GREEN.

### Stage 2 - Static semantic resolution

- [x] Solves `TargetType` including custom controls and validates the application through assignability.
- [x] Resolve unqualified properties on target and `$Name.Property` per application site, including forward references in the application namescope.
- [x] Infer type `T`, check `UiProperty<T>`, accessibility, read-only and compatible interpolator/mixer.
- [x] Specializes each Tween/Spring resource to `MotionSpec<T>` at the place of use and deduplicates only the identical constructions from the same generated scope.
- [x] Resolve `current` to the current Motion visual value, not to a stale reading of the base value.
- [x] Validates that each property in `@from` exists in `@to`; `@from` omitted starts from the current visual value.
- [x] Validate options exactly at `Restart|PreserveProgress`, Boolean and string; reject `conflict`, channel and reduced-motion options invented.
- [x] Reindex the solution.

**Gate stage 2**

- [x] Diagnostics differentiates type missing, property missing, wrong target, wrong value type, missing mixer and invalid spec type.
- [x] No runtime resolution by name appears in the generated code.

### Stage 3 - Activation by status and events

- [x] Reuses the observations resolver for `@when` and `@if`, so that all dependencies remain observed and the reevaluation respects the existing short-circuit.
- [x] Defines the activation: each relevant passage of the branch starts the declared execution; reevaluating without change does not restart the animation.
- [x] Resolves `@on EventName` to `IEventSymbol` on `TargetType`/base types and generates direct subscription/unsubscription `+=`/`-=`, including for routed event wrappers.
- [x] Issue a diagnosis if the event is missing, inaccessible or the homonymous member is not an event; does not search or modify methods.
- [x] Ignore event args in the language, according to the decision without `$event`.
- [x] Add tests with built-in event, custom CLR event, custom routed event, inherited event and `TargetType` too general.
- [x] Add attach/detach/reattach tests that count invocations and demonstrate a single active subscription.
- [x] Reindex the solution.

**Gate stage 3**

- [x] Custom events work without reflection and without method injection.
- [x] After detach, the event and observations can no longer start Motion.

### Stage 4 - Issue and lifecycle

- [x] Implements the runtime behavior per Aspect with new session at attach and idempotent cleanup at detach/dispose.
- [x] Issue the start of properties from a `@animate` in parallel and group handles for cancellation at detach, without changing the semantics of the public `MotionGroupHandle`.
- [x] Apply `@from` through the correct Motion path, then start `@to` with spec and options; avoid the intermediate flash between writes.
- [x] Ensure order: resources and Presence/Layout-independent properties are configured before attach, and animations require root Motion only after attach.
- [x] Add tests with two instances of the same Aspect to demonstrate independent sessions and handles.
- [x] Add replacement/detach test during animation and confirm zero active handles after cleanup.
- [x] Reindex the solution.

**Gate Stage 4**

- [x] The vertical hover/event from the proposal runs completely from the markup.
- [x] There are no subscriptions or graph nodes left after 100 attach/detach cycles.

## 6. Verification

- [x] Runs `dotnet test .\tests\Cerneala.Tests.SourceGen\Cerneala.Tests.SourceGen.csproj`.
- [x] Run the targeted tests `GeneratedMarkupMotionTests` from `Cerneala.Tests`.
- [x] Inspect the generated code for event wiring, spec specialization and absent reflection.
- [x] Runs `dotnet test .\Cerneala.slnx`.
- [x] Runs `git diff --check` and the final reindexing.

## 7. The definition of ready

- [x] Named and inline Aspects can start tween/spring animations via state or events.
- [x] `@from`, `@to`, `current`, target properties and options are typed and diagnosed at build.
- [x] Custom events are subscribed directly and cleaned deterministically.
- [x] API docs and conceptual documentation describe exactly the delivered vertical.
