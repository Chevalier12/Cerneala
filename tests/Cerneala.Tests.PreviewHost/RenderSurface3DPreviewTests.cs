namespace Cerneala.Tests.PreviewHost;

using Cerneala.PreviewHost;

[Collection(PreviewHostCollection.Name)]
public sealed class RenderSurface3DPreviewTests
{
    [Fact]
    public async Task PreviewInstantiatesPairedThreeDimensionalSurfaceWithSampleGeometry()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Cerneala.slnx")))
            directory = directory.Parent;
        string root = directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
        string document = Path.Combine(root, "tests", "Cerneala.SdlGpuSmoke", "RenderSurface3DPreview.crn");
        using PreviewCompiler compiler = new(prewarmBuildOutput: false);
        PreviewCompilation compilation = await compiler.CompileAsync(document, File.ReadAllText(document));
        Assert.Equal("Cerneala.SdlGpuSmoke.RenderSurface3DPreview", compilation.TargetTypeName);
        Assert.NotEmpty(compilation.AssemblyImage);

        Exception? error = null;
        int markerPixels = 0;
        Thread thread = new(() =>
        {
            try
            {
                using PreviewRenderSession session = PreviewRenderSession.Create(compilation, 320, 240);
                var (image, width, height, stride, _) = session.Capture();
                Assert.Equal(stride * height, image.Length);
                Assert.True(width > 0);
                for (int index = 0; index < image.Length; index += 4)
                    if (image[index] > 170 && image[index + 1] > 150 && image[index + 2] < 120)
                        markerPixels++;
            }
            catch (Exception exception) { error = exception; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(error);
        Assert.True(markerPixels > 0, "The preview did not rasterize its cyan 3D sample marker.");
    }
}
