# Ink

> Code: `UI/Ink`, `UI/Controls/InkCanvas.cs` · Verified at commit `0cb32300` (2026-10-10)

## Responsibility

Ink records pen and touch strokes as point lists. `UI/Ink` is only the data model: a stroke is an ordered list of `DrawPoint`, and a stroke collection reports additions and removals. `InkCanvas` turns stylus and touch points into strokes.

Ink does **not** draw strokes, does not take part in hit testing, and has no platform input path. Today it is a recording model that code reaches only by calling `InkCanvas.ApplyStylus`, `InkCanvas.ApplyTouch` or `InkCanvas.Strokes.Add` directly.

## Components

| Type | File | Role | Owner |
|---|---|---|---|
| `Stroke` | `UI/Ink/Stroke.cs` | Ordered `List<DrawPoint>`; `Points` exposes it as `IReadOnlyList<DrawPoint>`; `AddPoint` appends without any notification | the `InkCanvas` that created it, or the caller |
| `StrokeCollection` | `UI/Ink/StrokeCollection.cs` | `IReadOnlyList<Stroke>` with `Add`, `Remove` and a `Changed` event; no `Clear`, no insert, no indexer setter | `InkCanvas.Strokes` (get-only, one per canvas) |
| `StrokeCollectionChangedEventArgs` | `UI/Ink/StrokeCollectionChangedEventArgs.cs` | Record `(Kind, Stroke)`, one per mutation | transient |
| `StrokeCollectionChangeKind` | `UI/Ink/StrokeCollectionChangeKind.cs` | `Added`, `Removed` | value |
| `InkCanvas` | `UI/Controls/InkCanvas.cs` | `Layout.Panels.Canvas` subclass; maps stylus and touch points to strokes, keyed by input kind and pointer id; raises `StrokeCollected` on pointer up | element |
| `InkCanvasStrokeCollectedEventArgs` | `UI/Controls/InkCanvasEventArgs.cs` | Payload of `StrokeCollected` | transient |

`InkCanvas` derives from `Cerneala.UI.Layout.Panels.Canvas`, not from the separate `Cerneala.UI.Controls.Canvas` control. Its private nested types are `InkInputKey` (kind plus id), `InkInputKind` (`Stylus`, `Touch`) and `InkInputAction` (`Down`, `Move`, `Up`).

The input payloads come from `UI/Input`: `StylusInputPoint` (`Id`, `X`, `Y`, `Action`, plus `Pressure`, `IsInRange`, `Button`) and `TouchInputPoint` (`Id`, `X`, `Y`, `Action`).

## Data Flow

Example: three stylus points with pointer id `1`.

```csharp
InkCanvas canvas = new();
canvas.ApplyStylus(new StylusInputPoint(1, 1, 2, StylusInputAction.Down));
canvas.ApplyStylus(new StylusInputPoint(1, 3, 4, StylusInputAction.Move));
canvas.ApplyStylus(new StylusInputPoint(1, 5, 6, StylusInputAction.Up));
// canvas.Strokes.Count == 1; the stroke has 3 points: (1, 2), (3, 4), (5, 6)
```

1. `ApplyStylus` calls `Root?.Relay.VerifyAccess()`. The thread check runs only when the canvas is attached.
2. The stylus action becomes an `InkInputAction`. `Down`, `Move` and `Up` map directly. Every other `StylusInputAction` (for example `InRange` or `ButtonDown`) becomes `Move`. Touch works the same way through `ApplyTouch`.
3. **Down:** a new `Stroke` gets the first point, is stored as the active stroke for `(Stylus, 1)`, and is added to `Strokes`. `StrokeCollection.Add` adds it to the list, then raises `Changed`. `InkCanvas.OnStrokesChanged` calls `Invalidate(Measure | Render, "Ink strokes changed")`.
4. **Move:** the point is appended to the active stroke, then `Invalidate(Measure | Render, "Ink stroke point added")` runs. `Stroke.AddPoint` raises nothing, so the canvas invalidates itself here.
5. **Up:** the point is appended and the canvas invalidates, as for Move. Then the active entry for `(Stylus, 1)` is removed and `StrokeCollected` is raised with the finished stroke.
6. A Move or Up for a key with no active stroke is ignored.

Two pointers with different ids, or the same id on stylus and touch, build separate strokes at the same time.

## Rendering

`InkCanvas` does not override `OnRender`, and `UI/Ink` has no drawing code. Every Ink mutation still sets the `Render` flag, so the element's retained render cache is rebuilt on the next frame (see [rendering.md](rendering.md)). The rebuild produces no draw commands. `Measure` also runs, through `Canvas.MeasureCore`, which measures only child elements and ignores strokes.

An application that wants visible ink must draw `Strokes` itself, for example in a derived element's `OnRender`. `InkCanvas` is `sealed`, so that drawing element has to be a separate element.

## Lifecycle And Ownership

- The `InkCanvas` constructor subscribes to `Strokes.Changed`. It never unsubscribes, and nothing implements `IDisposable`.
- `InkCanvas` overrides no attach or detach hook. Active strokes and `Strokes` stay unchanged across detach and re-attach.
- An active stroke leaves the active table only on `Up`. There is no cancel path: losing the pointer or `OutOfRange` does not end the stroke.
- A second `Down` with the same kind and id replaces the active entry. The earlier stroke stays in `Strokes` and never raises `StrokeCollected`.
- All data is managed memory. A stroke's point list has no size limit.

## Thread Affinity

While the canvas is attached, `ApplyStylus` and `ApplyTouch` call `UiRelay.VerifyAccess`, and `OnStrokesChanged` throws `InvalidOperationException` off the UI thread. The message tells the caller to use `await root.Relay.InvokeAsync(() => canvas.Strokes.Add(stroke))`. A detached canvas has no thread check. See [relay.md](relay.md).

`StrokeCollection.Add` changes the list before it raises `Changed`. So when a worker thread adds a stroke to an attached canvas, the exception is thrown after the stroke is already in the list: the stroke stays, and no invalidation is queued.

## Frame Integration

Ink adds no frame phase and uses no queue directly. `Invalidate(Measure | Render)` becomes `Measure | Arrange | Render` in `DirtyPropagation`, which queues layout and render work. Hit testing is not invalidated. The work runs in the `Measure`, `Arrange` and `RenderCache` phases of the next frame (see [invalidation-and-frame.md](invalidation-and-frame.md)).

## Invariants

- One stylus pointer produces one stroke whose points keep their input order. `tests/Cerneala.Tests/Controls/InkCanvasTests.cs:InkCanvasRecordsStylusStrokeInOrder`.
- Concurrent touch pointers with different ids produce separate strokes (`X` values `[0, 1, 2]` and `[10, 11, 12]`). `InkCanvasTests.cs:InkCanvasKeepsConcurrentTouchStrokesSeparatedById`.
- `StrokeCollection` reports `Added`, then `Removed`, in mutation order. `InkCanvasTests.cs:StrokeCollectionNotifiesOnMutation`.
- A worker-thread `Strokes.Add` on an attached canvas throws an `InvalidOperationException` that names `Relay.InvokeAsync`, and queues no Relay or scheduler work. `tests/Cerneala.Tests/UI/Relay/FirstPartyRelayIntegrationTests.cs:UiOwnedCollectionsRejectWorkerNotificationsBeforeRetainedWork`.
- `StrokeCollected` raised exactly once on `Up`: netestat.
- Move or Up without a prior Down is ignored: netestat.
- Attached `ApplyStylus` and `ApplyTouch` off the UI thread throw: netestat.

## Known Limitations

- No production input path feeds Ink. `InputFrame` carries only pointer, keyboard and text input. `StylusInputBridge` and `TouchInputBridge` exist but are constructed only in tests, and `Cerneala.Platforms.Sdl3` has no stylus or touch input. The archived roadmap ([roadmap-v2.md](../archive/roadmap-v2/roadmap-v2.md)) marks Ink as a prototype frozen until platform stylus and touch input exist.
- Strokes are not rendered (see Rendering).
- `Pressure`, `IsInRange` and `Button` from `StylusInputPoint` are dropped.
- Unknown stylus actions become `Move` in `InkCanvas`, while `StylusInputBridge` throws on them.
- `Stroke.Points` returns the live backing list. `Stroke.AddPoint` on a stroke that is already in `Strokes`, called from outside `InkCanvas`, does not invalidate the canvas.
- A worker-thread `Strokes.Add` leaves the stroke in the collection after throwing (see Thread Affinity). The invariant test checks the exception, not the collection count.
