# AspectAuthoringKind Enum

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectOrigin.cs`

Identifies the authoring form that produced an Aspect package or element-local Aspect.

```csharp
public enum AspectAuthoringKind
```

## Examples

```csharp
AspectOrigin origin = new(
    AspectAuthoringKind.MarkupNamed,
    "Shell.crn",
    "PrimaryButton");
```

## Remarks

The value is diagnostic metadata only. It does not add a cascade coordinate or a `UiPropertyValueSource` band. Code-first, default markup, named markup, and inline markup still resolve through the same rules and `AspectEngine`.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `Code` | `0` | Code-first package or `ElementAspect`. |
| `MarkupDefault` | `1` | Unnamed reusable/default Aspect compiled from markup. |
| `MarkupNamed` | `2` | Named `ElementAspect` compiled from markup. |
| `MarkupInline` | `3` | Inline `ElementAspect` compiled from markup. |

## Applies to

Aspect origin metadata and resolution diagnostics.

## See also

- `AspectOrigin`
- `AspectResolutionStep`
