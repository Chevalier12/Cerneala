# MarkupValueConstraint Enum

## Definition
Namespace: `Cerneala.UI.Markup`  
Assembly/Project: `Cerneala`  
Source: `UI/Markup/MarkupValueConstraintAttribute.cs`

Declares numeric validation semantics understood by the source generator.

```csharp
public enum MarkupValueConstraint
```

## Members
| Name | Description |
| --- | --- |
| `None` | No additional sign constraint. |
| `NonNegative` | Value must be zero or greater. |
| `Positive` | Value must be greater than zero. |

## Remarks
The source generator uses this metadata when parsing numeric markup attributes.

## Applies to
`MarkupValueConstraintAttribute` and compiled markup property parsing.
