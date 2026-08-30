using Cerneala.Backends.SdlGpu;
using Cerneala.Drawing;
using Cerneala.Drawing.Prism.Graph;
using Cerneala.Platforms.Sdl3;
using Cerneala.UI.Controls;
using Cerneala.UI.Elements;

namespace Cerneala.Tests.SdlGpu;

public sealed class ColorPickerSdlGpuTests
{
    [Fact]
    public void DefaultColorPickerRendersWithoutUnsupportedBrushDescriptors()
    {
        FakeSdlApi api = new() { WindowPixelDensity = 1 };
        nint window = api.CreateWindow("color-picker", 260, 240, SdlWindowOptions.Hidden);
        using SdlGpuWindowGraphicsSessionFactory factory = new(api, useMultisampling: false);
        using SdlGpuWindowGraphicsSession session = Assert.IsType<SdlGpuWindowGraphicsSession>(factory.Create(
            new SdlWindowSurface(window, api.GetWindowId(window)),
            260,
            240,
            coordinateScale: 1));
        UIRoot root = new(260, 240);
        root.VisualChildren.Add(new ColorPicker());
        root.ProcessFrame();
        root.ProcessFrame();
        DrawCommandList commands = root.RetainedRenderer.Commit(root);
        DrawingFrameContext frame = new(new PrismFrameAnalyzer().Analyze(commands));

        Exception? failure = Record.Exception(() =>
        {
            session.BeginFrame(Color.Transparent);
            try
            {
                session.DrawingBackend.Render(commands, in frame);
            }
            finally
            {
                session.CompleteFrame(present: false);
            }
        });

        Assert.Null(failure);
    }
}
