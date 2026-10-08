# Motion API

Common examples:

```csharp
button.Motion().Opacity.To(0.6f, Motion.Tween<float>(TimeSpan.FromMilliseconds(120)));
button.Motion().TranslateX.To(24f, Motion.Spring<float>());
```

Implicit transactions animate effective-value changes on registered animatable
properties of elements attached to the same root. Use framework value sources
such as `AspectBase`, with no `Local` override masking the animation:

```csharp
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Media;
using Cerneala.UI.Motion.Specs;

UIRoot root = new();
Control panel = new();
root.VisualChildren.Add(panel);
panel.SetValue(Control.BackgroundProperty, new SolidColorBrush(Color.Black), UiPropertyValueSource.AspectBase);
root.ProcessFrame();

using (root.Motion.BeginTransaction(Motion.Tween(TimeSpan.FromMilliseconds(100))))
{
    panel.SetValue(Control.BackgroundProperty, new SolidColorBrush(Color.White), UiPropertyValueSource.AspectBase);
}
```

The root's frame processing samples the animation. Ordinary C# setters such as
`panel.Opacity = 0f` write the `Local` source, which outranks `Animation`; a
transaction does not change that precedence, so the visible value jumps rather
than animates. `Width` is not registered as animatable by default and is ignored
by transactions. Layout motion is a separate facility below.

State targets use interactive priority and return to the property's captured baseline when the state exits:

```csharp
button.Motion().States()
    .When(AspectState.Hover)
    .Set(
        UIElement.OpacityProperty,
        0.6f,
        Motion.Tween<float>(TimeSpan.FromMilliseconds(120)));
```

Explicit motion uses `MotionPriority.Normal` by default and therefore outranks state motion. Lower-priority start requests return a canceled handle without replacing the active motion.

Layout and presence:

```csharp
panel.LayoutMotionId = "settings-panel";
panel.LayoutMotion = LayoutMotionOptions.Spring(Motion.Tween<Transform>(TimeSpan.FromMilliseconds(160)));
panel.Presence = PresenceOptions.FadeAndScale(root.Motion.Tokens.Enter, root.Motion.Tokens.Exit);
```

Scroll-linked motion:

```csharp
ScrollTimeline timeline = scrollViewer.Motion().ScrollTimeline();
header.Motion().Opacity.Bind(timeline.Progress.Map(1f, 0f));
timeline.Update();
```

Named timelines can be shared through the root-owned registry:

```csharp
root.Motion.Timelines.Register("page-progress", timeline);
MotionTimeline shared = root.Motion.Timelines.Get("page-progress");
```

Avoid giant storyboard trees and avoid animating layout properties every frame unless layout work is explicitly intended.
