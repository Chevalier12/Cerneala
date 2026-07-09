# RenderCacheDumper Class

## Definition
Namespace: `Cerneala.UI.Diagnostics`

Assembly/Project: `Cerneala`

Source: `UI/Diagnostics/RenderCacheDumper.cs`

Creates a textual diagnostic dump of a `UIRoot` retained render cache and the per-element render cache snapshots for a selected element tree role.

```csharp
public sealed class RenderCacheDumper
```

Inheritance:
`Object` -> `RenderCacheDumper`

## Examples
Dump the retained render cache for a root after a frame has been processed.

```csharp
using System;
using Cerneala.UI.Diagnostics;
using Cerneala.UI.Elements;

UIRoot root = new();
root.ProcessFrame();

string dump = new RenderCacheDumper().Dump(root);
Console.WriteLine(dump);
```

The returned text starts with `Render cache`, then includes a root cache line and one line for each element returned by `ElementTreeWalker.PreOrder(root, role)`.

## Remarks
`RenderCacheDumper` is a diagnostics helper. It does not rebuild render caches and does not mutate the UI tree; it formats the current retained render cache state exposed by `UIRoot.RetainedRenderCache`.

`Dump` first captures root cache state with `RenderDiagnostics.CaptureRoot`, then walks the selected tree in pre-order and captures each element with `RenderDiagnostics.CaptureElement`. The default traversal role is `ElementChildRole.Visual`; pass `ElementChildRole.Logical` to inspect the logical tree instead.

Each element line is produced from `ElementRenderDiagnosticsSnapshot.ToString()` and includes the element type/id, whether the element cache is valid or stale, render versions, command count, content bounds, and render dependencies. The root line is produced from `RootRenderDiagnosticsSnapshot.ToString()` and includes root validity, cache version, and root command count.

## Constructors
| Name | Description |
| --- | --- |
| `RenderCacheDumper()` | Initializes a new instance of the `RenderCacheDumper` class. |

## Methods
| Name | Description |
| --- | --- |
| `Dump(UIRoot root, ElementChildRole role = ElementChildRole.Visual)` | Returns a trimmed, multi-line render cache dump for `root` and every element in pre-order traversal for `role`. Throws `ArgumentNullException` when `root` is `null`. |

## Applies to
`Cerneala` UI diagnostics for retained rendering.

## See also
- `RenderDiagnostics`
- `RootRenderDiagnosticsSnapshot`
- `ElementRenderDiagnosticsSnapshot`
- `UIRoot.RetainedRenderCache`
- `ElementTreeWalker`
