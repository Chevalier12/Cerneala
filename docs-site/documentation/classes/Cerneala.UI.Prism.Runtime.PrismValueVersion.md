# PrismValueVersion Struct

## Definition
Namespace: `Cerneala.UI.Prism.Runtime`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Runtime/PrismVersions.cs`

Represents the version of mutable typed values in a `PrismInstance`.

```csharp
public readonly record struct PrismValueVersion
```

## Remarks

The version changes only when effective instance data changes. Assigning an identical value and resetting an already-default instance are no-ops.

## Constructors

| Name | Description |
| --- | --- |
| `PrismValueVersion(long value)` | Initializes a version value. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Value` | `long` | Gets the numeric version. |

## Applies to

Prism presentation invalidation, Motion updates, and retained value stamps.
