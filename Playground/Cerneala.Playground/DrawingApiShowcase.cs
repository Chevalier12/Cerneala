using Cerneala.Drawing;
using Cerneala.Drawing.Text;
using Cerneala.UI.Controls;
using Cerneala.UI.Layout;
using Cerneala.UI.Media;
using Cerneala.UI.Resources;

namespace Cerneala.Playground;

public sealed class DrawingApiShowcase : RenderSurface2D
{
    private static readonly SolidColorBrush SurfaceBrush = new(new Color(15, 20, 27));
    private static readonly SolidColorBrush PanelBrush = new(new Color(28, 36, 47));
    private static readonly SolidColorBrush CyanBrush = new(new Color(77, 240, 255));
    private static readonly SolidColorBrush PinkBrush = new(new Color(255, 62, 165));
    private static readonly SolidColorBrush LimeBrush = new(new Color(198, 255, 61));
    private static readonly SolidColorBrush PaperBrush = new(new Color(237, 239, 243));
    private static readonly DrawPen AccentPen = new(
        CyanBrush,
        2,
        new DrawStrokeStyle(
            DrawLineCap.Round,
            DrawLineCap.Round,
            DrawLineJoin.Round,
            dashPattern: [6, 3]));
    private static readonly ImageResource MascotResource = new("drawing-api-mascot.png");

    private readonly DrawPath reusablePath;
    private readonly DrawPath geometricClip;
    private readonly DrawMesh2D mesh;
    private readonly DrawPointBatch points;
    private readonly DrawLineBatch lines;
    private readonly DrawTextLayout textLayout;
    private IDrawImage? mascot;
    private DrawSpriteBatch? sprites;

    public DrawingApiShowcase()
    {
        RedrawMode = RenderSurface2DRedrawMode.OnDemand;
        ClearColor = new Color(10, 14, 20);
        Margin = new Thickness(18);

        reusablePath = DrawPathParser.ParseSvg(
            "M0 18 C0 6 14 0 24 10 C34 0 48 6 48 18 C48 31 34 40 24 48 C14 40 0 31 0 18 Z");
        geometricClip = DrawPathFactory.Star(
            new DrawPoint(356, 249),
            34,
            18,
            6,
            -MathF.PI / 2);
        mesh = new DrawMesh2D(
            [
                new DrawVertex2D(new DrawPoint(28, 353), new Color(77, 240, 255)),
                new DrawVertex2D(new DrawPoint(86, 333), new Color(255, 62, 165)),
                new DrawVertex2D(new DrawPoint(112, 382), new Color(198, 255, 61)),
                new DrawVertex2D(new DrawPoint(48, 392), Color.White)
            ],
            [0, 1, 2, 0, 2, 3]);
        points = new DrawPointBatch(
            [
                new DrawPoint(147, 342),
                new DrawPoint(165, 361),
                new DrawPoint(187, 339),
                new DrawPoint(207, 374)
            ],
            new Color(255, 200, 87),
            7);
        lines = new DrawLineBatch(
            [
                new DrawLineSegment2D(new DrawPoint(132, 390), new DrawPoint(164, 370), new Color(77, 240, 255), 3),
                new DrawLineSegment2D(new DrawPoint(164, 370), new DrawPoint(200, 394), new Color(255, 62, 165), 3),
                new DrawLineSegment2D(new DrawPoint(200, 394), new DrawPoint(224, 358), new Color(198, 255, 61), 3)
            ]);

        SystemFontSource fontSource = new();
        IDrawFont font = fontSource.LoadFont("Segoe UI", 13);
        IDrawFont emojiFont = fontSource.LoadFont("Segoe UI Emoji", 13);
        textLayout = new DrawTextLayoutBuilder()
            .AddSpan("Reusable layout · ", font, 13, PaperBrush)
            .AddSpan("styled runs", font, 13, CyanBrush)
            .AddSpan(" · bidi مرحبا · emoji ", font, 13, PinkBrush)
            .AddSpan("🙂", emojiFont, 13, PinkBrush)
            .Build(new DrawTextLayoutOptions(
                maxWidth: 440,
                wrapping: DrawTextWrapping.Word,
                alignment: DrawTextAlignment.Center,
                maxLines: 2,
                trimming: DrawTextTrimming.WordEllipsis));
    }

    protected override void OnDraw(RenderSurface2DFrame frame)
    {
        EnsureImage();
        frame.FillRectangle(frame.Bounds, SurfaceBrush);
        frame.FillRoundedRectangle(
            new DrawRect(8, 8, MathF.Max(0, frame.Bounds.Width - 16), 424),
            new DrawCornerRadius(16),
            PanelBrush);

        DrawShapeCatalog(frame);
        DrawStateComposition(frame);
        DrawImageCatalog(frame);
        frame.DrawMesh(mesh);
        frame.DrawPointBatch(points);
        frame.DrawLineBatch(lines);
        frame.DrawTextLayout(textLayout, new DrawPoint(18, 414));
    }

    private void DrawShapeCatalog(RenderSurface2DFrame frame)
    {
        frame.FillRectangle(new DrawRect(22, 28, 34, 24), CyanBrush);
        frame.DrawRectangle(new DrawRect(20, 26, 38, 28), AccentPen);
        frame.FillRoundedRectangle(new DrawRect(72, 26, 42, 28), new DrawCornerRadius(4, 12, 4, 12), PinkBrush);
        frame.DrawRoundedRectangle(new DrawRect(70, 24, 46, 32), new DrawCornerRadius(6), AccentPen);
        frame.FillEllipse(new DrawRect(132, 25, 38, 30), LimeBrush);
        frame.DrawEllipse(new DrawRect(130, 23, 42, 34), AccentPen);
        frame.FillCircle(new DrawPoint(201, 40), 16, PinkBrush);
        frame.DrawPoint(new DrawPoint(239, 40), LimeBrush, 12);
        frame.DrawLine(new DrawPoint(260, 52), new DrawPoint(300, 27), AccentPen);
        frame.DrawPolyline(
            [new DrawPoint(316, 51), new DrawPoint(330, 26), new DrawPoint(344, 51), new DrawPoint(358, 26)],
            AccentPen);
        frame.FillPolygon(
            [new DrawPoint(374, 54), new DrawPoint(389, 24), new DrawPoint(406, 54)],
            PinkBrush);
        frame.DrawTriangle(
            new DrawPoint(421, 54),
            new DrawPoint(438, 24),
            new DrawPoint(455, 54),
            AccentPen);

        frame.DrawArc(new DrawPoint(42, 101), 22, 20, 0, MathF.PI * 1.5f, AccentPen);
        frame.FillPie(new DrawPoint(104, 101), 23, 21, -MathF.PI / 2, MathF.PI * 1.35f, PinkBrush);
        frame.FillChord(new DrawPoint(166, 101), 24, 21, 0, MathF.PI * 1.45f, LimeBrush);
        frame.FillRegularPolygon(new DrawPoint(228, 101), 23, 6, CyanBrush, MathF.PI / 6);
        frame.FillStar(new DrawPoint(292, 101), 25, 11, 5, PinkBrush, -MathF.PI / 2);
        frame.FillPath(
            reusablePath,
            reusablePath.Bounds,
            new DrawRect(334, 77, 46, 46),
            LimeBrush,
            DrawFillRule.EvenOdd);
        frame.DrawRegularPolygon(new DrawPoint(424, 101), 24, 8, AccentPen, MathF.PI / 8);
    }

    private void DrawStateComposition(RenderSurface2DFrame frame)
    {
        frame.PushClip(new DrawRect(20, 140, 220, 72));
        frame.PushLayer(new DrawLayerOptions(0.82f, DrawBlendMode.Screen));
        frame.FillCircle(new DrawPoint(72, 176), 34, CyanBrush);
        frame.FillCircle(new DrawPoint(111, 176), 34, PinkBrush);
        frame.FillCircle(new DrawPoint(150, 176), 34, LimeBrush);
        frame.PopLayer();
        frame.PushTransform(System.Numerics.Matrix3x2.CreateRotation(
            0.14f,
            new System.Numerics.Vector2(204, 176)));
        frame.DrawPath(
            reusablePath,
            reusablePath.Bounds,
            new DrawRect(178, 148, 50, 50),
            AccentPen);
        frame.PopTransform();
        frame.PopClip();
    }

    private void DrawImageCatalog(RenderSurface2DFrame frame)
    {
        if (mascot is null)
        {
            frame.FillRoundedRectangle(new DrawRect(260, 140, 198, 162), new DrawCornerRadius(12), new Color(40, 48, 60));
            return;
        }

        frame.DrawImage(
            mascot,
            new DrawRect(260, 140, 70, 70),
            new DrawImageOptions(
                opacity: 0.96f,
                sampling: DrawSamplingMode.Linear));
        frame.DrawImageQuad(
            mascot,
            new DrawPoint(344, 142),
            new DrawPoint(417, 151),
            new DrawPoint(408, 213),
            new DrawPoint(337, 204),
            new DrawImageOptions(opacity: 0.9f));
        frame.DrawNineSlice(
            mascot,
            new DrawRect(260, 225, 92, 70),
            new DrawInsets(24));

        frame.PushClip(geometricClip, DrawFillRule.EvenOdd);
        frame.DrawImage(
            mascot,
            new DrawRect(322, 215, 70, 70),
            new DrawImageOptions(
                rotation: 0.08f,
                origin: new DrawPoint(mascot.Width / 2f, mascot.Height / 2f),
                sampling: DrawSamplingMode.Point));
        frame.PopClip();

        if (sprites is not null)
        {
            frame.DrawSpriteBatch(sprites);
        }
    }

    private void EnsureImage()
    {
        if (mascot is not null || Root?.ImageResourceCache is not ImageResourceCache cache)
        {
            return;
        }

        mascot = cache.Resolve(MascotResource);
        sprites = new DrawSpriteBatch(
            mascot,
            [
                new DrawSprite2D(
                    new DrawRect(404, 226, 48, 32),
                    new DrawImageOptions(sampling: DrawSamplingMode.Point)),
                new DrawSprite2D(
                    new DrawRect(404, 263, 48, 32),
                    new DrawImageOptions(
                        flip: DrawImageFlip.Horizontal,
                        sampling: DrawSamplingMode.Point))
            ]);
    }
}
