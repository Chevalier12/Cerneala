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

The property store resolves concrete sources in increasing semantic precedence: inherited values, weak template values, canonical Aspect values, template-owner bindings, markup values, animation, and local values. Reusable, application, scoped, named, and inline Aspect declarations all publish through the same canonical Aspect sources; authoring origin does not create another property-store band.

## Fields

| Name | Value | Description |
| --- | --- | --- |
| `Default` | `0` | No concrete source has a value. The effective value is the framework default that Cerneala stored internally for the object, if any, or otherwise the property's metadata default value. |
| `Inherited` | `1` | Value inherited from an ancestor in the UI property tree. |
| `TemplateBinding` | `2` | Weak template-supplied value that the target's Aspect values override, such as a template token binding or a template binding created with this source. |
| `AspectBase` | `3` | Base value supplied by an element aspect. |
| `AspectVisualState` | `4` | Conditional value supplied by an element aspect visual state. |
| `TemplateOwnerBinding` | `5` | Owner-to-template-part value that remains above the part's own Aspect values. Template bindings, including markup `$owner.Property`, write it by default. |
| `MarkupBase` | `6` | Base value supplied by markup outside Aspect declarations. |
| `MarkupConditional` | `7` | Conditional value supplied by markup outside Aspect declarations. |
| `Animation` | `8` | Value supplied by animation. |
| `Local` | `9` | Value set directly on the object. |

`Default` is returned by `UiObject.GetValueSource` when no concrete source is stored, but attempts to set or clear it throw `ArgumentOutOfRangeException`.

## Applies to
Cerneala UI runtime and framework API consumers.
