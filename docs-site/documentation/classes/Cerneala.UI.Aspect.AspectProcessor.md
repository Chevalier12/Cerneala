# AspectProcessor Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectProcessor.cs`

Coordinates root-level aspect processing for `UIElement` instances by building the current aspect catalog, synchronizing token defaults, and delegating application and cleanup to an `AspectEngine`.

```csharp
public sealed class AspectProcessor
```

Inheritance:
`object` -> `AspectProcessor`

## Examples

Use the canonical processor exposed by a `UIRoot`:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;

UIRoot root = new();
Button button = new();

root.LogicalChildren.Add(button);
root.AspectProcessor.Process(button);

AspectDiagnostics.Snapshot diagnostics =
    root.AspectProcessor.Engine.GetDiagnostics(button);
```

Clear aspect state when manually coordinating element cleanup:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;

UIRoot root = new();
Button button = new();

root.LogicalChildren.Add(button);
root.AspectProcessor.Process(button);
root.AspectProcessor.Clear(button);
```

## Remarks

The processor is root-owned. `Process`, `Clear`, and environment synchronization verify `UIRoot.Relay` before reading or mutating retained Aspect state.

`AspectProcessor` is created by `UIRoot` and exposed through `UIRoot.AspectProcessor`. The root also wires `AspectProcessor.Process` into the aspect phase of its `UiFrameScheduler`, so normal frame processing uses this class rather than calling `AspectEngine` directly.

Each `Process` call builds an `AspectCatalog` from the root's `AspectRegistry`. When the catalog or active `Theme` changes, the processor rebuilds the effective token values from catalog defaults, overlays the semantic colors and brushes projected by `ThemeTokenBridge`, and publishes the changes through its stable runtime environment. Component-template token bindings share that environment and therefore observe the same updates.

The engine receives the target element, current catalog, synchronized environment, root theme provider, and the element's `AspectVariantSet` when the element is a `Control`; non-control elements are processed with `AspectVariantSet.Empty`. It also receives an `AspectDataContext` built from the element's current `DataContext`, so `AspectCondition.Data` works on the root frame-processing path.

`Clear` delegates to `AspectEngine.Clear`. Element lifecycle cleanup calls this during detach, which removes previously applied aspect-base values and clears the engine's tracked diagnostics and dependencies for that element.

The `Engine` property exposes the owned `AspectEngine` for diagnostics, dependency inspection, and low-level aspect operations. Mutating the engine directly can bypass the root-level catalog and token-default synchronization performed by `Process`.

## Constructors

| Name | Description |
| --- | --- |
| `AspectProcessor(UIRoot root)` | Initializes a processor bound to the specified root. Throws `ArgumentNullException` when `root` is `null`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Engine` | `AspectEngine` | Gets the processor-owned engine used to apply, clear, and inspect aspect state. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Process(UIElement element)` | `void` | Applies the current root aspect registry and theme context to `element`. Throws `ArgumentNullException` when `element` is `null`. |
| `Clear(UIElement element)` | `void` | Clears aspect-base values and tracked aspect state for `element` through the owned engine. |

## Applies to

Cerneala UI root-level aspect processing for elements attached to a `UIRoot`.

## See also

- `AspectEngine`
- `AspectCatalog`
- `AspectRegistry`
- `AspectEnvironment`
- `UIRoot`
- `Control.AspectVariants`
