# Hosting And Platform

> Code: `UI/Application.cs`, `UI/Hosting`, `UI/Hosting/Windowing`, `UI/Platform` · Verified at commit `0cb32300` (2026-10-10)

## Responsibility

Hosting turns a generated startup into running windows. It:

- takes the backend registered by the generated startup;
- builds the `Application`;
- creates one `UIRoot` and one `UiHost` for each shown window;
- pumps platform events and frames;
- closes windows and decides when the application exits.

`UI/Platform` declares the service contracts a platform can fill, such as cursor, clipboard and text input.

Hosting does not draw, route input or touch SDL. Drawing belongs to the graphics session of each window, input routing to `UiHost` and [input](input.md). SDL belongs to `Cerneala.Platforms.Sdl3` and `Cerneala.Backends.SdlGpu`, described in [sdl-desktop-backend.md](sdl-desktop-backend.md).

## Components

| Type | File | Role | Owner |
|---|---|---|---|
| `Application` | `UI/Application.cs` | Public application object: services, `Resources`, `MainWindow`, `Windows`, `ShutdownMode`, `Startup`/`Exit`, `TimbreRuntime`, `Shutdown` | one per UI thread (`Application.Current`) |
| `ApplicationShutdownMode` | `UI/ApplicationShutdownMode.cs` | `OnLastWindowClose` (default), `OnMainWindowClose`, `OnExplicitShutdown` | value |
| `ApplicationBackendAttribute` | `UI/Hosting/Windowing/ApplicationBackendAttribute.cs` | Assembly attribute that names the backend type (`AllowMultiple = false`) | user assembly |
| `IWindowingBackend`, `WindowingBackendRegistry` | `UI/Hosting/Windowing/WindowingBackendRegistry.cs` | Internal factory contract and the process-wide registry that holds one backend | process (static) |
| `GeneratedWindowStartupDescriptor`, `GeneratedWindowApplication` | `UI/Hosting/Windowing/GeneratedWindowApplication.cs` | The startup bundle the generator builds, and the entry point (`Run`, `RegisterStartup`) | generated code; static |
| `WindowApplicationRuntime` | `UI/Hosting/Windowing/WindowApplicationRuntime.cs` | Internal. Owns the platform, the shared `ThemeProvider`, the window list and the frame loop (`PumpOnce`, `RunStandalone`) | process (`WindowApplicationRuntime.Current`) |
| `WindowContext` (nested) | same file | Per-window state: `IPlatformWindow`, `UIRoot`, `UiHost`, render flags, Servo queues | runtime, one per shown window |
| `IWindowPlatform`, `IPlatformWindow`, `IWindowSurface`, `IWindowGraphicsSession`, `IWindowGraphicsSessionFactory`, `IWindowPlatformCallbacks` | `UI/Hosting/Windowing/IWindowPlatform.cs` | Internal seams to the native layer: create windows, pump events, begin and present frames, report close/focus/bounds | platform (backend) |
| `UiHost`, `UiHostOptions`, `UiFrame` | `UI/Hosting/UiHost.cs`, `UiHostOptions.cs`, `UiFrame.cs` | Per-window update and draw around the scheduler; see [invalidation-and-frame.md](invalidation-and-frame.md#where-the-scheduler-runs) | `WindowContext` |
| `UiViewport`, `UiCoordinateMapper` | `UI/Hosting/UiViewport.cs`, `UiCoordinateMapper.cs` | Logical size plus scale; logical/physical conversion | value; static |
| `IUiBackend` | `UI/Hosting/IUiBackend.cs` | Public bundle of `InputSource`, `DrawingBackend`, `BackdropFrameSource` for `UiHostOptions.Backend` | no production implementer |
| `IPlatformServices`, `PlatformServices` | `UI/Platform/IPlatformServices.cs`, `PlatformServices.cs` | Seven optional service slots; `PlatformServices.Empty` has all of them `null` | platform; each `UIRoot` holds one |
| `IClipboard`, `ICursorService`, `IFileDialogService`, `ITextInputPlatform`, `IDpiProvider`, `IAccessibilityPlatform` | `UI/Platform/*.cs` | The service contracts. The seventh slot, `IReducedMotionSource`, lives in `UI/Motion/Core` | platform |

`SdlGpuApplicationBackend` (`Cerneala.Backends.SdlGpu/Hosting/SdlGpuApplicationBackend.cs`, namespace `Cerneala.UI.Hosting.Sdl`) is the only production backend. Its nested `SdlGpuWindowingBackend` implements `IWindowingBackend`, and `SdlWindowPlatform` implements `IWindowPlatform`.

## Data Flow

### Backend registration

The application assembly names its backend once:

```csharp
[assembly: Cerneala.UI.Hosting.Windowing.ApplicationBackend(
    typeof(Cerneala.UI.Hosting.Sdl.SdlGpuApplicationBackend))]
```

The generated startup calls `SdlGpuApplicationBackend.EnsureRegistered()` before it calls `GeneratedWindowApplication.Run` or `RegisterStartup`. The generated shapes are described in [markup-and-sourcegen.md](markup-and-sourcegen.md#backend-selection-and-startup).

`EnsureRegistered` calls `WindowingBackendRegistry.Register(SdlGpuWindowingBackend.Instance)`. The registry, under one lock:

- stores the first backend;
- does nothing when a backend of the same type registers again;
- throws for a backend of a different type. Example message: `Windowing backend 'Cerneala.UI.Hosting.Sdl.SdlGpuApplicationBackend+SdlGpuWindowingBackend' is already registered.`

The platform is created only when the first runtime is created. `WindowingBackendRegistry.CreatePlatform(useMultisampling, coordinateScaleOverride: null)` calls the backend. Without a backend it throws `No windowing backend is registered. Register an application backend before creating a window.` For SDL, `CreatePlatform` builds one `NativeSdlApi`, an `SdlGpuWindowGraphicsSessionFactory`, a `NativeSdlAudioApi` and the `SdlWindowPlatform`.

### Startup

`GeneratedWindowApplication.Run(descriptor, args)` runs these stages in order:

1. `construct Application`: calls the generated `CreateApplication`.
2. Creates the runtime with `WindowApplicationRuntime.GetOrCreateDefault(application.UseMultisampling)`. This creates the platform, as described above.
3. `install Application.Current`: `Application.Install(runtime)` sets `Application.Current`, reads the platform's audio output and calls `runtime.SetApplication`.
4. `configure and build services`: runs the generated `ConfigureServices` and then the application's `ConfigureServices`, and builds the provider with `ValidateScopes = true`.
5. `run Application startup`: raises `Startup` with the arguments.
6. `resolve and create startup Window`: calls `CreateMainWindow(application.Services)`.
7. `show startup Window`: calls `runtime.StartMainWindow(window)`. This sets `Application.MainWindow` and shows the window.
8. `RunStandalone(application)` loops until shutdown is requested. Then `Run` returns `application.ExitCode`, and its `finally` block disposes the runtime.

If a stage throws, the exception keeps going, with two extra entries:

- `exception.Data["Cerneala.StartupStage"]`, for example `"run Application startup"`;
- `exception.Data["Cerneala.StartupTarget"]`, the startup window type name.

If `Startup` calls `Shutdown()`, stages 6 and 7 and the loop are skipped. `Run` then returns the exit code at once.

When the descriptor has no `CreateApplication`, a markup `MainWindow` without an `Application`, `Run` takes the legacy path. It builds the services, creates the window and runs `RunStandalone(window)` until that window closes. In this case it returns `0`.

### Showing a window

`window.Show()` calls `runtime.Show(window, modal: false)`. For a window that is not open yet, `GetOrCreateContext` does the following, in order:

1. `platform.CreateWindow(window, callbacks)`. SDL creates a hidden native window and its graphics session.
2. `new UIRoot(width, height, scale)` from the platform window's viewport.
3. On the root:
   - `SetThemeProvider(themeProvider)`, one provider shared by every window;
   - `SetResourceProvider(application?.Resources ?? resourceProvider)`;
   - `SetPlatformServices(platformServices)`;
   - `SetTimbreRuntime(application.TimbreRuntime)`, only when an application is attached;
   - `SetImageResourceCache(...)` from the graphics session.
4. A new `UiHost` with the root, the viewport, the platform window's `InputSource` and the platform services.
5. Registration in the runtime's dictionary and window list, `ApplyProperties`, and the native owner when `window.Owner` is set.
6. `window` is added to the root's logical and visual children.

`PresentShownWindow` then renders the first frame. It calls `PlatformWindow.Show()` only after that frame rendered, so the native window never appears empty.

### One loop iteration

`RunStandalone` measures time with a `Stopwatch` and calls `PumpOnce(now - previous)`:

1. `platform.PumpEvents()`. SDL turns native events into callbacks (`RequestClose`, `ActivationChanged`, `BoundsChanged`, `RenderRequested`) and into the window's input source.
2. For each window: a hidden window, or one with a zero-size viewport, is skipped. Otherwise the host advances its render time.
3. The window is rendered only when one of these is true:
   - `RenderRequested`;
   - `Root.Relay.HasPendingWork`;
   - `Root.Scheduler.HasWork`;
   - `Root.Motion.HasActiveMotion`;
   - an active pointer repeat.
4. `Render`:
   - collects the platform `InputFrame` and runs `UiHost.Update`;
   - calls `graphicsSession.BeginFrame(Color.White)` and `UiHost.Draw`;
   - calls `graphicsSession.CompleteFrame(present: !IsPreview)`;
   - raises the window's frame-rendered and, the first time, content-rendered notifications.
5. When no window rendered, the loop sleeps with `Thread.Sleep(1)`.

Example: a window with no changes and no animation. Steps 3 and 4 do nothing, and the loop pumps events and sleeps 1 ms on every iteration. Moving the mouse over it sets `RenderRequested` (step 1), so the next iteration renders exactly one frame.

### Closing and exit

`window.Close()` calls `runtime.Close(window, force: false)`. The steps, in order:

1. `Closing` is raised. Its handler can cancel the close, which then returns `false`.
2. Owned windows are closed with `force: true`.
3. The context is removed. If the window was modal, its owner is enabled again.
4. On the root:
   - the window is removed from the root's children;
   - `ReleaseDrawingResources()` runs;
   - `SetTimbreRuntime(null)` runs.
5. `PlatformWindow.Destroy()`. SDL disposes the graphics session first and then destroys the native window.
6. `context.Dispose()`. Pending Servo input and screenshot operations fail with a `ServoException`.
7. The window is marked closed, and `Closed` is raised.
8. `Application.HandleWindowClosed(window)` applies `ShutdownMode`:

| Mode | Shuts down when |
|---|---|
| `OnLastWindowClose` | `runtime.Windows.Count == 0` |
| `OnMainWindowClose` | the closed window is the current `MainWindow` (read at close time, not at startup) |
| `OnExplicitShutdown` | never; only `Shutdown()` |

`Shutdown(exitCode)`:

1. sets the flag;
2. closes every window with `force: true`;
3. calls `CompleteExit`, which raises `Exit` once, disposes the service provider, retires the owned Timbre runtime and clears `Application.Current`.

The loop then sees `IsShutdownRequested` and ends.

### Platform services

The runtime takes the services passed to its constructor, or else `platform.PlatformServices`, and gives the same object to every root. `UIRoot.SetPlatformServices(null)` stores `PlatformServices.Empty`. The SDL platform fills two of the seven slots:

| Slot | SDL value |
|---|---|
| `Cursor` | `SdlCursorService` |
| `TextInput` | `SdlTextInputPlatform`, which only reports `SupportsIme => true` |
| `Clipboard`, `FileDialogs`, `Dpi`, `Accessibility`, `ReducedMotion` | `null` |

## Lifecycle And Ownership

| Scope | State | Created | Released |
|---|---|---|---|
| Process | `WindowingBackendRegistry` backend | first `EnsureRegistered` | never; tests use `ResetForTesting` |
| Process | `WindowApplicationRuntime.Current` | first `GetOrCreateDefault`/`CurrentOrDefault` | `Dispose`; a second installed runtime throws `A Window application runtime is already installed in this process.` |
| UI thread | `Application.Current` | `Install` | `CompleteExit`; a second application throws `Only one Application may be installed on the UI thread.` |
| Runtime | `IWindowPlatform`, `ThemeProvider` (default `DefaultTheme.Create()`) | runtime constructor | `platform.Dispose()` in `Dispose` |
| Window | `WindowContext`: `IPlatformWindow`, `UIRoot`, `UiHost` | first `Show` | `Close` |
| Window | graphics session | `IWindowGraphicsSessionFactory.Create` inside the platform window | `IPlatformWindow.Destroy` |
| Application | service provider, owned `TimbreRuntime` | startup, first use | `CompleteExit` |

`UIRoot` is not `IDisposable`. Closing a window releases its drawing resources and detaches its Timbre runtime, and then the root is no longer referenced by the runtime.

`WindowApplicationRuntime.Dispose` closes every window with `force: true`, disposes the platform, clears `Current` and calls `CompleteExit` on the attached application. The SDL platform's `Dispose` order is:

1. `timbreOutput.Terminate()`;
2. remove the event watch;
3. dispose each window;
4. dispose the graphics session factory;
5. dispose the cursor service;
6. `SdlPlatformLifetime.Dispose()`, which calls `SDL_Quit`.

### Threads

Everything above runs on one UI thread, the thread that constructed the object:

| Owner | Check | Message |
|---|---|---|
| `Application` | `VerifyAccess` on every public member | `Application APIs must be called on the owning UI thread.` |
| `WindowApplicationRuntime` | `VerifyAccess` | `Window APIs must be called on the owning UI thread.` |
| `SdlPlatformLifetime` | `VerifyUiThread` in `CreateWindow`, `PumpEvents`, `Dispose` | `SDL platform access must remain on UI thread {id}.` |

Two SDL callbacks run outside the pump:

- the live-resize event watch, which renders immediately only when it runs on the owner thread;
- the audio-device-removed event, which arrives on an SDL audio thread and only notifies the Timbre output.

Work from other threads reaches a root through [Relay](relay.md).

## Frame Integration

Hosting adds no `FramePhase`. It owns the loop that decides whether a window renders and that calls `UiHost.Update` and `UiHost.Draw`. The phases inside `Update` are described in [invalidation-and-frame.md](invalidation-and-frame.md). `BeginFrame` and `CompleteFrame` belong to the window's graphics session.

## Invariants

- Registering the same backend type twice is accepted; the test passes when the second call does not throw. `tests/Cerneala.Tests/UI/Hosting/ApplicationBackendRegistrationTests.cs:SdlGpuRegistrationIsIdempotent`.
- A second backend of another type throws, in either order, with a message that names the backend already registered. `ApplicationBackendRegistrationTests.cs:SdlGpuThenAnotherBackendRegistrationFailsDeterministically`, `ApplicationBackendRegistrationTests.cs:AnotherBackendThenSdlGpuRegistrationFailsDeterministically`.
- `Application.UseMultisampling` reaches `IWindowingBackend.CreatePlatform` through `GeneratedWindowApplication.Run`. `ApplicationBackendRegistrationTests.cs:GeneratedStartupPassesApplicationMultisamplingPreferenceToBackend`.
- Every generated startup calls the selected `EnsureRegistered()` before `Run` or `RegisterStartup`. It uses `[STAThread]` for a generated `Main` and `[ModuleInitializer]` when the project has its own `Main`. `tests/Cerneala.Tests.SourceGen/UiMarkupGeneratorBackendSelectionTests.cs:ExplicitBackendSelectionControlsEveryGeneratedStartupPath`.
- An executable with no backend attribute gets the diagnostic "Exactly one ...". `UiMarkupGeneratorBackendSelectionTests.cs:ExecutableStartupRequiresAnExplicitBackendSelection`.
- `OnLastWindowClose` exits only after the last window closes; `Application.Current` becomes `null`. `tests/Cerneala.Tests/UI/Hosting/ApplicationRuntimeTests.cs:OnLastWindowCloseExitsOnlyAfterTheLastSuccessfulClose`.
- `OnMainWindowClose` uses the `MainWindow` set at close time. `ApplicationRuntimeTests.cs:OnMainWindowCloseUsesTheWindowDesignatedAtCloseTime`.
- `OnExplicitShutdown` keeps the application alive after every window closes. `ApplicationRuntimeTests.cs:OnExplicitShutdownKeepsApplicationAliveAfterAllWindowsClose`.
- The order is `Startup`, then `Exit` with the exit code, then service disposal, exactly once. `ApplicationRuntimeTests.cs:LifecyclePublishesServicesBeforeStartupAndDisposesAfterExit`.
- `Application` members throw from another thread. `ApplicationRuntimeTests.cs:LifecycleOperationsRejectAnotherThread`.
- Each window has its own graphics session. Showing renders one frame (one `BeginFrame`, one `Present`). Closing disposes only that window's session, and the other window keeps presenting. `tests/Cerneala.Tests/UI/Hosting/WindowRuntimeTests.cs:EachWindowUsesAndDisposesItsOwnGraphicsSession`.
- `Cerneala.Backends.SdlGpu` exports one public type, `SdlGpuApplicationBackend`, with one public method, `void EnsureRegistered()`. `tests/Cerneala.Tests.SdlGpu/SdlArchitectureTests.cs:SdlGpuBackendPublicApiIsLimitedToTheApplicationBootstrap`.
- The "No windowing backend is registered" exception: netestat.
- The SDL platform terminates audio before `SDL_Quit`: netestat at this level.

## Diagnostic

- Startup failures carry `Cerneala.StartupStage` and `Cerneala.StartupTarget` in `Exception.Data`. A cleanup failure during that unwind is stored as `Cerneala.CleanupFailure`.
- Each rendered frame publishes a `UiFrame` with `DiagnosticsTiming`. Its fields are input collection, retained update, begin frame, drawing, complete frame and the phase timings.
- Per-root diagnostics are in [Detective](detective.md).

## Known Limitations

- In production, the SDL platform fills only `Cursor` and `TextInput`, and `TextInput` only reports `SupportsIme`. Code that reads `PlatformServices.Clipboard`, `FileDialogs`, `Dpi`, `Accessibility` or `ReducedMotion` gets `null`.
- `IUiBackend` has no production implementer. Windows get their drawing backend from `IWindowGraphicsSession`, not from `UiHostOptions.Backend`.
- With a user `Main`, the generated module initializer only registers the descriptor (`RegisterStartup`). The descriptor runs through `GeneratedWindowApplication.PumpHosted`, which is `internal`. A text search found only tests calling it.
- The idle loop sleeps `Thread.Sleep(1)`; it does not wait on platform events or vsync. Idle CPU cost: nemăsurat.
- One backend type per process, one runtime per process, one `Application` per UI thread (see Lifecycle).
- `CreateDefault` always passes `coordinateScaleOverride: null`, so a generated application uses the scale reported by the platform.
