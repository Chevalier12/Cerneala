# PlatformDiagnosticsSnapshot Record

Namespace: `Cerneala.UI.Detective`

Assembly: `Cerneala.dll`

Source: [`UI/Detective/Detective.cs`](https://github.com/Chevalier12/Cerneala/blob/master/UI/Detective/Detective.cs)

## Definition

Stores availability flags for optional platform services configured on a `UIRoot`.

```csharp
public sealed record PlatformDiagnosticsSnapshot(
    bool HasClipboard,
    bool HasCursor,
    bool HasFileDialogs,
    bool HasTextInput,
    bool HasDpi,
    bool HasAccessibility);
```

## Examples

```csharp
PlatformDiagnosticsSnapshot platform = root.Detective.Capture(stats).Platform;
```

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `HasAccessibility` | `bool` | Gets whether accessibility services are configured. |
| `HasClipboard` | `bool` | Gets whether clipboard services are configured. |
| `HasCursor` | `bool` | Gets whether cursor services are configured. |
| `HasDpi` | `bool` | Gets whether DPI services are configured. |
| `HasFileDialogs` | `bool` | Gets whether file-dialog services are configured. |
| `HasTextInput` | `bool` | Gets whether text-input services are configured. |
