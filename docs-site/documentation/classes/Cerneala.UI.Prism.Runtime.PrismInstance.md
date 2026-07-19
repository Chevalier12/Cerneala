# PrismInstance Class

## Definition
Namespace: `Cerneala.UI.Prism.Runtime`

Assembly/Project: `Cerneala`

Source: `UI/Prism/Runtime/PrismInstance.cs`

Stores isolated mutable Prism values for one application of a shared immutable composition definition.

```csharp
public sealed class PrismInstance
```

## Examples

```csharp
PrismCompositionDefinition definition = new(
    "Blurred",
    [
        new PrismLayerDefinition(
            new PrismNodeId(1),
            "Content",
            filters: [new PrismFilterDefinition(PrismFilterId.Blur)])
    ]);

PrismInstance first = new(definition);
PrismInstance second = new(definition);

first.GetLayerState(new PrismNodeId(1)).Opacity = 0.5f;
// second still has the definition default opacity.
```

## Remarks

The instance owns dense CPU-side typed value arrays, not textures, render targets, shader objects, or other GPU resources. Multiple instances may share `Definition` without sharing values.

`StructuralVersion` changes only for topology replacement. `ValueVersion` changes only for effective data changes; identical writes are no-ops. `ResetToDefaults()` restores the current definition and catalog defaults.

## Constructors

| Name | Description |
| --- | --- |
| `PrismInstance(PrismCompositionDefinition definition)` | Creates isolated state initialized from a shared definition and catalog defaults. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Definition` | `PrismCompositionDefinition` | Gets the current shared immutable definition. |
| `Composition` | `PrismCompositionState` | Gets mutable composition-level state. |
| `Backdrop` | `PrismBackdropState?` | Gets the optional backdrop state. |
| `StructuralVersion` | `PrismStructuralVersion` | Gets the topology version. |
| `ValueVersion` | `PrismValueVersion` | Gets the typed-value version. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `GetNodeState(PrismNodeId id)` | `PrismNodeState` | Resolves state by numeric node ID. |
| `GetLayerState(PrismNodeId id)` | `PrismLayerState` | Resolves layer state and verifies its type. |
| `GetGroupState(PrismNodeId id)` | `PrismGroupState` | Resolves group state and verifies its type. |
| `GetBackdropState(PrismNodeId id)` | `PrismBackdropState` | Resolves backdrop state and verifies its type. |
| `ReplaceDefinition(PrismCompositionDefinition definition)` | `void` | Replaces the definition, resets values, and versions topology and data independently. |
| `ResetToDefaults()` | `void` | Restores current definition and catalog defaults when values differ. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `PrismInstance(...)`, `ReplaceDefinition(...)` | `ArgumentNullException` | `definition` is `null`. |
| `GetNodeState(...)` | `KeyNotFoundException` | The node ID is absent. |
| Typed `Get...State(...)` methods | `InvalidOperationException` | The node exists but has a different node kind. |

## Applies to

Per-element Prism value isolation, Motion targets, presentation invalidation, and retained cache versioning.
