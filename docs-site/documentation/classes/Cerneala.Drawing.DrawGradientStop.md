# DrawGradientStop Struct

## Definition
Namespace: `Cerneala.Drawing`  
Assembly/Project: `Cerneala`  
Source: `Drawing/IDrawBrush.cs`

Immutable drawing-layer gradient stop used by backend descriptors.

```csharp
public readonly record struct DrawGradientStop(float Offset, Color Color)
```

## Examples
```csharp
DrawGradientStop stop = new(0.5f, Color.White);
```

## Remarks
Validation and ordering are owned by `Cerneala.UI.Media.GradientStop` and its brush models. This type is the backend representation.

## Properties
| Name | Description |
| --- | --- |
| `Offset` | Position of the stop. |
| `Color` | Color at the stop. |

## Applies to
Backend implementation code.
