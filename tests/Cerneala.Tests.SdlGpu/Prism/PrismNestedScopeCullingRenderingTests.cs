using System.Numerics;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism;
using Cerneala.Drawing.Prism.Catalog;
using Cerneala.Tests.Drawing.Prism;
using Cerneala.Tests.Drawing.SdlGpu;

namespace Cerneala.Tests.SdlGpu.Prism;

[Collection(SdlNativeTestCollection.Name)]
public sealed class PrismNestedScopeCullingRenderingTests
{
    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void ClippedNestedInputMatchesOmittedSubtreeAndRestoresPixelsOnReentry()
    {
        using SdlDrawingFixture fixture = new(64, 64);
        DrawRect content = new(0, 0, 16, 16);
        DrawRect siblingBounds = new(40, 8, 16, 16);
        PrismDrawScope parent = PrismTestData.Scope(
            PrismTestData.Composition("Parent", PrismTestData.Layer(1, "Blur")),
            ownerToken: 1, bounds: content);
        PrismDrawScope child = Identity(2, content) with { InputBounds = content };
        PrismDrawScope sibling = Identity(3, siblingBounds);
        Color[] omitted = fixture.Render(Commands(null));
        Color[] visible = fixture.Render(Commands(8));
        Assert.NotEqual(fixture.Sample(omitted, 16, 16), fixture.Sample(visible, 16, 16));
        Assert.Equal(Color.White, fixture.Sample(omitted, 48, 16));

        for (int cycle = 0; cycle < 8; cycle++)
        {
            Assert.Equal(omitted, fixture.Render(Commands(-22)));
            Assert.Equal(visible, fixture.Render(Commands(8)));
        }

        DrawCommandList Commands(float? x)
        {
            DrawCommandList commands = new();
            commands.Add(DrawCommand.PushClip(new(0, 0, 64, 64)));
            if (x is float offset)
            {
                commands.Add(DrawCommand.PushTransform(Matrix3x2.CreateTranslation(offset, 8)));
                commands.Add(DrawCommand.BeginPrism(parent));
                commands.Add(DrawCommand.BeginPrism(child));
                commands.Add(DrawCommand.FillRectangle(content, Color.White));
                commands.Add(DrawCommand.EndPrism());
                commands.Add(DrawCommand.EndPrism());
                commands.Add(DrawCommand.PopTransform());
            }
            commands.Add(DrawCommand.BeginPrism(sibling));
            commands.Add(DrawCommand.FillRectangle(siblingBounds, Color.White));
            commands.Add(DrawCommand.EndPrism());
            commands.Add(DrawCommand.PopClip());
            return commands;
        }
    }

    [SdlNativeFact]
    [Trait("Category", "Native")]
    public void RequiredOffscreenChildStillContributesVisibleBlurPixels()
    {
        using SdlDrawingFixture fixture = new(64, 64);
        DrawRect source = new(-8, 8, 8, 16);
        // Logical content bounds include the source; the viewport is only an output clip.
        PrismDrawScope parent = PrismTestData.Scope(
            PrismTestData.Composition("Visible blur", PrismTestData.Layer(1, "Blur")),
            ownerToken: 1, bounds: new(-16, 0, 48, 32)) with { InputBounds = new(-16, 0, 48, 32) };
        PrismDrawScope child = Identity(2, source) with { InputBounds = source };
        // The source ends at x=0; the blur must reach the visible sample at x=1.
        parent.Instance.GetLayerState(new(1)).Filters[0].SetValue(
            PrismCatalogGenerated.PrismFilterParameterKeys.Blur.RadiusKey, 4f);

        Color[] flattened = fixture.Render(Commands(nested: false, includeSource: true));
        Color[] nested = fixture.Render(Commands(nested: true, includeSource: true));
        Color[] missingInput = fixture.Render(Commands(nested: false, includeSource: false));
        Assert.Equal(flattened, nested);
        Assert.NotEqual(fixture.Sample(missingInput, 1, 16), fixture.Sample(nested, 1, 16));

        DrawCommandList Commands(bool nested, bool includeSource)
        {
            DrawCommandList commands = new();
            commands.Add(DrawCommand.PushClip(new(0, 0, 64, 64)));
            PrismDrawScope frameParent = new(parent.Instance, parent.CacheOwnerToken,
                parent.ControlBounds, parent.EffectiveTransform, parent.PixelScale,
                visualContentVersion: includeSource ? 1 : 2) { InputBounds = parent.InputBounds };
            commands.Add(DrawCommand.BeginPrism(frameParent));
            if (nested) commands.Add(DrawCommand.BeginPrism(child));
            if (includeSource) commands.Add(DrawCommand.FillRectangle(source, Color.White));
            if (nested) commands.Add(DrawCommand.EndPrism());
            commands.Add(DrawCommand.EndPrism());
            commands.Add(DrawCommand.PopClip());
            return commands;
        }
    }

    private static PrismDrawScope Identity(long owner, DrawRect bounds) => PrismTestData.Scope(
        PrismTestData.Composition("Identity", PrismTestData.Layer(1, "Content", visible: false)),
        ownerToken: owner, bounds: bounds);
}
