# SymbolEntry Class

## Definition
Namespace: `RoslynRepoIndexer.Core`

Assembly/Project: `RoslynRepoIndexer.Core`

Source: `Tools/RoslynRepoIndexer/src/RoslynRepoIndexer.Core/Models.cs`

Represents the structured output contract for SymbolEntry operations.

```csharp
public sealed class SymbolEntry
```

## Remarks

This API is part of the local, deterministic index or MCP contract. It performs no network access. Inputs and outputs are scoped to the selected repository and current immutable index generation where applicable.

## Constructors

| Signature | Description |
| --- | --- |
| `SymbolEntry(string SymbolId, string DocumentId, string ProjectId, string Kind, string Name, string MetadataName, string FullyQualifiedName, string ContainerName, string Signature, string Accessibility, IReadOnlyList<string> Modifiers, string Path, int Line, int Column, int EndLine, int EndColumn, int SpanStart, int SpanLength, bool IsDefinition, bool IsPartial, IReadOnlyList<string> ParameterTypes, string ReturnType, string ProjectName, string SymbolKey, IReadOnlyList<string> BaseTypeIds, IReadOnlyList<string> InterfaceTypeIds, string OverriddenSymbolId)` | Initializes a new instance. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Accessibility` | `string` | Gets or sets the Accessibility value. |
| `BaseTypeIds` | `IReadOnlyList<string>` | Gets or sets the BaseTypeIds value. |
| `Column` | `int` | Gets or sets the Column value. |
| `ContainerName` | `string` | Gets or sets the ContainerName value. |
| `DocumentId` | `string` | Gets or sets the DocumentId value. |
| `EndColumn` | `int` | Gets or sets the EndColumn value. |
| `EndLine` | `int` | Gets or sets the EndLine value. |
| `FullyQualifiedName` | `string` | Gets or sets the FullyQualifiedName value. |
| `InterfaceTypeIds` | `IReadOnlyList<string>` | Gets or sets the InterfaceTypeIds value. |
| `IsDefinition` | `bool` | Gets or sets the IsDefinition value. |
| `IsPartial` | `bool` | Gets or sets the IsPartial value. |
| `Kind` | `string` | Gets or sets the Kind value. |
| `Line` | `int` | Gets or sets the Line value. |
| `MetadataName` | `string` | Gets or sets the MetadataName value. |
| `Modifiers` | `IReadOnlyList<string>` | Gets or sets the Modifiers value. |
| `Name` | `string` | Gets or sets the Name value. |
| `OverriddenSymbolId` | `string` | Gets or sets the OverriddenSymbolId value. |
| `ParameterTypes` | `IReadOnlyList<string>` | Gets or sets the ParameterTypes value. |
| `Path` | `string` | Gets or sets the Path value. |
| `ProjectId` | `string` | Gets or sets the ProjectId value. |
| `ProjectName` | `string` | Gets or sets the ProjectName value. |
| `ReturnType` | `string` | Gets or sets the ReturnType value. |
| `Signature` | `string` | Gets or sets the Signature value. |
| `SpanLength` | `int` | Gets or sets the SpanLength value. |
| `SpanStart` | `int` | Gets or sets the SpanStart value. |
| `SymbolId` | `string` | Gets or sets the SymbolId value. |
| `SymbolKey` | `string` | Gets or sets the SymbolKey value. |

## Applies to

Roslyn Repo Indexer schema 6 and MCP contract version 2.
