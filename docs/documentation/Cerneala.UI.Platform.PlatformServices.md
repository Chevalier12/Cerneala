# PlatformServices Class

## Definition
Namespace: `Cerneala.UI.Platform`

Assembly/Project: `Cerneala`

Source: `UI/Platform/PlatformServices.cs`

Provides an immutable implementation of `IPlatformServices` by grouping optional platform integration services.

```csharp
public sealed record PlatformServices(
    IClipboard? Clipboard = null,
    ICursorService? Cursor = null,
    IFileDialogService? FileDialogs = null,
    ITextInputPlatform? TextInput = null,
    IDpiProvider? Dpi = null,
    IAccessibilityPlatform? Accessibility = null,
    IReducedMotionSource? ReducedMotion = null) : IPlatformServices
```

Inheritance:
`object` -> `PlatformServices`

Implements:
`IPlatformServices`

## Examples

Create platform services for a UI host:

```csharp
using Cerneala.UI.Hosting;
using Cerneala.UI.Platform;

PlatformServices services = new(
    Clipboard: clipboardService,
    FileDialogs: fileDialogService);

UiHostOptions options = new()
{
    PlatformServices = services
};
```

Use the empty service set when no platform integrations are available:

```csharp
using Cerneala.UI.Platform;

IPlatformServices services = PlatformServices.Empty;
```

## Remarks

`PlatformServices` stores optional implementations for platform-specific features such as clipboard access, cursor handling, file dialogs, text input, DPI, accessibility, and reduced-motion preferences.

Each constructor parameter defaults to `null`, so callers can provide only the services available for the current host environment. `Empty` returns a shared instance with every service unset.

## Constructors

| Signature | Description |
| --- | --- |
| `PlatformServices(IClipboard? Clipboard = null, ICursorService? Cursor = null, IFileDialogService? FileDialogs = null, ITextInputPlatform? TextInput = null, IDpiProvider? Dpi = null, IAccessibilityPlatform? Accessibility = null, IReducedMotionSource? ReducedMotion = null)` | Initializes the platform service container with optional service implementations. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Clipboard` | `IClipboard?` | Gets the clipboard service, or `null` when clipboard integration is unavailable. |
| `Cursor` | `ICursorService?` | Gets the cursor service, or `null` when cursor integration is unavailable. |
| `FileDialogs` | `IFileDialogService?` | Gets the file dialog service, or `null` when file dialogs are unavailable. |
| `TextInput` | `ITextInputPlatform?` | Gets the text input platform service, or `null` when text input integration is unavailable. |
| `Dpi` | `IDpiProvider?` | Gets the DPI provider, or `null` when DPI integration is unavailable. |
| `Accessibility` | `IAccessibilityPlatform?` | Gets the accessibility platform service, or `null` when accessibility integration is unavailable. |
| `ReducedMotion` | `IReducedMotionSource?` | Gets the reduced-motion source, or `null` when reduced-motion preferences are unavailable. |
| `Empty` | `PlatformServices` | Gets a shared platform service set with no services configured. |

## Applies To

Cerneala UI hosting and platform integration APIs.

## See Also

- `Cerneala.UI.Platform.IPlatformServices`
- `Cerneala.UI.Hosting.UiHostOptions`
- `Cerneala.UI.Hosting.MonoGame.MonoGameUiHostOptions`
