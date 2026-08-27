# ApplicationBackendAttribute Class

## Definition

Namespace: `Cerneala.UI.Hosting.Windowing`

Assembly/Project: `Cerneala`

Source: `UI/Hosting/Windowing/ApplicationBackendAttribute.cs`

Selects the windowing backend used by source-generated application startup.

```csharp
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class ApplicationBackendAttribute : Attribute
```

Inheritance:
`object` -> `System.Attribute` -> `ApplicationBackendAttribute`

Attributes:
`System.AttributeUsageAttribute`

## Examples

Select WindowsDX once for the executable assembly:

```csharp
[assembly: Cerneala.UI.Hosting.Windowing.ApplicationBackend(
    typeof(Cerneala.UI.Hosting.Windows.WindowsDxApplicationBackend))]
```

Or select SDL3 + SDL_GPU from an executable that references the SDL backend:

```csharp
[assembly: Cerneala.UI.Hosting.Windowing.ApplicationBackend(
    typeof(Cerneala.UI.Hosting.Sdl.SdlGpuApplicationBackend))]
```

The selected type may be static or concrete, but it must be public and non-generic and expose this exact registration method:

```csharp
public static void EnsureRegistered();
```

## Remarks

`UiMarkupGenerator` requires exactly one valid declaration in an executable for which it generates a standalone `Main` or hosted module initializer. It resolves the selected type through Roslyn and emits a fully qualified call to `EnsureRegistered` before starting or registering the generated application.

The attribute belongs to the backend-neutral core and stores only a `System.Type`; it does not reference a concrete adapter. Referencing a backend package therefore makes that backend available, while this assembly declaration selects it explicitly.

Only one backend composition may be active in a process. Re-registering the same selection is idempotent; attempting to mix WindowsDX and SDL_GPU is rejected by the windowing registry.

Missing, duplicate, inaccessible, generic, abstract, non-class, or signature-incompatible selections produce `CERNEALAUI015`. The generator does not emit partial startup when this diagnostic is reported. A library that receives no generated startup does not need a backend declaration.

The constructor throws `ArgumentNullException` when `backendType` is `null`.

## Constructors

| Signature | Description |
| --- | --- |
| `ApplicationBackendAttribute(Type backendType)` | Creates an assembly-level backend selection and rejects a null type. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `BackendType` | `System.Type` | Gets the backend composition type selected for generated startup. |

## Applies to

Executable Cerneala projects that use generated `<Application>` or legacy `MainWindow` startup.

## See also

- `Cerneala.SourceGen.UiMarkupGenerator`
- `Cerneala.UI.Hosting.Sdl.SdlGpuApplicationBackend`
- `Cerneala.UI.Hosting.Windows.WindowsDxApplicationBackend`
- `docs/application-markup.md`
