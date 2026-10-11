# MarkupLoadOptions Class

## Definition
Namespace: `Cerneala.UI.Markup`

Assembly/Project: `Cerneala`

Source: `UI/Markup/MarkupLoadOptions.cs`

Specifies whether markup loading returns a partial result after errors or discards it.

```csharp
public sealed record MarkupLoadOptions(bool ContinueOnError = false)
```

Inheritance:
`Object` -> `MarkupLoadOptions`

## Examples

Both modes walk the whole markup tree and collect every diagnostic. Use `Strict` when any error should make the result `null`, or `Recover` when the partially built result should be returned together with the diagnostics. For example, `<StackPanel><Foo/><Bar/></StackPanel>` reports two `MARKUP021` errors in both modes; `Strict` returns a `null` value and `Recover` returns the `StackPanel`.

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
| `Strict` | `MarkupLoadOptions` | Gets an options instance that discards the result when any error is reported. |

## Applies to

- `Cerneala.UI.Markup.MarkupLoadOptions`

## See also

- `Cerneala.UI.Markup.MarkupDiagnostic`
- `Cerneala.UI.Markup.UiFactory`
