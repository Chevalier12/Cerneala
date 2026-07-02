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
│  MonoGameUiHost lives here, not inside core UI elements.       │
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
│          UI/Drawing           │  │          UI/Input          │
├───────────────────────────────┤  ├────────────────────────────┤
│ DrawingContext                │  │ IInputSource               │
│ DrawCommandList               │  │ InputFrame                 │
│ DrawCommand                   │  │ RoutedEvent metadata       │
│ DrawRect / DrawPoint          │  │ RoutedEventArgs            │
│ DrawColor                     │  │ command primitives         │
│ IDrawingBackend               │  │ MonoGameInputMapper        │
└───────────────────────────────┘  └────────────────────────────┘
                 │                              │
                 ▼                              ▼
┌───────────────────────────────┐  ┌────────────────────────────┐
│   UI/Drawing/MonoGame         │  │   UI/Input/MonoGame        │
├───────────────────────────────┤  ├────────────────────────────┤
│ MonoGameDrawingBackend        │  │ MonoGameInputSource        │
│ MonoGameImage                 │  │ Mouse.GetState             │
│ SpriteBatch                   │  │ Keyboard.GetState          │
│ Texture2D                     │  │ MonoGame keys/buttons      │
└───────────────────────────────┘  └────────────────────────────┘
```

## Boundary Rules

- UI core must not reference `SpriteBatch`, `Texture2D`, `Mouse.GetState()`, `Keyboard.GetState()`, Skia, or HarfBuzz.
- Controls render through retained render caches and `DrawingContext`.
- Controls consume input through retained input/focus/command services, not through MonoGame directly.
- `UI/Drawing` remains a command layer, not a scene graph.
- `UI/Input` remains an input foundation; v2 route ownership moves to the retained tree.
- MonoGame-specific code stays in adapter folders.

## Allowed Direction

```text
UI core -> UI/Drawing abstractions
UI core -> UI/Input abstractions
Adapters -> MonoGame
```

## Disallowed Direction

```text
UI core -> MonoGame
UI core -> Skia/HarfBuzz
UI/Drawing core -> MonoGame
UI/Input core -> MonoGame
Controls -> backend-specific rendering/input APIs
```
