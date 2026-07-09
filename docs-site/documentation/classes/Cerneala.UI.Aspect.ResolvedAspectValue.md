# ResolvedAspectValue Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/ResolvedAspectValue.cs`

Represents the winning resolved value for one UI property after aspect rule matching and cascade comparison.

```csharp
public sealed class ResolvedAspectValue
```

Inheritance:
`object` -> `ResolvedAspectValue`

## Examples

Read the winning resolved value for a property after resolving a catalog:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Aspect;
using Cerneala.UI.Controls;

Button button = new();
AspectDeclaration declaration = new(
    Control.BackgroundProperty,
    AspectValue<DrawColor>.Literal(DrawColor.White),
    diagnosticName: "button background");

AspectCatalog catalog = new AspectRegistry()
    .Register(AspectPackage.Create("App").Components(components =>
    {
        components.AddRule(new AspectRuleSet(
            "button",
            AspectLayer.App,
            new AspectTarget(typeof(Button)),
            [declaration],
            declarationOrder: 0));
    }))
    .BuildCatalog();

ResolvedAspect resolved = new AspectEngine()
    .Resolve(button, catalog, new AspectEnvironment("example"));

ResolvedAspectValue value = resolved.Values[Control.BackgroundProperty];
DrawColor background = (DrawColor)value.Value!;
AspectDeclaration source = value.SourceDeclaration;
```

## Remarks

`ResolvedAspectValue` is produced by `AspectEngine.Resolve` for each `UiProperty` that has at least one matching aspect declaration. The engine resolves each declaration's `AspectValue`, compares candidates with the internal cascade key, and stores only the winner in `ResolvedAspect.Values`.

`Property` identifies the UI property that won, and `Value` is the already-resolved runtime value that `AspectEngine.Apply` writes through `SetValueUntyped` with the `AspectBase` value source. `Value` is typed as `object?` because aspect resolution handles properties with different value types in one dictionary.

`SourceDeclaration` keeps the declaration that supplied the winning value. Diagnostics and traces use it to report the declaration name, token dependencies, and rejected competing declarations.

`Motion` carries optional motion metadata copied from the source declaration. The constructor and cascade key are internal, so consumers normally obtain instances from `ResolvedAspect.Values` rather than creating them directly.

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `Property` | `UiProperty` | Gets the UI property associated with the resolved value. |
| `Value` | `object?` | Gets the resolved runtime value for `Property`. |
| `SourceDeclaration` | `AspectDeclaration` | Gets the winning declaration that produced this value. |
| `Motion` | `AspectMotion?` | Gets optional motion metadata carried from the winning declaration. |

## Applies to

Cerneala UI aspect resolution, aspect diagnostics, and aspect application through `AspectEngine`.

## See also

- `Cerneala.UI.Aspect.ResolvedAspect`
- `Cerneala.UI.Aspect.AspectEngine`
- `Cerneala.UI.Aspect.AspectDeclaration`
- `Cerneala.UI.Aspect.AspectValue`
- `Cerneala.UI.Diagnostics.AspectTrace`
