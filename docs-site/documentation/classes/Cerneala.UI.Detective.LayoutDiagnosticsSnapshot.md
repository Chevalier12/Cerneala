# LayoutDiagnosticsSnapshot Class

## Definition
Namespace: `Cerneala.UI.Detective`
Assembly/Project: `Cerneala`
Source: `UI/Detective/LayoutDiagnostics.cs`

Represents an immutable diagnostic snapshot of a UI element's current layout state.

```csharp
public sealed record LayoutDiagnosticsSnapshot(
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

Inheritance:
`Object` -> `LayoutDiagnosticsSnapshot`

Implements:
`IEquatable<LayoutDiagnosticsSnapshot>`

## Examples

```csharp
using Cerneala.UI.Detective;
using Cerneala.UI.Layout;

var snapshot = new LayoutDiagnosticsSnapshot(
    ElementType: "Button",
    DesiredSize: new LayoutSize(96, 32),
    ArrangedBounds: new LayoutRect(12, 8, 96, 32),
    LayoutVersion: 4,
    Visibility: Visibility.Visible,
    LastMeasureAvailableSize: new LayoutSize(200, 100),
    LastMeasureLayoutVersion: 4,
    LastArrangeFinalRect: new LayoutRect(12, 8, 96, 32),
    LastArrangeLayoutVersion: 4);

string summary = snapshot.ToString();
```

## Remarks

`LayoutDiagnosticsSnapshot` stores the layout values captured from a `UIElement`: element type name, desired size, arranged bounds, layout version, visibility, and the most recent measure and arrange inputs with their layout versions.

Use `LayoutDiagnostics.Capture(UIElement)` when the values should come from a live element. The record constructor is useful when tests or diagnostics need to create a known snapshot directly.

`ToString()` returns a short invariant-culture summary starting with the element type (for example `Button`), followed by desired size, arranged bounds, layout version, and visibility.

## Constructors

| Name | Description |
| --- | --- |
| `LayoutDiagnosticsSnapshot(string, LayoutSize, LayoutRect, int, Visibility, LayoutSize?, int, LayoutRect?, int)` | Initializes a layout diagnostic snapshot with the element type, current layout values, and last measure/arrange diagnostic values. |

## Properties

| Name | Description |
| --- | --- |
| `ElementType` | Gets the captured runtime type name of the element. |
| `DesiredSize` | Gets the element's desired size at capture time. |
| `ArrangedBounds` | Gets the element's arranged bounds at capture time. |
| `LayoutVersion` | Gets the element layout version at capture time. |
| `Visibility` | Gets the element visibility at capture time. |
| `LastMeasureAvailableSize` | Gets the last available size passed to measure, or `null` when no value is recorded. |
| `LastMeasureLayoutVersion` | Gets the layout version associated with the last recorded measure. |
| `LastArrangeFinalRect` | Gets the last final rectangle passed to arrange, or `null` when no value is recorded. |
| `LastArrangeLayoutVersion` | Gets the layout version associated with the last recorded arrange. |

## Methods

| Name | Description |
| --- | --- |
| `ToString()` | Returns a compact invariant-culture diagnostic summary for the snapshot. |

## Applies to

Project: `Cerneala`

## See also

- Source: `UI/Detective/LayoutDiagnostics.cs`
- `LayoutDiagnostics`
- `LayoutSize`
- `LayoutRect`
- `Visibility`
