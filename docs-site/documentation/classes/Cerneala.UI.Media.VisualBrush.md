# VisualBrush Class

## Definition
Namespace: `Cerneala.UI.Media`  
Assembly/Project: `Cerneala`  
Source: `UI/Media/VisualBrush.cs`

Captures an existing `UIElement` as visual-only brush content.

```csharp
public sealed record VisualBrush : TileBrush
```

## Examples
```csharp
VisualBrush brush = new(existingElement, opacity: 0.6f);
targetShape.Fill = brush;
```

A hidden child of the source is not part of the brush content:

```csharp
UIElement panel = new();
panel.VisualChildren.Add(new Rectangle { Fill = new SolidColorBrush(new Color(255, 0, 0)) });
panel.VisualChildren.Add(new Rectangle { Fill = new SolidColorBrush(new Color(0, 0, 255)), Visibility = Visibility.Hidden });
targetShape.Fill = new VisualBrush(panel); // paints the red rectangle only, as the window draws the panel
```

## Remarks
The capture follows the source's rendered appearance. The source element's own `Render` output is captured in its arranged bounds, which become the brush content bounds; the source's own `Visibility`, `Opacity`, transforms and clip are not applied, because they describe how its parent places it. Use the brush `Opacity` to fade the content. Every descendant is captured the way the retained render path composes it on screen:

- A descendant that does not participate in rendering (`Visibility` is `Hidden` or `Collapsed`, `IsVisible` is `false`, or it is skipped by virtualization) contributes nothing, and neither do its children.
- A descendant's `Opacity` is multiplied with the opacity of its captured ancestors; a descendant whose combined opacity is 0 contributes nothing.
- A descendant's element transform (`Scale`, `ScaleX`, `ScaleY`, `SkewX`, `SkewY`, `Rotation`, `TranslateX`, `TranslateY` and `RenderTransform`, around `RenderTransformOrigin`) and its clip (`ClipToBounds` or a clip node) wrap its content and its children.

Prism effects attached to elements in the source and children that Motion is still animating out are not part of the capture.

The source visual is not made interactive and does not receive input or focus through the brush. Capture uses a thread-local active-source set; SDL_GPU also tracks sources while rendering nested captures. A cycle back to an active source throws `InvalidOperationException`. Source-generator markup keeps this type runtime-only because it needs a live element resolver.

## Constructors
| Name | Description |
| --- | --- |
| `VisualBrush(UIElement, DrawBrushStretch, DrawBrushAlignmentX, DrawBrushAlignmentY, DrawRect?, DrawRect?, DrawTileMode, float)` | Stores a reference to the supplied visual source. The visual is captured later, each time a backend requests the brush descriptor; the result is not cached. Throws `ArgumentNullException` when `visual` is `null`. |

## Properties
| Name | Description |
| --- | --- |
| `Visual` | Existing source element. |
| `Kind` | Always `DrawBrushKind.Visual`. |
| `Stretch`, `AlignmentX`, `AlignmentY`, `Viewport`, `Viewbox`, `TileMode`, `Opacity` | Inherited brush settings. |

## Applies to
Retained UI rendering with a brush-capable backend.
