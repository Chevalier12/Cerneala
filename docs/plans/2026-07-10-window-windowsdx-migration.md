# Plan: `Window` migration to Win32 + MonoGame WindowsDX

## Summary

We are replacing the generic Skia backend now used by native windows with MonoGame WindowsDX rendering. Cerneala remains Windows-first at this stage, keeps Win32 windows and uses Skia + HarfBuzz only for the text pipeline.

The application does not expose `Game1`, `Game.Run()` or startup boilerplate. The Cerneala runtime has a single Win32 message pump and a collection of graphics sessions, one for each `Window`.
```text
Proces Cerneala
└── WindowApplicationRuntime
    ├── WindowContext A
    │   ├── HWND A
    │   ├── GraphicsDevice A
    │   ├── swap-chain A
    │   ├── MonoGameDrawingBackend A
    │   ├── UIRoot A
    │   └── Win32InputSource A
    ├── WindowContext B
    └── WindowContext C
```
## Fixed decisions

- V1 is Windows-only and uses `MonoGame.Framework.WindowsDX` `3.8.4.1`.
- Win32 remains responsible for `HWND`, messages, input, focus, DPI, ownership, dialogues and lifecycle.
- MonoGame is the only general UI rendering backend.
- Each window gets its own `GraphicsDevice`, its own swap-chain and its own GPU resources.
- The runtime uses a single UI thread and a single message pump for all windows.
- We do not create instances of `Game` and we do not run multiple `Game.Run()`. We directly use the public APIs `GraphicsDevice`, `SpriteBatch` and `PresentationParameters`.
- `PresentationParameters.DeviceWindowHandle` receives the `HWND` of the Cerneala window.
- Skia and HarfBuzz remain exclusively in the measurement, shaping and rasterization of the text. The resulting texture is loaded and drawn by MonoGame.
- We don't start secondary processes and we don't use reflection to access MonoGame internals.
- The public API `Window`, the paired markup generator and the startup without `Program.cs` remain unchanged.

## Motivation

The current implementation correctly creates native Win32 windows, but `Win32WindowPlatform` builds a `SkiaDrawingBackend` that draws all UI commands in a BGRA bitmap and presents it through Win32. This violates the architectural limit of the project: Skia must serve the text, not become the general renderer.

WindowsDX allows avoiding a MonoGame fork:

- `GraphicsDevice` has a public builder;
- `PresentationParameters.DeviceWindowHandle` is public;
- the WindowsDX backend creates the swap-chain for the provided handle;
- `GraphicsDevice.Present()` presents the associated swap-chain.

DesktopGL is not used at this stage because its implementation binds the OpenGL context to the internal singleton `SdlGameWindow.Instance`, which prevents the clean association of multiple Cerneala windows through the public API.

References:

- <https://docs.monogame.net/api/Microsoft.Xna.Framework.Graphics.GraphicsDevice.html>
- <https://docs.monogame.net/api/Microsoft.Xna.Framework.Graphics.PresentationParameters.html>
- <https://github.com/MonoGame/MonoGame/blob/v3.8.4.1/MonoGame.Framework/Platform/Graphics/GraphicsDevice.DirectX.cs>
- <https://github.com/MonoGame/MonoGame/blob/v3.8.4.1/MonoGame.Framework/Platform/Graphics/GraphicsDevice.OpenGL.cs>

## Contracts kept

We do not change the already implemented public behavior:

- `Show()`, `Hide()`, `Activate()` and `Close()`;
- `ShowDialogAsync()`, `Owner`, `OwnedWindows` and `DialogResult`;
- `SourceInitialized`, `Initialized`, `Loaded`, `ContentRendered`, `Closing` and `Closed`;
- the prohibition to reopen a closed window;
- closing the owned windows and shutdown policy for `MainWindow`;
- thread affinity for all Window operations;
- Logical DPI for layout and physical pixels for backbuffer;
- `MainWindow.crn` + `MainWindow.crn.cs`;
- the constructor and the generated entry point;
- integration of DI and `App.ConfigureServices`;
- `@when`, `@if`, resources, aspects, names and event handlers.

## Non-objectives

- Linux, macOS, DesktopGL or Vulkan;
- several processes;
- courts independent of `Game`;
- sharing `Texture2D` objects between windows;
- a single `GraphicsDevice` with several swap-chains;
- custom chrome, transparency or new borderless windows;
- changes to the markup grammar;
- redesign for `MonoGameUiHost` used in existing games, apart from the necessary adaptations for WindowsDX.

## Target architecture

### `WindowApplicationRuntime`

The runtime remains the owner of all windows and continues to pump messages only once:
```csharp
while (windows.Count > 0)
{
    platform.PumpEvents();

    foreach (WindowContext context in contexts)
    {
        context.Update(elapsedTime);
        context.RenderIfNeeded();
    }
}
```
We do not introduce a second scheduler. `WindowApplicationRuntime` continues to decide when there is retained work, active motion or a requested repaint.

### `Win32WindowPlatform`

The platform continues to create and own each `HWND`. After creating the handle, ask a graphics factory to build the WindowsDX session.
```text
CreateWindow
├── CreateWindowEx
├── determine DPI and client size
├── create Win32InputSource
└── create WindowGraphicsSession(hwnd, pixelWidth, pixelHeight)
```
`WM_SIZE` and `WM_DPICHANGED` update the viewport and request the resizing of the backbuffer. The zero dimensions in the minimization do not reset the device and do not trigger rendering.

### `WindowGraphicsSession`

We introduce a testable internal abstraction:
```csharp
internal interface IWindowGraphicsSession : IDisposable
{
    IDrawingBackend DrawingBackend { get; }
    IImageLoader ImageLoader { get; }
    ImageResourceCache ImageResourceCache { get; }

    void Resize(int pixelWidth, int pixelHeight, float coordinateScale);
    void BeginFrame(Color clearColor);
    void Present();
}
```
The `WindowsDxWindowGraphicsSession` implementation has:

- `GraphicsDevice`;
- `SpriteBatch`;
- white texture 1x1;
- `SkiaTextRasterizer`;
- `MonoGameDrawingBackend`;
- `MonoGameImageLoader` and the image cache of the window;
- `PresentationParameters` currents.

Creating the device uses the public API:
```csharp
PresentationParameters parameters = new()
{
    DeviceWindowHandle = hwnd,
    BackBufferWidth = pixelWidth,
    BackBufferHeight = pixelHeight,
    BackBufferFormat = SurfaceFormat.Color,
    DepthStencilFormat = DepthFormat.None,
    IsFullScreen = false,
    PresentationInterval = PresentInterval.One
};

GraphicsDevice device = new(
    GraphicsAdapter.DefaultAdapter,
    GraphicsProfile.HiDef,
    parameters);
```
Creation errors must be converted into a descriptive Cerneala exception that includes the adapter, profile, size, and handle, without losing the original exception.

### GPU resources

MonoGame resources are bound to `GraphicsDevice`, so GPU caches become per-window:

- a path-backed image can share bytes/decoded pixels at the CPU level;
- each session loads its own instance `Texture2D`;
- shaped/rasterized text can share CPU results only if the DPI/font identity is compatible;
- the text texture remains in the `MonoGameDrawingBackend` cache of the session;
- a `MonoGameImage` created for device A cannot be drawn by device B.

`IWindowPlatform.ImageLoader` and `ImageResourceCache` can no longer be global. They move to `IPlatformWindow` or `IWindowGraphicsSession`, and `WindowApplicationRuntime` attaches them to the corresponding `UIRoot`.

### Text

The target pipeline is:
```text
TextBlock
→ HarfBuzz shaping
→ Skia metrics/rasterization
→ RGBA pixels
→ Texture2D pe GraphicsDevice-ul ferestrei
→ MonoGameDrawingBackend
→ swap-chain WindowsDX
```
`SkiaDrawingBackend` does not participate in this stream. `SkiaFont`, `SkiaTextShaper` and `SkiaTextRasterizer` remain valid.

### Input

`Win32InputSource` remains per window. We do not use static `Keyboard`, `Mouse` or `GamePad` from MonoGame for Window hosting.

Messages are naturally routed to `HWND`:

- pointer and wheel;
- keyboard and focus;
- input text;
- resize and move;
- activated/deactivated;
- close;
- DPI change.

Thus, two windows cannot consume or overwrite each other's input.

## Project changes

1. We replace `MonoGame.Framework.DesktopGL` with `MonoGame.Framework.WindowsDX` `3.8.4.1`.
2. We move the runtime projects to `net8.0-windows`:
   - `Cerneala.csproj`;
   - Playground;
   - runtime tests referencing Cerneala.
3. `Cerneala.SourceGen` remains `netstandard2.0`.
4. We remove `SkiaSharp.NativeAssets.Linux` from the Windows-first configuration.
5. We check the NuGet graph for Skia Windows native assets and explicitly add the Win32 package only if it is not already transitive.
6. We add the strictly necessary Windows SDK properties, without activating WinForms or WPF by default.
7. We keep `AllowUnsafeBlocks` disabled if WindowsDX and the existing interop do not require it.

## Implementation plan

### Stage 0: RED/GREEN technical tests

Before migrating the runtime, we add Windows smoke tests in a separate process:

1. create a test `HWND`;
2. create a `GraphicsDevice` WindowsDX with that handle;
3. draw a color and call `Present()`;
4. we check by readback/capture that the surface is not empty;
5. create two `HWND` and two `GraphicsDevice` in the same process;
6. we present different colors in each;
7. we close the first session and demonstrate that the second continues to render;
8. we resize independently both backbuffers;
9. we impose timeout and forced cleanup in order not to block the suite.

This sample is the gateway. We are not removing the current backend until the dual window scenario is demonstrated on Windows.

### Stage 1: internal graphic limit

- add `IWindowGraphicsSession`;
- we add an injectable factory for tests;
- we adapt existing platforms;
- we temporarily keep a Skia implementation only to keep the tests green during the migration;
- we move the image loader/cache from the global platform to the window session.

### Step 2: WindowsDX session

- we implement `WindowsDxWindowGraphicsSession`;
- cream `GraphicsDevice`, `SpriteBatch`, white pixel and `MonoGameDrawingBackend`;
- we connect `SkiaTextRasterizer`;
- we implement begin frame, clear, draw and present;
- we implement resize with `GraphicsDevice.Reset` and the same `PresentationParameters`/`HWND`;
- we handle minimize, device lost, resize failure and partial disposal.

### Step 3: Win32 integration

- `Win32PlatformWindow` creates the session after `CreateWindowEx`;
- `DrawingBackend` comes from the session;
- `Present()` delegates to `GraphicsDevice.Present()`;
- remove the BGRA buffer, `WM_PAINT` the Skia flash and the bitmap resize;
- `WM_PAINT` validates the paint region and marks the context for repaint, without re-entering rendering in `WndProc`;
- `WM_SIZE` and `WM_DPICHANGED` reprogram the resize at runtime.

### Stage 4: resources and text

- we connect `MonoGameImageLoader` per window;
- we demonstrate that the same image can be loaded in two devices without sharing `Texture2D`;
- we keep the text cache per backend;
- we check shaping, line metrics, baseline, clipping and DPI;
- we remove the specific `SkiaDrawImage` conversions from Window hosting.

### Step 5: More windows

- we demonstrate two and then three simultaneous windows in the same PID;
- we check update/draw/present independently;
- hiding a window does not suspend the others;
- closing a window releases only the device and its resources;
- owner/dialog disable affects the input, not the scheduler or the owner's device;
- the closure of `MainWindow` continues to apply the existing Cerneala policy.

### Step 6: Remove general Skia backend

After WindowsDX is completely green:

- we remove `SkiaDrawingBackend` from `Win32WindowPlatform`;
- we eliminate `SkiaDrawImage` and `SkiaImageLoader` if Roslyn confirms that they no longer have legitimate consumers;
- we remove the tests of the general Skia backend or replace them with tests for Skia text components;
- we add an architecture test that prohibits the dependency of `UI/Hosting/Windows` on `Cerneala.Drawing.Skia.SkiaDrawingBackend`;
- we add a test that asks for `MonoGameDrawingBackend` for each real Window session.

### Stage 7: Playground and generator

- the markup generator and API do not change;
- Playground remains without `Program.cs` and without `Game1.cs`;
- the generated startup starts `WindowApplicationRuntime`;
- `MainWindow` is rendered by WindowsDX;
- we add a minimal secondary window in the integration tests, not necessarily in the initial showcase;
- we check that all windows have the PID of the Playground process.

## Testing

### Unit and contract

- the graphic factory receives exactly `HWND`, pixel size and DPI scale;
- each `WindowContext` receives another session;
- resize updates the viewport and the backbuffer only once;
- minimize does not create backbuffer `0x0`;
- `Hide()` keeps the device;
- `Close()` has backend, caches, SpriteBatch and GraphicsDevice exactly once;
- partial initialization errors do not leave `HWND`, devices or native resources active;
- thread affinity remains imposed.

### Windows rendering

- all `DrawCommandKind` are rendered by `MonoGameDrawingBackend`;
- clipping and scissoring are restored between frames;
- the Skia/HarfBuzz text ends up in a MonoGame texture and is visible;
- the path-backed image is visible;
- DPI 100%, 125%, 150% and 200%;
- repeated resize does not lose content and does not drain resources;
- distinct pixel checks for two simultaneous windows.

### Multi-window

- two and three `HWND` have the same PID;
- each has a distinct `GraphicsDevice`;
- focus and input are routed only to the target window;
- closing A does not affect the rendering of B;
- owner handle and modal disable are correct;
- nested dialogs complete their tasks with the correct result;
- the main shutdown releases all devices and handles.

### Compatibility

- all SourceGen tests remain green;
- `UserControl`, standalone markup, resources, aspects and `@when/@if` remain unchanged;
- `MonoGameUiHost` continues to work in an existing WindowsDX game;
- no runtime project references DesktopGL anymore;
- Skia general rendering is no longer accessible from Window hosting.

## Final check
```powershell
dotnet restore Cerneala.slnx
dotnet test Cerneala.slnx --no-restore
dotnet build Cerneala.slnx --no-restore
dotnet format Cerneala.slnx --no-restore --verify-no-changes
```
In addition:

- we run the Win32/WindowsDX smoke test in a separate process with timeout;
- launch Playground and check `MainWindow` plus at least one secondary window;
- check PID, `HWND`, resize, input, rendering and exit code `0`;
- we run `git diff --check`;
- we regenerate `FileTree.md`;
- we reindex `Cerneala.slnx` with RoslynIndexer after each code or project change.

## Risks and measures

### GPU cost per window

Each `GraphicsDevice` doubles GPU resources. V1 accepts the cost for isolation and simplicity. We are adding diagnostic counters per session and descriptive limits for allocation failures.

### Device lost and resize

WindowsDX can lose or reset the device. All resources created by the session should be rebuildable, and errors should be isolated to the affected window when possible.

### VSync with multiple windows

Multiple sequential `PresentInterval.One` calls can block the same thread. The technical test measures the cost. If necessary, the runtime uses VSync only for the active window or switches secondary sessions to `PresentInterval.Immediate`, without changing the public API.

### WindowsDX dependency
WindowsDX makes the runtime Windows-only. This is an explicit decision for the V1, not a hidden accident. Internal contracts remain separate so that a future backend can implement the same `IWindowGraphicsSession`.

## Acceptance criteria

The migration is complete only when:

- no Cerneala window uses `SkiaDrawingBackend` for general rendering;
- `MainWindow` and secondary windows are rendered by `MonoGameDrawingBackend`;
- each window has its own `GraphicsDevice` and its own swap-chain;
- two windows can render, receive input, be resized and closed independently in the same process;
- Skia/HarfBuzz are used only for text;
- Playground does not contain `Game1.cs`, `Program.cs` or startup boilerplate;
- all tests, the build, the formatter and the native smoke test are green;
- the process closes with exit code `0` and no handles/devices left active.