# DirtyTreeDumper.DirtyTraceInfo Record

## Definition
Namespace: `Cerneala.UI.Diagnostics`

Assembly/Project: `Cerneala`

Source: `UI/Diagnostics/DirtyTreeDumper.cs`

Stores the latest invalidation reason and optional source property name found for a dirty element while dumping a dirty tree.

```csharp
private sealed record DirtyTraceInfo(string Reason, string? SourcePropertyName);
```

Containing type:
`DirtyTreeDumper`

## Examples

```csharp
// Created internally by DirtyTreeDumper.FindLatestEntry.
DirtyTraceInfo info = new(reason, sourcePropertyName);
```

## Remarks

`DirtyTraceInfo` is a private implementation detail of `DirtyTreeDumper`. It is created when an `InvalidationTrace` contains a dirty-causing entry for the current element.

`Reason` is appended to the dump output as `reason=...`. `SourcePropertyName` is appended as `source=...` only when a source property name is available.

## Constructors

| Name | Description |
| --- | --- |
| `DirtyTraceInfo(string, string?)` | Initializes the dirty trace payload with a reason and optional source property name. |

## Properties

| Name | Description |
| --- | --- |
| `Reason` | Gets the invalidation reason associated with the latest dirty-causing trace entry. |
| `SourcePropertyName` | Gets the optional source property name associated with the dirty state. |

## Applies to

Cerneala retained UI diagnostics internals.

## See also

- `Cerneala.UI.Diagnostics.DirtyTreeDumper`
- `Cerneala.UI.Diagnostics.InvalidationTrace`
