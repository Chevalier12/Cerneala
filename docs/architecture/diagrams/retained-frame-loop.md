# Retained Frame Loop

This diagram shows how retained UI work flows through the game loop.

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
│  Read InputFrame  │         │ Get cached root   │
│  from IInputSource│         │ DrawCommandList   │
└───────────────────┘         └───────────────────┘
        │                             │
        ▼                             ▼
┌───────────────────┐         ┌───────────────────┐
│ Hit test / focus  │         │ IDrawingBackend   │
│ command routing   │         │ Render(commands)  │
└───────────────────┘         └───────────────────┘
        │
        ▼
┌───────────────────┐
│ State changes     │
│ properties/style  │
│ resources/input   │
└───────────────────┘
        │
        ▼
┌───────────────────────────────────────────────────────────────┐
│                       Invalidation                            │
├───────────────────────────────────────────────────────────────┤
│ Measure dirty | Arrange dirty | Render dirty | HitTest dirty  │
└───────────────────────────────────────────────────────────────┘
        │
        ▼
┌───────────────────┐
│ LayoutQueue       │
│ RenderQueue       │
│ HitTestQueue      │
└───────────────────┘
        │
        ▼
┌───────────────────┐
│ UiFrameScheduler  │
└───────────────────┘
        │
        ├──────────────► process measure/arrange if layout dirty
        │
        ├──────────────► rebuild subtree render caches if render dirty
        │
        ├──────────────► rebuild hit-test data if hit-test dirty
        │
        └──────────────► no-op if nothing is dirty
```

## Required Behavior

- The game loop may call update and draw every frame.
- An unchanged UI tree must not re-measure.
- An unchanged UI tree must not re-arrange.
- An unchanged UI tree must not regenerate render commands.
- The draw step may reuse the cached root command list.
- Dirty flags must clear only after the corresponding phase succeeds.
- `FrameBudget` does not defer work in MVP.
