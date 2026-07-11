# AspectPackageBuilder Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/AspectPackageBuilder.cs`

Builds an `AspectPackage` by collecting token defaults, component rules, component templates, and content templates through a fluent API.

```csharp
public sealed class AspectPackageBuilder
```

Inheritance:
`object` -> `AspectPackageBuilder`

## Examples

Create a package with a token default, a component template, and a content template:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;
using Cerneala.UI.Media;
using Cerneala.UI.Controls.Templates;

AspectToken<Color> accentToken = AspectToken.Color("app.accent");
ComponentTemplateDefinition buttonTemplate = new("App.Button", typeof(Button), ButtonTemplates.Modern);
ContentTemplateDefinition stringTemplate = new("App.String", typeof(string), key: null, template: null);

AspectPackage package = AspectPackage.Create("App")
    .Tokens(tokens => tokens.Set(accentToken, Color.White))
    .Components(components => components.AddTemplate(buttonTemplate))
    .Content(content => content.Add(stringTemplate));
```

Create and register a package that contributes an aspect rule:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectPackage package = AspectPackage.Create("App")
    .Components(components => components.AddRule(new AspectRuleSet(
        "button.surface",
        AspectLayer.App,
        new AspectTarget(typeof(Button)),
        [
            new AspectDeclaration(
                Control.BackgroundProperty,
                AspectValue<Brush?>.Literal(new SolidColorBrush(Color.White)))
        ],
        declarationOrder: 0)));

AspectCatalog catalog = new AspectRegistry()
    .Register(package)
    .BuildCatalog();
```

## Remarks

`AspectPackageBuilder` is created by `AspectPackage.Create(string name)`. Its constructor is internal, so callers normally start from `AspectPackage.Create` instead of constructing the builder directly.

The builder keeps the package name supplied at creation time and appends contributions into internal collections. `Tokens`, `Components`, and `Content` each create a specialized builder over the same pending package data, invoke the supplied callback, and return the original `AspectPackageBuilder` so calls can be chained.

`Build()` materializes the current builder state into a new `AspectPackage` by copying the collected tokens, rules, component templates, and content templates to arrays. The implicit conversion operator calls `Build()`, which allows a fluent builder expression to be assigned to an `AspectPackage`.

The builder validates only the callback arguments it receives. Package name validation is handled when `AspectPackage.Create` creates the builder, and package-level conflicts such as duplicate registered package names or incompatible token defaults are handled later by `AspectRegistry` and `AspectCatalog`.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Name` | `string` | Gets the package name that will be assigned to the built `AspectPackage`. |

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Tokens(Action<AspectTokenBuilder> build)` | `AspectPackageBuilder` | Invokes `build` with an `AspectTokenBuilder` that appends token default definitions to the package, then returns this builder. |
| `Components(Action<ComponentAspectBuilder> build)` | `AspectPackageBuilder` | Invokes `build` with a `ComponentAspectBuilder` that appends component rules and component templates, then returns this builder. |
| `Content(Action<ContentTemplateBuilder> build)` | `AspectPackageBuilder` | Invokes `build` with a `ContentTemplateBuilder` that appends content template definitions, then returns this builder. |
| `Build()` | `AspectPackage` | Creates an `AspectPackage` from the currently collected package data. |

## Operators

| Name | Return Type | Description |
| --- | --- | --- |
| `implicit operator AspectPackage(AspectPackageBuilder builder)` | `AspectPackage` | Converts a non-null builder to an `AspectPackage` by calling `Build()`. |

## Exceptions

| Member | Exception | Condition |
| --- | --- | --- |
| `Tokens(Action<AspectTokenBuilder>)` | `ArgumentNullException` | `build` is `null`. |
| `Components(Action<ComponentAspectBuilder>)` | `ArgumentNullException` | `build` is `null`. |
| `Content(Action<ContentTemplateBuilder>)` | `ArgumentNullException` | `build` is `null`. |
| `implicit operator AspectPackage(AspectPackageBuilder)` | `ArgumentNullException` | `builder` is `null`. |

## Applies to

Cerneala UI aspect package composition.

## See also

- `Cerneala.UI.Aspect.AspectPackage`
- `Cerneala.UI.Aspect.AspectRegistry`
- `Cerneala.UI.Aspect.AspectCatalog`
- `Cerneala.UI.Aspect.AspectTokenBuilder`
- `Cerneala.UI.Aspect.ComponentAspectBuilder`
- `Cerneala.UI.Aspect.ContentTemplateBuilder`
