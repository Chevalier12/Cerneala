# DrawCommandKind Enum

## Definition
Namespace: `Cerneala.Drawing`

Assembly/Project: `Cerneala`

Source: `Drawing/DrawCommandKind.cs`

Provides the `Cerneala.Drawing.DrawCommandKind` API surface.

```csharp
public enum DrawCommandKind
```

## Remarks

`FillPath` identifies a typed or compatibility-SVG path fill. `DrawPath` identifies a native typed-path stroke whose complete style is retained in `DrawCommand.Pen`. Advanced image and batch kinds retain immutable mesh payloads and platform-neutral image dependencies.

## Values

| Name | Description |
| --- | --- |
| `FillRectangle` | Fills a rectangle. |
| `DrawRectangle` | Strokes a rectangle. |
| `FillRoundedRectangle` | Fills a rounded rectangle through the dedicated backend fast path. |
| `DrawRoundedRectangle` | Strokes a rounded rectangle through cached native stroke geometry. |
| `FillEllipse` | Fills an ellipse. |
| `DrawEllipse` | Strokes an ellipse. |
| `DrawLine` | Draws a line segment. |
| `FillPath` | Fills SVG path data within destination bounds. |
| `DrawText` | Draws a text run. |
| `DrawTextLayout` | Draws one immutable positioned multi-line layout command. |
| `DrawImage` | Draws an image. |
| `DrawImageQuad` | Draws one arbitrary 2D image quad as exactly two affine-textured triangles. |
| `DrawNineSlice` | Draws nine deterministic image regions from one validated mesh. |
| `DrawMesh` | Draws an indexed colored or textured 2D triangle mesh. |
| `DrawPointBatch` | Draws an immutable point batch in one primitive submission. |
| `DrawLineBatch` | Draws an immutable line batch in one primitive submission. |
| `DrawSpriteBatch` | Draws same-image sprites in one primitive submission. |
| `RenderSurface2D` | Composes a backend-managed 2D game surface into the retained command stream. |
| `PushClip` | Pushes a rectangular clip. |
| `PopClip` | Removes the current clip. |
| `BeginPrism` | Begins a typed retained Prism capture scope. |
| `EndPrism` | Ends the innermost retained Prism capture scope. |
| `DrawPath` | Strokes reusable typed path geometry with a `DrawPen`. |
| `PushTransform` / `PopTransform` | Begins or ends an affine transform. |
| `PushPathClip` | Begins a geometric typed-path clip. |
| `PushOpacity` / `PopOpacity` | Begins or ends real group opacity. |
| `PushBlend` / `PopBlend` | Begins or ends a blend mode. |
| `PushLayer` / `PopLayer` | Begins or ends an isolated layer. |

## Applies to

Cerneala UI runtime and framework API consumers.
