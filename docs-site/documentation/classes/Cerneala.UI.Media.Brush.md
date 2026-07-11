# Brush Class

## Definition
Namespace: `Cerneala.UI.Media`
Assembly/Project: `Cerneala`
Source: `UI/Media/Brush.cs`

Semantic base record for all Cerneala brush values.

```csharp
public abstract record Brush : IDrawBrush
```

## Examples
```csharp
Brush brush = new SolidColorBrush(Color.White, opacity: 0.5f);
```

## Remarks
Brushes are immutable. Opacity is validated in the inclusive range `0..1`. `SolidColor` is a fast-path shortcut and returns `null` for gradients, images, drawings, and visuals.

## Properties
| Name | Description |
| --- | --- |
| `Kind` | Semantic brush kind. |
| `Opacity` | Brush opacity. |
| `SolidColor` | Solid color shortcut, or `null`. |

## Explicit Interface Implementations
| Name | Description |
| --- | --- |
| `IDrawBrush.CreateDescriptor()` | Creates the backend descriptor for this brush. |

## Applies to
All UI shape and drawing APIs that accept a `Brush`.
