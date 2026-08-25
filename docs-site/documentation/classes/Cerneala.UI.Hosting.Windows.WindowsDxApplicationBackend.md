# WindowsDxApplicationBackend Class

## Definition
Namespace: `Cerneala.UI.Hosting.Windows`  
Assembly/Project: `Cerneala.Backends.MonoGame`  
Source: `UI/Hosting/Windows/WindowsDxApplicationBackend.cs`

Registers the Win32 window platform and WindowsDX graphics implementation used by Cerneala desktop applications.

```csharp
public static class WindowsDxApplicationBackend
```

## Examples

```csharp
WindowsDxApplicationBackend.EnsureRegistered();
int exitCode = GeneratedWindowApplication.Run(descriptor, args);
```

## Remarks

Generated Cerneala application startup code calls `EnsureRegistered` before creating a native window. Call it explicitly when using `GeneratedWindowApplication` without generated startup code.

The method composes two independent adapters: `Cerneala.Platforms.Win32` supplies native windows, input, cursors, DPI handling, and Windows GPU preference setup, while `Cerneala.Backends.MonoGame` supplies the WindowsDX graphics session. Registration is idempotent for this pair. Registering a different window platform or graphics backend in the same process is rejected by the host registries.

## Methods

| Name | Description |
| --- | --- |
| `EnsureRegistered` | Ensures that both the Win32 window platform and WindowsDX graphics backend are available to the window host. |

## Applies to

Windows desktop applications that reference the `Cerneala.Backends.MonoGame` project or package. The backend carries `Cerneala.Platforms.Win32` as a project or package dependency.

## See also

- `GeneratedWindowApplication`
- `GeneratedWindowStartupDescriptor`
