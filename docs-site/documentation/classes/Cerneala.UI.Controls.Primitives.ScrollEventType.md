# ScrollEventType Enum

## Definition
Namespace: `Cerneala.UI.Controls.Primitives`  
Assembly/Project: `Cerneala`  
Source: `UI/Controls/Primitives/ScrollEventArgs.cs`

Identifies the user action that produced a scroll event.

```csharp
public enum ScrollEventType
```

## Members
| Name | Description |
| --- | --- |
| `SmallDecrement`, `SmallIncrement` | Small step backward or forward. |
| `LargeDecrement`, `LargeIncrement` | Large page step backward or forward. |
| `ThumbTrack`, `ThumbPosition` | Thumb movement or final thumb position. |
| `First`, `Last` | Move to the first or last extent. |
| `EndScroll` | Scrolling interaction ended. |

## Remarks
Use the event type together with `ScrollEventArgs.NewValue` to distinguish discrete steps from thumb tracking.

## Applies to
`ScrollEventArgs`.
