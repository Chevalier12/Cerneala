# UiMarkupGenerator.MarkupSource Struct

## Definition
Namespace: `Cerneala.SourceGen`

Assembly/Project: `Cerneala.SourceGen`

Source: `Cerneala.SourceGen/UiMarkupGenerator.cs`

Carries one compiler additional-text file and its parsed markup representations while `UiMarkupGenerator` runs.

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
| `LanguageDocument` | `CernealaDocument?` | Gets the shared language document, or `null` when text is unavailable. |
| `Document` | `EmissionMarkupDocument?` | Gets the emission document when the parsed markup has exactly one element root; otherwise `null`. |

## Remarks

When text is available, the constructor preserves source positions while replacing an XML declaration with whitespace, creates a `CernealaDocument`, and converts exactly one root element into an emission document. Missing text leaves both parsed-document properties `null`; multiple or missing roots leave `Document` `null`.

## Applies to

Cerneala source-generation internals used by `UiMarkupGenerator`.
