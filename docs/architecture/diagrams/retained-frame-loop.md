# Retained Frame Loop

This diagram shows how retained UI work flows through one host frame
(`UiHost.UpdateCore` and `UiHost.DrawCore`). The text version with line
references is in [Invalidation And Frame Scheduling](../invalidation-and-frame.md).

```text
┌───────────────────────────────────────────────────────────────┐
│                         Game Loop                             │
└───────────────────────────────────────────────────────────────┘
                       │
        ┌──────────────┴──────────────┐
        ▼                             ▼
┌───────────────────┐         ┌───────────────────┐
│      Update       │         │       Draw        │
└───────────────────┘         └───────────────────┘
        │                             │
        ▼                             ▼
┌───────────────────┐         ┌───────────────────┐
│ UIRoot.BeginUpdate│         │ RetainedRenderer  │
│ drains Relay      │         │ Render: committed │
└───────────────────┘         │ root command list │
        │                     └───────────────────┘
        ▼                             │
┌───────────────────┐                 ▼
│ Scheduled pass    │         ┌───────────────────┐
│ (if queued work)  │         │ IDrawingBackend   │
└───────────────────┘         │ Render(commands,  │
        │                     │   frameContext)   │
        ▼                     └───────────────────┘
┌───────────────────┐
│ ElementInputBridge│
│ Dispatch: hit test│
│ focus, routing,   │
│ commands          │
└───────────────────┘
        │
        ▼
┌───────────────────┐
│ Input pass        │
│ (queued work, or  │
│ Motion not yet    │
│ sampled)          │
└───────────────────┘
        │
        ▼
┌───────────────────┐
│ RetainedRenderer  │
│ Commit            │
└───────────────────┘

Any state change (properties, Aspect, resources, input handlers, tree edits):

┌───────────────────────────────────────────────────────────────┐
│ UIRoot.Invalidate → DirtyPropagation → DirtyState flags       │
└───────────────────────────────────────────────────────────────┘
        │
        ▼
┌───────────────────────────────────────────────────────────────┐
│ Queues owned by UIRoot                                        │
│ InheritedPropertyQueue | CommandStateQueue | AspectQueue      │
│ LayoutQueue (measure, arrange) | RenderQueue | HitTestQueue   │
└───────────────────────────────────────────────────────────────┘
        │
        ▼
┌───────────────────┐
│ UiFrameScheduler  │  (one pass = these phases, in order)
└───────────────────┘
        │
        ├──► InheritedProperties
        ├──► CommandState
        ├──► Aspect
        ├──► InheritedProperties (work added by CommandState/Aspect)
        ├──► Measure, then Arrange
        ├──► RenderCache: rebuild invalidated element render caches
        ├──► HitTest: rebuild the input route cache if needed
        │
        └──► no queued work: count a no-work frame and stop
             (with active Motion: Motion hooks + Measure, Arrange,
              RenderCache, HitTest only)
```

## Required Behavior

- The game loop may call update and draw every frame.
- An unchanged UI tree must not re-measure.
- An unchanged UI tree must not re-arrange.
- An unchanged UI tree must not regenerate render commands.
- The draw step reuses the root command list committed during update.
- A failed phase leaves the element queued with its dirty flag set.
- `FrameBudget` does not defer work.
