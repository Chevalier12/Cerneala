# DefaultAspectTokens.Typography Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/DefaultAspectTokens.cs`

Groups the built-in aspect tokens for default text font family and font size values.

```csharp
public static class DefaultAspectTokens.Typography
```

Inheritance:
`object` -> `DefaultAspectTokens.Typography`

## Examples

Read the default typography token values from the default aspect environment:

```csharp
using Cerneala.UI.Aspect;

AspectEnvironment environment = DefaultAspectPackage.CreateEnvironment();

if (environment.TryGet(DefaultAspectTokens.Typography.FontFamily, out string? fontFamily) &&
    environment.TryGet(DefaultAspectTokens.Typography.FontSize, out float fontSize))
{
    // fontFamily is "Default" and fontSize is 16f in the default environment.
}
```

Override the built-in typography tokens in an application package:

```csharp
using Cerneala.UI.Aspect;

AspectPackage package = AspectPackage.Create("App")
    .Tokens(tokens =>
    {
        tokens.Set(DefaultAspectTokens.Typography.FontFamily, "Inter");
        tokens.Set(DefaultAspectTokens.Typography.FontSize, 14f);
    })
    .Build();
```

## Remarks

`DefaultAspectTokens.Typography` is a static token group. It defines token identities only; it does not store typography values by itself. Values are supplied by an `AspectPackage`, an `AspectEnvironment`, or another token source.

`DefaultAspectPackage.Create()` registers `FontFamily` with `"Default"` and `FontSize` with `16f`. `DefaultAspectPackage.CreateEnvironment()` sets the same values in the returned `AspectEnvironment`.

The typography token names are semantic strings: `typography.font-family` and `typography.font-size`. Rules and components can use `AspectToken<T>.Ref()` to defer the actual value lookup to the active aspect environment.

These tokens are separate from `Control.FontFamilyProperty` and `Control.FontSizeProperty`. The control properties also default to `"Default"` and `16`, inherit through the UI tree, affect measure and render, and validate non-empty font family names and positive finite font sizes.

## Fields

| Name | Type | Token Name | Default Value | Description |
| --- | --- | --- | --- | --- |
| `FontFamily` | `AspectToken<string>` | `typography.font-family` | `"Default"` | Identifies the semantic font family token used by the default aspect package and environment. |
| `FontSize` | `AspectToken<float>` | `typography.font-size` | `16f` | Identifies the semantic font size token used by the default aspect package and environment. |

## Applies to

Cerneala UI aspect packages and aspect environments that resolve default typography token values.

## See also

- `Cerneala.UI.Aspect.DefaultAspectTokens`
- `Cerneala.UI.Aspect.DefaultAspectPackage`
- `Cerneala.UI.Aspect.AspectToken`
- `Cerneala.UI.Aspect.AspectToken<T>`
- `Cerneala.UI.Aspect.AspectEnvironment`
- `Cerneala.UI.Controls.Control`
