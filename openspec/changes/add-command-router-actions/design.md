## Context

Cerneala already has low-level command primitives (`ICommand`, `RoutedCommand`, `CommandBinding`, `CommandEvents`) and retained input routing through `ElementInputBridge`. `RoutedCommand` currently cannot execute because no retained command route exists yet. `ButtonBase` also has pressed visual state but no command properties or click-to-command behavior.

This change connects those pieces with an explicit retained command router. The design intentionally avoids WPF-style global `CommandManager` requery behavior because Cerneala's retained UI is invalidation-driven and should make command state refreshes visible and testable.

## Goals / Non-Goals

**Goals:**

- Route `CanExecute` and `Execute` through retained visual/logical routes using explicit APIs.
- Let retained elements own command bindings in a collection that can be exported into route dispatch.
- Make `RoutedCommand` usable through `CommandRouter` without hidden global state.
- Provide `ActionCommand` for direct delegate-backed command usage.
- Add `ButtonBase.Command` and `ButtonBase.CommandParameter` typed properties.
- Trigger button commands from retained click behavior.
- Update `ROADMAPv2.md` checkboxes during implementation so progress stays resumable.

**Non-Goals:**

- No global `CommandManager`.
- No automatic app-wide requery pulse.
- No keyboard gesture/input binding system unless implementation exposes a hard MVP blocker.
- No full `Button` visual control; this phase only extends the existing `ButtonBase` primitive.
- No markup or string-based command lookup.

## Decisions

### Use `CommandRouter` as the explicit command execution service

`CommandRouter` will own the retained command route behavior. Callers pass a `RoutedCommandContext` containing command, command target, parameter, and route map. The router raises preview/bubble `CanExecute` or `Executed` command events using the retained route infrastructure.

Alternative considered: make `RoutedCommand.Execute` search globally. That recreates WPF's implicit magic and makes tests depend on ambient state, so the routed behavior stays in `CommandRouter`.

### Keep `ICommand` direct and make `RoutedCommand` route-aware only through context

`ActionCommand` will implement `ICommand` directly and can be called without retained routing. `RoutedCommand` will continue to represent commands that need a route; direct `Execute(object?)` cannot know the target route, so routed execution must use `CommandRouter.Execute`.

Alternative considered: store a router singleton inside `RoutedCommand`. That would hide dependencies and fight Cerneala's explicit host/service model.

### Store command bindings on retained elements

`UIElement` will expose a `CommandBindings` collection. The route builder or router can inspect retained route elements and invoke bindings in order. The collection keeps behavior near the element that owns it and avoids duplicating command handlers in generic routed event handler stores.

Alternative considered: only use existing routed event handlers. That works mechanically, but it makes command ownership awkward and loses the familiar command-binding API promised by the roadmap.

### Button command execution flows through retained input click behavior

`ElementInputBridge` already owns click synthesis. When a left-button click is produced for a `ButtonBase`, the bridge or a small command invoker will query `CanExecute` and execute the assigned command. If the command cannot execute, the button remains non-executing.

Alternative considered: make `ButtonBase` listen to its own mouse events. That couples the primitive to route setup details and is harder to test than using the bridge's existing click point.

### Command state invalidation is explicit

Button command state refreshes will be triggered during input/update or by explicit invalidation hooks. There is no hidden global requery. If command state changes affect enabled/visual state, the affected element invalidates input visual/render state through existing typed-property metadata.

Alternative considered: poll every frame. That violates the retained no-work-frame direction and makes command state updates expensive for large UI trees.

## Risks / Trade-offs

- `RoutedCommand.Execute(object?)` still cannot infer a retained route by itself -> mitigation: tests and docs require `CommandRouter.Execute` for routed commands.
- Command binding order can become subtle with separate logical and visual trees -> mitigation: MVP uses the same retained route order as input routing and tests parent/child precedence.
- Button command execution might duplicate click behavior later when full `Button` exists -> mitigation: keep behavior in `ButtonBase`, the intended primitive base.
- Command state refresh without global requery requires explicit invalidation by callers -> mitigation: expose focused APIs and tests for command state refresh rather than background polling.
