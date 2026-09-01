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
<SceneItems2D ItemsSource="$DataContext.Pieces:OneWay">
    @templates
    {
        <ContentTemplate DataType="Game.PieceSpriteModel">
            <Sprite2D
                Source="$DataContext.Image:OneWay"
                SourceRect="$DataContext.SourceRect:OneWay"
                Destination="$DataContext.Destination:OneWay"
                Tint="$DataContext.Tint:OneWay" />
        </ContentTemplate>
    }
</SceneItems2D>
```

## Remarks

Items are realized in enumeration order and recorded in that same order. A matching `ContentTemplate` must create a `SceneNode2D`. When no template matches, an item that already is a `SceneNode2D` is used directly; other values cause `InvalidOperationException`.

The control observes Cerneala `IObservableList` and standard `INotifyCollectionChanged` sources. A collection change rebuilds the realized node set and invalidates the owning surface. This initial API realizes every item; it does not virtualize or incrementally recycle scene nodes.

Template `DataType` supplies the data-context type inside the template. Bindings retain Cerneala's `$DataContext.Member:Mode` syntax.

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
