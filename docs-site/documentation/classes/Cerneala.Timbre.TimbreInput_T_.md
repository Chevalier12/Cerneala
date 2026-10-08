# TimbreInput&lt;T&gt; Struct

## Definition

Namespace: `Cerneala.Timbre`

Assembly/Project: `Cerneala`

Source: `Timbre/TimbreInput.cs`

A modifier input that is either a constant value or a clip parameter.

```csharp
public readonly struct TimbreInput<T> where T : struct
```

## Examples

```csharp
using Cerneala.Timbre;

var toneCutoff = new TimbreParameter<float>("ToneCutoff", 1200f);
var fixedFilter = new LowPass(cutoff: 1200f);       // constant
var driven = new LowPass(cutoff: toneCutoff);       // parameter
```

## Remarks

Both forms convert implicitly, so modifier constructors accept a `float` or a `TimbreParameter<float>` directly. Constants are validated by the modifier constructor against the catalog range of that input. A `default(TimbreInput<T>)` is a constant equal to `default(T)`.

## Constructors

| Name | Description |
| --- | --- |
| `TimbreInput(T value)` | A constant input. |
| `TimbreInput(TimbreParameter<T> parameter)` | An input driven by a parameter; `parameter` must not be `null`. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `IsParameter` | `bool` | Whether the input is driven by a parameter. |
| `Parameter` | `TimbreParameter<T>?` | The parameter, or `null` for a constant. |
| `Value` | `T` | The constant, or the parameter's default value. |

## Operators

| Name | Description |
| --- | --- |
| `implicit operator TimbreInput<T>(T value)` | Creates a constant input. |
| `implicit operator TimbreInput<T>(TimbreParameter<T> parameter)` | Creates a parameter input. |
