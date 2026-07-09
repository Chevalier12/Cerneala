# RoutedEventTraceStep Class

## Definition
Namespace: `Cerneala.UI.Diagnostics`

Assembly/Project: `Cerneala`

Source: `UI/Diagnostics/RoutedEventTrace.cs`

Represents one element visited by a routed event diagnostic trace.

```csharp
public sealed record RoutedEventTraceStep(string? ElementId, string ElementType)
```

Inheritance:
`object` -> `RoutedEventTraceStep`

## Examples

Create a trace step directly and format it for diagnostic output:

```csharp
using Cerneala.UI.Diagnostics;

RoutedEventTraceStep step = new("42", "Button");

string text = step.ToString();
// "Button#42"
```

When the element id is not available, the formatted value uses `unattached`:

```csharp
RoutedEventTraceStep step = new(null, "UIElement");

string text = step.ToString();
// "UIElement#unattached"
```

`RoutedEventTrace.Trace` creates steps from the routed event route:

```csharp
using Cerneala.UI.Diagnostics;
using Cerneala.UI.Elements;
using Cerneala.UI.Input;

UIRoot root = new();
UIElement child = new();
root.VisualChildren.Add(child);

RoutedEventTraceSnapshot snapshot = RoutedEventTrace.Trace(child, InputEvents.MouseDownEvent);
RoutedEventTraceStep firstStep = snapshot.Steps[0];
```

## Remarks

`RoutedEventTraceStep` is a small immutable diagnostic record used by `RoutedEventTraceSnapshot.Steps`.
Each step stores the traced element id as text and the element type name captured while building the route.

`RoutedEventTrace.Trace` creates each step with `element.ElementId?.ToString()` and `element.GetType().Name`.
The route order depends on the routed event strategy: bubble traces run from target to root, tunnel traces run from root to target, and direct traces contain only the target.
Disabled ancestors are skipped by the trace builder.

`ToString()` returns `ElementType#ElementId`. If `ElementId` is `null`, it returns `ElementType#unattached`.

Because this type is a C# record, it also has record value equality and compiler-generated members for the primary constructor parameters.

## Constructors

| Name | Description |
| --- | --- |
| `RoutedEventTraceStep(string? ElementId, string ElementType)` | Initializes a trace step with the optional element id text and the element type name. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ElementId` | `string?` | The traced element id converted to text, or `null` when no id is available. |
| `ElementType` | `string` | The element type name captured for the traced route step. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `ToString()` | `string` | Formats the step as `ElementType#ElementId`, using `unattached` when `ElementId` is `null`. |

## Applies to

Cerneala UI routed event diagnostics.

## See also

- `Cerneala.UI.Diagnostics.RoutedEventTrace`
- `Cerneala.UI.Diagnostics.RoutedEventTraceSnapshot`
- `Cerneala.UI.Input.RoutedEvent`
- `Cerneala.UI.Input.RoutingStrategy`
