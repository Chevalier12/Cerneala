# PrismGraphNodeId Struct

## Definition
Namespace: `Cerneala.Drawing.Prism.Graph`

Assembly/Project: `Cerneala`

Source: `Drawing/Prism/Graph/PrismGraph.cs`

Identifies a structural operation node within one retained Prism scope.

```csharp
public readonly record struct PrismGraphNodeId
```

## Examples

```csharp
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Graph;

PrismGraphNodeId id = new(
    new PrismCacheOwnerToken(42),
    definitionNodeId: 7,
    kind: PrismGraphNodeKind.Filter,
    ordinal: 0);
```

## Remarks

The identifier combines the retained scope owner, definition node identifier, operation kind, and operation ordinal. `PrismGraphBuilder` derives the same ID for the same structural operation across value-only frames. A `DefinitionNodeId` of `0` is reserved by the builder for scope-level synthetic nodes.

`ToString()` returns `owner:definition:kind:ordinal` using invariant formatting.

## Constructors

| Name | Description |
| --- | --- |
| `PrismGraphNodeId(PrismCacheOwnerToken scopeOwnerToken, int definitionNodeId, PrismGraphNodeKind kind, int ordinal)` | Creates and validates a structural graph node identifier. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ScopeOwnerToken` | `PrismCacheOwnerToken` | Gets the retained owner of the analyzed scope. |
| `DefinitionNodeId` | `int` | Gets the definition node ID, or `0` for a scope-level synthetic node. |
| `Kind` | `PrismGraphNodeKind` | Gets the operation kind. |
| `Ordinal` | `int` | Gets the operation ordinal within the definition node and kind. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `ToString()` | `string` | Returns the invariant diagnostic form `owner:definition:kind:ordinal`. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `PrismGraphNodeId(...)` | `ArgumentOutOfRangeException` | The owner token is default, a numeric component is negative, or `kind` is undefined. |

## Applies to

Cerneala retained Prism graph lookup, diagnostics, and cache identity.

## See also

- `Cerneala.Drawing.Prism.Graph.PrismGraphNode`
- `Cerneala.Drawing.Prism.Graph.PrismGraph`
- `Cerneala.Drawing.Prism.PrismCacheOwnerToken`
