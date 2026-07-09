# MarkupLoadOptions Class

## Definition
Namespace: `Cerneala.UI.Markup`

Assembly/Project: `Cerneala`

Source: `UI/Markup/MarkupLoadOptions.cs`

Specifies whether markup loading should stop on the first error or continue collecting diagnostics.

```csharp
public sealed record MarkupLoadOptions(bool ContinueOnError = false)
```

Inheritance:
`Object` -> `MarkupLoadOptions`

## Examples

Use `Strict` for fail-fast loading or `Recover` when diagnostics should be collected after errors.

```csharp
using Cerneala.UI.Markup;

MarkupLoadOptions strict = MarkupLoadOptions.Strict;
MarkupLoadOptions recover = MarkupLoadOptions.Recover;
```

## Remarks

`MarkupLoadOptions` is an immutable record used by markup loading APIs to choose error handling behavior.

`Strict` creates the default option with `ContinueOnError` set to `false`. `Recover` creates an option with `ContinueOnError` set to `true`.

## Constructors

| Name | Description |
| --- | --- |
| `MarkupLoadOptions(bool)` | Initializes markup load options with a continue-on-error setting. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ContinueOnError` | `bool` | Gets whether markup loading should continue after recoverable errors. |
| `Recover` | `MarkupLoadOptions` | Gets an options instance that continues after recoverable errors. |
| `Strict` | `MarkupLoadOptions` | Gets an options instance that stops on errors. |

## Applies to

- `Cerneala.UI.Markup.MarkupLoadOptions`

## See also

- `Cerneala.UI.Markup.MarkupDiagnostic`
- `Cerneala.UI.Markup.UiFactory`
