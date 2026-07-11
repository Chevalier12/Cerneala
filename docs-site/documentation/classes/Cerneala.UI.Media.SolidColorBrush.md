# SolidColorBrush Class

## Definition
Namespace: `Cerneala.UI.Media`
Assembly/Project: `Cerneala`
Source: `UI/Media/SolidColorBrush.cs`

Represents one solid color with optional brush opacity.

```csharp
public sealed record SolidColorBrush : Brush
```

## Examples
```csharp
shape.Fill = new SolidColorBrush(Color.CornflowerBlue, opacity: 0.8f);
```

## Remarks
This is the renderer fast path. It exposes its color through both `Color` and the inherited `SolidColor` shortcut and does not allocate an additional brush texture.

## Constructors
| Name | Description |
| --- | --- |
| `SolidColorBrush(Color, float)` | Creates a solid brush; opacity defaults to `1`. |

## Properties
| Name | Description |
| --- | --- |
| `Color` | Stored color. |
| `Kind` | Always `DrawBrushKind.SolidColor`. |
| `Opacity` | Inherited brush opacity. |

## Applies to
Shape fills, strokes, and drawing commands.
