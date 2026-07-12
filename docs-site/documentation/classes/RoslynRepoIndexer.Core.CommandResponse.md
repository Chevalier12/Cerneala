# CommandResponse Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/Models.cs`

Provides factory methods for command response contracts.

```csharp
public static class CommandResponse
```

## Remarks

The factory creates successful and failed `CommandResponse<T>` values while keeping the CLI response metadata consistent. MCP adapters convert these values to the compact MCP envelope and omit the legacy `results` alias.

## Methods

| Signature | Returns | Description |
| --- | --- | --- |
| `Success<T>(T, IReadOnlyList<string>?)` | `CommandResponse<T>` | Creates a successful response. |
| `Success<T>(T, IReadOnlyList<string>?, string?, string?, string?, long?, DateTimeOffset?, bool)` | `CommandResponse<T>` | Creates a successful response with command metadata and optional CLI results alias. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
