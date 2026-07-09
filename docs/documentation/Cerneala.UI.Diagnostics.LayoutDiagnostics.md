# LayoutDiagnostics Class

## Definition
Namespace: `Cerneala.UI.Diagnostics`

Assembly/Project: `Cerneala`

Source: `UI/Diagnostics/LayoutDiagnostics.cs`

Captures the current layout diagnostic state of a `UIElement`.

```csharp
public static class LayoutDiagnostics
```

Inheritance:
`object` -> `LayoutDiagnostics`

## Examples

```csharp
using System;
using Cerneala.UI.Diagnostics;
using Cerneala.UI.Elements;

UIElement element = new();

LayoutDiagnosticsSnapshot snapshot = LayoutDiagnostics.Capture(element);

Console.WriteLine(snapshot.ElementType);
Console.WriteLine(snapshot);
```

## Remarks

`LayoutDiagnostics` is a static helper for reading layout-related state from an element without mutating it. `Capture` copies the element identity, runtime type name, current desired size, arranged bounds, layout version, visibility, and the most recent measure/arrange inputs recorded by the element.

The returned `LayoutDiagnosticsSnapshot` is a point-in-time value. It does not stay connected to the source element after capture.

`Capture` throws `ArgumentNullException` when `element` is `null`.

## Methods

| Name | Description |
| --- | --- |
| `Capture(UIElement element)` | Creates a `LayoutDiagnosticsSnapshot` from the supplied element's current layout state. |

## Return Snapshot

`Capture` returns `LayoutDiagnosticsSnapshot`.

```csharp
public sealed record LayoutDiagnosticsSnapshot(
    string? ElementId,
    string ElementType,
    LayoutSize DesiredSize,
    LayoutRect ArrangedBounds,
    int LayoutVersion,
    Visibility Visibility,
    LayoutSize? LastMeasureAvailableSize,
    int LastMeasureLayoutVersion,
    LayoutRect? LastArrangeFinalRect,
    int LastArrangeLayoutVersion)
```

### Snapshot Properties

| Name | Type | Description |
| --- | --- | --- |
| `ElementId` | `string?` | The element identifier converted to text, or `null` when the element has no identifier. |
| `ElementType` | `string` | The runtime type name of the captured element. |
| `DesiredSize` | `LayoutSize` | The element's current `DesiredSize`. |
| `ArrangedBounds` | `LayoutRect` | The element's current `ArrangedBounds`. |
| `LayoutVersion` | `int` | The element's current layout version. |
| `Visibility` | `Visibility` | The element's current layout visibility value. |
| `LastMeasureAvailableSize` | `LayoutSize?` | The last measure available size recorded by the element, or `null` before measure data is recorded. |
| `LastMeasureLayoutVersion` | `int` | The layout version associated with the last recorded measure pass. The source element initializes this value to `-1`. |
| `LastArrangeFinalRect` | `LayoutRect?` | The last arrange final rectangle recorded by the element, or `null` before arrange data is recorded. |
| `LastArrangeLayoutVersion` | `int` | The layout version associated with the last recorded arrange pass. The source element initializes this value to `-1`. |

### Snapshot Methods

| Name | Description |
| --- | --- |
| `ToString()` | Formats the element type, element id or `unattached`, desired size, arranged bounds, layout version, and visibility using invariant culture. |

## Applies to

Cerneala UI layout diagnostics in the `Cerneala` project.

## See also

- `Cerneala.UI.Elements.UIElement`
- `Cerneala.UI.Layout.LayoutSize`
- `Cerneala.UI.Layout.LayoutRect`
- `Cerneala.UI.Layout.Visibility`
