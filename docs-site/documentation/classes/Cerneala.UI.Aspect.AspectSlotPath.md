# AspectSlotPath Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectSlotPath.cs`

Stores the current aspect slot together with an optional diagnostic path label for aspect matching, dependency tracking, and trace output.

```csharp
public sealed class AspectSlotPath
```

Inheritance:
`object` -> `AspectSlotPath`

## Examples

Create a slot path with a diagnostic path:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectSlot<Button, ContentPresenter> slot =
    AspectSlot.For<Button, ContentPresenter>("Content");

AspectSlotPath path = new(slot, "Root/Content");

Console.WriteLine(path.Slot);           // Button.Content
Console.WriteLine(path.DiagnosticPath); // Root/Content
Console.WriteLine(path);                // Button.Content (Root/Content)
```

Pass a slot path into an aspect match context:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

Button button = new();
AspectSlot<Button, Button> rootSlot = AspectSlot.Root<Button>();
AspectSlotPath rootPath = new(rootSlot, "Root");

AspectMatchContext context = new(button, slotPath: rootPath);

bool matchesRootSlot = new AspectTarget(typeof(Button), rootSlot)
    .Matches(context);
```

## Remarks

`AspectSlotPath` is an immutable holder for the slot currently being resolved. `AspectMatchContext` exposes it through `SlotPath`, and `AspectTarget.Matches` compares `SlotPath?.Slot` with the target slot for slot-specific aspect rules.

The constructor requires a non-null `AspectSlot`. The optional `diagnosticPath` is stored only when it contains non-whitespace text; `null`, empty, and whitespace-only values are normalized to `null`.

`AspectEngine.Resolve` includes `SlotPath?.Slot` in the produced `AspectDependencySet`, so slot-targeted resolution can be tracked as an aspect dependency. Diagnostics and tests use `DiagnosticPath` through `ToString()` to produce labels such as `Button.Content (Root/Content)`.

`ToString()` returns the slot's own string form when no diagnostic path is available. When `DiagnosticPath` is present, it appends the path in parentheses.

## Constructors

| Name | Description |
| --- | --- |
| `AspectSlotPath(AspectSlot slot, string? diagnosticPath = null)` | Initializes a path for `slot` and an optional diagnostic path label. Throws `ArgumentNullException` when `slot` is `null`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `DiagnosticPath` | `string?` | Gets the optional human-readable path used in diagnostics, or `null` when the constructor receives no non-whitespace value. |
| `Slot` | `AspectSlot` | Gets the slot represented by this path. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `ToString()` | `string` | Returns `Slot.ToString()` when `DiagnosticPath` is `null`; otherwise returns the slot string followed by the diagnostic path in parentheses. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `AspectSlotPath(AspectSlot slot, string? diagnosticPath = null)` | `ArgumentNullException` | `slot` is `null`. |

## Applies to

Cerneala UI aspect slot matching, aspect dependency tracking, and aspect diagnostics.

## See also

- `Cerneala.UI.Aspect.AspectSlot`
- `Cerneala.UI.Aspect.AspectMatchContext`
- `Cerneala.UI.Aspect.AspectTarget`
- `Cerneala.UI.Aspect.AspectEngine`
