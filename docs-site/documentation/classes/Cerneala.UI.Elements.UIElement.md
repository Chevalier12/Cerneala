# UIElement Class

## Definition
Namespace: `Cerneala.UI.Elements`

Assembly/Project: `Cerneala`

Source: `UI/Elements/UIElement.cs`

Defines the base element for Cerneala's retained UI tree, including logical and visual parenting, property-backed layout, rendering, invalidation, input state, command state, bindings, and motion/presence hooks.

```csharp
public class UIElement : UiObject, IUiPropertyOwner, ILayoutElement, IRenderableElement
```

Inheritance:
`object` -> `UiObject` -> `UIElement`

Implements:
`IUiPropertyOwner`, `ILayoutElement`, `IRenderableElement`

## Examples
Create a small custom element by deriving from `UIElement` and overriding layout behavior.

```csharp
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;

sealed class FixedElement : UIElement
{
    protected override LayoutSize MeasureCore(MeasureContext context)
    {
        return new LayoutSize(80, 30);
    }

    protected override LayoutRect ArrangeCore(ArrangeContext context)
    {
        return context.FinalRect;
    }
}

var element = new FixedElement
{
    Width = 80,
    Height = 30,
    Margin = new Thickness(4),
    HorizontalAlignment = HorizontalAlignment.Center,
    Opacity = 0.75f
};

LayoutSize desired = element.Measure(new MeasureContext(new LayoutSize(200, 60)));
LayoutRect arranged = element.Arrange(new ArrangeContext(new LayoutRect(0, 0, 200, 60)));
```

## Remarks

An attached element's property mutations are UI-thread-affine and use `Root.Relay` as their authority. Off-thread typed or untyped property sets and clears throw `InvalidOperationException` before changing the value, value source, dirty state, or retained queues. Detached elements remain freely configurable and adopt the root's authority when attached. The exception is a `SceneNode2D` inside a `SceneSimulationContext2D`: it uses the context's relay even without a root, so off-thread writes to it throw `InvalidOperationException`.
`UIElement` owns two child collections: `LogicalChildren` and `VisualChildren`. The corresponding `LogicalParent` and `VisualParent` properties are set by the element tree infrastructure. When an element is attached to a `UIRoot`, `Root`, `ElementId`, and `IsAttached` describe that retained-tree attachment.

Most element settings are registered `UiProperty<T>` values. Setting properties such as `Margin`, `Visibility`, `Opacity`, focus state, or transform values flows through `UiObject.SetValue`, raises property notifications, and, when the effective value changes, calls `OnPropertyInvalidated` with the metadata options from the registered property.

`Aspect` holds an optional `ElementAspect`. Assignment validates the aspect target type and every default and conditional declaration against the runtime element type before committing the property-store change. Compatible aspects are composed by the attached root's `AspectProcessor`. A generated behavior lifetime exists only while the element is attached: detach disposes it, reattach creates a new lifetime, and replacement or clear also disposes the active lifetime.

`IsHitTestVisible` controls direct pointer hit testing without changing layout or rendering. When it is `false`, the element and its visual subtree are skipped by hit testing; the default is `true`.

`Timbre` returns the element's [TimbreScope](Cerneala.Timbre.TimbreScope.md), connected to `Root.TimbreRuntime`. It requires the owner thread and an attached element whose root has a runtime; otherwise it throws `InvalidOperationException`. The scope belongs to one attachment: detach disposes it and cancels its playbacks, and after reattachment `Timbre` returns a new scope without resurrecting old playbacks. Hiding the element or an ancestor does not affect its audio, for every element, including `Scene2D` and its nodes. A node in a headless `SceneSimulationContext2D` has no root, so its `Timbre` getter throws `InvalidOperationException`.

```csharp
TimbrePlayback playback = button.Timbre.Play(confirmTimbre);
```

Changing `Visibility` from `Visible` to `Hidden` or `Collapsed`, or changing `IsVisible` to `false`, cancels active property motion for the element and its visual descendants. Canceled motion does not resume automatically when the subtree becomes visible again; callers or markup conditions can start it again explicitly.

Layout uses `Measure` and `Arrange`. `Measure` caches the last available size and layout version; `Arrange` caches the last final rectangle and layout version. A `Measure` that changes `DesiredSize` clears the arrange cache of the element and of its visual parent, because the element aligns itself in its slot by its desired size and a panel such as `Canvas` or `StackPanel` places its children by theirs; the next `Arrange` of either with the same final rectangle then runs `ArrangeCore` again. For example, a vertical `StackPanel` with the slot `(0, 0, 0, 60)` holds `a` (`Height = 20`) and `b` (`Height = 40`); after `a.Height = 40` and `b.Height = 20` are measured, `Arrange` with the same slot places `a` at Y 0 with height 40 and `b` at Y 40 with height 20, also when the panel's own desired size stays 0 by 60. When the element is attached to a `UIRoot`, the same `Measure` also queues the element and its visual parent as arrange work, as WPF's `InvalidateArrange` does: the frame's arrange phase arranges each of them with its last final rectangle. Only these two arrange caches are cleared; the ancestors above the parent keep theirs, and an ancestor whose final rectangle and desired size did not change returns its cached result without arranging its children. The queued work still arranges the parent. For example, in `Border > Canvas > Border > StackPanel > a, b`, where the inner `Border` and the `StackPanel` keep the slot `(0, 0, 0, 60)` while `a` and `b` swap heights, the inner `Border` reuses its arrange, and the frame still arranges the `StackPanel`, so `a` is at Y 0 with height 40 and `b` is at Y 40. A measure that runs during the arrange phase leaves its queued work to the next frame. Elements that do not participate in layout return zero desired size and zero arranged size. Otherwise, `Measure` constrains `MeasureCore` with explicit `Width` and `Height` values when present, substitutes those values into the desired size, and then applies `Margin`. When the `MeasureContext` has layout rounding enabled, the available size that `MeasureCore` receives (after `Margin` and the explicit dimensions) has its `Width` and `Height` each rounded to the nearest device pixel with `LayoutRounding.Round(float)`, as WPF rounds the size it passes to `MeasureOverride`; an infinite size stays infinite. An element that fills its slot is therefore measured at the same size its `ArrangeCore` receives. For example, at scale 1 an element with `Margin = new Thickness(0, 0.4f, 0, 0)` in a 100 by 100 slot has 99.6 pixels of height after the margin; `MeasureCore` receives a height of 100, and `ArrangeCore` also receives a height of 100. A `ScrollViewer` placed like this therefore reports the same `ViewportHeight` in measure and in arrange, and its layout settles. With layout rounding enabled, an explicit `Width` or `Height` is itself rounded to the nearest device pixel with `LayoutRounding.Round(float)` before it constrains `MeasureCore` and before it is substituted into the desired size, as WPF rounds an explicit `Width` and `Height`. `Arrange` never gives the element more than that rounded dimension, even when the rounded-up `DesiredSize` is larger. For example, at scale 1 an element with `Height = 99.4f` is measured at 99 and arranged at 99, and `ArrangedBounds.Height` is 99; at scale 2 an element with `Height = 49.7f` (99.4 device pixels) is measured and arranged at 49.5. With `Height = 99.4f` and `Margin = new Thickness(0, 0.3f, 0, 0)` at scale 1, `DesiredSize.Height` rounds 99.3 up to 100, but the element is still arranged at 99. With layout rounding enabled, `Measure` adds the margin to the content size in device pixels: a size that lies on a device pixel counts as its whole number of device pixels (see `LayoutRounding`). For example, at scale 1.5 an element with `Width = 2.5f` (4 device pixels) and `Margin = new Thickness(2, 0, 0, 0)` (3 device pixels) has a `DesiredSize.Width` of 7 device pixels, and in a horizontal `StackPanel` it is arranged at device pixels 3..7. A float sum of the two sizes would be slightly above 7 device pixels and would round up to 8. A `ScrollViewer` with `Height = 99.4f` and 100 pixels of content therefore shows its `Auto` vertical scroll bar in both measure and arrange, and its layout settles. `Arrange` applies `Margin`, the explicit dimensions, `HorizontalAlignment`, and `VerticalAlignment` before calling `ArrangeCore`. When the `ArrangeContext` has layout rounding enabled, the rectangle that `ArrangeCore` receives is rounded with `LayoutRounding.Round(LayoutRect)`: its `X`, `Y`, `Width`, and `Height` are each rounded to the nearest device pixel, so it is the element's own rounded `ArrangedBounds`. As in WPF, where an element's offset and arrange size are rounded and its children are arranged inside them, a panel therefore places its children inside its rounded bounds. Children whose sizes are whole device pixels stay next to each other, and a child aligned to the far edge ends on the panel's rounded far edge. For example, a horizontal `StackPanel` 12 pixels wide with `HorizontalAlignment.Center` in a 13-pixel slot at scale 1 starts at x = 0.5; `ArrangeCore` receives x = 0, and four 3-pixel children are arranged at x = 0, 3, 6, and 9. At scale 2, a stretched `Panel` with `Margin = new Thickness(0.75f, 0, 0, 0)` in a 10-pixel-wide slot has the rectangle x = 0.75, width 9.25 after the margin; `ArrangeCore` receives x = 1, width 9 (device pixels 2..20), and a 1.5-pixel child with `HorizontalAlignment.Right` is arranged at x = 8.5 (device pixels 17..20). An unset dimension is represented by `float.NaN` and continues to use content measurement or stretch behavior.

Changing an attached element into or out of `Visibility.Collapsed` between layout passes schedules its visual parent for measure and arrange. The next scheduled layout removes or restores the element's contribution and repositions affected siblings. `Visibility.Hidden` still participates in layout; hiding an element does not remove its layout space.

Rendering calls `Render`, which null-checks the `RenderContext` and delegates to `OnRender`. Render-scope properties such as `RenderTransform`, `Opacity`, translation, scale, rotation, skew, and `ClipToBounds` update `RenderScopeVersion`; render content dependencies can be updated by derived classes with `SetRenderDependencies`.

Retained composition surrounds a transformed subtree with affine drawing scopes. Local drawing transforms run before the element and ancestor transforms. Rectangle, ellipse, path, text, image, and clip geometry therefore retain rotation and skew rather than being reduced to axis-aligned bounds. Inspectors of the composed command stream must include its active drawing state to obtain final coordinates. `Shape` uses these same transform and opacity properties.

Invalidation is routed to the attached `UIRoot` when available. Detached elements mark their local `DirtyState`; measure invalidation also propagates to visual ancestors until a layout boundary is reached.

Value validation is enforced by property metadata. `Width` and `Height` accept `float.NaN` for automatic sizing or a finite non-negative value; `RenderTransform` cannot be `null`; `RenderTransformOrigin` must be normalized from `0` to `1`; `Opacity` must be finite and between `0` and `1`; transform scalar values must be finite; `TabIndex` must be non-negative; and `LayoutMotionId` must be either `null` or non-blank.

## Constructors
| Name | Description |
| --- | --- |
| `UIElement()` | Initializes logical and visual child collections, handler storage, command bindings, and binding subscriptions. |

## Fields
| Name | Description |
| --- | --- |
| `AspectProperty` | Property identifier for `Aspect`; defaults to `null` and affects Aspect processing. |
| `IsEnabledProperty` | Property identifier for `IsEnabled`; defaults to `true` and affects hit testing, input visual state, aspect matching, and semantics. |
| `IsVisibleProperty` | Property identifier for `IsVisible`; defaults to `true`, affects render, hit testing, and semantics, and cancels property motion for the visual subtree when set to `false`. |
| `IsHitTestVisibleProperty` | Property identifier for `IsHitTestVisible`; defaults to `true` and affects hit testing. |
| `MarginProperty` | Property identifier for `Margin`; defaults to `Thickness.Zero` and affects measure. |
| `WidthProperty` | Property identifier for `Width`; defaults to `float.NaN`, affects measure and arrange, and accepts automatic sizing or a finite non-negative value. |
| `HeightProperty` | Property identifier for `Height`; defaults to `float.NaN`, affects measure and arrange, and accepts automatic sizing or a finite non-negative value. |
| `HorizontalAlignmentProperty` | Property identifier for `HorizontalAlignment`; defaults to `HorizontalAlignment.Stretch` and affects arrange. |
| `VerticalAlignmentProperty` | Property identifier for `VerticalAlignment`; defaults to `VerticalAlignment.Stretch` and affects arrange. |
| `VisibilityProperty` | Property identifier for `Visibility`; defaults to `Visibility.Visible`, affects measure, arrange, render, hit testing, and semantics, and cancels property motion for the visual subtree when changed from `Visible` to `Hidden` or `Collapsed`. |
| `RenderTransformProperty` | Property identifier for `RenderTransform`; defaults to `Transform.Identity`, affects render, and rejects `null`. |
| `RenderTransformOriginProperty` | Property identifier for `RenderTransformOrigin`; defaults to `(0.5, 0.5)`, affects render, and requires normalized finite coordinates. |
| `OpacityProperty` | Property identifier for `Opacity`; defaults to `1`, affects render, and accepts finite values from `0` through `1`. |
| `TranslateXProperty` | Property identifier for `TranslateX`; defaults to `0`, affects render, and requires a finite value. |
| `TranslateYProperty` | Property identifier for `TranslateY`; defaults to `0`, affects render, and requires a finite value. |
| `ScaleProperty` | Property identifier for `Scale`; defaults to `1`, affects render, and requires a finite value. |
| `ScaleXProperty` | Property identifier for `ScaleX`; defaults to `1`, affects render, and requires a finite value. |
| `ScaleYProperty` | Property identifier for `ScaleY`; defaults to `1`, affects render, and requires a finite value. |
| `RotationProperty` | Property identifier for `Rotation`; defaults to `0`, affects render, and requires a finite value. |
| `SkewXProperty` | Property identifier for `SkewX`; defaults to `0`, affects render, and requires a finite value. |
| `SkewYProperty` | Property identifier for `SkewY`; defaults to `0`, affects render, and requires a finite value. |
| `ClipToBoundsProperty` | Property identifier for `ClipToBounds`; defaults to `false` and affects render and hit testing. |
| `LayoutMotionIdProperty` | Property identifier for `LayoutMotionId`; defaults to `null` and rejects blank IDs. |
| `LayoutMotionOptionsProperty` | Property identifier for `LayoutMotion`; defaults to `null`. |
| `PresenceProperty` | Property identifier for `Presence`; defaults to `null`. |
| `IsPointerOverProperty` | Property identifier for `IsPointerOver`; defaults to `false` and affects render, input visual state, aspect matching, and semantics. |
| `IsKeyboardFocusedProperty` | Property identifier for `IsKeyboardFocused`; defaults to `false` and affects render, input visual state, aspect matching, and semantics. |
| `IsKeyboardFocusWithinProperty` | Property identifier for `IsKeyboardFocusWithin`; defaults to `false` and affects render, input visual state, aspect matching, and semantics. |
| `FocusableProperty` | Property identifier for `Focusable`; defaults to `false` and affects hit testing and aspect matching. |
| `IsTabStopProperty` | Property identifier for `IsTabStop`; defaults to `false` and affects aspect matching. |
| `TabIndexProperty` | Property identifier for `TabIndex`; defaults to `0`, affects hit testing, and requires a non-negative value. |
| `CursorProperty` | Property identifier for `Cursor`; defaults to `null`. |

## Properties
| Name | Type | Description |
| --- | --- | --- |
| `LogicalParent` | `UIElement?` | The element's logical parent, if any. |
| `VisualParent` | `UIElement?` | The element's visual parent, if any. |
| `LogicalChildren` | `UIElementCollection` | Logical child collection owned by this element. |
| `VisualChildren` | `UIElementCollection` | Visual child collection owned by this element. |
| `Root` | `UIRoot?` | The attached retained UI root, or `null` when detached. |
| `IsAttached` | `bool` | `true` when `Root` is not `null`. |
| `ElementId` | `UiElementId?` | The retained input/render element identifier assigned during root attachment. |
| `Handlers` | `ElementHandlerStore` | Event handler storage for this element. |
| `CommandBindings` | `CommandBindingCollection` | Command bindings associated with this element. |
| `Bindings` | `BindingSubscriptionCollection` | Data-binding subscriptions associated with this element. |
| `InputBindings` | `InputBindingCollection` | Input gesture bindings associated with this element. |
| `Timbre` | `TimbreScope` | The element's sound scope for its current attachment; requires an attached element with a root `TimbreRuntime`. |
| `DirtyState` | `DirtyState` | Local invalidation state tracked for this element. |
| `DesiredSize` | `LayoutSize` | The last measured desired size. |
| `ArrangedBounds` | `LayoutRect` | The last arranged layout bounds. |
| `LayoutVersion` | `int` | Version incremented by layout-affecting invalidation. |
| `RenderVersion` | `int` | Version incremented by render-content changes. |
| `RenderScopeVersion` | `int` | Version incremented by render-scope visual changes. |
| `RenderDependencies` | `RenderDependency` | Resource and content dependencies reported by the element renderer. |
| `IsLayoutBoundary` | `bool` | Stops detached measure invalidation propagation through visual ancestors when set. |
| `PresenceOpacity` | `float` | Current presence-animation opacity value. |
| `PresenceScale` | `float` | Current presence-animation scale value. |
| `Aspect` | `ElementAspect?` | Per-element Aspect definition composed and resolved by the attached root's `AspectProcessor`. |
| `IsEnabled` | `bool` | Enables or disables element participation in input-related state; defaults to `true`. |
| `IsVisible` | `bool` | Boolean visibility flag; defaults to `true`. Setting it to `false` cancels property motion for this element and its visual descendants. |
| `IsHitTestVisible` | `bool` | Includes the element and its visual subtree in pointer hit testing when `true`; defaults to `true`. |
| `Margin` | `Thickness` | Space included around the measured and arranged content; defaults to `Thickness.Zero`. |
| `Width` | `float` | Explicit content width, or `float.NaN` for automatic sizing; defaults to `float.NaN`. With layout rounding enabled, layout uses it rounded to the nearest device pixel in both measure and arrange. |
| `Height` | `float` | Explicit content height, or `float.NaN` for automatic sizing; defaults to `float.NaN`. With layout rounding enabled, layout uses it rounded to the nearest device pixel in both measure and arrange. |
| `HorizontalAlignment` | `HorizontalAlignment` | Horizontal placement inside the arranged content rectangle; defaults to `Stretch`. |
| `VerticalAlignment` | `VerticalAlignment` | Vertical placement inside the arranged content rectangle; defaults to `Stretch`. |
| `Visibility` | `Visibility` | Layout/render/hit-test visibility mode; defaults to `Visible`. Changing it from `Visible` to `Hidden` or `Collapsed` cancels property motion for this element and its visual descendants; changing between `Hidden` and `Collapsed` does not. |
| `RenderTransform` | `Transform` | Transform applied at render time; defaults to `Transform.Identity`. |
| `RenderTransformOrigin` | `LayoutPoint` | Normalized transform origin; defaults to `(0.5, 0.5)`. |
| `Opacity` | `float` | Render opacity from `0` through `1`; defaults to `1`. |
| `TranslateX` | `float` | Render-time horizontal translation; defaults to `0`. |
| `TranslateY` | `float` | Render-time vertical translation; defaults to `0`. |
| `Scale` | `float` | Uniform render-time scale; defaults to `1`. |
| `ScaleX` | `float` | Render-time horizontal scale; defaults to `1`. |
| `ScaleY` | `float` | Render-time vertical scale; defaults to `1`. |
| `Rotation` | `float` | Render-time rotation value; defaults to `0`. |
| `SkewX` | `float` | Render-time horizontal skew; defaults to `0`. |
| `SkewY` | `float` | Render-time vertical skew; defaults to `0`. |
| `ClipToBounds` | `bool` | Clips rendering and hit testing to the element bounds when `true`; defaults to `false`. |
| `LayoutMotionId` | `LayoutMotionId?` | Optional identity used by layout motion. |
| `LayoutMotion` | `LayoutMotionOptions?` | Optional layout motion options. |
| `Presence` | `PresenceOptions?` | Optional presence animation options. |
| `IsPointerOver` | `bool` | Pointer-over visual/input state. |
| `IsKeyboardFocused` | `bool` | Keyboard focus state for this element. |
| `IsKeyboardFocusWithin` | `bool` | Keyboard focus-within state for this element and descendants. |
| `Focusable` | `bool` | Indicates whether the element can receive focus. |
| `IsTabStop` | `bool` | Indicates whether the element participates as a tab stop. |
| `TabIndex` | `int` | Non-negative tab navigation order value. |
| `Cursor` | `Cursor?` | Optional cursor requested by this element. |

## Methods
| Name | Description |
| --- | --- |
| `QueueCommandStateRefresh()` | Queues command-state refresh for elements that implement `ICommandStateSource`; remembers the request while detached. |
| `Measure(MeasureContext)` | Measures the element, applying visibility participation, margin, rounding, cache checks, and `MeasureCore`. |
| `Arrange(ArrangeContext)` | Arranges the element, applying visibility participation, margin, alignment, rounding, cache checks, and `ArrangeCore`. |
| `Render(RenderContext)` | Validates the render context and calls `OnRender`. |
| `Invalidate(InvalidationFlags, string)` | Creates an `InvalidationRequest` for this element and passes it to `Invalidate(InvalidationRequest)`. |
| `Invalidate(InvalidationRequest)` | Routes invalidation to the attached root or marks local dirty state when detached. |
| `OnPropertyInvalidated(UiPropertyChangedEventArgs, UiPropertyOptions)` | Maps property metadata to invalidation flags, updates layout/render versions, and invalidates the element. |

## Protected Methods
| Name | Description |
| --- | --- |
| `OnAttached()` | Called when the element attaches to a root; queues any pending command-state refresh. |
| `OnDetached()` | Called before the element clears binding/root attachment state, after `Unloaded` and the lifecycle behaviors' detach. It runs even when an earlier detach callback, such as an `Unloaded` handler, threw. When it throws, that state is still cleared before the exception propagates. |
| `MeasureCore(MeasureContext)` | Override point for derived element measurement; receives the available size after margin and explicit dimensions, rounded to device pixels when layout rounding is enabled. The base implementation returns `LayoutSize.Zero`. |
| `ArrangeCore(ArrangeContext)` | Override point for derived element arrangement; receives the rectangle after margin and alignment, with its position and size rounded to device pixels when layout rounding is enabled. The base implementation returns `context.FinalRect`. |
| `OnRender(RenderContext)` | Override point for derived element rendering; the base implementation does nothing. |
| `SetRenderDependencies(RenderDependency)` | Updates render dependencies, increments render version, and invalidates rendering when dependencies change. |

## Inherited Members
| Name | Description |
| --- | --- |
| `PropertyChanged` | Event raised by `UiObject` when an effective UI property value changes. |
| `GetValue`, `SetValue`, `ClearValue` | `UiObject` property-store APIs used by the registered `UiProperty<T>` fields exposed by `UIElement`. |
| `GetValueSource`, `GetSourceValue` | `UiObject` APIs for inspecting the active source of a UI property value. |

## Applies to
Cerneala retained UI elements in the `Cerneala` project.

## See also
- `UI/Elements/UIElement.cs`
- `UI/Core/UiObject.cs`
- `UI/Layout/ILayoutElement.cs`
- `UI/Rendering/IRenderableElement.cs`
