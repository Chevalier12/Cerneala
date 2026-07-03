# Retained Rendering Cache

## Purpose

Defines retained rendering, local element render caches, root command-list cache composition, render dependency tracking, clip command emission, and cache diagnostics for Cerneala.
## Requirements
### Requirement: Render context records local drawing commands
Cerneala SHALL provide a retained render context that lets retained elements record local visual commands through the existing drawing command layer.

#### Scenario: Render context exposes drawing context
- **WHEN** an element render hook runs
- **THEN** the hook receives a `RenderContext` that exposes a `DrawingContext`

#### Scenario: Render context exposes layout bounds
- **WHEN** an element render hook runs after layout
- **THEN** the render context exposes the element's arranged layout bounds

#### Scenario: Render context remains backend-neutral
- **WHEN** retained rendering records commands
- **THEN** `UI/Rendering` does not depend on MonoGame, Skia, HarfBuzz, `Texture2D`, `SpriteBatch`, or concrete drawing backends

### Requirement: Retained elements can render local content
Cerneala SHALL allow retained elements to render local content without owning child traversal.

#### Scenario: Element render hook records local commands
- **WHEN** a renderable retained element is render-dirty
- **THEN** its local render hook can record commands into its element render cache

#### Scenario: Local render hook does not compose children
- **WHEN** an element render hook completes
- **THEN** child command composition remains owned by the retained renderer

### Requirement: Element render cache stores local commands
Cerneala SHALL provide per-element render caches for local drawing commands and render dependency state.

#### Scenario: Dirty element cache rebuilds
- **WHEN** an element is queued for render-cache processing
- **THEN** its local `ElementRenderCache` is rebuilt from that element's render hook

#### Scenario: Unchanged element cache is reused
- **WHEN** an element's render state and render dependencies are unchanged
- **THEN** its local command list is reused instead of regenerated

#### Scenario: Sibling cache is not rebuilt by child change
- **WHEN** one child is render-dirty
- **THEN** unrelated sibling local command lists are not regenerated

### Requirement: Retained render cache stores root command list
Cerneala SHALL maintain a retained root or subtree render cache that can provide a cached root `DrawCommandList` to a drawing backend.

#### Scenario: Root command list is built from local caches
- **WHEN** retained rendering composes a root
- **THEN** the root command list is built from cached local element command lists

#### Scenario: Unchanged root command list is reused across draw frames
- **WHEN** no retained render state changes between draw frames
- **THEN** the same cached root command list can be submitted to `IDrawingBackend.Render`

#### Scenario: Root cache version changes after render update
- **WHEN** a local element render cache changes
- **THEN** the retained root render cache version changes

### Requirement: Draw command list pooling is deferred from correctness
Cerneala SHALL keep `DrawCommandListPool` out of the retained rendering correctness path until profiling justifies a dedicated pooling contract.

#### Scenario: Retained rendering works without command-list pooling
- **WHEN** retained rendering rebuilds local element caches or the root command list
- **THEN** no production rendering path requires a `DrawCommandListPool`

#### Scenario: DrawCommandListPool is deferred
- **WHEN** the retained rendering cache MVP is implemented without a command-list pool
- **THEN** the OpenSpec design records that `DrawCommandListPool is deferred`

### Requirement: Render queue processor handles dirty render work
Cerneala SHALL provide a render queue processor that updates retained render caches for queued elements.

#### Scenario: Render queue processor rebuilds queued elements
- **WHEN** the frame scheduler processes render-cache work
- **THEN** queued render-dirty elements are processed by the retained render queue processor

#### Scenario: Render queue processor preserves failure behavior
- **WHEN** rebuilding an element render cache fails
- **THEN** the matching render dirty flag and queued work remain available for retry or diagnostics

### Requirement: Draw command list builder composes visual order
Cerneala SHALL compose retained render caches into drawing commands using deterministic retained visual order.

#### Scenario: Parent renders before visual children
- **WHEN** retained commands are composed for a subtree
- **THEN** the parent local commands appear before descendant commands

#### Scenario: Siblings render in visual child order
- **WHEN** sibling retained elements are composed
- **THEN** command order follows retained visual child order

#### Scenario: Collapsed elements do not emit visible commands
- **WHEN** an element is collapsed
- **THEN** its local commands and subtree commands are excluded from the root command list

### Requirement: Clip commands are balanced
Cerneala SHALL provide retained clip metadata that is translated to balanced drawing clip commands.

#### Scenario: Clipped subtree emits push and pop
- **WHEN** a subtree has active clip metadata
- **THEN** composition emits a matching `PushClip` before the subtree and `PopClip` after it

#### Scenario: Empty clipped subtree remains balanced
- **WHEN** a clipped subtree has no visible local commands
- **THEN** the emitted clip commands remain balanced

### Requirement: Render dependencies invalidate cached commands
Cerneala SHALL track render dependencies that affect cached drawing commands.

#### Scenario: Text dependency change invalidates render cache
- **WHEN** an element's text render dependency changes
- **THEN** its local render cache is considered stale

#### Scenario: Image dependency change invalidates render cache
- **WHEN** an element's image render dependency changes
- **THEN** its local render cache is considered stale

#### Scenario: Resource dependency change invalidates render cache
- **WHEN** an element's theme or resource render dependency changes
- **THEN** its local render cache is considered stale

### Requirement: Render counters report cache behavior
Cerneala SHALL provide render counters for cache diagnostics and tests.

#### Scenario: Counter records local cache rebuild
- **WHEN** an element local command list is regenerated
- **THEN** render counters record a cache miss or rebuild

#### Scenario: Counter records cache reuse
- **WHEN** an unchanged element cache is reused
- **THEN** render counters record a cache hit or reuse

### Requirement: Retained rendering is tested
Cerneala SHALL include focused tests for retained rendering cache behavior.

#### Scenario: Required rendering tests exist
- **WHEN** this implementation phase is complete
- **THEN** tests exist under `tests/Cerneala.Tests/UI/Rendering` for element caches, root caches, render queue processing, retained renderer composition, draw-command building, dependencies, and counters

#### Scenario: Full tests pass
- **WHEN** this implementation phase is complete
- **THEN** `dotnet test` passes

### Requirement: Host draw submits retained root cache
Cerneala SHALL expose retained rendering cache submission through `UiHost.Draw(...)`.

#### Scenario: Host draw uses retained renderer
- **WHEN** `UiHost.Draw(...)` is called
- **THEN** it submits the retained root command cache through the root retained renderer and the provided `IDrawingBackend`

#### Scenario: Cached root commands survive unchanged frames
- **WHEN** host update and draw run repeatedly without retained invalidation
- **THEN** the retained root command cache remains reusable across draw frames

#### Scenario: Host draw keeps backend-neutral cache contract
- **WHEN** cached retained commands are submitted by the host
- **THEN** the submission remains expressed as `DrawCommandList` rendered by `IDrawingBackend`

### Requirement: Controls render through retained render cache
Cerneala SHALL render retained controls by rebuilding only dirty local element command lists and composing them through existing retained render caches.

#### Scenario: Control render hook emits local commands
- **WHEN** a control is render-dirty
- **THEN** its render hook emits local drawing commands into its element render cache

#### Scenario: Unchanged control render cache is reused
- **WHEN** a control has unchanged render-affecting state across frames
- **THEN** its local render command list is reused

#### Scenario: Control child composition remains retained renderer owned
- **WHEN** a control renders local visuals
- **THEN** child command composition remains owned by the retained renderer

### Requirement: Retained rendering tracks text layout dependencies
Cerneala SHALL include text layout dependency identity in retained render cache staleness checks for text-rendering elements.

#### Scenario: Unchanged text dependency reuses render cache
- **WHEN** a text-rendering element has unchanged render version and unchanged text layout dependency identity
- **THEN** its retained local render command cache can be reused

#### Scenario: Text metrics dependency invalidates render cache
- **WHEN** text content, resolved font identity, font size, wrapping width, wrapping mode, trimming mode, or scale changes
- **THEN** the text-rendering element's local render command cache is considered stale

#### Scenario: Foreground-only change avoids metrics invalidation
- **WHEN** only text foreground color changes
- **THEN** retained rendering invalidates visible render output without changing the text layout dependency identity

### Requirement: Retained rendering tracks resource dependency versions
Cerneala SHALL include resource dependency versions in retained render cache staleness checks.

#### Scenario: Resource dependency change invalidates local cache
- **WHEN** a retained element depends on a resource and that resource version changes
- **THEN** the element local render command cache is considered stale

#### Scenario: Unchanged resource dependency reuses cache
- **WHEN** a retained element has unchanged render state and unchanged resource dependency version
- **THEN** its retained local render command cache can be reused

#### Scenario: Resource render dependency remains backend-neutral
- **WHEN** retained rendering tracks resource dependencies
- **THEN** `UI/Rendering` stores only resource dependency identity or versions, not backend resource objects

### Requirement: Retained rendering composes advanced media commands
Cerneala SHALL compose advanced media and shape drawing commands through existing retained render caches and `DrawCommandList`.

#### Scenario: Shape local cache emits advanced commands
- **WHEN** a shape control is render-dirty
- **THEN** its local retained render cache records shape drawing commands without composing child visuals

#### Scenario: Advanced commands preserve retained visual order
- **WHEN** advanced shape commands are composed into the root command list
- **THEN** their order follows the existing parent-before-children and sibling visual order rules

#### Scenario: Advanced media commands remain backend-neutral in rendering
- **WHEN** retained rendering composes advanced media output
- **THEN** `UI/Rendering` does not reference MonoGame, Skia, HarfBuzz, `Texture2D`, `SpriteBatch`, or concrete drawing backends

