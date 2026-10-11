# MarkupDiagnosticSeverity Enum

## Definition
Namespace: `Cerneala.UI.Markup`

Assembly/Project: `Cerneala`

Source: `UI/Markup/MarkupDiagnostic.cs`

Specifies how serious a runtime markup diagnostic is.

```csharp
public enum MarkupDiagnosticSeverity
```

## Examples

Read markup that is missing its closing tag:

```csharp
using Cerneala.UI.Markup;

MarkupResult<UiMarkupDocument> result = new UiMarkupReader().Read("<StackPanel>");
MarkupDiagnostic diagnostic = result.Diagnostics[0];

Console.WriteLine($"{diagnostic.Severity} {diagnostic.Code} {diagnostic.Line}:{diagnostic.Column} {result.Value is null}");
// Error MARKUP002 1:13 True
```

## Remarks

The runtime markup APIs (`UiMarkupReader`, `UiMarkupWriter`, `UiFactory`, `GeneratedUiFactory`, and `BrushMarkupReader`) report only `Error` diagnostics. `MarkupDiagnostic.Warning` exists, but no Cerneala code calls it, and nothing produces `Info`.

The severity decides the result, not an exception. With the default `MarkupLoadOptions.Strict`, any `Error` makes `UiFactory.Create` return a `null` value. With `ContinueOnError` set to `true`, the partial result is returned together with the diagnostics. `MarkupResult<T>.HasErrors` and `UiMarkupDocument.HasErrors` report whether an `Error` is present.

Diagnostics from `.crn` files at build time do not use this enum; they are Roslyn diagnostics from `Cerneala.SourceGen`.

## Fields

| Name | Value | Description |
| --- | ---: | --- |
| `Info` | `0` | Information only. |
| `Warning` | `1` | A problem that does not stop loading. |
| `Error` | `2` | A problem that stops loading unless `ContinueOnError` is `true`. |

## Applies to

`MarkupDiagnostic.Severity`.

## See also

- `Cerneala.UI.Markup.MarkupDiagnostic`
- `Cerneala.UI.Markup.MarkupLoadOptions`
- `Cerneala.UI.Markup.UiMarkupReader`
- `Cerneala.UI.Markup.UiFactory`
