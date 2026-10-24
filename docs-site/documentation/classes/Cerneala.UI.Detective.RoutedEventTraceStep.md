# RoutedEventTraceStep Class

## Definition
Namespace: `Cerneala.UI.Detective`

Assembly/Project: `Cerneala`

Source: `UI/Detective/RoutedEventTrace.cs`

Represents one element visited by a routed event diagnostic trace.

```csharp
public sealed record RoutedEventTraceStep(string ElementType)
```

Inheritance:
`object` -> `RoutedEventTraceStep`

## Examples

Create a trace step directly and format it for diagnostic output:

```csharp
using Cerneala.UI.Detective;

RoutedEventTraceStep step = new("Button");

string text = step.ToString();
// "Button"
```

`RoutedEventTrace.Trace` creates steps from the routed event route:

```csharp
using Cerneala.UI.Detective;
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
Each step stores the element type name captured while building the route.

`RoutedEventTrace.Trace` creates each step with `element.GetType().Name`.
The route order depends on the routed event strategy: bubble traces run from the target up through its enabled ancestors, tunnel traces run the same steps in reverse order, and direct traces contain only the target. Disabled ancestors, including a disabled root, are not steps.
Disabled ancestors are skipped by the trace builder.

`ToString()` returns `ElementType`.

Because this type is a C# record, it also has record value equality and compiler-generated members for the primary constructor parameters.

## Constructors

| Name | Description |
| --- | --- |
| `RoutedEventTraceStep(string ElementType)` | Initializes a trace step with the element type name. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ElementType` | `string` | The element type name captured for the traced route step. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `ToString()` | `string` | Returns `ElementType`. |

## Applies to

Cerneala UI routed event diagnostics.

## See also

- `Cerneala.UI.Detective.RoutedEventTrace`
- `Cerneala.UI.Detective.RoutedEventTraceSnapshot`
- `Cerneala.UI.Input.RoutedEvent`
- `Cerneala.UI.Input.RoutingStrategy`
