# TimbreParameter Class

## Definition

Namespace: `Cerneala.Timbre`

Assembly/Project: `Cerneala`

Source: `Timbre/TimbreParameter.cs`

Untyped base of a clip parameter descriptor. Use [TimbreParameter&lt;T&gt;](Cerneala.Timbre.TimbreParameter_T_.md) to declare parameters.

```csharp
public abstract class TimbreParameter
```

Inheritance:
`object` -> `TimbreParameter`

Derived:
`TimbreParameter<T>`

## Remarks

The base type lets a [TimbreClip](Cerneala.Timbre.TimbreClip.md) hold parameters of different value types in one list. It cannot be derived from outside the core assembly. Identity is the descriptor instance; `Name` is used for diagnostics and markup lowering, and names must be unique within one clip.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Name` | `string` | Identifier-style parameter name. |
| `ValueType` | `Type` | The parameter's value type. |

## Methods

| Name | Returns | Description |
| --- | --- | --- |
| `ToString()` | `string` | Returns `Name`. |
