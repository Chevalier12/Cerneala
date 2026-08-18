# Plan index: Motion markup

> Date: 2026-07-15
> Status: completed
> Decision source: `docs/motion-markup-syntax-proposal.md`
> Goal: we implement the declarative Motion language in deliverable verticals, without reflection, without the second animation engine and without hiding runtime deficiencies under workarounds.

## 1. Summary

The proposal is too big for a single healthy checklist. The implementation is divided into six dependent plans, each with its own contract, RED/GREEN tests and gates. Each stage must leave the product compilable and verifiable; we don't throw the whole generator into a blender and hope that IntelliSense comes out.

## 2. Joint decisions

- The markup is analyzed and validated by Roslyn during the build; the runtime does not look for properties, events or resources through reflection.
- `Aspect` owns activation, observation, events and lifecycle. `MotionClip` remains a generator-owned recipe, without the homonymous runtime class.
- Unqualified properties target the Aspect element; `$Name.Property` is statically resolved for each place where the Aspect is applied.
- `@on` is resolved exclusively to a `IEventSymbol` from `TargetType` or its basic types. The generator does not inject code into methods with the same name.
- All subscriptions, observations and handles are per Aspect instance and are released upon detachment/replacement.
- The generator lowers the syntax to existing Motion APIs. When the runtime contract cannot support the required lifecycle, a RED test is first written and the real contract is repaired; the defect in the generated code is not masked.
- The CSS-like Motion syntax does not support binding modes. There are no `$event`, `@else`, transactions or programmable layout sequences.

## 3. Plans and dependencies

1. `docs/plans/2026-07-15-motion-markup-foundation.md` DONE
2. `docs/plans/2026-07-15-motion-markup-composition-and-clips.md`, dependent on plan 1 DONE
3. `docs/plans/2026-07-15-motion-markup-timelines-and-specs.md`, dependent on plans 1-2 DONE
4. `docs/plans/2026-07-15-motion-markup-presence-and-layout.md`, dependent on plan 1 DONE
5. `docs/plans/2026-07-15-motion-markup-scroll-and-input.md`, dependent on plan 1 DONE
6. `docs/plans/2026-07-15-motion-markup-integration-and-hardening.md`, dependent on plans 1-5 DONE

Plans 4 and 5 can run after foundation without waiting for timelines/clips. Plane 6 is the final gate and does not start until the accepted surfaces from the other planes are GREEN.

## 4. Stop conditions

- No scrubbing, seek, reverse or external progress is implemented for keyframes.
- Presence, Layout, Scroll, Drag or Gesture do not extend beyond the runtime capabilities documented in the proposal.
- Do not add `Motion` as a resource or attachable object; the reusable resources are specs, `Aspect` and `MotionClip`.
- A complete Visual Studio/LSP extension is not built in this set of plans. However, the generator must offer sufficiently good diagnostics and source locations for subsequent tooling.
- Decay does not receive the execution syntax invented in the implementation. The timeline plan accepts it only after defining a declarative contract that does not claim that `@to` is used by the sampler when the runtime ignores it.

## 5. Global gates

- [x] After each C# or project change, run `dotnet run --no-build --project .\Tools\RoslynRepoIndexer\src\RoslynRepoIndexer.Cli\RoslynRepoIndexer.Cli.csproj -- index .\Cerneala.slnx --json` indexing.
- [x] Any new or modified public API has the synchronized page in `docs-site/documentation/classes/`, created with the `writing-api-documentation` skill; the manifest is updated when a page is added or renamed.
- [x] Each dependent plan starts only after the final gate of the dependency is GREEN.
- [x] No lifecycle test is limited to the first attach: each new controller is checked by attach/detach/reattach and replacement.
- [x] Full suite remains GREEN with `dotnet test .\Cerneala.slnx`.

## 6. The definition of ready

- [x] All six plans are finalized and ticked off based on evidence, not optimism.
- [x] The accepted examples from the proposal compile and have the documented behavior; Deliberately invalid examples produce accurate diagnoses.
- [x] The generated markup does not use reflection and does not lookup after string in hot path.
- [x] Detach, cancel, replacement and repeated execution do not leave subscriptions, handles or Motion graph nodes active.
- [x] The conceptual documentation, API docs, the real sample and the proposal describe the same implemented surface.