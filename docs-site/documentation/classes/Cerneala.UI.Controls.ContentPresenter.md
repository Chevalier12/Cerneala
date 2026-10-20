# ContentPresenter Class

## Definition
Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/ContentPresenter.cs`

Displays a single content value by materializing it as a child `UIElement`.

```csharp
public class ContentPresenter : Control
```

Inheritance:
`object` -> `UiObject` -> `UIElement` -> `Control` -> `ContentPresenter`

## Examples

Present an existing element directly:

```csharp
using Cerneala.UI.Controls;

TextBlock text = new() { Text = "Hello" };
ContentPresenter presenter = new()
{
    Content = text
};
```

Create a presented element from data with a `ContentTemplate`:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;

ContentPresenter presenter = new()
{
    Content = "Ada",
    ContentTemplate = new ContentTemplate<string>(
        "NameText",
        key: null,
        priority: 0,
        context => new TextBlock { Text = context.Data ?? string.Empty })
};
```

Use a modern content template from the local template registry:

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Controls.Templates;

ContentTemplateRegistry registry = new();
registry.Register(new ContentTemplate<string>(
    "NameText",
    key: "name",
    priority: 0,
    context => new TextBlock { Text = context.Data ?? string.Empty }));

ContentPresenter presenter = new()
{
    Content = "Ada",
    ContentTemplateKey = "name",
    LocalTemplateRegistry = registry
};
```

## Remarks

`ContentPresenter` is a lightweight control for turning `Content` into one realized child. It adds the realized child to both `LogicalChildren` and `VisualChildren`, exposes it through `PresentedChild`, and measures and arranges that child to fill the presenter's layout slot.

Content is resolved in this order:

| Step | Behavior |
| --- | --- |
| `ContentTemplate` | If set, creates the child with a `ContentTemplateContext` containing `Content`, this presenter, and `ContentIndex`. |
| `LocalTemplateRegistry` | If set and a template resolves for `Content` and `ContentTemplateKey`, creates the child from that template. |
| `UIElement` content | Hosts the element directly. The element must not already have a logical or visual parent. |
| `string` content | Creates or reuses a generated `TextBlock` and copies the presenter's `FontFamily`, `FontSize`, `Foreground`, `ResourceProvider`, and `FontResourceId`. |
| Other content | Produces no child unless a template handles the content first. |

Changing `Content`, `ContentTemplate`, or `ContentTemplateKey` marks the presentation dirty and refreshes the child. `Content` uses reference equality for change detection, so two different object instances that compare equal still rematerialize the presented child. Generated text content is special: when the presenter already owns a generated `TextBlock`, string changes update and reuse that `TextBlock`.

A template can place `UIElement` content inside the element it creates, for example `context => new Border { Child = (UIElement?)context.Data }`. When a refresh builds a new child for the same element content, for example after `ContentTemplate`, `ContentTemplateKey`, or `LocalTemplateRegistry` changes, the presenter first removes the element from the old template's element, so the next template, or the presenter when no template applies any more, receives an element without a parent. When the presenter hosts the element directly, either without a template or because the template returned the element itself (`context => context.Data as UIElement`), the element stays in place while the next template runs: if that template returns the element again, the presented child does not change and the element raises no `Unloaded` and no `Loaded`; if the template adds the element to an element it creates, the presenter removes the element at that moment, so the new element can take it. An element whose parent is outside the presenter is not removed, and the refresh throws `InvalidOperationException`. When the new template throws, or its element cannot be added to the presenter, the refresh rethrows that exception and keeps the previous presentation: `PresentedChild` stays the previous child, and the element content goes back to the parent it had before the refresh, for example the old template's `Border`.

The rollback after a failed refresh goes through the same property as the release. When the element's previous parent was a `Decorator` whose `Child` was the element, the presenter sets that `Child` back to the element; when it was a `ContentControl` that hosted the element directly through `Content`, the presenter sets that `Content` back. For example, after `context => new Border { Child = (UIElement?)context.Data }` and a template that throws, the old `Border` has `Child` equal to the element again and measures it. When the failed template had already put the element into a `Decorator` or a direct-hosting `ContentControl` of its own, for example `new Border { Child = (UIElement?)context.Data }` whose `Border` then cannot be added, that `Child` or `Content` is set to `null`, so it no longer names the element. Any other previous parent, for example a `StackPanel`, gets the element back in `VisualChildren` first and then in `LogicalChildren`, at its old index, or at the end of the list when the failed template removed other children so that the old index is past the end. The refresh then rethrows the template's own exception. A generated `TextBlock` that stays the presented child stays generated: later `FontFamily`, `FontSize`, and `Foreground` changes reach it, and a later string refresh reuses it.

A refresh that fails leaves the presentation dirty. The next measure or arrange, for example the next `UIRoot.ProcessFrame`, runs the refresh again without any other property change: a template that threw once because its data was not ready presents its element on the next frame, and a template that still throws throws again from that frame while the previous presentation stays in place.

Removing a child from the presenter raises its `Unloaded` event, and adding one raises its `Loaded` event, while the refresh runs. When a handler of either event changes `Content`, `ContentTemplate`, or `ContentTemplateKey`, the nested refresh presents the new value, and the outer refresh does not present the child it built for the old value: it removes that child from the presenter and takes the element content out of it. For example, a `Loaded` handler of an element `content` that sets `presenter.Content = "other"` while `presenter.Content = content` runs leaves a `TextBlock` with the text `other` as the only child, and `content` has no parent.

A template can place `UIElement` content inside the element it creates, for example `context => new Border { Child = (UIElement)context.Data! }`. When `Content` changes away from that element, the presenter removes the element from the template's element at once, before the presented child is rebuilt. The element then has no parent, so it can be added elsewhere right away, for example to the items panel after an `ItemsControl` loses its `ItemTemplate`. The same holds when the template returns the content element itself, for example `context => (UIElement?)context.Data`, and when no template applies and the element is the presented child: when `Content` changes away from it, the presenter removes it from its own children at once.

A presented child that leaves because `Content` changed, for example a directly presented element after `presenter.Content = null` or after `Content` is replaced by another value, is removed from `LogicalChildren` first and from `VisualChildren` second. So an element with `UIElement.Presence` set is still attached through the presenter when the visual removal starts its exit animation: on an attached presenter it stays an exiting visual child of the presenter (listed by `PresenceCoordinator.GetExitingVisualChildren`) until the exit ends, as `PresenceOptions` describes. For example, after `presenter.Content = null`, `root.Motion.Presence.GetState(oldChild)` is `PresenceState.Exiting` while the exit runs, and `PresenceState.Detached` after it ends. Setting the exiting element as `Content` again cancels its exit. A move is not an exit: when a template takes the directly presented element into the element it creates, the presenter removes the element at once. The same holds when the presenter is inside the presented child of another presenter, for example one built by `context => new ContentPresenter { Content = context.Data }`, and that other presenter's next template takes the element: the inner presenter lets go of it at once, its `Content` becomes `null`, and the element is `PresenceState.Present` in the element that the next template creates. A child that never became the presented child, because adding it failed or because a `Loaded` handler changed `Content` while it was added, also leaves at once, without an exit.

In both cases, when the element's parent is a `Decorator` (for example a `Border`) whose `Child` is the element, the presenter removes it by setting that decorator's `Child` to `null`, so `Child` never names an element that the decorator no longer holds. A template that reuses one container, for example `context => { cached.Child = (UIElement?)context.Data; return cached; }`, therefore puts the element back into `cached` on every refresh: after `ContentTemplate` is replaced by another template with the same factory, `PresentedChild` is `cached` and the element's `VisualParent` is `cached`.

In both cases, when the element's parent is a `ContentControl` inside the presented child that hosts the element directly (it has no `ComponentTemplate` or `ComponentTemplateKey`) and whose `Content` is the element, the presenter removes it by setting that control's `Content` to `null`, so `Content` never names an element that the control no longer holds. A template that reuses one container, for example `context => { cached.Content = context.Data; return cached; }`, therefore puts the element back into `cached` on every refresh: after `ContentTemplate` is replaced by another template with the same factory, `PresentedChild` is `cached`, `cached.Content` is the element, and the element's `VisualParent` is `cached`. The write goes through the `Content` setter, so it is the same local value write as `cached.Content = null` in code.

In both cases, when the element's parent is another `ContentPresenter` inside the presented child whose `PresentedChild` and `Content` are the element, the presenter removes it by setting that inner presenter's `Content` to `null`, so `PresentedChild` never names an element that the inner presenter no longer holds. A template that reuses one inner presenter, for example `context => { cached.Content = context.Data; return cached; }`, therefore puts the element back into `cached` on every refresh: after `ContentTemplate` is replaced by another template with the same factory, `PresentedChild` is `cached`, `cached.Content` and `cached.PresentedChild` are the element, and the element's `VisualParent` is `cached`. When the new template throws, the refresh gives the element back by setting the inner presenter's `Content` to the element again.

In both cases, when the element's parent is a `TabItem` inside the presented child that hosts the element directly as its `Header` (it has no `ComponentTemplate`, `ComponentTemplateKey`, `HeaderTemplate`, or `HeaderTemplateKey`), the presenter removes it by setting that tab's `Header` to `null`, so `Header` never names an element that the tab no longer holds. A template that reuses one tab, for example `context => { cached.Header = context.Data; return cached; }`, therefore puts the element back into `cached` on every refresh: after `ContentTemplate` is replaced by another template with the same factory, `PresentedChild` is `cached`, `cached.Header` is the element, and the element's `VisualParent` is `cached`. When the new template throws, the refresh gives the element back by setting the tab's `Header` to the element again.

`LocalTemplateRegistry` is not a UI property. Assigning it refreshes the presented child and invalidates measure, arrange, and render. `ContentIndex` marks the presentation dirty and is supplied to modern template creation contexts; it does not itself perform layout invalidation.

## Constructors

| Name | Description |
| --- | --- |
| `ContentPresenter()` | Creates a new presenter with no content and no presented child. |

## Fields

| Name | Type | Description |
| --- | --- | --- |
| `ContentProperty` | `UiProperty<object?>` | Identifies the `Content` UI property. The default value is `null`; metadata affects measure and render and uses reference equality. |
| `ContentTemplateProperty` | `UiProperty<ContentTemplate?>` | Identifies the `ContentTemplate` UI property. The default value is `null`; metadata affects measure and render. |
| `ContentTemplateKeyProperty` | `UiProperty<string?>` | Identifies the `ContentTemplateKey` UI property. The default value is `null`; metadata affects measure and render. |

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Content` | `object?` | Gets or sets the value to present. UI elements can be hosted directly; strings can be presented as generated `TextBlock` instances; other values require a template to produce a child. |
| `ContentTemplate` | `ContentTemplate?` | Gets or sets the explicit content template used before registry and fallback presentation paths. |
| `ContentTemplateKey` | `string?` | Gets or sets the key requested from `LocalTemplateRegistry`. |
| `LocalTemplateRegistry` | `ContentTemplateRegistry?` | Gets or sets the local registry used to resolve a `ContentTemplate` for the current content and key. |
| `FontResourceId` | `ResourceId<FontResource>?` | Gets or sets the font resource copied to generated `TextBlock` children. |
| `ResourceProvider` | `IResourceProvider?` | Gets or sets the resource provider copied to generated `TextBlock` children. |
| `ContentIndex` | `int` | Gets or sets the item index passed to modern content template creation contexts. The default is `-1`. |
| `PresentedChild` | `UIElement?` | Gets the currently realized child, or `null` when the content does not produce a child. |

## Methods

| Name | Description |
| --- | --- |
| `MeasureCore(MeasureContext)` | Refreshes the presented child and returns the child's desired size, or `LayoutSize.Zero` when there is no child. |
| `ArrangeCore(ArrangeContext)` | Refreshes and arranges the presented child, then returns the final layout rectangle. |
| `OnPropertyChanged(UiPropertyChangedEventArgs)` | Refreshes presentation when one of the content or template UI properties changes. |

## Applies To

`Cerneala` retained UI controls and template infrastructure.

## See Also

- `Cerneala.UI.Controls.ContentControl`
- `Cerneala.UI.Controls.Templates.ContentTemplate`
- `Cerneala.UI.Controls.Templates.ContentTemplate`
- `Cerneala.UI.Controls.Templates.ContentTemplateRegistry`
- `Cerneala.UI.Controls.TextBlock`
- `Cerneala.UI.Motion.Presence.PresenceOptions`
