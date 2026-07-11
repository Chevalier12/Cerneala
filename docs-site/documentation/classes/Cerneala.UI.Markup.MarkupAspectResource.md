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

## Constructors
| Name | Description |
| --- | --- |
| `MarkupAspectResource(string?, Type, IReadOnlyList<string>, bool)` | Creates aspect metadata. |

## Properties
| Name | Description |
| --- | --- |
| `Name` | Optional resource name. |
| `TargetType` | Element type targeted by the aspect. |
| `DefaultPropertyNames` | Copied property-name list. |
| `IsConditional` | Whether the aspect has conditional behavior. |

## Applies to
Generated markup resources.
