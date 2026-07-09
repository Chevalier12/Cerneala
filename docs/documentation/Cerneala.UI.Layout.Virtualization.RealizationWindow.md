# RealizationWindow Struct

## Definition
Namespace: `Cerneala.UI.Layout.Virtualization`

Assembly/Project: `Cerneala`

Source: `UI/Layout/Virtualization/RealizationWindow.cs`

Represents the half-open item index range that should be realized by a virtualized layout.

```csharp
public readonly record struct RealizationWindow(int StartIndex, int EndIndexExclusive)
```

Inheritance:
`ValueType` -> `RealizationWindow`

## Examples

Create a realization window from requested bounds and an item count.

```csharp
using Cerneala.UI.Layout.Virtualization;

RealizationWindow window = RealizationWindow.Create(
    itemCount: 100,
    startIndex: 20,
    endIndexExclusive: 35);

if (window.Contains(24))
{
    RealizeItem(24);
}
```

## Remarks

`RealizationWindow` stores a half-open interval: `StartIndex` is included and `EndIndexExclusive` is excluded.

`Create` clamps the requested range to the available item count. If the item count is zero or negative, it returns `Empty`.

`Count` never returns a negative value. `IsEmpty` is true when `Count` is zero.

## Constructors

| Name | Description |
| --- | --- |
| `RealizationWindow(int, int)` | Initializes a realization window with a start index and an exclusive end index. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Count` | `int` | Gets the non-negative number of indices in the window. |
| `Empty` | `RealizationWindow` | Gets an empty realization window. |
| `EndIndexExclusive` | `int` | Gets the exclusive end index. |
| `IsEmpty` | `bool` | Gets whether the window contains no indices. |
| `StartIndex` | `int` | Gets the inclusive start index. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Contains(int)` | `bool` | Returns whether an index is inside the half-open window. |
| `Create(int, int, int)` | `RealizationWindow` | Creates a window clamped to the supplied item count. |

## Applies to

- `Cerneala.UI.Layout.Virtualization.RealizationWindow`

## See also

- `Cerneala.UI.Layout.Virtualization.VirtualizationContext`
- `Cerneala.UI.Layout.Panels.VirtualizingStackPanel`
