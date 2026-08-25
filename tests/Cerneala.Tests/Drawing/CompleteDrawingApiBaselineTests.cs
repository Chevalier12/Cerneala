using System.Reflection;
using Cerneala.Drawing;
using Cerneala.UI.Controls;
using Cerneala.UI.Media;

namespace Cerneala.Tests.Drawing;

public sealed class CompleteDrawingApiBaselineTests
{
    [Fact]
    public void ExistingPrimitiveFamiliesRecordTheirPublicPayloads()
    {
        DrawCommandList commands = new();
        DrawingContext drawing = new(commands);
        SolidColorBrush brush = new(Color.Tomato);
        TestFont font = new();
        DrawTextRun text = new(font, "baseline", 14);
        TestImage image = new();

        drawing.FillRectangle(new DrawRect(1, 2, 3, 4), Color.White);
        drawing.DrawRectangle(new DrawRect(2, 3, 4, 5), brush, 2);
        drawing.FillEllipse(new DrawRect(3, 4, 5, 6), brush);
        drawing.DrawEllipse(new DrawRect(4, 5, 6, 7), Color.Black, 3);
        drawing.DrawLine(new DrawPoint(5, 6), new DrawPoint(7, 8), brush, 4);
        drawing.FillPath(
            "M0 0L10 0L10 10Z",
            new DrawRect(0, 0, 10, 10),
            new DrawRect(6, 7, 20, 20),
            brush);
        drawing.DrawText(text, new DrawPoint(8, 9), Color.White);
        drawing.DrawImage(
            image,
            new DrawRect(9, 10, 11, 12),
            new DrawRect(1, 2, 3, 4),
            Color.HotPink,
            rotation: 0.25f,
            origin: new DrawPoint(2, 3),
            DrawImageFlip.Horizontal,
            layerDepth: 0.75f);
        drawing.PushClip(new DrawRect(0, 0, 30, 40));
        drawing.PopClip();

        Assert.Equal(
            [
                DrawCommandKind.FillRectangle,
                DrawCommandKind.DrawRectangle,
                DrawCommandKind.FillEllipse,
                DrawCommandKind.DrawEllipse,
                DrawCommandKind.DrawLine,
                DrawCommandKind.FillPath,
                DrawCommandKind.DrawText,
                DrawCommandKind.DrawImage,
                DrawCommandKind.PushClip,
                DrawCommandKind.PopClip
            ],
            commands.Select(command => command.Kind));

        Assert.Equal(new DrawRect(1, 2, 3, 4), commands[0].Rect);
        Assert.Equal(Color.White, commands[0].Color);
        Assert.Same(brush, commands[1].Brush);
        Assert.Equal(2, commands[1].Thickness);
        Assert.Equal(new DrawPoint(5, 6), commands[4].Position);
        Assert.Equal(new DrawPoint(7, 8), commands[4].EndPoint);
        Assert.Equal("M0 0L10 0L10 10Z", commands[5].PathData);
        Assert.Equal(new DrawRect(0, 0, 10, 10), commands[5].SourceRect);
        Assert.Same(text, commands[6].TextRun);
        Assert.Same(font, commands[6].Font);
        Assert.Same(image, commands[7].Image);
        Assert.Equal(new DrawRect(1, 2, 3, 4), commands[7].ImageSource);
        Assert.Equal(0.25f, commands[7].ImageRotation);
        Assert.Equal(new DrawPoint(2, 3), commands[7].ImageOrigin);
        Assert.Equal(DrawImageFlip.Horizontal, commands[7].ImageFlip);
        Assert.Equal(0.75f, commands[7].LayerDepth);
        Assert.Equal(new DrawRect(0, 0, 30, 40), commands[8].Rect);
    }

    [Fact]
    public void ExistingCommandEqualityAndHashingCoverPayloadAndResourceIdentity()
    {
        SolidColorBrush brush = new(Color.Tomato);
        DrawCommand first = DrawCommand.FillRectangle(
            new DrawRect(1, 2, 3, 4),
            brush,
            opacity: 0.5f);
        DrawCommand same = DrawCommand.FillRectangle(
            new DrawRect(1, 2, 3, 4),
            brush,
            opacity: 0.5f);
        DrawCommand changedPayload = DrawCommand.FillRectangle(
            new DrawRect(2, 2, 3, 4),
            brush,
            opacity: 0.5f);
        DrawCommand changedResource = DrawCommand.FillRectangle(
            new DrawRect(1, 2, 3, 4),
            new SolidColorBrush(Color.Black),
            opacity: 0.5f);

        Assert.Equal(first, same);
        Assert.Equal(first.GetHashCode(), same.GetHashCode());
        Assert.NotEqual(first, changedPayload);
        Assert.NotEqual(first, changedResource);
    }

    [Fact]
    public void ExistingFrameTracksImageDependenciesAndRejectsUseAfterCompletion()
    {
        DrawCommandList commands = new();
        List<IDrawImage> dependencies = [];
        RenderSurface2DFrame frame = new(
            commands,
            new DrawRect(0, 0, 64, 48),
            TimeSpan.FromMilliseconds(16),
            dependencies.Add);
        TestImage image = new();

        frame.DrawImage(image, new DrawRect(1, 2, 3, 4), Color.White);
        frame.DrawSprite(image, new DrawRect(5, 6, 7, 8), Color.White);
        frame.Complete();

        Assert.Equal([image, image], dependencies);
        Assert.Throws<ObjectDisposedException>(
            () => frame.FillRectangle(new DrawRect(0, 0, 1, 1), Color.White));
    }

    private sealed class TestFont : IDrawFont
    {
        public string FamilyName => "Baseline";

        public float Size => 14;
    }

    private sealed class TestImage : IDrawImage
    {
        public int Width => 16;

        public int Height => 16;
    }
}

public sealed class CompleteDrawingApiRedTests
{
    private static readonly Assembly DrawingAssembly = typeof(DrawingContext).Assembly;

    [Fact]
    [Trait("PlanStage", "1")]
    public void TypedPathsRequireFiniteSvgEndpointArcsAndPreserveOpenAndClosedContours()
    {
        Type path = RequireType("Cerneala.Drawing.DrawPath");
        Type builder = RequireType("Cerneala.Drawing.DrawPathBuilder");
        Type parser = RequireType("Cerneala.Drawing.DrawPathParser");
        Type fillRule = RequireType("Cerneala.Drawing.DrawFillRule");

        RequireProperties(path, "Bounds", "Contours", "StableId");
        RequireMethods(builder, "MoveTo", "LineTo", "QuadraticTo", "CubicTo", "ArcTo", "Close", "Build");
        RequireMethods(parser, "ParseSvg");
        RequireEnumValues(fillRule, "NonZero", "EvenOdd");
        RequireMethods(typeof(DrawingContext), "FillPath");
    }

    [Fact]
    [Trait("PlanStage", "2")]
    public void StrokeContractRequiresValidatedCapsJoinsDashesMiterAndClosedContourAlignment()
    {
        Type pen = RequireType("Cerneala.Drawing.DrawPen");
        Type style = RequireType("Cerneala.Drawing.DrawStrokeStyle");
        Type cap = RequireType("Cerneala.Drawing.DrawLineCap");
        Type join = RequireType("Cerneala.Drawing.DrawLineJoin");
        Type alignment = RequireType("Cerneala.Drawing.DrawStrokeAlignment");

        RequireProperties(pen, "Brush", "Thickness", "Style");
        RequireProperties(style, "StartCap", "EndCap", "Join", "MiterLimit", "DashPattern", "DashOffset", "Alignment");
        RequireEnumValues(cap, "Flat", "Square", "Round", "Triangle");
        RequireEnumValues(join, "Miter", "Bevel", "Round");
        RequireEnumValues(alignment, "Inside", "Center", "Outside");
    }

    [Fact]
    [Trait("PlanStage", "3")]
    public void StateContractRequiresLifoScopesWorldBoundsGeometricClipAndIsolatedGroupCompositing()
    {
        Type blendMode = RequireType("Cerneala.Drawing.DrawBlendMode");
        Type layerOptions = RequireType("Cerneala.Drawing.DrawLayerOptions");
        Type analyzer = RequireType("Cerneala.Drawing.DrawCommandStateAnalyzer");

        RequireEnumValues(blendMode, "Normal", "Opaque", "Additive", "Multiply", "Screen");
        RequireProperties(layerOptions, "Opacity", "BlendMode");
        RequireMethods(
            typeof(DrawingContext),
            "PushTransform", "PopTransform", "PushClip", "PopClip",
            "PushOpacity", "PopOpacity", "PushBlend", "PopBlend",
            "PushLayer", "PopLayer", "Transform", "Clip", "Opacity", "Blend", "Layer");
        RequireMethods(analyzer, "Analyze");
    }

    [Fact]
    [Trait("PlanStage", "4")]
    public void ShapeContractRequiresRadianAnglesNormalizedCornerRadiiAndSharedReusablePaths()
    {
        Type cornerRadius = RequireType("Cerneala.Drawing.DrawCornerRadius");
        Type arcDirection = RequireType("Cerneala.Drawing.DrawArcDirection");
        Type pathFactory = RequireType("Cerneala.Drawing.DrawPathFactory");

        RequireProperties(cornerRadius, "TopLeft", "TopRight", "BottomRight", "BottomLeft");
        RequireEnumValues(arcDirection, "Clockwise", "CounterClockwise");
        RequireMethods(pathFactory, "Polygon", "Polyline", "Arc", "Pie", "Chord", "RegularPolygon", "Star");
        RequireMethods(
            typeof(DrawingContext),
            "FillRoundedRectangle", "DrawRoundedRectangle", "FillPolygon", "DrawPolygon",
            "DrawPolyline", "DrawArc", "FillPie", "DrawPie", "FillChord", "DrawChord",
            "DrawPoint", "FillCircle", "FillTriangle", "DrawTriangle",
            "FillRegularPolygon", "DrawRegularPolygon", "FillStar", "DrawStar");
    }

    [Fact]
    [Trait("PlanStage", "5")]
    public void ImageMeshBatchContractRequiresImmutableVersionedPayloadsAndDeterministicFractionalSampling()
    {
        Type imageOptions = RequireType("Cerneala.Drawing.DrawImageOptions");
        Type sampling = RequireType("Cerneala.Drawing.DrawSamplingMode");
        Type address = RequireType("Cerneala.Drawing.DrawAddressMode");
        Type insets = RequireType("Cerneala.Drawing.DrawInsets");
        Type vertex = RequireType("Cerneala.Drawing.DrawVertex2D");
        Type mesh = RequireType("Cerneala.Drawing.DrawMesh2D");
        Type topology = RequireType("Cerneala.Drawing.DrawPrimitiveTopology");
        Type pointBatch = RequireType("Cerneala.Drawing.DrawPointBatch");
        Type lineBatch = RequireType("Cerneala.Drawing.DrawLineBatch");
        Type spriteBatch = RequireType("Cerneala.Drawing.DrawSpriteBatch");

        RequireProperties(imageOptions, "Source", "Tint", "Opacity", "Rotation", "Origin", "Flip", "LayerDepth", "Sampling", "AddressMode");
        RequireEnumValues(sampling, "Point", "Linear");
        RequireEnumValues(address, "Clamp", "Wrap");
        RequireProperties(insets, "Left", "Top", "Right", "Bottom");
        RequireProperties(vertex, "Position", "Color", "TextureCoordinate");
        RequireProperties(mesh, "Vertices", "Indices", "Topology", "Image", "Version", "Bounds");
        RequireEnumValues(topology, "TriangleList", "TriangleStrip");
        RequireProperties(pointBatch, "Version", "Bounds");
        RequireProperties(lineBatch, "Version", "Bounds");
        RequireProperties(spriteBatch, "Version", "Bounds", "Image");
        RequireMethods(typeof(DrawingContext), "DrawImageQuad", "DrawNineSlice", "DrawMesh", "DrawTriangles", "DrawPointBatch", "DrawLineBatch", "DrawSpriteBatch");
    }

    [Fact]
    [Trait("PlanStage", "6")]
    public void TextContractRequiresClusterSafeWrappingTrimmingStyledRunsAndReusableBidiLayout()
    {
        Type span = RequireType("Cerneala.Drawing.DrawTextSpan");
        Type options = RequireType("Cerneala.Drawing.DrawTextLayoutOptions");
        Type wrapping = RequireType("Cerneala.Drawing.DrawTextWrapping");
        Type alignment = RequireType("Cerneala.Drawing.DrawTextAlignment");
        Type trimming = RequireType("Cerneala.Drawing.DrawTextTrimming");
        Type builder = RequireType("Cerneala.Drawing.DrawTextLayoutBuilder");
        Type layout = RequireType("Cerneala.Drawing.DrawTextLayout");

        RequireProperties(span, "Text", "Font", "Size", "Brush");
        RequireProperties(options, "MaxWidth", "MaxHeight", "Wrapping", "Alignment", "LineSpacing", "MaxLines", "Trimming", "Direction");
        RequireEnumValues(wrapping, "NoWrap", "Word", "Character");
        RequireEnumValues(alignment, "Start", "Center", "End", "Justify");
        RequireEnumValues(trimming, "None", "CharacterEllipsis", "WordEllipsis");
        RequireMethods(builder, "AddSpan", "Build");
        RequireProperties(layout, "Lines", "Bounds", "StableId");
        RequireMethods(typeof(DrawingContext), "DrawTextLayout");
    }

    [Fact]
    [Trait("PlanStage", "7")]
    public void IntegrationContractRequiresCentralRetainedIdentityDamageResourcesPrismAndDeviceResetMetadata()
    {
        Type metadata = RequireType("Cerneala.Drawing.DrawCommandMetadata");
        Type analyzer = RequireType("Cerneala.Drawing.DrawCommandStateAnalyzer");

        RequireProperties(metadata, "Bounds", "Resources", "IsContextSensitive", "RetainedIdentity");
        RequireMethods(analyzer, "Analyze");
        RequireMethods(typeof(RenderSurface2DFrame), "DrawImage", "DrawSprite");
    }

    private static Type RequireType(string fullName)
    {
        Type? type = DrawingAssembly.GetType(fullName, throwOnError: false);
        Assert.True(type is not null, $"Missing planned drawing capability type '{fullName}'.");
        return type!;
    }

    private static void RequireMethods(Type type, params string[] names)
    {
        MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
        foreach (string name in names)
        {
            Assert.Contains(methods, method => method.Name == name);
        }
    }

    private static void RequireProperties(Type type, params string[] names)
    {
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
        foreach (string name in names)
        {
            Assert.Contains(properties, property => property.Name == name);
        }
    }

    private static void RequireEnumValues(Type type, params string[] names)
    {
        Assert.True(type.IsEnum, $"Planned contract type '{type.FullName}' must be an enum.");
        string[] values = Enum.GetNames(type);
        foreach (string name in names)
        {
            Assert.Contains(name, values);
        }
    }
}
