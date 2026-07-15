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

An attached element's property mutations are UI-thread-affine and use `Root.Relay` as their authority. Off-thread typed or untyped property sets and clears throw `InvalidOperationException` before changing the value, value source, dirty state, or retained queues. Detached elements remain freely configurable and adopt the root's authority when attached.
`UIElement` owns two child collections: `LogicalChildren` and `VisualChildren`. The corresponding `LogicalParent` and `VisualParent` properties are set by the element tree infrastructure. When an element is attached to a `UIRoot`, `Root`, `ElementId`, and `IsAttached` describe that retained-tree attachment.

Most element settings are registered `UiProperty<T>` values. Setting properties such as `Margin`, `Visibility`, `Opacity`, focus state, or transform values flows through `UiObject.SetValue`, raises property notifications, and calls `OnPropertyInvalidated` with the metadata options from the registered property.

Layout uses `Measure` and `Arrange`. `Measure` caches the last available size and layout version; `Arrange` caches the last final rectangle and layout version. Elements that do not participate in layout return zero desired size and zero arranged size. Otherwise, `Measure` constrains `MeasureCore` with explicit `Width` and `Height` values when present, substitutes those values into the desired size, and then applies `Margin`. `Arrange` applies `Margin`, the explicit dimensions, `HorizontalAlignment`, and `VerticalAlignment` before calling `ArrangeCore`. An unset dimension is represented by `float.NaN` and continues to use content measurement or stretch behavior.

Rendering calls `Render`, which null-checks the `RenderContext` and delegates to `OnRender`. Render-scope properties such as `RenderTransform`, `Opacity`, translation, scale, rotation, skew, and `ClipToBounds` update `RenderScopeVersion`; render content dependencies can be updated by derived classes with `SetRenderDependencies`.

Invalidation is routed to the attached `UIRoot` when available. Detached elements mark their local `DirtyState`; measure invalidation also propagates to visual ancestors until a layout boundary is reached.

Value validation is enforced by property metadata. `Width` and `Height` accept `float.NaN` for automatic sizing or a finite non-negative value; `RenderTransform` cannot be `null`; `RenderTransformOrigin` must be normalized from `0` to `1`; `Opacity` must be finite and between `0` and `1`; transform scalar values must be finite; `TabIndex` must be non-negative; and `LayoutMotionId` must be either `null` or non-blank.

## Constructors
| Name | Description |
| --- | --- |
| `UIElement()` | Initializes logical and visual child collections, handler storage, command bindings, and binding subscriptions. |

## Fields
| Name | Description |
| --- | --- |
| `IsEnabledProperty` | Property identifier for `IsEnabled`; defaults to `true` and affects hit testing, input visual state, aspect matching, and semantics. |
| `IsVisibleProperty` | Property identifier for `IsVisible`; defaults to `true` and affects render, hit testing, and semantics. |
| `MarginProperty` | Property identifier for `Margin`; defaults to `Thickness.Zero` and affects measure. |
| `WidthProperty` | Property identifier for `Width`; defaults to `float.NaN`, affects measure and arrange, and accepts automatic sizing or a finite non-negative value. |
| `HeightProperty` | Property identifier for `Height`; defaults to `float.NaN`, affects measure and arrange, and accepts automatic sizing or a finite non-negative value. |
| `HorizontalAlignmentProperty` | Property identifier for `HorizontalAlignment`; defaults to `HorizontalAlignment.Stretch` and affects arrange. |
| `VerticalAlignmentProperty` | Property identifier for `VerticalAlignment`; defaults to `VerticalAlignment.Stretch` and affects arrange. |
| `VisibilityProperty` | Property identifier for `Visibility`; defaults to `Visibility.Visible` and affects measure, arrange, render, hit testing, and semantics. |
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
| `IsEnabled` | `bool` | Enables or disables element participation in input-related state; defaults to `true`. |
| `IsVisible` | `bool` | Boolean visibility flag; defaults to `true`. |
| `Margin` | `Thickness` | Space included around the measured and arranged content; defaults to `Thickness.Zero`. |
| `Width` | `float` | Explicit content width, or `float.NaN` for automatic sizing; defaults to `float.NaN`. |
| `Height` | `float` | Explicit content height, or `float.NaN` for automatic sizing; defaults to `float.NaN`. |
| `HorizontalAlignment` | `HorizontalAlignment` | Horizontal placement inside the arranged content rectangle; defaults to `Stretch`. |
| `VerticalAlignment` | `VerticalAlignment` | Vertical placement inside the arranged content rectangle; defaults to `Stretch`. |
| `Visibility` | `Visibility` | Layout/render/hit-test visibility mode; defaults to `Visible`. |
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
| `OnDetached()` | Called before the element clears binding/root attachment state. |
| `MeasureCore(MeasureContext)` | Override point for derived element measurement; the base implementation returns `LayoutSize.Zero`. |
| `ArrangeCore(ArrangeContext)` | Override point for derived element arrangement; the base implementation returns `context.FinalRect`. |
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
