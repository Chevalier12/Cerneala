# ServoElement Class

## Definition
Namespace: `Cerneala.UI.Servo`
Assembly/Project: `Cerneala`
Source: `UI/Servo/ServoElement.cs`

Represents a read-only snapshot of one Servo query result.

```csharp
public sealed class ServoElement
```

## Examples

```csharp
ServoElement status = await servo.FindAsync(ServoTarget.ById("status"));
Console.WriteLine($"{status.Name}: {status.Bounds}");
```

## Remarks

The snapshot copies semantic properties and current live state at resolution time. It contains no public reference to the retained UI element. Reuse the original `ServoTarget` to observe later tree or layout changes.

## Properties

| Name | Description |
| --- | --- |
| `TypeName` | Gets the matched Cerneala element type name. |
| `Id` | Gets the Servo ID at resolution time. |
| `Name` | Gets the semantic name at resolution time. |
| `Role` | Gets the semantic role. |
| `Bounds` | Gets current arranged bounds in client DIP coordinates. |
| `IsVisible` | Gets effective visibility at resolution time. |
| `IsEnabled` | Gets enabled state at resolution time. |
| `IsFocused` | Gets keyboard-focus state at resolution time. |
| `Value` | Gets the string semantic value when present. |
| `Properties` | Gets a copied read-only semantic property dictionary. |

## Applies to

Servo query results.
