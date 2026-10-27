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

- From a family name and size, optionally with a weight, a style, and a stretch.
- From a `TextAspect`, using `TextAspect.FontResourceId` when present or `TextAspect.FontFamily` with `TextAspect.FontSize * TextAspect.Scale` and the `FontWeight`, `FontStyle`, and `FontStretch` of the aspect otherwise.
- From a `ResourceId<FontResource>`, when the resolver was constructed with an `IResourceProvider`.

The parameterless constructor uses `SystemFontSource`, so family-name resolution performs the normal system-font lookup used by the drawing stack. A deterministic fallback `IDrawFont` is used when there is no font source: with the one-parameter `FontResolver(IResourceProvider)` constructor, or when the two-parameter constructor is given a `null` font source, for example `new FontResolver(null, null)`; it stores only the requested family name and size and does not perform global font lookup or rasterization by itself.

When an `IFontSource` is supplied, `Resolve(string, float)` delegates directly to `IFontSource.LoadFont`. If that source returns `null`, `ResolvedTextFont` throws `ArgumentNullException`; `FontResolver` does not silently fall back.

The styled overload `Resolve(familyName, size, weight, style, stretch)` returns the plain `Resolve(familyName, size)` result when the weight, style, and stretch are all `Normal`, and its `ResolvedTextFont.Identity` is then the unchanged `"family:size"`, for example `"Test:13"`. For any other combination the identity also holds the face: `"Test:13:700:Normal:Normal"` for `FontWeight.Bold` (the weight as its number, the style and the stretch by name), so different faces of one family never share a cache entry. A source that has no styled faces (an `IFontSource` that does not override the five-argument `LoadFont`) returns its plain font, and the identity still holds the requested weight, style, and stretch. When the resolver has no font source, the deterministic fallback font is used for every weight, style, and stretch.

Resource-backed resolution requires an `IResourceProvider`. The returned `ResolvedTextFont.Identity` has the form `"resource:{id}:{version}"`, so replacing the font resource produces a new identity and text measured with the new font does not reuse the cached layout of the old font. If the provider is a `ResourceStore`, the version is the version of that one resource: it goes up only when that resource is set. If the provider is a `ResourceDictionary`, the version is the version of the whole dictionary: it goes up when any entry of the dictionary is added, replaced, or removed, also when nobody listens to `ResourceChanged`. For example, with a `ResourceDictionary` that holds `Body`, the identity is `"resource:Cerneala.UI.Resources.FontResource:Body:1"` after `SetResource(body, regular)` and `"resource:Cerneala.UI.Resources.FontResource:Body:2"` after `SetResource(body, large)`. For other provider implementations, the version component is `0`.

## Constructors
| Name | Description |
| --- | --- |
| `FontResolver()` | Creates a resolver backed by the default `SystemFontSource`; it has no resource provider. |
| `FontResolver(IFontSource fontSource)` | Creates a resolver that loads family-name requests through `fontSource`. Throws `ArgumentNullException` when `fontSource` is `null`. |
| `FontResolver(IResourceProvider resourceProvider)` | Creates a resolver that can resolve `FontResource` values and has no font source, so family names resolve to the deterministic fallback font. Throws `ArgumentNullException` when `resourceProvider` is `null`. |
| `FontResolver(IFontSource? fontSource, IResourceProvider? resourceProvider)` | Creates a resolver with optional family-name and resource-backed font services. `null` values are accepted. |

## Properties
| Name | Description |
| --- | --- |
| `Default` | Gets a shared resolver constructed with the default `SystemFontSource` and no resource provider. |

## Methods
| Name | Description |
| --- | --- |
| `Resolve(string familyName, float size)` | Resolves a font by family name and size through the configured font source, or creates the deterministic fallback when no source is configured. Throws `ArgumentException` for empty or whitespace family names, and `ArgumentOutOfRangeException` for non-positive or non-finite sizes. |
| `Resolve(string familyName, float size, FontWeight weight, FontStyle style, FontStretch stretch)` | Resolves a font face by family name, size, weight, style, and stretch through `IFontSource.LoadFont(familyName, size, weight, style, stretch)`. With `Normal` for all three it is the same as `Resolve(familyName, size)`. Throws `ArgumentException` for empty or whitespace family names, and `ArgumentOutOfRangeException` for non-positive or non-finite sizes. |
| `Resolve(TextAspect aspect)` | Resolves the font described by `aspect`. A font resource id takes precedence over family name and scaled font size, and a font resource ignores the weight, style, and stretch of the aspect. Otherwise the family, the scaled size, and the weight, style, and stretch of the aspect select the face. |
| `Resolve(ResourceId<FontResource> id)` | Resolves a `FontResource` through the configured resource provider. Throws `InvalidOperationException` when no provider is configured, and `KeyNotFoundException` when the resource provider cannot find the resource. |

## Applies To
`Cerneala` UI text services that need an `IDrawFont`, including text measurement and rendering paths.

## See Also
- `Cerneala.UI.Text.ResolvedTextFont`
- `Cerneala.UI.Text.TextAspect`
- `Cerneala.UI.Resources.FontResource`
- `Cerneala.Drawing.IFontSource`
- `Cerneala.Drawing.IDrawFont`
