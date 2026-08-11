# UiPropertyValueSource Enum
## Definition
Namespace: `Cerneala.UI.Core`
Assembly/Project: `Cerneala`
Source: `UI/Core/UiPropertyValueSource.cs`
Provides the `Cerneala.UI.Core.UiPropertyValueSource` API surface.
```csharp
public enum UiPropertyValueSource
```

## Examples

```csharp
button.SetValue(
    Control.BackgroundProperty,
    brush,
    UiPropertyValueSource.ApplicationAspectBase);
```

## Remarks

The property store resolves sources by framework precedence rather than by the enum's numeric value. Application aspect sources override framework aspect sources and remain below local aspects, animation, markup, and local values.

## Fields

| Name | Description |
| --- | --- |
| `ApplicationAspectBase` | Base value produced by an unnamed application `Aspect`. |
| `ApplicationAspectVisualState` | Conditional value produced by an unnamed application `Aspect`. |

## Applies to
Cerneala UI runtime and framework API consumers.
