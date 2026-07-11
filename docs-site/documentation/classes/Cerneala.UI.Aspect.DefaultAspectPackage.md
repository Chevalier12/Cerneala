# DefaultAspectPackage Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/DefaultAspectPackage.cs`

Creates the built-in aspect package and matching default aspect environment used by Cerneala UI.

```csharp
public static class DefaultAspectPackage
```

Inheritance:
`object` -> `DefaultAspectPackage`

## Examples

Register the default package and create the environment used for aspect resolution:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

AspectCatalog catalog = new AspectRegistry()
    .Register(DefaultAspectPackage.Create())
    .BuildCatalog();

AspectEnvironment environment = DefaultAspectPackage.CreateEnvironment();
AspectEngine engine = new();

Button button = new();
engine.Apply(button, catalog, environment);
```

Read one of the default token values from the environment:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Aspect;

AspectEnvironment environment = DefaultAspectPackage.CreateEnvironment();

if (environment.TryGet(DefaultAspectTokens.Color.Accent, out Color accent))
{
    // accent is Color(37, 99, 235).
}
```

## Remarks

`DefaultAspectPackage` is a convenience factory for the framework's built-in aspect configuration. `Create()` builds an `AspectPackage` named `Default`; `CreateEnvironment()` builds an `AspectEnvironment` named `default` and sets the same default token values into it.

The package contributes default token definitions, a `Button.Modern` component template for `Button`, and two theme-layer rules:

| Rule | Target | Declarations |
| --- | --- | --- |
| `button.base` | `Button` | Sets `Control.BackgroundProperty`, `Control.ForegroundProperty`, `Control.BorderBrushProperty`, `Control.BorderThicknessProperty`, and `Control.PaddingProperty` from button/default tokens. |
| `border.base` | `Border` | Sets `Control.BackgroundProperty` from `DefaultAspectTokens.Color.Surface` and `Control.BorderBrushProperty` from `DefaultAspectTokens.Color.Border`. |

The default token values include neutral surface colors, a blue accent color, `Default` typography, 8-unit control padding, 1-unit control border thickness, fast and normal tween motion specs, and button-specific background, foreground, border, hover, pressed, disabled opacity, and padding tokens.

`CreateEnvironment()` does not register rules or templates. It only prepares token values for lookup during aspect resolution. Register the package through `AspectRegistry` and pass the environment to `AspectEngine` or related processing code when both default rules/templates and default token values are needed.

## Methods

| Name | Return Type | Description |
| --- | --- | --- |
| `Create()` | `AspectPackage` | Creates the built-in package named `Default`, including default token definitions, button and border rules, and the `Button.Modern` component template definition. |
| `CreateEnvironment()` | `AspectEnvironment` | Creates an environment named `default` and populates it with the same default token values used by the package. |

## Applies to

Cerneala UI default aspect registration, token resolution, button templating, and theme-layer control styling.

## See also

- `Cerneala.UI.Aspect.AspectPackage`
- `Cerneala.UI.Aspect.AspectRegistry`
- `Cerneala.UI.Aspect.AspectEnvironment`
- `Cerneala.UI.Aspect.DefaultAspectTokens`
- `Cerneala.UI.Controls.Buttons.ButtonTemplates`
- `Cerneala.UI.Controls.Buttons.ButtonTokens`
