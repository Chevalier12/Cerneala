# MarkupSource Struct

## Definition
Namespace: `Cerneala.SourceGen.UiMarkupGenerator`

Assembly/Project: `Cerneala.SourceGen`

Source: `Cerneala.SourceGen/UiMarkupGenerator.cs`

Provides the `Cerneala.SourceGen.UiMarkupGenerator.MarkupSource` API surface.

```csharp
private readonly struct MarkupSource
```

## Constructors

| Name | Description |
| --- | --- |
| `MarkupSource(string path, string? text)` | Creates a markup source record with its path and optional text. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Path` | `string` | Gets the additional-text path. |
| `Text` | `string?` | Gets the loaded markup text, or `null` when unavailable. |

## Remarks

This page is generated from the repository API index so the documentation surface stays aligned with the source tree.

## Applies to

Cerneala UI runtime and framework API consumers.
