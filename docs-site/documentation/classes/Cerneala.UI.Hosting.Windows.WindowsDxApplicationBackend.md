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
[assembly: Cerneala.UI.Hosting.Windowing.ApplicationBackend(
    typeof(Cerneala.UI.Hosting.Windows.WindowsDxApplicationBackend))]
```

## Remarks

Generated Cerneala application startup calls `EnsureRegistered` when this type is selected through `ApplicationBackendAttribute`. The selection is explicit and assembly-scoped; merely referencing the WindowsDX backend does not make the generator choose it. Call `EnsureRegistered` directly when using `GeneratedWindowApplication` without generated startup code.

The method registers one atomic windowing backend: `Cerneala.Platforms.Win32` supplies native windows, input, cursors, DPI handling, and Windows GPU preference setup, while `Cerneala.Backends.MonoGame` supplies the WindowsDX graphics session. Registration is idempotent for this backend. Registering SDL_GPU after WindowsDX, or WindowsDX after SDL_GPU, is rejected deterministically because a process may own only one windowing backend.

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
- `ApplicationBackendAttribute`
- `Cerneala.UI.Hosting.Sdl.SdlGpuApplicationBackend`
