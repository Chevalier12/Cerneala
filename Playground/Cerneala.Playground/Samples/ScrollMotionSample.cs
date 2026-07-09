#nullable enable

using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Core;
using Cerneala.UI.Elements;
using Cerneala.UI.Layout;
using Cerneala.UI.Motion;
using Cerneala.UI.Motion.Input;
using Cerneala.UI.Resources;
using Grid = Cerneala.UI.Layout.Panels.Grid;
using GridLength = Cerneala.UI.Layout.Panels.GridLength;
using PanelOrientation = Cerneala.UI.Layout.Orientation;
using RowDefinition = Cerneala.UI.Layout.Panels.RowDefinition;

namespace Cerneala.Playground.Samples;

public sealed class ScrollMotionSample : IPlaygroundSample
{
    private readonly PlaygroundText text;

    public ScrollMotionSample(IResourceProvider? resourceProvider = null, ResourceId<FontResource>? fontResourceId = null)
    {
        text = new PlaygroundText(resourceProvider, fontResourceId);
    }

    public string Name => "Scroll Motion";

    public UIElement Build()
    {
        Border header = new()
        {
            Padding = new Thickness(10),
            Background = new DrawColor(14, 165, 233),
            Child = text.Create("Header fade", 18, DrawColor.White)
        };
        Border parallax = new()
        {
            Padding = new Thickness(14),
            Background = new DrawColor(99, 102, 241),
            Child = text.Create("Parallax", 16, DrawColor.White)
        };
        Border parallaxViewport = new()
        {
            Background = new DrawColor(103, 232, 249),
            ClipToBounds = true,
            Child = parallax
        };
        Border progress = new()
        {
            Background = new DrawColor(34, 197, 94),
            RenderTransformOrigin = new LayoutPoint(0, 0.5f)
        };
        StackPanel content = new() { Orientation = PanelOrientation.Vertical };
        for (int i = 0; i < 24; i++)
        {
            content.VisualChildren.Add(new Border
            {
                Padding = new Thickness(12),
                Background = i % 2 == 0 ? new DrawColor(241, 245, 249) : new DrawColor(226, 232, 240),
                Child = text.Create($"Scroll row {i + 1}", 16, new DrawColor(30, 41, 59))
            });
        }

        ScrollMotionViewer scrollViewer = new(header, parallax, progress)
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = content
        };
        Grid.SetRow(header, 0);
        Grid.SetRow(parallaxViewport, 1);
        Grid.SetRow(progress, 2);
        Grid.SetRow(scrollViewer, 3);

        Grid root = new();
        root.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        root.RowDefinitions.Add(new RowDefinition(GridLength.Pixels(72)));
        root.RowDefinitions.Add(new RowDefinition(GridLength.Pixels(4)));
        root.RowDefinitions.Add(new RowDefinition(GridLength.Star));
        root.VisualChildren.Add(header);
        root.VisualChildren.Add(parallaxViewport);
        root.VisualChildren.Add(progress);
        root.VisualChildren.Add(scrollViewer);
        return root;
    }

    private sealed class ScrollMotionViewer(Border header, Border parallax, Border progress) : ScrollViewer
    {
        private ScrollTimeline? timeline;

        protected override LayoutRect ArrangeCore(ArrangeContext context)
        {
            LayoutRect arranged = base.ArrangeCore(context);
            EnsureTimeline();
            timeline?.Update();
            return arranged;
        }

        private void EnsureTimeline()
        {
            if (timeline is not null || Root is null)
            {
                return;
            }

            timeline = this.Motion().ScrollTimeline();
            header.Motion().Opacity.Bind(timeline.Progress.Map(1f, 0.6f));
            parallax.Motion().TranslateY.Bind(timeline.Progress.Map(8f, -8f));
            progress.Motion().Animate(UIElement.ScaleXProperty).Bind(timeline.Progress.Map(0.04f, 1f));
            Presenter.PropertyChanged += OnPresenterPropertyChanged;
        }

        private void OnPresenterPropertyChanged(object? sender, UiPropertyChangedEventArgs args)
        {
            timeline?.Update();
        }
    }
}
