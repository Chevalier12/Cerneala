# ViewportDiagnosticsSnapshot Record

Namespace: `Cerneala.UI.Detective`

Assembly: `Cerneala.dll`

Source: [`UI/Detective/Detective.cs`](https://github.com/Chevalier12/Cerneala/blob/master/UI/Detective/Detective.cs)

## Definition

Stores the logical viewport dimensions and scale copied from a `UIRoot`.

```csharp
public sealed record ViewportDiagnosticsSnapshot(
    float LogicalWidth,
    float LogicalHeight,
    float Scale);
```

## Examples

```csharp
ViewportDiagnosticsSnapshot viewport = root.Detective.Capture(stats).Viewport;
```

## Properties

| Name | Type | Description |
| --- | --- | --- |
| `LogicalHeight` | `float` | Gets the logical viewport height. |
| `LogicalWidth` | `float` | Gets the logical viewport width. |
| `Scale` | `float` | Gets the viewport scale. |
