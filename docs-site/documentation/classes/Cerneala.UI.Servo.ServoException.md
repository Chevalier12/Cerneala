# ServoException Class

## Definition
Namespace: `Cerneala.UI.Servo`
Assembly/Project: `Cerneala`
Source: `UI/Servo/ServoException.cs`

Provides the base exception for Servo-specific failures.

```csharp
public class ServoException : Exception
```

Inheritance: `Object` -> `Exception` -> `ServoException`

## Examples

```csharp
try
{
    await servo.FindAsync(target);
}
catch (ServoException exception)
{
    Console.Error.WriteLine(exception.Message);
}
```

## Remarks

External cancellation remains an `OperationCanceledException`; it is not wrapped as a Servo exception.

## Constructors

| Name | Description |
| --- | --- |
| `ServoException(string)` | Creates an exception with a message. |
| `ServoException(string, Exception?)` | Creates an exception with a message and optional inner exception. |

## Applies to

Servo-specific operation failures.
