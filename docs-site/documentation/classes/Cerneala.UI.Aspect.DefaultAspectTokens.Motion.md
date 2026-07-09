# DefaultAspectTokens.Motion Class

## Definition
Namespace: `Cerneala.UI.Aspect`

Assembly/Project: `Cerneala`

Source: `UI/Aspect/DefaultAspectTokens.cs`

Groups the built-in aspect tokens for default motion specifications.

```csharp
public static class DefaultAspectTokens.Motion
```

Inheritance:
`object` -> `DefaultAspectTokens.Motion`

## Examples

Read the default motion specifications from the default aspect environment:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Motion.Specs;

AspectEnvironment environment = DefaultAspectPackage.CreateEnvironment();

if (environment.TryGet(DefaultAspectTokens.Motion.Fast, out MotionSpec? fast) &&
    environment.TryGet(DefaultAspectTokens.Motion.Normal, out MotionSpec? normal))
{
    // fast is a TweenSpec<float> with a 120 ms duration.
    // normal is a TweenSpec<float> with a 200 ms duration.
}
```

Override the built-in motion tokens in an application package:

```csharp
using Cerneala.UI.Aspect;
using Cerneala.UI.Motion.Specs;

AspectPackage package = AspectPackage.Create("App")
    .Tokens(tokens =>
    {
        tokens.Set(DefaultAspectTokens.Motion.Fast, new TweenSpec<float>(TimeSpan.FromMilliseconds(90)));
        tokens.Set(DefaultAspectTokens.Motion.Normal, new TweenSpec<float>(TimeSpan.FromMilliseconds(180)));
    })
    .Build();
```

## Remarks

`DefaultAspectTokens.Motion` is a static token group. It defines token identities only; it does not store motion specifications by itself. Values are supplied by an `AspectPackage`, an `AspectEnvironment`, or another token source.

Each field is an `AspectToken<MotionSpec>` created with `AspectToken.Motion(string)`. `MotionSpec` is the untyped base for motion specifications; the default package stores `TweenSpec<float>` instances because `TweenSpec<T>` derives from `MotionSpec<T>`, which derives from `MotionSpec`.

`DefaultAspectPackage.Create()` registers `Fast` with `new TweenSpec<float>(TimeSpan.FromMilliseconds(120))` and `Normal` with `new TweenSpec<float>(TimeSpan.FromMilliseconds(200))`. `DefaultAspectPackage.CreateEnvironment()` sets the same values in the returned `AspectEnvironment`.

The motion token names are semantic strings: `motion.fast` and `motion.normal`. Use these tokens when aspect rules or application packages need to refer to shared motion timing rather than embedding a motion specification directly.

## Fields

| Name | Type | Token Name | Default Value | Description |
| --- | --- | --- | --- | --- |
| `Fast` | `AspectToken<MotionSpec>` | `motion.fast` | `new TweenSpec<float>(TimeSpan.FromMilliseconds(120))` | Identifies the fast default motion token. |
| `Normal` | `AspectToken<MotionSpec>` | `motion.normal` | `new TweenSpec<float>(TimeSpan.FromMilliseconds(200))` | Identifies the normal default motion token. |

## Applies to

Cerneala UI aspect packages and aspect environments that resolve default motion token values.

## See also

- `Cerneala.UI.Aspect.DefaultAspectTokens`
- `Cerneala.UI.Aspect.DefaultAspectPackage`
- `Cerneala.UI.Aspect.AspectToken`
- `Cerneala.UI.Aspect.AspectToken<T>`
- `Cerneala.UI.Aspect.AspectEnvironment`
- `Cerneala.UI.Motion.Specs.MotionSpec`
- `Cerneala.UI.Motion.Specs.TweenSpec<T>`
