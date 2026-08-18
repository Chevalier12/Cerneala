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

| Name | Value | Description |
| --- | --- | --- |
| `Default` | `0` | The property's metadata default value; this source is never stored in `UiPropertyStore`. |
| `Inherited` | `1` | Value inherited from an ancestor in the UI property tree. |
| `TemplateBinding` | `2` | Value supplied through a template binding. |
| `AspectBase` | `3` | Base value supplied by an element aspect. |
| `AspectVisualState` | `4` | Conditional value supplied by an element aspect visual state. |
| `LocalAspectBase` | `5` | Base value supplied by a local aspect. |
| `LocalAspectConditional` | `6` | Conditional value supplied by a local aspect. |
| `Animation` | `7` | Value supplied by animation. |
| `MarkupBase` | `8` | Base value supplied by markup. |
| `MarkupConditional` | `9` | Conditional value supplied by markup. |
| `Local` | `10` | Value set directly on the object. |
| `ApplicationAspectBase` | `11` | Base value supplied by an application aspect. |
| `ApplicationAspectVisualState` | `12` | Conditional value supplied by an application aspect visual state. |

`Default` is returned by `UiObject.GetValueSource` when no concrete source is stored, but attempts to set or clear it throw `ArgumentOutOfRangeException`.

## Applies to
Cerneala UI runtime and framework API consumers.
