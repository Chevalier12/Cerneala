# RuntimePlatformDiagnosticsSnapshot Record

## Definition
Namespace: `Cerneala.UI.Diagnostics`

Assembly/Project: `Cerneala`

Source: [`UI/Diagnostics/RuntimeDiagnostics.cs`](../../UI/Diagnostics/RuntimeDiagnostics.cs)

Represents platform-service availability flags captured during a runtime diagnostics snapshot.

```csharp
public sealed record RuntimePlatformDiagnosticsSnapshot(
    bool HasClipboard,
    bool HasCursor,
    bool HasFileDialogs,
    bool HasTextInput,
    bool HasDpi,
    bool HasAccessibility)
```

Inheritance:
`Object` -> `RuntimePlatformDiagnosticsSnapshot`

Implements:
`IEquatable<RuntimePlatformDiagnosticsSnapshot>`

## Examples

Capture platform diagnostics through `RuntimeDiagnostics.Capture`:

```csharp
using Cerneala.UI.Diagnostics;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Invalidation;

UIRoot root = new(100, 100);
UiViewport viewport = new(100, 100);
FrameStats stats = new();

RuntimeDiagnosticsSnapshot snapshot = RuntimeDiagnostics.Capture(root, viewport, stats);

bool hasClipboard = snapshot.Platform.HasClipboard;
bool hasCursor = snapshot.Platform.HasCursor;
bool hasTextInput = snapshot.Platform.HasTextInput;
```

## Remarks

`RuntimePlatformDiagnosticsSnapshot` is the `Platform` component of `RuntimeDiagnosticsSnapshot`. `RuntimeDiagnostics.Capture` creates it from `UIRoot.PlatformServices` by checking whether each optional service reference is available.

The captured flags report the presence of clipboard, cursor, file dialog, text input, DPI, and accessibility services. They do not invoke those services and do not validate whether a service can complete a platform operation.

When a root has no optional platform services configured, all flags are `false`. `RuntimeDiagnosticsSnapshot.ToString()` includes selected platform flags for clipboard and cursor in the compact runtime diagnostics line.

The type is a positional record. Its public constructor stores the supplied flags as-is, so direct construction should pass values that match the platform services being described.

## Constructors

| Name | Description |
| --- | --- |
| `RuntimePlatformDiagnosticsSnapshot(bool hasClipboard, bool hasCursor, bool hasFileDialogs, bool hasTextInput, bool hasDpi, bool hasAccessibility)` | Initializes the snapshot with platform-service availability flags. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `HasClipboard` | `bool` | Gets whether clipboard platform services were available on the captured root. |
| `HasCursor` | `bool` | Gets whether cursor platform services were available on the captured root. |
| `HasFileDialogs` | `bool` | Gets whether file dialog platform services were available on the captured root. |
| `HasTextInput` | `bool` | Gets whether text input platform services were available on the captured root. |
| `HasDpi` | `bool` | Gets whether DPI platform services were available on the captured root. |
| `HasAccessibility` | `bool` | Gets whether accessibility platform services were available on the captured root. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Deconstruct(out bool HasClipboard, out bool HasCursor, out bool HasFileDialogs, out bool HasTextInput, out bool HasDpi, out bool HasAccessibility)` | `void` | Deconstructs the positional record into its public component values. |
| `ToString()` | `string` | Returns the compiler-generated positional record string for the snapshot. |

## Applies To

Cerneala retained UI runtime diagnostics.

## See Also

- [`RuntimeDiagnostics`](Cerneala.UI.Diagnostics.RuntimeDiagnostics.md)
- [`RuntimeDiagnosticsSnapshot`](Cerneala.UI.Diagnostics.RuntimeDiagnosticsSnapshot.md)
- [`UIRoot`](Cerneala.UI.Elements.UIRoot.md)
- [`PlatformServices`](Cerneala.UI.Platform.PlatformServices.md)
