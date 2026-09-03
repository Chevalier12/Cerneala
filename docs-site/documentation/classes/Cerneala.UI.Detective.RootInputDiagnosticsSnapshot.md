# RootInputDiagnosticsSnapshot Record

Namespace: `Cerneala.UI.Detective`

Assembly: `Cerneala.dll`

Source: [`UI/Detective/Detective.cs`](https://github.com/Chevalier12/Cerneala/blob/master/UI/Detective/Detective.cs)

## Definition

Stores root input-cache dirtiness, rebuild count, and the last invalidation reason.

```csharp
public sealed record RootInputDiagnosticsSnapshot(
    bool IsDirty,
    int RebuildCount,
    string LastInvalidationReason);
```

## Examples

```csharp
RootInputDiagnosticsSnapshot input = root.Detective.Capture(stats).Input;
```

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `IsDirty` | `bool` | Gets whether the input cache currently requires rebuilding. |
| `LastInvalidationReason` | `string` | Gets the last input-cache invalidation reason. |
| `RebuildCount` | `int` | Gets the number of input-cache rebuilds. |
