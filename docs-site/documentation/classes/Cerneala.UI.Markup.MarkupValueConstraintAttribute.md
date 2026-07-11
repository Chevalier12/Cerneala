# MarkupValueConstraintAttribute Class

## Definition
Namespace: `Cerneala.UI.Markup`  
Assembly/Project: `Cerneala`  
Source: `UI/Markup/MarkupValueConstraintAttribute.cs`

Annotates a property with numeric constraints for source-generated markup parsing.

```csharp
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class MarkupValueConstraintAttribute : Attribute
```

## Examples
```csharp
[MarkupValueConstraint(MarkupValueConstraint.NonNegative)]
public float Width { get; set; }
```

## Constructors
| Name | Description |
| --- | --- |
| `MarkupValueConstraintAttribute(MarkupValueConstraint)` | Stores the declared constraint. |

## Properties
| Name | Description |
| --- | --- |
| `Constraint` | Constraint consumed by the generator. |

## Applies to
Markup-aware public properties.
