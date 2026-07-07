# Motion API

Common examples:

```csharp
button.Motion().Opacity.To(0.6f, Motion.Tween<float>(TimeSpan.FromMilliseconds(120)));
button.Motion().TranslateX.To(24f, Motion.Spring<float>());
```

Implicit transactions animate property changes:

```csharp
using (root.Motion.BeginTransaction(Motion.Spring()))
{
    panel.Width = 320;
}
```

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

Avoid giant storyboard trees and avoid animating layout properties every frame unless layout work is explicitly intended.
