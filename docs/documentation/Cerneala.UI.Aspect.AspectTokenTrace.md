# AspectTokenTrace Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectTokenTrace.cs`

Represents one token dependency recorded in an aspect diagnostics snapshot, including the token, provider name, raw value, and resolved value.

```csharp
public sealed record AspectTokenTrace(
    AspectToken Token,
    string ProviderName,
    object? RawValue,
    object? ResolvedValue);
```

Inheritance:
`object` -> `AspectTokenTrace`

## Examples

Print token resolution lines after applying an aspect catalog:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectToken<DrawColor> accent = AspectToken.Color("app.accent");
AspectEnvironment environment = new("app");
environment.Set(accent, DrawColor.White);

Button button = new();
AspectEngine engine = new();

// Apply a catalog whose resolved declarations depend on aspect tokens.
engine.Apply(button, catalog, environment);

AspectDiagnostics.Snapshot diagnostics = engine.GetDiagnostics(button);

foreach (AspectTokenTrace trace in diagnostics.TokenTraces)
{
    Console.WriteLine(
        $"{trace.Token.Name} provider={trace.ProviderName} " +
        $"raw={trace.RawValue} resolved={trace.ResolvedValue}");
}
```

## Remarks

`AspectTokenTrace` is a diagnostics record produced by `AspectEngine.Apply`. During diagnostics creation, the engine walks each resolved aspect value and records every token dependency from the winning declaration's `AspectValue.Dependencies`.

`ProviderName` is taken from the `AspectEnvironment.Name` used for the apply operation. The current engine stores the resolved aspect value in both `RawValue` and `ResolvedValue`; `AspectTrace.Capture` later formats these fields into `token:` trace lines.

The record is immutable after construction and uses normal C# record value equality. It describes diagnostic evidence only; it does not resolve tokens, mutate the aspect environment, or apply values to UI properties.

## Constructors

| Name | Description |
| --- | --- |
| `AspectTokenTrace(AspectToken Token, string ProviderName, object? RawValue, object? ResolvedValue)` | Initializes a token trace with the token dependency, provider name, raw value, and resolved value. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Token` | `AspectToken` | Gets the aspect token dependency recorded for the resolved declaration. |
| `ProviderName` | `string` | Gets the name of the aspect environment that provided the token context. |
| `RawValue` | `object?` | Gets the raw diagnostic value recorded for the token dependency. |
| `ResolvedValue` | `object?` | Gets the resolved diagnostic value recorded for the token dependency. |

## Applies to

Cerneala UI aspect diagnostics produced by `AspectEngine.Apply` and exposed through `AspectDiagnostics.Snapshot.TokenTraces`.

## See also

- `AspectDiagnostics`
- `AspectEngine`
- `AspectEnvironment`
- `AspectToken`
- `AspectTrace`
