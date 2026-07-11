# InkCanvasStrokeCollectedEventArgs Class

## Definition
Namespace: `Cerneala.UI.Controls`  
Assembly/Project: `Cerneala`  
Source: `UI/Controls/InkCanvasEventArgs.cs`

Event arguments carrying a newly collected ink stroke.

```csharp
public sealed class InkCanvasStrokeCollectedEventArgs : EventArgs
```

## Examples
```csharp
void OnStroke(object? sender, InkCanvasStrokeCollectedEventArgs args)
{
    Stroke stroke = args.Stroke;
}
```

## Constructors
| Name | Description |
| --- | --- |
| `InkCanvasStrokeCollectedEventArgs(Stroke)` | Creates args for a collected stroke. |

## Properties
| Name | Description |
| --- | --- |
| `Stroke` | Collected stroke. |

## Applies to
`InkCanvas` input and rendering.
