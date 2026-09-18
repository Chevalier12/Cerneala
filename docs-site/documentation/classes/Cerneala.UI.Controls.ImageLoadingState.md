# ImageLoadingState Enum

## Definition

Namespace: `Cerneala.UI.Controls`

Assembly/Project: `Cerneala`

Source: `UI/Controls/Image.cs`

Describes the last processed bitmap preparation state of an ordinary UI `Image` control.

```csharp
public enum ImageLoadingState { Loading, Ready, Error }
```

## Remarks

Read the state through the readonly `Image.LoadingState` UI property. A cold path starts asynchronous preparation; pending and failed preparations produce no bitmap. A direct, embedded or resident image can become ready without an observable `Loading` interval. `Ready` also includes a null source or unresolved resource id; it does not guarantee that a bitmap exists.

The default is `Ready`. State changes are published when the control processes its source, not immediately upon every source assignment. Asynchronous completion schedules UI-thread processing through the owning root. Detaching releases preparation interest and prevents late completion from updating that detached control; its last observed state is not reset automatically.

This state concerns this control's bitmap only. It does not represent the readiness of a window, a scene, Prism effects or other resource consumers. Loading does not virtualize or disable the control. Explicit dimensions reserve layout space; unknown intrinsic dimensions can cause relayout after loading.

## Fields

| Name | Value | Description |
| --- | --- | --- |
| `Loading` | `0` | The current source has a pending image preparation. No bitmap is recorded. |
| `Ready` | `1` | The last source resolution completed without error, including an empty source. |
| `Error` | `2` | Source preparation failed. `Image.LoadingError` exposes the exception; no bitmap is recorded. |

## Examples

The root in this example already has a backend-compatible `IAsyncImageLoader` configured by its host or through `UIRoot.SetImageLoader`.

```csharp
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Resources;

static Image AddLogo(UIRoot root)
{
    ResourceId<ImageResource> id = new("app.logo");
    root.Resources.SetResource(id, new ImageResource("Assets/logo.png"));
    Image logo = new() { SourceResourceId = id, Width = 128, Height = 64 };
    logo.PropertyChanged += (_, args) =>
    {
        if (ReferenceEquals(args.Property, Image.LoadingStateProperty) &&
            logo.LoadingState == ImageLoadingState.Error)
        {
            Console.WriteLine(logo.LoadingError);
        }
    };
    root.VisualChildren.Add(logo);
    return logo;
}
```

## See also

- [Image](Cerneala.UI.Controls.Image.md)
- [IAsyncImageLoader](Cerneala.UI.Resources.IAsyncImageLoader.md)
- [ImageResourceCache](Cerneala.UI.Resources.ImageResourceCache.md)
