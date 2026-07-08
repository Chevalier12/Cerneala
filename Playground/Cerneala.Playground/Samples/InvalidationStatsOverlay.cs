#nullable enable

using System;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;
using Cerneala.UI.Hosting;
using Cerneala.UI.Invalidation;
using Cerneala.UI.Layout;
using Cerneala.UI.Rendering;
using Cerneala.UI.Resources;
using Cerneala.UI.Text;

namespace Cerneala.Playground.Samples;

public sealed class InvalidationStatsOverlay
{
    private readonly DiagnosticsTextBlock text;

    public InvalidationStatsOverlay(IResourceProvider? resourceProvider = null, ResourceId<FontResource>? fontResourceId = null)
    {
        text = new DiagnosticsTextBlock
        {
            Foreground = DrawColor.White,
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            ResourceProvider = resourceProvider,
            FontResourceId = fontResourceId
        };
        text.SetDiagnosticsText(Format(null));

        Root = new Border
        {
            Margin = new Thickness(32, 8, 32, 0),
            Padding = new Thickness(10),
            Background = new DrawColor(24, 28, 36, 230),
            BorderColor = new DrawColor(74, 86, 104),
            BorderThickness = new Thickness(1),
            Child = text
        };
    }

    public UIElement Root { get; }

    public string Text => text.DiagnosticsText;

    public void Update(UiFrame? frame)
    {
        if (frame is null)
        {
            return;
        }

        text.SetDiagnosticsText(Format(frame));
    }

    public static string Format(UiFrame? frame)
    {
        if (frame is null)
        {
            return "Frame stats: waiting for first retained frame";
        }

        return string.Create(
            System.Globalization.CultureInfo.InvariantCulture,
            $"Frame stats: scale={frame.Viewport.Scale}, inherited={frame.Stats.InheritedElements}, commandState={frame.Stats.CommandStateElements}, aspect={frame.Stats.AspectElements}, queuedMeasure={frame.Stats.MeasuredElements}, queuedArrange={frame.Stats.ArrangedElements}, measureCalls={frame.Stats.MeasureCalls}, arrangeCalls={frame.Stats.ArrangeCalls}, renderCache={frame.Stats.RenderedElements}, hitTest={frame.Stats.HitTestElements}, reusedCaches={frame.Stats.ReusedCaches}, noWork={frame.Stats.NoWorkFrames}, motion={frame.Stats.MotionFrames}, sampled={frame.Stats.MotionNodesSampled}, motionValues={frame.Stats.MotionValuesChanged}, motionWrites={frame.Stats.MotionPropertyWrites}, completed={frame.Stats.MotionCompleted}, motionRender={frame.Stats.MotionRenderInvalidations}, motionLayout={frame.Stats.MotionLayoutInvalidations}, reduced={frame.Stats.MotionSkippedByReducedMotion}");
    }

    private sealed class DiagnosticsTextBlock : TextBlock
    {
        private const string LayoutReservationText =
            "Frame stats: scale=1, inherited=0000, commandState=0000, aspect=0000, queuedMeasure=0000, queuedArrange=0000, measureCalls=0000, arrangeCalls=0000, renderCache=0000, hitTest=0000, reusedCaches=0000, noWork=0000, motion=0000, sampled=0000, motionValues=0000, motionWrites=0000, completed=0000, motionRender=0000, motionLayout=0000, reduced=0000";

        private readonly TextLayoutCache resourceTextLayoutCache = new();
        private string diagnosticsText = string.Empty;

        public string DiagnosticsText => diagnosticsText;

        public void SetDiagnosticsText(string? value)
        {
            string next = value ?? string.Empty;
            if (diagnosticsText == next)
            {
                return;
            }

            diagnosticsText = next;
            Invalidate(InvalidationFlags.Render | InvalidationFlags.Semantics, "Diagnostics text changed");
            FlushDiagnosticsRender();
        }

        private void FlushDiagnosticsRender()
        {
            UIRoot? root = Root;
            if (root is null)
            {
                return;
            }

            root.RenderQueueProcessor.Process(this);
            root.RenderQueue.Remove(this);
            DirtyState.Clear(InvalidationFlags.Render | InvalidationFlags.Semantics);
        }

        protected override LayoutSize MeasureCore(MeasureContext context)
        {
            TextAspect aspect = CreateTextAspect();
            TextMeasureResult measurement = GetTextMeasurer().Measure(LayoutReservationText, aspect, context.AvailableSize.Width);
            SetRenderDependencies(RenderDependency.None.WithTextLayoutIdentity(measurement.CacheKey.ToString()));
            return measurement.Size;
        }

        protected override void OnRender(RenderContext context)
        {
            if (string.IsNullOrEmpty(diagnosticsText))
            {
                return;
            }

            TextAspect aspect = CreateTextAspect();
            _ = GetTextRenderer().Render(
                context.DrawingContext,
                diagnosticsText,
                aspect,
                context.Bounds.Width,
                new DrawPoint(context.Bounds.X, context.Bounds.Y),
                Foreground);
        }

        private TextAspect CreateTextAspect()
        {
            return new TextAspect(FontFamily, FontSize, wrapping: TextWrapping, color: Foreground, fontResourceId: FontResourceId);
        }

        private TextMeasurer GetTextMeasurer()
        {
            IResourceProvider? provider = ResourceProvider ?? Root?.ResourceProvider;
            if (FontResourceId is not null && provider is not null)
            {
                return new TextMeasurer(new FontResolver(provider), LineBreakService.Default, resourceTextLayoutCache);
            }

            return TextMeasurer;
        }

        private TextRenderer GetTextRenderer()
        {
            IResourceProvider? provider = ResourceProvider ?? Root?.ResourceProvider;
            if (FontResourceId is not null && provider is not null)
            {
                TextMeasurer measurer = new(new FontResolver(provider), LineBreakService.Default, resourceTextLayoutCache);
                return new TextRenderer(new FontResolver(provider), measurer);
            }

            return TextRenderer;
        }
    }
}
