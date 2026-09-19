# UI Layer Boundaries

This diagram shows where each layer is allowed to depend.

```text
┌───────────────────────────────────────────────────────────────┐
│                    Application / Playground                   │
└───────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌───────────────────────────────────────────────────────────────┐
│                         UI Hosting                            │
│  WindowApplicationRuntime composes registered platform/backend │
│  owners; concrete SDL code does not live in core UI elements.   │
└───────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌───────────────────────────────────────────────────────────────┐
│                       Retained UI Core                        │
├───────────────────────────────────────────────────────────────┤
│ UI/Core       typed state                                      │
│ UI/Elements   logical + visual trees                           │
│ UI/Layout     measure / arrange                                │
│ UI/Rendering  retained render cache                            │
│ UI/Input      retained route bridge / focus / commands         │
│ UI/Styling    metadata-driven visual state                     │
└───────────────────────────────────────────────────────────────┘
                 │                              │
                 ▼                              ▼
┌───────────────────────────────┐  ┌────────────────────────────┐
│          Drawing           │  │          UI/Input          │
├───────────────────────────────┤  ├────────────────────────────┤
│ DrawingContext                │  │ IInputSource               │
│ DrawCommandList               │  │ InputFrame                 │
│ DrawCommand                   │  │ RoutedEvent metadata       │
│ DrawRect / DrawPoint          │  │ RoutedEventArgs            │
│ Color                         │  │ command primitives         │
│ IDrawingBackend               │  │ retained route bridge      │
└───────────────────────────────┘  └────────────────────────────┘
                 │                              │
                 ▼                              ▼
┌───────────────────────────────┐  ┌────────────────────────────┐
│ Cerneala.Backends.SdlGpu      │  │ Cerneala.Platforms.Sdl3   │
├───────────────────────────────┤  ├────────────────────────────┤
│ SdlGpuDrawingBackend          │  │ SdlWindowPlatform          │
│ Cerberus                      │  │ SdlInputSource             │
│ SdlGpuImageLoader             │  │ SDL window/input events    │
│ SDL_GPU resources             │  │ platform services          │
└───────────────────────────────┘  └────────────────────────────┘
```

## Boundary Rules

- UI core must not reference SDL3/SDL_GPU handles or calls, Skia, HarfBuzz, or
  another concrete platform/backend API.
- Controls render through retained render caches and `DrawingContext`.
- Controls consume input through retained input/focus/command services, not
  through SDL3 directly.
- `Drawing` remains a command layer, not a scene graph.
- `UI/Input` remains an input foundation; v2 route ownership moves to the retained tree.
- SDL3 platform code and SDL_GPU rendering code stay in their adapter projects.

## Allowed Direction

```text
UI core -> Drawing abstractions
UI core -> UI/Input abstractions
Platform adapter -> SDL3
Drawing backend -> SDL_GPU
```

## Disallowed Direction

```text
UI core -> SDL3/SDL_GPU
UI core -> Skia/HarfBuzz
Drawing core -> SDL_GPU
UI/Input core -> SDL3
Controls -> backend-specific rendering/input APIs
```
