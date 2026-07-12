# DoctorCheck Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/Models.cs`

Represents the public DoctorCheck contract used by Roslyn Repo Indexer.

```csharp
public sealed class DoctorCheck
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `DoctorCheck(string Name, string Status, string Severity, string Message, IReadOnlyDictionary<string, string> Details)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Details` | `IReadOnlyDictionary<string, string>` | Gets or sets the Details value. |
| `Message` | `string` | Gets or sets the Message value. |
| `Name` | `string` | Gets or sets the Name value. |
| `Severity` | `string` | Gets or sets the Severity value. |
| `Status` | `string` | Gets or sets the Status value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
