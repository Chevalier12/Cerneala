# AspectOrigin Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectOrigin.cs`

Stores immutable authoring metadata for an Aspect package or `ElementAspect`.

```csharp
public sealed record AspectOrigin
```

## Examples

```csharp
AspectPackage package = AspectPackage.Create("Shell.Buttons")
    .Origin(new AspectOrigin(
        AspectAuthoringKind.MarkupDefault,
        "Shell.crn",
        "Button"));
```

## Remarks

`Document` and `Name` are trimmed; `null`, empty, and whitespace values normalize to `null`. The metadata is copied into catalog-owned rule projections and reported by diagnostics without mutating reusable source rules.

Origin metadata explains where a rule came from. It is not part of `AspectCascadeKey` and cannot change the winner.

## Constructors

| Name | Description |
| --- | --- |
| `AspectOrigin(AspectAuthoringKind kind, string? document = null, string? name = null)` | Creates immutable origin metadata. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Kind` | `AspectAuthoringKind` | Gets the authoring form. |
| `Document` | `string?` | Gets the markup document name, or `null` for code-first origins. |
| `Name` | `string?` | Gets the named resource, target, or diagnostic origin name when available. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Code(string? name = null)` | `AspectOrigin` | Creates code-first origin metadata with an optional name. |

## Applies to

Aspect packages, element-local Aspects, catalog projections, and diagnostics.

## See also

- `AspectAuthoringKind`
- `AspectPackage`
- `ElementAspect`
- `AspectResolutionStep`
