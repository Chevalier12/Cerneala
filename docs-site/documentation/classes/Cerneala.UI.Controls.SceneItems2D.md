# SceneItems2D Class

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/SceneItems2D.cs`

Materializes an enumerable source into retained scene nodes through content templates.

```csharp
public sealed class SceneItems2D : SceneNode2D
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `SceneNode2D` -> `SceneItems2D`

## Examples

```xml
<SceneItems2D>
    @templates
    {
        <ContentTemplate DataType="System.String">
            <Sprite2D>
                <Sprite2D.Aspect>
                    @on Loaded
                    {
                        @animate with Tween(100ms)
                        {
                            @to { Opacity = 0.5; }
                        }
                    }
                </Sprite2D.Aspect>
                @prism
                {
                    @layer SpriteContent
                    {
                        Opacity = 1;
                        @filter Blur { Radius = 1; }
                    }
                }
            </Sprite2D>
        </ContentTemplate>
    }
</SceneItems2D>
```

Assign or bind `ItemsSource` to a sequence of `System.String` values to create instances in this minimal example.

## Remarks

Items are realized in enumeration order and recorded in that same order. A matching `ContentTemplate` must create a `SceneNode2D`. When no template matches, an item that already is a `SceneNode2D` is used directly; other values cause `InvalidOperationException`.

The control observes Cerneala `IObservableList` and standard `INotifyCollectionChanged` sources. Precise `Add`, `Remove`, `Move`, and equal-count `Replace` notifications update only the range whose item or template index changed. Nodes outside that range keep their identity and attachment. Appending one item therefore creates and attaches one node, independent of the existing item count.

Template indices are immutable values captured when a node is created. An insertion or removal rebuilds the suffix whose indices changed; a move rebuilds the span between the old and new positions; and an equal-count replacement rebuilds only the replaced range. `Reset`, collection notifications without usable indices/counts, a different `ItemsSource`, and template changes rebuild the full realized set intentionally.

Detaching the control unsubscribes from the current observable source, and reattaching subscribes once and rematerializes the current source. Replaced or removed realized nodes are detached before they leave `LogicalChildren`, so their bindings, Aspect state, Motion state, Prism attachments, resources, and surface association follow the normal scene-node lifecycle. `SceneItems2D` does not add an effect layer of its own; Aspect, Motion, and Prism belong on the nodes created by `@templates`.

All items are retained; this API does not virtualize or recycle scene nodes.

Template `DataType` supplies the data-context type inside the template. Bindings retain Cerneala's `$DataContext.Member:Mode` syntax.

The template root is an ordinary scene node. It can declare its own `<Sprite2D.Aspect>`, `@animate with ...`, and `@prism { ... }` blocks using the same syntax as a directly declared sprite. Those declarations are instantiated once per realized node and are disposed through that node's normal detach lifecycle.

In `.crn` markup, declare the collection exclusively with `@templates { ... }`. The legacy `SceneItems2D.Templates` property-element wrapper is rejected.

## Constructors

| Name | Description |
| --- | --- |
| `SceneItems2D()` | Creates an empty template collection. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `ItemsSourceProperty` | `UiProperty<IEnumerable?>` | Identifies the external item source. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `ItemsSource` | `IEnumerable?` | Gets or sets the source materialized into scene nodes. |
| `Templates` | `Collection<ContentTemplate>` | Gets the templates resolved for source items. |
| `RealizedItemCount` | `int` | Gets the number of currently materialized scene nodes. |

## Property Information

| Property | Identifier field | Default value | Metadata/options |
| --- | --- | --- | --- |
| `ItemsSource` | `ItemsSourceProperty` | `null` | `AffectsRender` |

## Applies to

Project: `Cerneala`

## See also

- `RenderSurface2D`
- `Scene2D`
- `SceneNode2D`
- `ContentTemplate`
