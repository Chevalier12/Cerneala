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
using Cerneala.UI.Hosting.Windowing;
using Cerneala.UI.Hosting.Windows;

WindowsDxApplicationBackend.EnsureRegistered();
int exitCode = GeneratedWindowApplication.Run(descriptor, args);
```

## Remarks

Generated Cerneala application startup code calls `EnsureRegistered` before creating a native window. Call it explicitly when using `GeneratedWindowApplication` without generated startup code.

The method registers one atomic windowing backend: `Cerneala.Platforms.Win32` supplies native windows, input, cursors, DPI handling, and Windows GPU preference setup, while `Cerneala.Backends.MonoGame` supplies the WindowsDX graphics session. Registration is idempotent for this backend. Registering a different windowing backend in the same process is rejected.

The generic host exchanges an opaque window surface. The WindowsDX factory accepts only the Win32 surface owned by `Cerneala.Platforms.Win32`, so an incompatible platform and graphics implementation fail before graphics-device creation.

## Methods

| Name | Description |
| --- | --- |
| `EnsureRegistered` | Ensures that the composed Win32 and WindowsDX windowing backend is available to the window host. |

## Applies to

Windows desktop applications that reference the `Cerneala.Backends.MonoGame` project or package. The backend carries `Cerneala.Platforms.Win32` as a project or package dependency.

## See also

- `GeneratedWindowApplication`
- `GeneratedWindowStartupDescriptor`
