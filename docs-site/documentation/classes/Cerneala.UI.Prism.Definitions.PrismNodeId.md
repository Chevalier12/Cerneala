# PrismNodeId Struct

## Definition
Namespace: `Cerneala.UI.Prism.Definitions`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Definitions/PrismNodeId.cs`

Represents a positive numeric identifier that is stable within one Prism composition definition.

```csharp
public readonly record struct PrismNodeId
```

## Examples

```csharp
PrismNodeId contentLayer = new(1);
```

## Constructors

| Name | Description |
| --- | --- |
| `PrismNodeId(int value)` | Initializes an identifier from a positive integer. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Value` | `int` | Gets the numeric identifier. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `ToString()` | `string` | Returns the invariant-culture decimal representation. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `PrismNodeId(int)` | `ArgumentOutOfRangeException` | `value` is zero or negative. |

## Applies to

Prism definition nodes and per-instance state lookup.
