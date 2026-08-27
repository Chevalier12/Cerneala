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
    UiPropertyValueSource.AspectBase);
```

## Remarks

The property store resolves concrete sources in increasing semantic precedence: inherited values, ordinary template bindings, canonical Aspect values, explicit template-owner bindings, markup values, animation, and local values. Reusable, application, scoped, named, and inline Aspect declarations all publish through the same canonical Aspect sources; authoring origin does not create another property-store band.

## Fields

| Name | Value | Description |
| --- | --- | --- |
| `Default` | `0` | The property's metadata default value; this source is never stored in `UiPropertyStore`. |
| `Inherited` | `1` | Value inherited from an ancestor in the UI property tree. |
| `TemplateBinding` | `2` | Value supplied through a template binding. |
| `AspectBase` | `3` | Base value supplied by an element aspect. |
| `AspectVisualState` | `4` | Conditional value supplied by an element aspect visual state. |
| `TemplateOwnerBinding` | `5` | Explicit owner-to-template-part value that must remain above the part's own Aspect values. |
| `MarkupBase` | `6` | Base value supplied by markup outside Aspect declarations. |
| `MarkupConditional` | `7` | Conditional value supplied by markup outside Aspect declarations. |
| `Animation` | `8` | Value supplied by animation. |
| `Local` | `9` | Value set directly on the object. |

`Default` is returned by `UiObject.GetValueSource` when no concrete source is stored, but attempts to set or clear it throw `ArgumentOutOfRangeException`.

## Applies to
Cerneala UI runtime and framework API consumers.
