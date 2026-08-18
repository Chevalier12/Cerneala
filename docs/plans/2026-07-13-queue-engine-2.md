# Plan: Queue Engine 2.0

> Date: 2026-07-13
> Status: completed
> Purpose: modernization of invalidation queues and element ordering without changing public contracts or frame semantics

## 1. Executive summary

Queue Engine 2.0 will replace the repeated `HashSet<UIElement> + List<UIElement>` implementation of invalidation queues with a common internal core, with O(1) operations for common queries and a single indexing of the visual order for each version of the tree.

The change follows four concrete problems:

- `HasWork` builds snapshots and can traverse the tree only to find out if a queue is empty;
- each queue recalculates the order of the elements separately;
- removing elements uses searches and linear deletions from the list;
- the same queue logic is duplicated in several classes and risks evolving differently.

The implementation will preserve existing public classes and observable behavior. We are not making a revolution with a fork in the scheduler; we change the engine under the hood and check every screw.

## 2. Baseline and the current problem

### 2.1 Current implementation

The following queues use the same basic structure:

- `LayoutQueue`;
- `RenderQueue`;
- `AspectQueue`;
- `HitTestQueue`;
- `InheritedPropertyQueue`;
- `CommandStateQueue`.

The current model is, in essence:
```text
HashSet<UIElement> membership
List<UIElement> order
Snapshot() -> ElementQueueOrder.Sort(root, order)
Remove(element) -> order.RemoveAll(...)
HasWork -> Snapshot().Count > 0
```
### 2.2 Observed costs

- [x] We capture a reproducible baseline for `HasWork`, snapshot and drain.
- [x] We confirm the number of complete traversals of the tree on an idle frame.
- [x] We confirm the allocations produced by repeated queries `HasWork`.
- [x] We confirm the scaling curve for emptying a queue with 100, 1,000 and 10,000 elements.
- [x] We save the baseline results in the project's benchmark artifact.

The current observation, which must be transformed into a test and measure:

- `ElementQueueOrder.Sort` builds a dictionary of the entire tree for each snapshot;
- `LayoutQueue.HasWork` asks for two snapshots, one for measure and one for arrange;
- `Scheduler.HasWork` can be consulted from several points of the same update;
- an idle frame can end up traversing the same tree several times without any real thing;
- `List.RemoveAll` executed for each element can turn emptying a large queue into a cost close to O(Q²).

## 3. Measurable objectives

- [x] `HasWork` is O(1) for each queue.
- [x] `HasWork` does not build snapshots.
- [x] `HasWork` does not cross the visual tree.
- [x] `Contains`, `Enqueue` and `Remove` are O(1) amortized.
- [x] The Snapshot sorts only the elements actually in the queue.
- [x] The complete visual order is indexed at most once for the same value `UIRoot.TreeVersion`.
- [x] The order index is reused by all queues of the same root.
- [x] Emptying a queue no longer causes repeated linear deletions from an auxiliary list.
- [x] The public order of the snapshots remains identical to the current visual order.
- [x] The semantics of the scheduler snapshot remains unchanged.
- [x] No public signature is modified.
- [x] All existing tests remain green.
- [x] Playground Diagnostic for the repaired scenario remains at a single useful measurement.

## 4. Non-objectives

Queue Engine 2.0 will not include the following changes:

- [x] We do not rewrite the `MeasureCore` or `ArrangeCore` algorithms of the controls.
- [x] We do not introduce a new state machine for layout.
- [x] We do not add multi-pass layout limited by a frame budget.
- [x] We do not optimize separately `Grid`, `StackPanel` or other panels.
- [x] We do not change the property invalidation policy.
- [x] We do not change the phase order from `UiFrameScheduler`.
- [x] We do not introduce concurrency or parallel processing of queues.
- [x] We are not exposing a new public API just for diagnostics.
- [x] We do not combine here the render cache, hit-testing or other optimizations without direct connection with the queue engine.

These can become separate projects after the queue engine has predictable costs. Otherwise we would put the turbo on the car while changing the wheels, which is spectacular only until the fence comes into question.

## 5. Contracts to be kept

### 5.1 Queues Agreement

- [x] An element can exist at most once in a queue.
- [x] The identity of the element is referential, not based on equal value.
- [x] Re-enqueue after `Remove` works normally.
- [x] `Snapshot` does not implicitly modify the valid elements in the queue.
- [x] The detached elements are not returned to the consumer.
- [x] A tree mutation can leave in the queue the invalidation of the root that represents that mutation.
- [x] Public Snapshots preserve deterministic visual order.

### 5.2 Special contract of `LayoutQueue`

- [x] Metadata `LayoutQueueEntryKind` is preserved.
- [x] The priority remains `Direct > Required > Propagated`.
- [x] A higher priority invalidation promotes the existing entry.
- [x] An invalidation with a lower priority does not demote the existing entry.
- [x] `SnapshotMeasure` public remains parent-first.
- [x] The internal snapshot used by the incremental measure can remain bottom-up where the current contract requires it.
- [x] Measure and arrange remain logically distinct queues.

### 5.3 Scheduler Agreement

- [x] Each phase processes a stable snapshot.
- [x] Re-enqueue in the same phase is postponed for the next frame.
- [x] The work produced for a later phase can be consumed in the same frame.
- [x] If the processing throws an exception, the current element and the rest of the snapshot are not lost.
- [x] The order of the phases remains unchanged.
- [x] A frame without invalidations does not start artificial work.

## 6. The proposed architecture

### 6.1 Reusable inner core

We introduce a generic internal type, with a final name established in the implementation, of the form:
```csharp
internal sealed class ElementWorkQueue<TMetadata>
{
    private readonly Dictionary<UIElement, TMetadata> entries;

    public int Count { get; }
    public bool HasWork { get; }
    public bool Contains(UIElement element);
    public void Enqueue(UIElement element, TMetadata metadata);
    public bool Remove(UIElement element);
    public IReadOnlyList<ElementWorkItem<TMetadata>> Snapshot(UIRoot root);
}
```
Responsibilities:

- stores a single entry for each instance `UIElement`;
- uses comparison by reference;
- apply an explicit merge/promotion function for metadata;
- exposes `Count` and `HasWork` without snapshot;
- remove entries directly from the dictionary;
- sorts only the keys in the queue when the snapshot is requested;
- defensively cleans entries that no longer belong to root.

For queues without metadata we use a minimal internal type, not six copied implementations with the same shit in a different hat.

### 6.2 Common index of the visual order

We introduce an internal service associated with the root, in the form:
```text
ElementQueueOrderIndex
  root
  indexedTreeVersion
  Dictionary<UIElement, int> preorderOrdinal
```
Rules:

- the index is built lazy, only on the first snapshot that actually has elements;
- the validation key is `UIRoot.TreeVersion`;
- all queues of the same root use the same index;
- a tree mutation logically invalidates the index by changing the version;
- the reconstruction completely replaces the old dictionary in order not to keep stale references;
- the order is exactly the visual preorder used now;
- elements absent from the index are considered detached and are defensively removed from the snapshot.

### 6.3 Keeping existing wrappers

The current public classes remain compatible facade:
```text
RenderQueue -------------------+
AspectQueue -------------------|
HitTestQueue ------------------|--> ElementWorkQueue<Unit>
InheritedPropertyQueue --------|
CommandStateQueue -------------+

LayoutQueue ----------------------> ElementWorkQueue<LayoutQueueEntryKind>
                                     + two instances: measure and arrange
```
Advantages:

- consumers must not be modified;
- The public API and documentation remain stable;
- migration can be done queue by queue;
- the common core can be tested separately from the scheduler.

### 6.4 Cleaning of detached elements

We use two levels of protection:

1. Active cleanup on detach, via a unique internal root point that removes the element or subtree from all relevant queues.
2. Defensive snapshot cleaning, for cases where a rare detachment path bypasses the main mechanism.

Conditions:

- [x] Active cleanup does not remove root invalidation caused by tree mutation.
- [x] Clearing a subtree does not traverse the entire remaining tree.
- [x] Snapshot checks only the elements in the queue, not all visual elements.
- [x] `HasWork` remains O(1); we don't hide a crossing under an innocent face getter.

## 7. Estimated file structure

Possible new files:

- `Cerneala/UI/Invalidation/ElementWorkQueue.cs`;
- `Cerneala/UI/Invalidation/ElementQueueOrderIndex.cs`;
- `tests/Cerneala.Tests/UI/Invalidation/ElementWorkQueueTests.cs`;
- `tests/Cerneala.Tests/UI/Invalidation/ElementQueueOrderIndexTests.cs`;
- `tests/Cerneala.Tests/UI/Invalidation/ElementQueueContractTests.cs`;
- `benchmarks/Cerneala.Benchmarks/Cerneala.Benchmarks.csproj`;
- `benchmarks/Cerneala.Benchmarks/QueueEngineBenchmarks.cs`;
- `benchmarks/Cerneala.Benchmarks/README.md`.

Existing files possibly modified:

- `Cerneala/UI/Invalidation/ElementQueueOrder.cs`;
- `Cerneala/UI/Invalidation/LayoutQueue.cs`;
- `Cerneala/UI/Invalidation/RenderQueue.cs`;
- `Cerneala/UI/Invalidation/AspectQueue.cs`;
- `Cerneala/UI/Invalidation/HitTestQueue.cs`;
- `Cerneala/UI/Invalidation/InheritedPropertyQueue.cs`;
- `Cerneala/UI/Invalidation/CommandStateQueue.cs`;
- `Cerneala/UI/Invalidation/UiFrameScheduler.cs`;
- the internal class that detaches elements from `UIRoot`;
- existing queues and scheduler tests.

The list is indicative. We don't create files or abstractions just because they look nice in a diagram.

## 8. Implementation plan

### Stage 0 - Baseline and safety net

- [x] Run the `FileTree.md` generator and update the Roslyn index.
- [x] Run the entire suite of tests and write down the number of tests and the duration.
- [x] Add characterization tests for the order of all snapshots.
- [x] Add characterization tests for deduplication and re-enqueue.
- [x] Add characterization tests for detachment.
- [x] Add characterization tests for exceptions in the scheduler.
- [x] Add characterization tests for the work produced between phases.
- [x] Internally instruments the number of builds of the visual order for tests.
- [x] Measures the baseline for the scenarios in the benchmark section.
- [x] Save the results with build, runtime and hardware information.

**Gate Stage 0**

- [x] All characterization tests pass on the current implementation.
- [x] The baseline is reproducible.
- [x] We have measurable proof of current crossings and allocations.
- [x] There are no changes in behavior at this stage.

### Stage 1 - Common index of visual order

- [x] Enter `ElementQueueOrderIndex` as internal type.
- [x] Binds the index to a single `UIRoot`.
- [x] Constructs an ordinal preorder identical to the current algorithm.
- [x] Caches the result after `TreeVersion`.
- [x] Rebuild the index only when the tree version has changed.
- [x] Replaces the old dictionary on rebuild to release stale references.
- [x] Exposes an internal diagnostic hook for the number of builds available to tests.
- [x] Add sorting of a small set of elements using cached ordinals.
- [x] Deterministically treats elements that do not belong to the root.
- [x] Keep `ElementQueueOrder` temporarily as an adapter, if this reduces the risk of migration. (It wasn't necessary; the old adapter was removed.)

Tests stage 1:

- [x] The first snapshot builds the index only once.
- [x] Repeated snapshots on the same `TreeVersion` reuse the index.
- [x] Snapshots from different queues reuse the same index.
- [x] A tree mutation produces exactly one rebuild on the next use.
- [x] The order after rebuild reflects the new visual structure.
- [x] A detached element does not receive a valid ordinal.
- [x] An empty tree and a single root are handled correctly.
- [x] Deep trees do not use a new recursion riskier than the existing implementation.

**Gate stage 1**

- [x] The resulting order is byte-for-byte equivalent in the characterization tests.
- [x] At most one full build occurs for each `TreeVersion` used.
- [x] No query `HasWork` asks for the index.
- [x] All tests remain green.

### Stage 2 - Nucleus `ElementWorkQueue<TMetadata>`

- [x] Enter the generic internal type.
- [x] Uses an explicit referential identity comparer.
- [x] Implements `Count` and `HasWork` directly over the number of entries.
- [x] Implements `Contains` without snapshot.
- [x] Implements enqueue with deduplication O(1) amortized.
- [x] Implements metadata merging through a simple strategy injected into the constructor.
- [x] Implements `Remove` by direct deletion from the dictionary.
- [x] Implements stable snapshot over a copy of current entries.
- [x] Sorts the copy using the common index of the root.
- [x] Defensively removes the detached entries discovered in the snapshot.
- [x] Avoid LINQ on hot paths if it produces avoidable allocations.
- [x] Do not add pooling until the profiler proves that it is necessary.

Tests stage 2:

- [x] Repeated enqueue does not duplicate the element.
- [x] Two distinct courts remain distinct even if they have equal value.
- [x] Remove nonexistent is safe.
- [x] Remove followed by enqueue re-adds the element only once.
- [x] The snapshot is stable if enqueues are made after its capture.
- [x] The metadata merge promotes, but does not demote.
- [x] The detached elements are cleaned without crossing the entire shaft.
- [x] `HasWork` and `Count` do not build the index.
- [x] The empty queue does not allocate to `HasWork` after warmup.

**Gate stage 2**

- [x] The common core passes all isolated tests.
- [x] Simple operations no longer depend on an auxiliary list.
- [x] There is no `RemoveAll` on the drain path.
- [x] No public contract has been modified yet.

### Stage 3 - Migration of queues without metadata

Recommended migration order:

- [x] `RenderQueue`.
- [x] `AspectQueue`.
- [x] `HitTestQueue`.
- [x] `InheritedPropertyQueue`.
- [x] `CommandStateQueue`.

For each queue:

- [x] Keeps the class name and public signatures.
- [x] Replaces duplicate collections with the common core.
- [x] Keeps the validation of existing arguments and exceptions.
- [x] Keeps the order of the snapshot.
- [x] Keeps the detach semantics.
- [x] Run the queue-specific tests after migration.
- [x] Run the scheduler tests after migration.
- [x] Re-indexes the repository after each code or project modification.

**Gate stage 3**

- [x] All five queues use the same core.
- [x] Common contract tests pass for each wrapper.
- [x] There are no remaining copies of the `HashSet + List` model.
- [x] API public diff is empty.
- [x] All tests remain green.

### Stage 4 - Migration `LayoutQueue`

- [x] Model measure and arrange as two instances of the common core.
- [x] Keeps the number of entries for each phase separately.
- [x] Implement the `LayoutQueueEntryKind` merge with the current priority.
- [x] Keeps the public measure snapshot in parent-first order.
- [x] Keep the internal inversion for incremental measure where necessary.
- [x] Keeps the arrange snapshot in the order requested by the scheduler.
- [x] Keeps the individual remove and complete remove methods.
- [x] Checks the situations where the same element is simultaneously in measure and arrange.
- [x] Check enqueue during measure and arrange processing.
- [x] Remove the old implementation only after complete equivalence.

Tests stage 4:

- [x] `Direct` promotes `Required` and `Propagated`.
- [x] `Required` promotes `Propagated`, but not `Direct`.
- [x] `Propagated` does not downgrade anything.
- [x] Parent-first public remains unchanged.
- [x] Internal bottom-up remains unchanged.
- [x] Measure and arrange do not corrupt each other's inputs.
- [x] `HasWork` does not build any of the snapshots.
- [x] A single measure input from the Playground produces a single useful measurement.

**Gate Stage 4**

- [x] `LayoutQueue` no longer contains collections or duplicate sorting.
- [x] All layout and scheduler tests pass.
- [x] The Playground scenario remains fixed.
- [x] API public diff is empty.

### Stage 5 - Active cleaning at detachment

- [x] Identifies the unique point through which an element or subtree leaves a `UIRoot`.
- [x] Add an internal scheduler/root method to remove the item from all queues.
- [x] Clean all the elements of the detached sub-shaft.
- [x] Keeps valid root invalidations generated by mutation.
- [x] Keeps the defensive fallback from the snapshot.
- [x] Avoid public exposure of the mechanism.
- [x] Checks the reattachment of the same element and re-enqueues afterwards.

Tests stage 5:

- [x] Detach removes the element from each queue type.
- [x] Detach from subtree removes all scheduled descendants.
- [x] Brothers remaining in the tree are not eliminated.
- [x] The root remains programmed if the mutation has invalidated it.
- [x] Reattach allows new invalidations.
- [x] Defensive Snapshot clears a simulated stable entry.
- [x] `HasWork` becomes false as soon as the last real input is detached.

**Gate Stage 5**

- [x] Queues do not intentionally keep references to detached subtrees.
- [x] The semantics of the existing tests for detach is preserved.
- [x] There are no complete traversals of the tree left for cleanup.

### Stage 6 - Integration and stability of the scheduler

- [x] Keep the snapshot plus remove model per element.
- [x] Take advantage of the new `Remove` O(1) without introducing a premature destructive API.
- [x] Check all points that refer to `Scheduler.HasWork`.
- [x] Confirm that repeated queries are cheap and without side effects.
- [x] Checks the restoration of exceptions.
- [x] Checks the postponement of the re-enqueue in the same phase.
- [x] Check the downstream processing in the same frame.
- [x] Checks the working limit and the continuation conditions of the frame.
- [x] Remove the remaining temporary adapters from `ElementQueueOrder`, if they are no longer used.

Tests stage 6:

- [x] Re-enqueue measure in measure is postponed.
- [x] The invalidation of arrange produced by measure is processed in the same frame when the contract allows it.
- [x] Render invalidation produced by the layout is processed in the same frame.
- [x] The exception does not lose the current element.
- [x] The exception does not lose the unprocessed elements of the snapshot.
- [x] The order of elements remains deterministic after recovery.
- [x] Frame idle does not build index and does not allocate via `HasWork`.
- [x] Frame without changes reports zero layout work.

**Gate stage 6**

- [x] `UiFrameSchedulerTests` pass in full.
- [x] `FrameSchedulerStabilityTests` pass in full.
- [x] All repository tests pass.
- [x] There are no observable differences outside of performance.

### Stage 7 - Benchmarks and performance thresholds

Add a separate BenchmarkDotNet project just for stable queue engine scenarios. Do not put benchmarks with fragile time thresholds in the unitary suite.

Scenarios:

- [x] `HasWork` idle on trees of 100, 1,000 and 10,000 elements.
- [x] `HasWork` repeated several times in the same frame.
- [x] Snapshot with 1, 10, 100 and 1,000 programmed elements in a large tree.
- [x] Drain for 100, 1,000 and 10,000 entries.
- [x] Successive snapshots from different queues on the same `TreeVersion`.
- [x] Rebuild after a tree mutation.
- [x] Promotion of metadata in `LayoutQueue`.
- [x] Detachment of sub-tree with programmed elements.

Metrics:

- [x] Average time and distribution.
- [x] Bytes allocated per operation.
- [x] Gen0/Gen1 collections where relevant.
- [x] Number of index builds.
- [x] Number of nodes visited for indexing.
- [x] Number of elements sorted per snapshot.

Mandatory functional thresholds:
- [x] `HasWork` makes zero visual crossings.
- [x] `HasWork` allocates zero bytes after warmup.
- [x] A `TreeVersion` produces at most one build of the common index.
- [x] Drain no longer contains O(Q) deletions for each element.
- [x] The cost of the drain increases approximately linearly with the number of entries.
- [x] Snapshot depends on Q scheduled elements plus at most one rebuild per version, not N for each queue.

The absolute thresholds in milliseconds are established after the baseline on the same hardware and are documented in the benchmark artifact. We don't put in CI a hysterical timer that drops because Windows decided to scratch its antivirus.

**Gate stage 7**

- [x] The before/after results are saved and comparable.
- [x] All functional thresholds are met.
- [x] There is no significant regression in small snapshots.
- [x] Any accepted regression is explained explicitly. (It was not necessary to accept any regression.)

### Stage 8 - Cleaning, documentation and final verification

- [x] Eliminate old code and unused adapters.
- [x] Eliminate diagnostic hooks that are not needed for tests or benchmarks.
- [x] Keep the remaining hooks `internal`, not public.
- [x] Runs the project formatter.
- [x] Run `dotnet build` for the solution.
- [x] Run all tests.
- [x] Run the final benchmarks in Release configuration.
- [x] Run the Playground scenario and save the relevant diagnosis.
- [x] Regenerate `FileTree.md`.
- [x] Re-index the solution with RoslynIndexer.
- [x] Runs `git diff --check`.
- [x] Manually check the public API diff.
- [x] Update this plan by ticking off the executed tasks.

Documentation:

- [x] Confirm that there are no public API changes.
- [x] If a public change inevitably occurs, stop the implementation and discuss the contract separately. (Did not apply: public API remained unchanged.)
- [x] For any approved public change, update the documentation from `docs-site/documentation/classes/` using the mandatory skill. (Not applied: there are no public changes.)
- [x] Document the benchmarks and their running mode in the README of the benchmark project.

**Gate Stage 8**

- [x] Green build.
- [x] All green tests.
- [x] Final benchmarks archived.
- [x] Public API unchanged.
- [x] Clean repository, no temporary files or leftover processes.

## 9. Test strategy

### 9.1 Unit tests

Unit tests validate invariants, not runtimes:

- [x] referential identity;
- [x] deduplication;
- [x] metadata promotion;
- [x] remove and re-enqueue;
- [x] stable snapshot;
- [x] visual order;
- [x] cache after `TreeVersion`;
- [x] detach and reattach;
- [x] empty tail;
- [x] exceptions.

### 9.2 Contract tests for wrappers
The same basic suite must be applied to all simple queues:

- [x] enqueue once;
- [x] duplicate enqueue;
- [x] contains;
- [x] remove;
- [x] snapshot order;
- [x] detached pruning;
- [x] repeated `HasWork`.

Thus we avoid the situation in which five tails use the same engine, but one puts on a fake mustache and decides that the rules do not apply to it.

### 9.3 Integration Tests

- [x] frame idle;
- [x] frame with a single measure invalidation;
- [x] frame with invalidations in all phases;
- [x] invalidation produced by an upstream phase;
- [x] re-enqueue in the same phase;
- [x] exception in the middle of the snapshot;
- [x] tree mutation between two frames;
- [x] detach during processing;
- [x] Playground without interaction.

### 9.4 Benchmarks

Benchmarks measure performance, but do not replace functional tests. The results must be compared in Release, on the same runtime and the same hardware, with enough iterations for stabilization.

## 10. Risks and mitigations

### Risk: the visual order changes subtly

Mitigation:

- [x] characterization tests before the refactor;
- [x] the same preorder as the current implementation;
- [x] explicit comparisons on trees with multiple levels and siblings.

### Risk: the shared index keeps stale references

Mitigation:

- [x] the dictionary is completely replaced when changing `TreeVersion`;
- [x] cleanup active at detach;
- [x] defensive fallback to snapshot;
- [x] tests with detach/reattach and collection where practical.

### Risk: `HasWork` reports detached entries

Mitigation:

- [x] synchronous cleanup at detach;
- [x] tests that ask for `HasWork == false` immediately after removing the last entry;
- [x] snapshot pruning remains secondary protection, not the main mechanism.

### Risk: layout metadata is lost or downgraded

Mitigation:

- [x] works isolated and exhaustively tested;
- [x] migration `LayoutQueue` is done last;
- [x] current tests remain the source of truth for behavior.

### Risk: exceptions lose work from the snapshot

Mitigation:

- [x] we do not change the scheduler's snapshot contract;
- [x] we keep the restoration explicit;
- [x] tests for the current element and the rest unprocessed.

### Risk: generic abstraction becomes too clever

Mitigation:

- [x] the core knows only membership, metadata, snapshot and order index;
- [x] the policy of each phase remains in the wrapper/scheduler;
- [x] without pooling, destructive batching or competition until there is proof that they are necessary.

### Risk: the benchmark measures system noise

Mitigation:

- [x] BenchmarkDotNet in Release;
- [x] same machine and same runtime for comparisons;
- [x] structural thresholds in tests, temporal thresholds in reports;
- [x] more input sizes to see the curve, not just a sexy number.

## 11. Stop conditions
The implementation stops for re-evaluation if any of the following situations occur:

- [ ] a public API change is required;
- [ ] the current order cannot be reproduced without changing the contract;
- [ ] `TreeVersion` does not cover all the relevant mutations of the tree;
- [ ] the active cleanup at the detachment requires extensive changes in the ownership of the elements;
- [ ] the exception recovery cannot be kept with the proposed kernel;
- [ ] benchmarks show persistent regressions for small tails;
- [ ] the solution starts asking for pooling, locks or concurrency without data to justify them.

In these cases, the problem is documented and decided separately. We do not cover the crack with silicone and optimism.

## 12. Recommended sequence of commits

The implementation was delivered as a single working-tree batch; no commits were created because the user did not request a commit or publish. The list remains unchecked intentionally and does not represent outstanding technical work.

- [ ] `test: characterize queue engine behavior`
- [ ] `perf: cache visual queue order per tree version`
- [ ] `refactor: add shared element work queue core`
- [ ] `refactor: migrate invalidation queues to shared core`
- [ ] `refactor: migrate layout queue metadata handling`
- [ ] `fix: remove detached subtrees from pending queues`
- [ ] `test: expand scheduler queue stability coverage`
- [ ] `bench: add queue engine performance scenarios`
- [ ] `docs: record queue engine 2.0 results`

The commits can be merged if the diffs are small, but the conceptual order must be preserved. Each commit must build and have the relevant tests green.

## 13. Final acceptance checklist

### Fairness

- [x] All existing and new tests pass.
- [x] The order of the snapshots is identical to the baseline.
- [x] Deduplication and promotion work.
- [x] Detach doesn't leave fake thing in queues.
- [x] Exceptions do not lose work.
- [x] The semantics of the scheduler phases is unchanged.

### Performance

- [x] `HasWork` is O(1), without crossings and without allocations after warmup.
- [x] The visual order is built at most once per `TreeVersion`.
- [x] All queues reuse the same root index.
- [x] `Remove` no longer performs linear scans.
- [x] Drain no longer has quadratic behavior.
- [x] Snapshot sorts only programmed entries.
- [x] Playground remains at only one useful measurement in the validated scenario.

### Architecture

- [x] There is only one core for membership and snapshot.
- [x] Public wrappers remain thin and compatible.
- [x] Phase-specific policies are not pushed into the generic core.
- [x] There is no duplicate code left for the same queue operation.
- [x] No unused abstractions were entered.

### Delivery

- [x] Build Release green.
- [x] Complete green suite.
- [x] Benchmark before/after available.
- [x] public API unchanged.
- [x] The relevant documentation is synchronized.
- [x] `FileTree.md` and the Roslyn index are updated.
- [x] The plan is ticked according to the actual stage, without decoration checkboxes.

## 14. The definition of "ready"

Queue Engine 2.0 is ready when an idle frame can ask for work as many times as it needs without traversing the tree, queues can drain large batches without quadratic cost, all phases share the same cache-forgotten visual order, and the user doesn't notice any change apart from the fact that the framework no longer makes mistakes like after climbing ten floors with the refrigerator in its arms.
