## 1. Drawing Command Surface

- [x] 1.1 Add advanced draw command payloads and command kinds only for supported shape primitives.
- [x] 1.2 Add matching `DrawingContext` methods and command factory validation.
- [x] 1.3 Add MonoGame backend adapter behavior or intentional tested failure behavior for each new command.
- [x] 1.4 Add `tests/Cerneala.Tests/Drawing/AdvancedDrawCommandTests.cs`.

## 2. Media Descriptors

- [x] 2.1 Add `Brush`, `SolidColorBrush`, `LinearGradientBrush`, and `RadialGradientBrush` with validation and stable identity.
- [x] 2.2 Add `Pen` with brush and thickness validation.
- [x] 2.3 Add `Geometry`, `RectangleGeometry`, `EllipseGeometry`, and `PathGeometry` with bounds and structured data.
- [x] 2.4 Add `Matrix3x2` and `Transform` value APIs.
- [x] 2.5 Add `OpacityLayer` and `ShadowEffect` metadata descriptors.
- [x] 2.6 Add `ImageSource`, `BitmapImage`, and `RenderTargetImage` above `IDrawImage`.

## 3. Shape Controls

- [x] 3.1 Add retained `Shape` base control with fill, stroke, stroke thickness, geometry, transform, opacity, and shadow properties.
- [x] 3.2 Add `Rectangle`, `Ellipse`, and `Path` shape controls.
- [x] 3.3 Ensure shape controls measure, arrange, render, and invalidate through existing retained UI services.
- [x] 3.4 Ensure shape controls and media stay backend-neutral through architecture tests.

## 4. Tests and Roadmap

- [x] 4.1 Add brush, geometry, transform, image source, and shape tests listed in `ROADMAPv2.md`.
- [x] 4.2 Run OpenSpec validation and full project tests.
- [x] 4.3 Update `ROADMAPv2.md` section 22 checkboxes for implemented files, tests, and acceptance criteria.
