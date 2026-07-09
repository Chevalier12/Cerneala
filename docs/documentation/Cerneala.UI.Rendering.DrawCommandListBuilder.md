# DrawCommandListBuilder Class

## Definition
Namespace: `Cerneala.UI.Rendering`

Assembly/Project: `Cerneala`

Source: `UI/Rendering/DrawCommandListBuilder.cs`

Builds the retained root `DrawCommandList` for a UI subtree from cached element render commands.

```csharp
public sealed class DrawCommandListBuilder
```

Inheritance:
`Object` -> `DrawCommandListBuilder`

## Examples

Build the root command list for a `UIRoot` after layout and local render caches have been prepared:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Elements;
using Cerneala.UI.Rendering;

UIRoot root = new(viewportWidth: 800, viewportHeight: 600);
RetainedRenderCache cache = root.RetainedRenderCache;
RenderCounters counters = root.RenderCounters;

root.RenderQueueProcessor.Process(root);

DrawCommandListBuilder builder = new();
builder.Build(root, cache, counters);

DrawCommandList commands = cache.RootCommands;
```

Most application code uses `RetainedRenderer.Commit`, which calls `DrawCommandListBuilder.Build` when the root cache is invalid:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Elements;

UIRoot root = new();

DrawCommandList commands = root.RetainedRenderer.Commit(root);
```

## Remarks

`DrawCommandListBuilder` composes a single root command stream from the per-element command lists stored in `RetainedRenderCache`. `Build` clears `renderCache.RootCommands`, appends commands for the supplied root subtree, and then marks the root cache as built.

Composition walks the visible visual tree depth-first. An element's own local commands are emitted before its visual children, and siblings are emitted in `VisualChildren` order. Elements that do not participate in rendering, according to `UIElementVisibility.ParticipatesInRendering`, are skipped with their subtree.

The builder applies render-scope state while composing commands. It combines ancestor and element transforms, applies `Opacity` and `PresenceOpacity` to command colors, and includes presence scale in the element transform. Rectangular commands are transformed to the axis-aligned bounds of their transformed corners.

Clips are emitted as balanced `PushClip` and `PopClip` commands. A clip comes from `ClipNode` when one is attached to the element, otherwise from `ArrangedBounds` when `ClipToBounds` is true. Clip commands wrap the element's local commands, visual children, and exiting visual children.

When the element belongs to a `UIRoot`, exiting presence children returned by `root.Motion.Presence.GetExitingVisualChildren(element)` are also composed so exit animations can continue rendering after normal visual removal.

`Build` expects local element render caches to already be valid. If a required local cache is stale, `ElementRenderCache.GetValidCommands` throws `InvalidOperationException`. The builder can reuse valid cached commands by translating them when only the element's arranged position changed and its cached size and render dependencies still match.

## Constructors

| Name | Description |
| --- | --- |
| `DrawCommandListBuilder()` | Initializes a draw command list builder. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Build(UIElement, RetainedRenderCache, RenderCounters)` | `void` | Rebuilds `renderCache.RootCommands` for the supplied root element subtree and records composition counters. Throws `ArgumentNullException` when `root`, `renderCache`, or `counters` is null. |

## Applies To

Cerneala retained UI rendering, root command-list composition, clipping, transform composition, opacity composition, and presence exit rendering.

## See Also

- `Cerneala.Drawing.DrawCommandList`
- `Cerneala.UI.Rendering.RetainedRenderer`
- `Cerneala.UI.Rendering.RetainedRenderCache`
- `Cerneala.UI.Rendering.ElementRenderCache`
- `Cerneala.UI.Rendering.RenderQueueProcessor`
