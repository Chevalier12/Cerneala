namespace Cerneala.Tests.PreviewHost;

using System.Diagnostics;
using System.Reflection;
using Cerneala.Preview;
using Cerneala.PreviewHost;
using Cerneala.UI.Controls;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class PreviewHostCollection
{
    public const string Name = "PreviewHost";
}

[Collection(PreviewHostCollection.Name)]
public sealed class PreviewHostTests
{
    [Fact]
    public async Task UnchangedSavedMarkupReusesTheCurrentBuildOutput()
    {
        string root = Path.Combine(Path.GetTempPath(), "Cerneala", "PreviewCompilerTests", Guid.NewGuid().ToString("N"));
        string outputDirectory = Path.Combine(root, "bin", "Debug", "net10.0");
        Directory.CreateDirectory(outputDirectory);
        try
        {
            string projectPath = Path.Combine(root, "Sample.csproj");
            string documentPath = Path.Combine(root, "View.crn");
            await File.WriteAllTextAsync(projectPath, """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                  </PropertyGroup>
                  <ItemGroup>
                    <AdditionalFiles Include="*.crn" />
                  </ItemGroup>
                </Project>
                """);
            await File.WriteAllTextAsync(documentPath, "<UserControl />");

            string expectedAssembly = typeof(PreviewHostTests).Assembly.Location;
            string builtAssembly = Path.Combine(outputDirectory, "Sample.dll");
            File.Copy(expectedAssembly, builtAssembly);
            File.SetLastWriteTimeUtc(builtAssembly, DateTime.UtcNow.AddSeconds(1));

            using PreviewCompiler compiler = new();
            PreviewCompilation compilation = await compiler.CompileAsync(
                documentPath,
                await File.ReadAllTextAsync(documentPath));

            Assert.Equal(typeof(PreviewHostTests).Assembly.GetName().Name, compilation.AssemblyName);
            Assert.Equal(await File.ReadAllBytesAsync(builtAssembly), compilation.AssemblyImage);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task UnsavedMarkupOverridesTheAdditionalFileOnDisk()
    {
        string documentPath = OpeningViewPath();
        string valid = File.ReadAllText(documentPath);
        string invalid = valid.Replace(
            "@run $LoadingSequence as Loading;",
            "@run $LoadingSequence as Loading",
            StringComparison.Ordinal);
        Assert.NotEqual(valid, invalid);

        using PreviewCompiler compiler = new(prewarmBuildOutput: false);
        PreviewCompilationException exception = await Assert.ThrowsAsync<PreviewCompilationException>(
            () => compiler.CompileAsync(documentPath, invalid));

        Assert.Contains("OpeningView", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OpeningViewCapturesThePresentedBgraFrameWithoutPngEncoding()
    {
        string documentPath = OpeningViewPath();
        using PreviewCompiler compiler = new(prewarmBuildOutput: false);
        PreviewCompilation compilation = await compiler.CompileAsync(
            documentPath,
            File.ReadAllText(documentPath));

        const int width = 320;
        const int height = 180;
        (byte[] Image, int Width, int Height, int Stride, TimeSpan RenderTime) frame = RunOnStaThread(() =>
        {
            using PreviewRenderSession session = PreviewRenderSession.Create(compilation, width, height);
            return session.Capture();
        });

        Assert.Equal(frame.Width * 4, frame.Stride);
        Assert.Equal(frame.Stride * frame.Height, frame.Image.Length);
        Assert.NotEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, frame.Image.Take(8));
    }

    [Fact]
    public async Task PreviewRenderScaleIsIndependentOfDesktopDpi()
    {
        string documentPath = OpeningViewPath();
        using PreviewCompiler compiler = new(prewarmBuildOutput: false);
        PreviewCompilation compilation = await compiler.CompileAsync(
            documentPath,
            File.ReadAllText(documentPath));

        const int width = 640;
        const int height = 360;
        (byte[] Image, int Width, int Height, int Stride, TimeSpan RenderTime) frame = RunOnStaThread(() =>
        {
            using PreviewRenderSession session = PreviewRenderSession.Create(compilation, width, height);
            return session.Capture();
        });

        Assert.Equal((int)MathF.Ceiling(width * PreviewRenderSession.RenderScale), frame.Width);
        Assert.Equal((int)MathF.Ceiling(height * PreviewRenderSession.RenderScale), frame.Height);
    }

    [Fact]
    public async Task PreviewRuntimeCanBeRecreatedAfterARecompile()
    {
        string documentPath = OpeningViewPath();
        using PreviewCompiler compiler = new(prewarmBuildOutput: false);
        PreviewCompilation compilation = await compiler.CompileAsync(
            documentPath,
            File.ReadAllText(documentPath));

        int[] frameSizes = RunOnStaThread(() => Enumerable.Range(0, 2)
            .Select(_ =>
            {
                using PreviewRenderSession session = PreviewRenderSession.Create(compilation, 640, 360);
                return session.Capture().Image.Length;
            })
            .ToArray());

        Assert.All(frameSizes, size => Assert.True(size > 4_096));
    }

    [Fact]
    public async Task HostProcessRendersThroughTheBinaryProtocol()
    {
        string executable = Path.Combine(AppContext.BaseDirectory, "Cerneala.PreviewHost.exe");
        Assert.True(File.Exists(executable), $"Preview host executable '{executable}' is missing.");
        using Process process = new()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = executable,
                WorkingDirectory = AppContext.BaseDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true
            }
        };
        Assert.True(process.Start());

        try
        {
            string documentPath = OpeningViewPath();
            PreviewProtocol.WriteRequest(process.StandardInput.BaseStream, new PreviewRequest
            {
                RequestId = 17,
                Kind = PreviewRequestKind.Render,
                DocumentPath = documentPath,
                SourceText = File.ReadAllText(documentPath),
                Width = 640,
                Height = 360
            });
            PreviewResponse response = await Task.Run(() =>
                    PreviewProtocol.ReadResponse(process.StandardOutput.BaseStream))
                .WaitAsync(TimeSpan.FromSeconds(60))
                ?? throw new EndOfStreamException("The preview host closed without a response.");

            Assert.Equal(17, response.RequestId);
            Assert.Equal(PreviewResponseKind.Frame, response.Kind);
            Assert.True(response.Image.Length > 4_096);

            PreviewProtocol.WriteRequest(process.StandardInput.BaseStream, new PreviewRequest
            {
                RequestId = 18,
                Kind = PreviewRequestKind.PointerMove,
                X = 80,
                Y = 40
            });
            PreviewResponse inputResponse = await Task.Run(() =>
                    PreviewProtocol.ReadResponse(process.StandardOutput.BaseStream))
                .WaitAsync(TimeSpan.FromSeconds(5))
                ?? throw new EndOfStreamException("The preview host did not acknowledge pointer input.");

            Assert.Equal(18, inputResponse.RequestId);
            Assert.Equal(PreviewResponseKind.Acknowledged, inputResponse.Kind);
        }
        finally
        {
            if (!process.HasExited)
            {
                PreviewProtocol.WriteRequest(process.StandardInput.BaseStream, new PreviewRequest
                {
                    RequestId = 19,
                    Kind = PreviewRequestKind.Shutdown
                });
                process.StandardInput.Close();
                if (!process.WaitForExit(2_000))
                {
                    process.Kill();
                }
            }
        }
    }

    [Fact]
    public async Task LiteralPropertyEditUpdatesTheLiveTreeWithoutRecompiling()
    {
        string executable = Path.Combine(AppContext.BaseDirectory, "Cerneala.PreviewHost.exe");
        Assert.True(File.Exists(executable), $"Preview host executable '{executable}' is missing.");
        using Process process = new()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = executable,
                WorkingDirectory = AppContext.BaseDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true
            }
        };
        Assert.True(process.Start());

        try
        {
            string documentPath = OpeningViewPath();
            string source = File.ReadAllText(documentPath);
            PreviewProtocol.WriteRequest(process.StandardInput.BaseStream, new PreviewRequest
            {
                RequestId = 51,
                Kind = PreviewRequestKind.Render,
                DocumentPath = documentPath,
                SourceText = source,
                Width = 320,
                Height = 180
            });
            PreviewResponse initial = await Task.Run(() =>
                    PreviewProtocol.ReadResponse(process.StandardOutput.BaseStream))
                .WaitAsync(TimeSpan.FromSeconds(60))
                ?? throw new EndOfStreamException("The preview host closed without the initial frame.");
            Assert.Equal(PreviewResponseKind.Frame, initial.Kind);

            string edited = source.Replace(
                "Background=\"#FF080A0D\"",
                "Background=\"#FF203040\"",
                StringComparison.Ordinal);
            Assert.NotEqual(source, edited);
            PreviewProtocol.WriteRequest(process.StandardInput.BaseStream, new PreviewRequest
            {
                RequestId = 52,
                Kind = PreviewRequestKind.Render,
                DocumentPath = documentPath,
                SourceText = edited,
                Width = 320,
                Height = 180
            });
            PreviewResponse updated = await Task.Run(() =>
                    PreviewProtocol.ReadResponse(process.StandardOutput.BaseStream))
                .WaitAsync(TimeSpan.FromSeconds(10))
                ?? throw new EndOfStreamException("The preview host closed without the updated frame.");

            Assert.Equal(PreviewResponseKind.Frame, updated.Kind);
            Assert.Equal(0, updated.CompileMilliseconds);
            Assert.True(updated.Image.Length > 4_096);

            string nestedEdit = edited.Replace(
                "Text=\"Ready to step inside the frame?\"",
                "Text=\"Ready for hot reload.\"",
                StringComparison.Ordinal);
            Assert.NotEqual(edited, nestedEdit);
            PreviewProtocol.WriteRequest(process.StandardInput.BaseStream, new PreviewRequest
            {
                RequestId = 53,
                Kind = PreviewRequestKind.Render,
                DocumentPath = documentPath,
                SourceText = nestedEdit,
                Width = 320,
                Height = 180
            });
            PreviewResponse nestedUpdate = await Task.Run(() =>
                    PreviewProtocol.ReadResponse(process.StandardOutput.BaseStream))
                .WaitAsync(TimeSpan.FromSeconds(10))
                ?? throw new EndOfStreamException("The preview host closed without the nested-property frame.");

            Assert.Equal(PreviewResponseKind.Frame, nestedUpdate.Kind);
            Assert.Equal(0, nestedUpdate.CompileMilliseconds);
        }
        finally
        {
            if (!process.HasExited)
            {
                PreviewProtocol.WriteRequest(process.StandardInput.BaseStream, new PreviewRequest
                {
                    RequestId = 54,
                    Kind = PreviewRequestKind.Shutdown
                });
                process.StandardInput.Close();
                if (!process.WaitForExit(2_000))
                {
                    process.Kill();
                }
            }
        }
    }

    [Fact]
    public async Task CustomControlTextEditUpdatesTheLiveTreeWithoutRecompiling()
    {
        string executable = Path.Combine(AppContext.BaseDirectory, "Cerneala.PreviewHost.exe");
        Assert.True(File.Exists(executable), $"Preview host executable '{executable}' is missing.");
        using Process process = new()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = executable,
                WorkingDirectory = AppContext.BaseDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true
            }
        };
        Assert.True(process.Start());

        try
        {
            string documentPath = BrandMarkPath();
            string source = File.ReadAllText(documentPath);
            PreviewProtocol.WriteRequest(process.StandardInput.BaseStream, new PreviewRequest
            {
                RequestId = 61,
                Kind = PreviewRequestKind.Render,
                DocumentPath = documentPath,
                SourceText = source,
                Width = 320,
                Height = 180
            });
            PreviewResponse initial = await Task.Run(() =>
                    PreviewProtocol.ReadResponse(process.StandardOutput.BaseStream))
                .WaitAsync(TimeSpan.FromSeconds(60))
                ?? throw new EndOfStreamException("The preview host closed without the initial BrandMark frame.");
            Assert.Equal(PreviewResponseKind.Frame, initial.Kind);

            string edited = source.Replace(
                "Text=\"CERNEALA\"",
                "Text=\"CERNEALA1\"",
                StringComparison.Ordinal);
            Assert.NotEqual(source, edited);
            PreviewProtocol.WriteRequest(process.StandardInput.BaseStream, new PreviewRequest
            {
                RequestId = 62,
                Kind = PreviewRequestKind.Render,
                DocumentPath = documentPath,
                SourceText = edited,
                Width = 320,
                Height = 180
            });
            PreviewResponse updated = await Task.Run(() =>
                    PreviewProtocol.ReadResponse(process.StandardOutput.BaseStream))
                .WaitAsync(TimeSpan.FromSeconds(10))
                ?? throw new EndOfStreamException("The preview host closed without the updated BrandMark frame.");

            Assert.Equal(PreviewResponseKind.Frame, updated.Kind);
            Assert.Equal(0, updated.CompileMilliseconds);
            Assert.True(updated.Image.Length > 4_096);
        }
        finally
        {
            if (!process.HasExited)
            {
                PreviewProtocol.WriteRequest(process.StandardInput.BaseStream, new PreviewRequest
                {
                    RequestId = 63,
                    Kind = PreviewRequestKind.Shutdown
                });
                process.StandardInput.Close();
                if (!process.WaitForExit(2_000))
                {
                    process.Kill();
                }
            }
        }
    }

    [Fact]
    public void IncompleteLiteralIsDeferredWithoutMutatingTheLiveTree()
    {
        Border border = new() { Opacity = 0.5f };

        PreviewMarkupUpdateResult result = PreviewMarkupHotReload.TryApply(
            border,
            "<Border Opacity=\"0.5\" />",
            "<Border Opacity=\"-\" />");

        Assert.Equal(PreviewMarkupUpdateResult.DeferredInvalidEdit, result);
        Assert.Equal(0.5f, border.Opacity);

        result = PreviewMarkupHotReload.TryApply(
            border,
            "<Border Opacity=\"0.5\" />",
            "<Border Opacity=\"0.75\" />");

        Assert.Equal(PreviewMarkupUpdateResult.Applied, result);
        Assert.Equal(0.75f, border.Opacity);
    }

    [Fact]
    public void StructuralMarkupEditStillRequiresCompilation()
    {
        Border border = new();

        PreviewMarkupUpdateResult result = PreviewMarkupHotReload.TryApply(
            border,
            "<Border />",
            "<Border><TextBlock /></Border>");

        Assert.Equal(PreviewMarkupUpdateResult.RequiresCompilation, result);
    }

    [Fact]
    public void InteractiveInputRoundTripsThroughThePreviewProtocol()
    {
        PreviewRequest expected = new()
        {
            RequestId = 41,
            Kind = PreviewRequestKind.PointerButton,
            X = 123.5,
            Y = 67.25,
            Button = "Left",
            IsDown = true
        };
        using MemoryStream stream = new();

        PreviewProtocol.WriteRequest(stream, expected);
        stream.Position = 0;
        PreviewRequest actual = Assert.IsType<PreviewRequest>(PreviewProtocol.ReadRequest(stream));

        Assert.Equal(expected.RequestId, actual.RequestId);
        Assert.Equal(expected.Kind, actual.Kind);
        Assert.Equal(expected.X, actual.X);
        Assert.Equal(expected.Y, actual.Y);
        Assert.Equal(expected.Button, actual.Button);
        Assert.Equal(expected.IsDown, actual.IsDown);
    }

    [Fact]
    public void FrameResponsesReuseTheCallersImageBuffer()
    {
        MethodInfo? reusableRead = typeof(PreviewProtocol).GetMethod(
            nameof(PreviewProtocol.ReadResponse),
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(Stream), typeof(byte[]) },
            modifiers: null);

        Assert.NotNull(reusableRead);
        byte[] pixels = Enumerable.Range(0, 64).Select(index => (byte)index).ToArray();
        using MemoryStream stream = new();
        PreviewProtocol.WriteResponse(stream, new PreviewResponse
        {
            Kind = PreviewResponseKind.Frame,
            RequestId = 73,
            Image = pixels,
            Width = 4,
            Height = 4,
            Stride = 16
        });
        stream.Position = 0;
        byte[] reusable = new byte[pixels.Length];

        PreviewResponse response = Assert.IsType<PreviewResponse>(reusableRead!.Invoke(
            obj: null,
            parameters: new object?[] { stream, reusable }));

        Assert.Same(reusable, response.Image);
        Assert.Equal(pixels, response.Image);
    }

    private static T RunOnStaThread<T>(Func<T> action)
    {
        T? result = default;
        Exception? failure = null;
        Thread thread = new(() =>
        {
            try
            {
                result = action();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (failure is not null)
        {
            throw failure;
        }

        return result!;
    }

    private static string OpeningViewPath() => Path.Combine(
        RepositoryRoot(),
        "CernealaPresentation",
        "OpeningView.crn");

    private static string BrandMarkPath() => Path.Combine(
        RepositoryRoot(),
        "CernealaPresentation",
        "BrandMark.crn");

    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Cerneala.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new DirectoryNotFoundException("Could not locate the Cerneala repository root.");
    }
}
