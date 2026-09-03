# ServoModifiers Enum

## Definition
Namespace: `Cerneala.UI.Servo`
Assembly/Project: `Cerneala`
Source: `UI/Servo/ServoModifiers.cs`

Specifies modifier keys held during a Servo key chord.

```csharp
[Flags]
public enum ServoModifiers
```

## Examples

```csharp
ServoModifiers selectAll = ServoModifiers.Control;
```

## Remarks

Values can be combined with bitwise OR.

## Fields

| Name | Description |
| --- | --- |
| `None` | No modifier key. |
| `Shift` | The Shift modifier. |
| `Control` | The Control modifier. |
| `Alt` | The Alt modifier. |

## Applies to

Servo keyboard actions.
