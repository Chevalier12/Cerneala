# FontResolver Class

## Definition
Namespace: `Cerneala.UI.Text`

Assembly/Project: `Cerneala`

Source: `UI/Text/FontResolver.cs`

Resolves text font requests into `ResolvedTextFont` instances by using an optional drawing font source or an optional resource provider.

```csharp
public sealed class FontResolver
```

Inheritance:
`Object` -> `FontResolver`

## Examples
Resolve a font through an explicit `IFontSource`:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Text;

IFontSource fontSource = new DemoFontSource();
FontResolver resolver = new(fontSource);

ResolvedTextFont font = resolver.Resolve("Inter", 16);
IDrawFont drawFont = font.Font;

sealed class DemoFontSource : IFontSource
{
    public IDrawFont LoadFont(string familyName, float size)
    {
        return new DemoFont(familyName, size);
    }
}

sealed record DemoFont(string FamilyName, float Size) : IDrawFont;
```

Resolve a font resource through a `ResourceStore`:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Resources;
using Cerneala.UI.Text;

ResourceStore resources = new();
ResourceId<FontResource> bodyFontId = new("Body");

resources.SetResource(bodyFontId, new FontResource(new DemoFont("Inter", 16)));

FontResolver resolver = new(resourceProvider: resources);
ResolvedTextFont font = resolver.Resolve(bodyFontId);

sealed record DemoFont(string FamilyName, float Size) : IDrawFont;
```

## Remarks
`FontResolver` is the bridge between text layout/rendering code and the font objects consumed by the drawing backend. It can resolve fonts in three ways:

- From a family name and size.
- From a `TextAspect`, using `TextAspect.FontResourceId` when present or `TextAspect.FontFamily` with `TextAspect.FontSize * TextAspect.Scale` otherwise.
- From a `ResourceId<FontResource>`, when the resolver was constructed with an `IResourceProvider`.

When no `IFontSource` is supplied, family-name resolution creates a deterministic fallback `IDrawFont` that stores only the requested family name and size. The fallback does not perform global font lookup or rasterization by itself.

When an `IFontSource` is supplied, `Resolve(string, float)` delegates directly to `IFontSource.LoadFont`. If that source returns `null`, `ResolvedTextFont` throws `ArgumentNullException`; `FontResolver` does not silently fall back.

Resource-backed resolution requires an `IResourceProvider`. If the provider is a `ResourceStore`, the returned `ResolvedTextFont.Identity` includes the current resource version. For other provider implementations, the version component is `0`.

## Constructors
| Name | Description |
| --- | --- |
| `FontResolver()` | Creates a resolver with no font source and no resource provider. Family-name resolution uses the deterministic fallback font. |
| `FontResolver(IFontSource fontSource)` | Creates a resolver that loads family-name requests through `fontSource`. Throws `ArgumentNullException` when `fontSource` is `null`. |
| `FontResolver(IResourceProvider resourceProvider)` | Creates a resolver that can resolve `FontResource` values. Throws `ArgumentNullException` when `resourceProvider` is `null`. |
| `FontResolver(IFontSource? fontSource, IResourceProvider? resourceProvider)` | Creates a resolver with optional family-name and resource-backed font services. `null` values are accepted. |

## Properties
| Name | Description |
| --- | --- |
| `Default` | Gets a shared resolver constructed with no font source and no resource provider. |

## Methods
| Name | Description |
| --- | --- |
| `Resolve(string familyName, float size)` | Resolves a font by family name and size. Throws `ArgumentException` for empty or whitespace family names, and `ArgumentOutOfRangeException` for non-positive or non-finite sizes. |
| `Resolve(TextAspect aspect)` | Resolves the font described by `aspect`. A font resource id takes precedence over family name and scaled font size. |
| `Resolve(ResourceId<FontResource> id)` | Resolves a `FontResource` through the configured resource provider. Throws `InvalidOperationException` when no provider is configured, and `KeyNotFoundException` when the resource provider cannot find the resource. |

## Applies To
`Cerneala` UI text services that need an `IDrawFont`, including text measurement and rendering paths.

## See Also
- `Cerneala.UI.Text.ResolvedTextFont`
- `Cerneala.UI.Text.TextAspect`
- `Cerneala.UI.Resources.FontResource`
- `Cerneala.Drawing.IFontSource`
- `Cerneala.Drawing.IDrawFont`
