# IRenderableElement Interface
## Definition
Namespace: `Cerneala.UI.Rendering`
Assembly/Project: `Cerneala`
Source: `UI/Rendering/IRenderableElement.cs`
Provides the `Cerneala.UI.Rendering.IRenderableElement` API surface.
```csharp
public interface IRenderableElement
```

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `RenderVersion` | `int` | Gets the version of the element's render content. |
| `RenderDependencies` | `RenderDependency` | Gets the resource and content dependencies reported by the renderer. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Render(RenderContext context)` | `void` | Renders the element into the supplied context. |
## Remarks
This page is generated from the repository API index so the documentation surface stays aligned with the source tree.
## Applies to
Cerneala UI runtime and framework API consumers.
