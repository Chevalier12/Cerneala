namespace Cerneala.Tests.PreviewHost;

using System.Diagnostics;
using System.Reflection;
using Cerneala.Preview;
using Cerneala.PreviewHost;
using Cerneala.UI.Controls;
using Cerneala.UI.Data;
using Cerneala.UI.Input;
using Cerneala.UI.Markup;

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
    public async Task OpeningViewCapturesThePresentedRgbaFrameWithoutPngEncoding()
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

        // Most of the view is PanelAltBrush #FF0D1014, sent as R, G, B, A.
        string mostCommonPixel = Enumerable.Range(0, frame.Image.Length / 4)
            .GroupBy(pixel => string.Join(",", frame.Image.Skip(pixel * 4).Take(4)))
            .OrderByDescending(group => group.Count())
            .First()
            .Key;
        Assert.Equal("13,16,20,255", mostCommonPixel);
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
    public async Task HostProcessAnswersTextAndKeyInputAndKeepsRendering()
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
            PreviewResponse opened = await RenderAsync(process, 31, documentPath, source, audioEnabled: false);
            Assert.True(opened.Kind == PreviewResponseKind.Frame, opened.Error);

            PreviewProtocol.WriteRequest(process.StandardInput.BaseStream, new PreviewRequest
            {
                RequestId = 32,
                Kind = PreviewRequestKind.Text,
                Text = "a"
            });
            PreviewResponse textResponse = await Task.Run(() =>
                    PreviewProtocol.ReadResponse(process.StandardOutput.BaseStream))
                .WaitAsync(TimeSpan.FromSeconds(5))
                ?? throw new EndOfStreamException("The preview host did not answer text input.");
            Assert.Equal(32, textResponse.RequestId);
            Assert.True(textResponse.Kind == PreviewResponseKind.Frame, textResponse.Error);

            PreviewProtocol.WriteRequest(process.StandardInput.BaseStream, new PreviewRequest
            {
                RequestId = 33,
                Kind = PreviewRequestKind.Key,
                Key = nameof(InputKey.Back)
            });
            PreviewResponse keyResponse = await Task.Run(() =>
                    PreviewProtocol.ReadResponse(process.StandardOutput.BaseStream))
                .WaitAsync(TimeSpan.FromSeconds(5))
                ?? throw new EndOfStreamException("The preview host did not answer key input.");
            Assert.Equal(33, keyResponse.RequestId);
            Assert.True(keyResponse.Kind == PreviewResponseKind.Frame, keyResponse.Error);

            PreviewResponse rendered = await RenderAsync(process, 34, documentPath, source, audioEnabled: false);
            Assert.Equal(34, rendered.RequestId);
            Assert.True(rendered.Kind == PreviewResponseKind.Frame, rendered.Error);
        }
        finally
        {
            if (!process.HasExited)
            {
                PreviewProtocol.WriteRequest(process.StandardInput.BaseStream, new PreviewRequest
                {
                    RequestId = 35,
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

    // A string attribute follows the build's markup rules: "\$" is consumed, a standalone
    // '$' stays literal, and a value that interpolates a path is not a literal edit.
    [Theory]
    [InlineData("Hello", "Hello", "World", "Applied", "World")]
    [InlineData("Cost $5", "Cost $5", "Cost $6", "Applied", "Cost $6")]
    [InlineData("Cost $5", "Cost \\$5", "Cost \\$6", "Applied", "Cost $6")]
    [InlineData("Literal: $DataContext.Name", "Literal: \\$DataContext.Name","Literal: \\$DataContext.Title", "Applied", "Literal: $DataContext.Title")]
    [InlineData("User: Ana", "User: $DataContext.Name", "Usr: $DataContext.Name", "RequiresCompilation", "User: Ana")]
    [InlineData("Hello", "Hello", "Hello $DataContext.Name", "RequiresCompilation", "Hello")]
    [InlineData("Hello", "Hello", "Hello $Box.Text", "RequiresCompilation", "Hello")]
    [InlineData("Price in EUR", "Price in EUR", "Price in $USD", "Applied", "Price in $USD")]
    [InlineData("Hello", "Hello", "Hello $self", "Applied", "Hello $self")]
    public void StringAttributeEditUsesTheMarkupLiteralEscapeAndInterpolationRules(
        string liveText,
        string oldAttribute,
        string newAttribute,
        string expectedResult,
        string expectedText)
    {
        TextBlock text = new() { Text = liveText };

        PreviewMarkupUpdateResult result = PreviewMarkupHotReload.TryApply(
            text,
            $"<TextBlock Text=\"{oldAttribute}\" />",
            $"<TextBlock Text=\"{newAttribute}\" />");

        Assert.Equal(Enum.Parse<PreviewMarkupUpdateResult>(expectedResult), result);
        Assert.Equal(expectedText, text.Text);
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

    // A value that binds owns a binding in the live tree. Setting the new literal in place would
    // leave that binding attached, and a TwoWay binding would write the literal into its source,
    // while a build of the edited markup has no binding at all; so the edit is compiled.
    [Theory]
    [InlineData("$Source.Text:TwoWay", "Bob", "TwoWay")]
    [InlineData("$Source.Text", "Bob", "OneWay")]
    [InlineData("User: $Source.Text", "User", null)]
    public void EditFromABindingToALiteralIsCompiledAndLeavesTheBindingSourceAlone(
        string boundAttribute,
        string literalAttribute,
        string? directMode)
    {
        TextBlock source = new() { Text = "Ana" };
        TextBox box = new();
        MarkupObservation observation = GeneratedMarkup.ObserveProperty(source, TextBlock.TextProperty);
        if (directMode is null)
        {
            GeneratedMarkup.AttachComputedBinding(
                box, box, TextBox.TextProperty, [observation], () => "User: " + source.Text, "Box.Text <- " + boundAttribute);
        }
        else
        {
            GeneratedMarkup.AttachPropertyBinding(
                box, box, TextBox.TextProperty, observation, Enum.Parse<BindingMode>(directMode), value => (string)value!, "Box.Text <- " + boundAttribute);
        }

        string boundText = box.Text;
        PreviewMarkupUpdateResult result = PreviewMarkupHotReload.TryApply(
            box,
            $"<TextBox Text=\"{boundAttribute}\" />",
            $"<TextBox Text=\"{literalAttribute}\" />");

        Assert.Equal("Ana", source.Text);
        Assert.Equal(PreviewMarkupUpdateResult.RequiresCompilation, result);
        Assert.Equal(boundText, box.Text);
    }

    [Fact]
    public void RenderRequestCarriesTheRecompileFlag()
    {
        using MemoryStream stream = new();
        PreviewProtocol.WriteRequest(stream, new PreviewRequest
        {
            RequestId = 85,
            Kind = PreviewRequestKind.Render,
            DocumentPath = "View.crn",
            SourceText = "<Border />",
            Width = 320,
            Height = 180,
            Recompile = true
        });
        stream.Position = 0;

        PreviewRequest request = Assert.IsType<PreviewRequest>(PreviewProtocol.ReadRequest(stream));

        Assert.True(request.Recompile);
        Assert.Equal(320, request.Width);
        Assert.False(new PreviewRequest().Recompile);
    }

    // Refresh ("Recompile and refresh Live Preview") sends the unchanged markup with Recompile set:
    // it compiles again, so a saved code-behind edit is shown. A plain Render of the same text
    // (an edit that ends where it started) keeps the live tree.
    [Fact]
    public async Task RepeatedRenderOfUnchangedMarkupPicksUpASavedCodeBehindEdit()
    {
        using CodeBehindFixture fixture = CodeBehindFixture.Create();
        using Process process = StartHostProcess();
        try
        {
            string markup = File.ReadAllText(fixture.DocumentPath);
            PreviewResponse initial = await RenderAsync(process, 71, fixture.DocumentPath, markup, audioEnabled: false);
            Assert.True(initial.Kind == PreviewResponseKind.Frame, initial.Error);
            Assert.Equal("10,200,30,255", MostCommonPixel(initial.Image));

            fixture.WriteCodeBehind(red: 200, green: 20, blue: 40);
            PreviewResponse typed = await RenderAsync(process, 72, fixture.DocumentPath, markup, audioEnabled: false);
            Assert.True(typed.Kind == PreviewResponseKind.Frame, typed.Error);
            Assert.Equal(0, typed.CompileMilliseconds);

            PreviewResponse refreshed = await RenderAsync(process, 73, fixture.DocumentPath, markup, audioEnabled: false, recompile: true);
            Assert.True(refreshed.Kind == PreviewResponseKind.Frame, refreshed.Error);
            Assert.True(MostCommonPixel(refreshed.Image) == "200,20,40,255",
                "Refresh re-rendered with the saved code-behind changed on disk, but the frame still shows " +
                MostCommonPixel(refreshed.Image) + " (CompileMilliseconds " + refreshed.CompileMilliseconds + ").");
            Assert.True(refreshed.CompileMilliseconds > 0, "Refresh did not recompile.");

            fixture.WriteCodeBehind(red: 30, green: 60, blue: 220);
            PreviewResponse refreshedAgain = await RenderAsync(process, 74, fixture.DocumentPath, markup, audioEnabled: false, recompile: true);
            Assert.True(refreshedAgain.Kind == PreviewResponseKind.Frame, refreshedAgain.Error);
            Assert.True(refreshedAgain.CompileMilliseconds > 0, "The second Refresh did not recompile.");
            Assert.Equal("30,60,220,255", MostCommonPixel(refreshedAgain.Image));
        }
        finally
        {
            StopHostProcess(process);
        }
    }

    [Fact]
    public async Task StructuralMarkupEditRecompilesAndPicksUpASavedCodeBehindEdit()
    {
        // Control: when the markup itself needs compilation the same code-behind edit is visible.
        using CodeBehindFixture fixture = CodeBehindFixture.Create();
        using Process process = StartHostProcess();
        try
        {
            string markup = File.ReadAllText(fixture.DocumentPath);
            PreviewResponse initial = await RenderAsync(process, 81, fixture.DocumentPath, markup, audioEnabled: false);
            Assert.True(initial.Kind == PreviewResponseKind.Frame, initial.Error);
            Assert.Equal("10,200,30,255", MostCommonPixel(initial.Image));

            fixture.WriteCodeBehind(red: 200, green: 20, blue: 40);
            PreviewResponse recompiled = await RenderAsync(
                process,
                82,
                fixture.DocumentPath,
                "<UserControl><Border /></UserControl>",
                audioEnabled: false);

            Assert.True(recompiled.Kind == PreviewResponseKind.Frame, recompiled.Error);
            Assert.True(recompiled.CompileMilliseconds > 0, "The structural edit did not recompile.");
            Assert.Equal("200,20,40,255", MostCommonPixel(recompiled.Image));
        }
        finally
        {
            StopHostProcess(process);
        }
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
        bool audioEnabled,
        bool recompile = false)
    {
        PreviewProtocol.WriteRequest(process.StandardInput.BaseStream, new PreviewRequest
        {
            RequestId = requestId,
            Kind = PreviewRequestKind.Render,
            DocumentPath = documentPath,
            SourceText = source,
            Width = 320,
            Height = 180,
            AudioEnabled = audioEnabled,
            Recompile = recompile
        });
        return await Task.Run(() => PreviewProtocol.ReadResponse(process.StandardOutput.BaseStream))
                .WaitAsync(TimeSpan.FromSeconds(60))
            ?? throw new EndOfStreamException("The preview host closed without a frame.");
    }

    private static string MostCommonPixel(byte[] image) => Enumerable.Range(0, image.Length / 4)
        .GroupBy(pixel => string.Join(",", image.Skip(pixel * 4).Take(4)))
        .OrderByDescending(group => group.Count())
        .First()
        .Key;

    private static Process StartHostProcess()
    {
        string executable = Path.Combine(AppContext.BaseDirectory, "Cerneala.PreviewHost.exe");
        Assert.True(File.Exists(executable), $"Preview host executable '{executable}' is missing.");
        Process process = new()
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
        return process;
    }

    private static void StopHostProcess(Process process)
    {
        if (process.HasExited)
        {
            return;
        }

        PreviewProtocol.WriteRequest(process.StandardInput.BaseStream, new PreviewRequest
        {
            RequestId = 999,
            Kind = PreviewRequestKind.Shutdown
        });
        process.StandardInput.Close();
        if (!process.WaitForExit(2_000))
        {
            process.Kill();
            process.WaitForExit();
        }
    }

    // A Debug-built project with a code-behind constructor that paints the whole view one color.
    private sealed class CodeBehindFixture : IDisposable
    {
        private CodeBehindFixture(string root)
        {
            Root = root;
            DocumentPath = Path.Combine(root, "View.crn");
            CodePath = DocumentPath + ".cs";
        }

        public string Root { get; }

        public string DocumentPath { get; }

        public string CodePath { get; }

        public static CodeBehindFixture Create()
        {
            string root = Path.Combine(Path.GetTempPath(), "Cerneala", "PreviewCodeBehindTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            CodeBehindFixture fixture = new(root);
            File.WriteAllText(Path.Combine(root, "CodeBehindFixture.csproj"), $"""
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0-windows</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                    <AssemblyName>CodeBehindFixture</AssemblyName>
                  </PropertyGroup>
                  <ItemGroup>
                    <ProjectReference Include="{Path.Combine(RepositoryRoot(), "Cerneala.csproj")}" />
                    <AdditionalFiles Include="*.crn" />
                  </ItemGroup>
                </Project>
                """);
            File.WriteAllText(fixture.DocumentPath, "<UserControl />");
            fixture.WriteCodeBehind(red: 10, green: 200, blue: 30);
            fixture.Build();
            return fixture;
        }

        public void WriteCodeBehind(byte red, byte green, byte blue) => File.WriteAllText(CodePath, $$"""
            using Cerneala.Drawing;
            using Cerneala.UI.Controls;
            using Cerneala.UI.Media;

            namespace Fixture;

            public partial class View : UserControl
            {
                public View()
                {
                    Background = new SolidColorBrush(new Color({{red}}, {{green}}, {{blue}}));
                }
            }
            """);

        public void Dispose()
        {
            // Best effort: a just-stopped host or build node can still hold the directory for a moment,
            // and a cleanup failure must not replace the assertion that is unwinding.
            for (int attempt = 0; attempt < 10 && Directory.Exists(Root); attempt++)
            {
                try
                {
                    Directory.Delete(Root, recursive: true);
                }
                catch (IOException)
                {
                    Thread.Sleep(300);
                }
            }
        }

        private void Build()
        {
            ProcessStartInfo start = new("dotnet")
            {
                WorkingDirectory = Root,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            start.ArgumentList.Add("build");
            start.ArgumentList.Add(Path.Combine(Root, "CodeBehindFixture.csproj"));
            start.ArgumentList.Add("--configuration");
            start.ArgumentList.Add("Debug");
            start.ArgumentList.Add("--nologo");
            start.ArgumentList.Add("--verbosity");
            start.ArgumentList.Add("quiet");
            start.ArgumentList.Add("-p:NuGetAudit=false");
            using Process process = Process.Start(start) ?? throw new InvalidOperationException("Could not start dotnet build.");
            Task<string> output = process.StandardOutput.ReadToEndAsync();
            Task<string> error = process.StandardError.ReadToEndAsync();
            Assert.True(process.WaitForExit(110_000), "Fixture build timed out.");
            Assert.True(process.ExitCode == 0, output.Result + Environment.NewLine + error.Result);
        }
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
