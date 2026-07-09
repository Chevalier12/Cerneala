# MarkupDiagnostic Class

## Definition
Namespace: `Cerneala.UI.Markup`

Assembly/Project: `Cerneala`

Source: `UI/Markup/MarkupDiagnostic.cs`

Represents a single markup diagnostic with severity, code, message, and optional source location.

```csharp
public sealed record MarkupDiagnostic(
    MarkupDiagnosticSeverity Severity,
    string Code,
    string Message,
    int? Line = null,
    int? Column = null)
```

Inheritance:
`object` -> `MarkupDiagnostic`

Related types:
`MarkupDiagnosticSeverity`

## Examples

Create an error diagnostic with a source location:

```csharp
using Cerneala.UI.Markup;

MarkupDiagnostic diagnostic = MarkupDiagnostic.Error(
    "MARKUP021",
    "Unknown markup element 'FancyPanel'.",
    line: 12,
    column: 5);

if (diagnostic.HasSourceLocation)
{
    Console.WriteLine($"{diagnostic.Code} at {diagnostic.Line}:{diagnostic.Column}");
}
```

Create a warning diagnostic without a source location:

```csharp
using Cerneala.UI.Markup;

MarkupDiagnostic diagnostic = MarkupDiagnostic.Warning(
    "MARKUP099",
    "Optional markup feature was ignored.");
```

## Remarks

`MarkupDiagnostic` is the diagnostic value used by the markup reader and factory pipeline. A `MarkupResult<T>` reports errors by checking whether any diagnostic has `Severity` set to `MarkupDiagnosticSeverity.Error`.

Line and column are optional. `HasSourceLocation` returns `true` only when both `Line` and `Column` are present; a diagnostic with only one coordinate is treated as not having a complete source location.

The `Error` and `Warning` factory methods only assign the corresponding severity and pass the supplied code, message, line, and column into the record constructor. `MarkupDiagnostic` does not validate or normalize diagnostic codes or messages.

Because this type is a C# record, it has value-based equality, generated deconstruction for the primary constructor parameters, support for `with` expressions, and generated string formatting.

## Constructors

| Name | Description |
| --- | --- |
| `MarkupDiagnostic(MarkupDiagnosticSeverity Severity, string Code, string Message, int? Line = null, int? Column = null)` | Initializes a diagnostic with severity, code, message, and optional source location. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Severity` | `MarkupDiagnosticSeverity` | Gets the diagnostic severity. |
| `Code` | `string` | Gets the diagnostic code. |
| `Message` | `string` | Gets the diagnostic message. |
| `Line` | `int?` | Gets the optional source line. |
| `Column` | `int?` | Gets the optional source column. |
| `HasSourceLocation` | `bool` | Gets whether both `Line` and `Column` are not `null`. |

## Methods

| Name | Returns | Description |
| --- | --- | --- |
| `Error(string code, string message, int? line = null, int? column = null)` | `MarkupDiagnostic` | Creates a diagnostic with `Severity` set to `MarkupDiagnosticSeverity.Error`. |
| `Warning(string code, string message, int? line = null, int? column = null)` | `MarkupDiagnostic` | Creates a diagnostic with `Severity` set to `MarkupDiagnosticSeverity.Warning`. |

## Applies to

Cerneala markup loading and generated UI factory diagnostics.

## See also

- `MarkupDiagnosticSeverity`
- `UiMarkupDocument`
- `MarkupResult<T>`
- `UiFactory`
