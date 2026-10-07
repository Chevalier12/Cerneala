# SoundParameter Class

## Definition

Namespace: `Cerneala.Timbre`

Assembly/Project: `Cerneala`

Source: `Timbre/SoundParameter.cs`

Untyped base of a clip parameter descriptor. Use [SoundParameter&lt;T&gt;](Cerneala.Timbre.SoundParameter_T_.md) to declare parameters.

```csharp
public abstract class SoundParameter
```

Inheritance:
`object` -> `SoundParameter`

Derived:
`SoundParameter<T>`

## Remarks

The base type lets a [SoundClip](Cerneala.Timbre.SoundClip.md) hold parameters of different value types in one list. It cannot be derived from outside the core assembly. Identity is the descriptor instance; `Name` is used for diagnostics and markup lowering, and names must be unique within one clip.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Name` | `string` | Identifier-style parameter name. |
| `ValueType` | `Type` | The parameter's value type. |

## Methods

| Name | Returns | Description |
| --- | --- | --- |
| `ToString()` | `string` | Returns `Name`. |
