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
    public async Task UnsavedValidMarkupCompilesThroughTheDynamicProject()
    {
        string documentPath = OpeningViewPath();
        string source = File.ReadAllText(documentPath) + Environment.NewLine;

        using PreviewCompiler compiler = new(prewarmBuildOutput: false);
        PreviewCompilation compilation = await compiler.CompileAsync(documentPath, source);

        Assert.Equal("Cerneala.Presentation.OpeningView", compilation.TargetTypeName);
        Assert.NotEmpty(compilation.AssemblyImage);
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
            Assert.True(
                response.Kind == PreviewResponseKind.Frame,
                response.Error);
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
            Assert.True(
                initial.Kind == PreviewResponseKind.Frame,
                initial.Error);

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
            Assert.True(
                initial.Kind == PreviewResponseKind.Frame,
                initial.Error);

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

    [Fact]
    public void TimbreMarkupEditsRequireCompilationInsteadOfTheAttributeFastPath()
    {
        const string resources = "<Button.Resources><TimbreClip Name=\"Tone\">@sound Tone { Source = \"tone.wav\"; }</TimbreClip></Button.Resources>";
        const string aspect = "<Button.Aspect>@timbre $Tone; @on Click { @play $self.timbre.Tone; }</Button.Aspect>";
        string Document(string opacity, string clip, string body) =>
            $"<Button Opacity=\"{opacity}\">{resources.Replace("Source = \"tone.wav\";", clip, StringComparison.Ordinal)}" +
            aspect.Replace("@play $self.timbre.Tone;", body, StringComparison.Ordinal) + "</Button>";
        string current = Document("0.5", "Source = \"tone.wav\";", "@play $self.timbre.Tone;");
        Button button = new() { Opacity = 0.5f };

        Assert.Equal(
            PreviewMarkupUpdateResult.RequiresCompilation,
            PreviewMarkupHotReload.TryApply(button, current, Document("0.5", "Source = \"tone.wav\";", "@stop $self.timbre.Tone;")));
        Assert.Equal(
            PreviewMarkupUpdateResult.RequiresCompilation,
            PreviewMarkupHotReload.TryApply(button, current, Document("0.5", "Source = \"tone.wav\"; Loop = true;", "@play $self.timbre.Tone;")));
        Assert.Equal(
            PreviewMarkupUpdateResult.RequiresCompilation,
            PreviewMarkupHotReload.TryApply(button, current, current.Replace("Name=\"Tone\"", "Name=\"Chime\"", StringComparison.Ordinal)));
        Assert.Equal(0.5f, button.Opacity);

        Assert.Equal(
            PreviewMarkupUpdateResult.Applied,
            PreviewMarkupHotReload.TryApply(button, current, Document("0.75", "Source = \"tone.wav\";", "@play $self.timbre.Tone;")));
        Assert.Equal(0.75f, button.Opacity);
    }

    [Fact]
    public void PreviewProtocolCarriesTheExplicitAudioPolicyAndItsVisibleState()
    {
        using MemoryStream requestStream = new();
        PreviewProtocol.WriteRequest(requestStream, new PreviewRequest
        {
            RequestId = 81,
            Kind = PreviewRequestKind.Render,
            DocumentPath = "View.crn",
            SourceText = "<Border />",
            Width = 320,
            Height = 180,
            AudioEnabled = true
        });
        requestStream.Position = 0;
        PreviewRequest request = Assert.IsType<PreviewRequest>(PreviewProtocol.ReadRequest(requestStream));
        Assert.True(request.AudioEnabled);
        Assert.False(new PreviewRequest().AudioEnabled);

        using MemoryStream responseStream = new();
        PreviewProtocol.WriteResponse(responseStream, new PreviewResponse
        {
            Kind = PreviewResponseKind.Frame,
            RequestId = 82,
            Image = new byte[64],
            Width = 4,
            Height = 4,
            Stride = 16,
            BlockedAudioRequests = 3
        });
        responseStream.Position = 0;
        PreviewResponse response = Assert.IsType<PreviewResponse>(PreviewProtocol.ReadResponse(responseStream));
        Assert.False(response.AudioEnabled);
        Assert.Equal(3, response.BlockedAudioRequests);

        Assert.Equal("audio off", PreviewAudioStatus.Describe(audioEnabled: false, blockedAudioRequests: 0));
        Assert.Equal("audio off (3 blocked)", PreviewAudioStatus.Describe(audioEnabled: false, blockedAudioRequests: 3));
        Assert.Equal("audio on", PreviewAudioStatus.Describe(audioEnabled: true, blockedAudioRequests: 0));
    }

    [Fact]
    public async Task UnsavedTimbreMarkupCompilesAndInvalidTimbreIsNotReportedAsSuccess()
    {
        string documentPath = BrandMarkPath();
        using PreviewCompiler compiler = new(prewarmBuildOutput: false);

        PreviewCompilation compilation = await compiler.CompileAsync(
            documentPath,
            WithPreviewTimbre(File.ReadAllText(documentPath), "C:/preview/tone.wav", "Volume = 0.5;"));
        Assert.NotEmpty(compilation.AssemblyImage);

        PreviewCompilationException exception = await Assert.ThrowsAsync<PreviewCompilationException>(() =>
            compiler.CompileAsync(
                documentPath,
                WithPreviewTimbre(File.ReadAllText(documentPath), "C:/preview/tone.wav", "Volume = 2;")));
        Assert.Contains("CERNEALAUI032", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Volume = 0.5;", "@when IsEnabled { @play $self.timbre.Tone; }")]
    [InlineData("Volume = 0.5; AutoPlay = true;", "")]
    public async Task PreviewAudioIsDisabledByDefaultAndARecompiledSessionRetiresTheOldScopes(string sound, string commands)
    {
        string root = Path.Combine(Path.GetTempPath(), "Cerneala", "PreviewTimbre", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            string tonePath = Path.Combine(root, "tone.wav").Replace('\\', '/');
            WriteSilentFloatWav(tonePath, frames: 4_800);
            string documentPath = BrandMarkPath();
            using PreviewCompiler compiler = new(prewarmBuildOutput: false);
            PreviewCompilation compilation = await compiler.CompileAsync(
                documentPath,
                WithPreviewTimbre(File.ReadAllText(documentPath), tonePath, sound, commands));

            (bool AudioEnabled, int FirstBlocked, bool FirstRetired, int FirstAfterRetire, int ReplacementBlocked) result =
                RunOnStaThread(() =>
                {
                    PreviewRenderSession first = PreviewRenderSession.Create(compilation, 320, 180);
                    int firstBlocked;
                    try
                    {
                        firstBlocked = PumpUntilBlocked(first);
                    }
                    finally
                    {
                        first.Dispose();
                    }

                    bool firstRetired = first.Timbre.IsDisposed;
                    int firstAfterRetire = first.BlockedAudioRequests;
                    PreviewRenderSession replacement = PreviewRenderSession.Create(compilation, 320, 180);
                    try
                    {
                        return (first.AudioEnabled, firstBlocked, firstRetired, firstAfterRetire, PumpUntilBlocked(replacement));
                    }
                    finally
                    {
                        replacement.Dispose();
                    }
                });

            Assert.False(result.AudioEnabled);
            Assert.True(result.FirstBlocked == 1, $"The initial sound (@play or AutoPlay) reached the disabled preview output {result.FirstBlocked} times instead of once.");
            Assert.True(result.FirstRetired);
            Assert.Equal(1, result.FirstAfterRetire);
            Assert.True(result.ReplacementBlocked == 1, $"The recompiled session activated {result.ReplacementBlocked} sounds instead of only its own initial one.");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task HostReportsDisabledAudioByDefaultAndRecreatesTheSessionWhenAudioIsEnabled()
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
            PreviewResponse disabled = await RenderAsync(process, 91, documentPath, source, audioEnabled: false);
            Assert.True(disabled.Kind == PreviewResponseKind.Frame, disabled.Error);
            Assert.False(disabled.AudioEnabled);

            PreviewResponse enabled = await RenderAsync(process, 92, documentPath, source, audioEnabled: true);
            Assert.True(enabled.Kind == PreviewResponseKind.Frame, enabled.Error);
            Assert.True(enabled.AudioEnabled);
            Assert.Equal(0, enabled.BlockedAudioRequests);
        }
        finally
        {
            if (!process.HasExited)
            {
                PreviewProtocol.WriteRequest(process.StandardInput.BaseStream, new PreviewRequest
                {
                    RequestId = 93,
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

    private static async Task<PreviewResponse> RenderAsync(
        Process process,
        int requestId,
        string documentPath,
        string source,
        bool audioEnabled)
    {
        PreviewProtocol.WriteRequest(process.StandardInput.BaseStream, new PreviewRequest
        {
            RequestId = requestId,
            Kind = PreviewRequestKind.Render,
            DocumentPath = documentPath,
            SourceText = source,
            Width = 320,
            Height = 180,
            AudioEnabled = audioEnabled
        });
        return await Task.Run(() => PreviewProtocol.ReadResponse(process.StandardOutput.BaseStream))
                .WaitAsync(TimeSpan.FromSeconds(60))
            ?? throw new EndOfStreamException("The preview host closed without a frame.");
    }

    private static int PumpUntilBlocked(PreviewRenderSession session)
    {
        Stopwatch elapsed = Stopwatch.StartNew();
        while (session.BlockedAudioRequests == 0 && elapsed.Elapsed < TimeSpan.FromSeconds(10))
        {
            session.Capture();
            Thread.Sleep(10);
        }

        // Keep pumping so a repeated or restored activation would be counted too.
        for (int frame = 0; frame < 20; frame++)
        {
            session.Capture();
            Thread.Sleep(10);
        }

        return session.BlockedAudioRequests;
    }

    // An initially true reactive rule (or AutoPlay) plays the sound as soon as the preview attaches.
    private static string WithPreviewTimbre(
        string brandMark,
        string sourcePath,
        string volume,
        string commands = "@when IsEnabled { @play $self.timbre.Tone; }")
    {
        string sound =
            "<UserControl>" +
            "<UserControl.Resources><TimbreClip Name=\"PreviewTone\">@sound Tone { Source = \"" + sourcePath + "\"; " + volume + " }</TimbreClip></UserControl.Resources>" +
            "<UserControl.Aspect>@timbre $PreviewTone; " + commands + "</UserControl.Aspect>";
        Assert.StartsWith("<UserControl>", brandMark, StringComparison.Ordinal);
        return sound + brandMark["<UserControl>".Length..];
    }

    private static void WriteSilentFloatWav(string path, int frames)
    {
        const int sampleRate = 48_000;
        int dataBytes = frames * 2 * sizeof(float);
        using BinaryWriter writer = new(File.Create(path));
        writer.Write("RIFF"u8);
        writer.Write(36 + dataBytes);
        writer.Write("WAVE"u8);
        writer.Write("fmt "u8);
        writer.Write(16);
        writer.Write((short)3);
        writer.Write((short)2);
        writer.Write(sampleRate);
        writer.Write(sampleRate * 8);
        writer.Write((short)8);
        writer.Write((short)32);
        writer.Write("data"u8);
        writer.Write(dataBytes);
        writer.Write(new byte[dataBytes]);
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
