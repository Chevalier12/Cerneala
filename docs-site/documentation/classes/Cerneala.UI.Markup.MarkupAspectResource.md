# MarkupAspectResource Class

## Definition
Namespace: `Cerneala.UI.Markup`  
Assembly/Project: `Cerneala`  
Source: `UI/Markup/MarkupAspectResource.cs`

Metadata describing a compiled aspect resource for runtime lookup and diagnostics.

```csharp
public sealed class MarkupAspectResource
```

## Examples
```csharp
var resource = new MarkupAspectResource("Card", typeof(Border), ["Background"], false);
```

## Remarks
Property names are copied on construction. A blank name is normalized to `null`; target type and property-name input cannot be `null`.

Generated markup uses the applicator overload for application aspects and supported named or unnamed element-resource aspects. A `UIRoot` resolves unnamed aspects implicitly. Named aspects can be retrieved from resources and applied explicitly to dynamically created elements, including aspects that provide a component template. Each resource applies at most once per element. `TargetType.IsInstanceOfType` provides derived-type matching, so controls created in markup and code-behind follow the same aspect contract.

## Constructors
| Name | Description |
| --- | --- |
| `MarkupAspectResource(string?, Type, IReadOnlyList<string>, bool)` | Creates aspect metadata. |
| `MarkupAspectResource(string?, Type, IReadOnlyList<string>, bool, Action<UIElement>?)` | Creates aspect metadata with an optional runtime applicator for matching elements. |

## Properties
| Name | Description |
| --- | --- |
| `Name` | Optional resource name. |
| `TargetType` | Element type targeted by the aspect. |
| `DefaultPropertyNames` | Copied property-name list. |
| `IsConditional` | Whether the aspect has conditional behavior. |

## Methods
| Name | Description |
| --- | --- |
| `ApplyTo(UIElement)` | Applies a named or unnamed aspect to a compatible element once. Missing applicators, incompatible target types, and repeated calls are ignored. |

## Applies to
Generated markup resources.
