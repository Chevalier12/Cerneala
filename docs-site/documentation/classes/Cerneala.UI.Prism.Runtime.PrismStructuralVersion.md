# PrismStructuralVersion Struct

## Definition
Namespace: `Cerneala.UI.Prism.Runtime`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Runtime/PrismVersions.cs`

Represents the version of a `PrismInstance` topology.

```csharp
public readonly record struct PrismStructuralVersion
```

## Remarks

The version changes when definition replacement changes node or operation topology. Ordinary parameter writes do not change it.

## Constructors

| Name | Description |
| --- | --- |
| `PrismStructuralVersion(long value)` | Initializes a version value. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Value` | `long` | Gets the numeric version. |

## Applies to

Prism topology invalidation and retained pipeline keys.
