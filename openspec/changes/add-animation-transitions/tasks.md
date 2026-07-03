## 1. Animation Core

- [x] 1.1 Add `UI/Animation/AnimationClock.cs` with explicit elapsed-time ticking and normalized progress.
- [x] 1.2 Add `UI/Animation/Easing.cs` with deterministic easing helpers.
- [x] 1.3 Add `UI/Animation/Animation.cs` and `UI/Animation/Animation{T}.cs` with typed interpolation.
- [x] 1.4 Add `UI/Animation/AnimatedValueSource.cs` for applying and clearing animation values.

## 2. Scheduling and Transitions

- [x] 2.1 Add `UI/Animation/AnimationScheduler.cs` to tick active animations and report pending work.
- [x] 2.2 Add `UI/Animation/Transition.cs` and `UI/Animation/Transition{T}.cs`.
- [x] 2.3 Add lightweight `UI/Animation/Storyboard.cs` only for grouped scheduler handles.
- [x] 2.4 Add `UI/Styling/StyleTransition.cs` as a descriptor, not a runtime scheduler.

## 3. Tests and Invalidation

- [x] 3.1 Add `tests/Cerneala.Tests/UI/Animation/AnimationClockTests.cs`.
- [x] 3.2 Add `tests/Cerneala.Tests/UI/Animation/AnimationSchedulerTests.cs`.
- [x] 3.3 Add `tests/Cerneala.Tests/UI/Animation/TypedAnimationTests.cs`.
- [x] 3.4 Add `tests/Cerneala.Tests/UI/Animation/TransitionTests.cs`.
- [x] 3.5 Add `tests/Cerneala.Tests/UI/Animation/AnimationInvalidationTests.cs`.
- [x] 3.6 Ensure animation and style transition APIs stay backend-neutral through architecture tests.

## 4. Verification and Roadmap

- [x] 4.1 Run OpenSpec validation and full project tests.
- [x] 4.2 Update `ROADMAPv2.md` section 23 checkboxes for implemented files, tests, and acceptance criteria.
