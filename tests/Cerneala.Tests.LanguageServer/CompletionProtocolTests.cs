using Cerneala.LanguageServer.Protocol;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Diagnostics;
using System.Xml.Linq;

namespace Cerneala.Tests.LanguageServer;

public sealed class CompletionProtocolTests
{
    [Fact]
    public async Task DeferredWorkspaceUsesTheBuiltOwnerForTheFirstCompletionRequest()
    {
        using ImmediateSemanticWorkspace fixture = ImmediateSemanticWorkspace.Create();
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
        await using ProtocolTestClient client = ProtocolTestClient.Start();
        await client.InitializeAsync(
            timeout.Token,
            fixture.SolutionPath,
            deferWorkspaceLoad: true);
        await client.Rpc.NotifyWithParameterObjectAsync(
            "textDocument/didOpen",
            new DidOpenTextDocumentParams
            {
                TextDocument = new TextDocumentItem
                {
                    Uri = fixture.MarkupUri,
                    LanguageId = "cerneala",
                    Version = 1,
                    Text = fixture.Markup
                }
            });

        int completionOffset = fixture.Markup.IndexOf("$DataContext.", StringComparison.Ordinal) +
            "$DataContext.".Length;
        Stopwatch firstCompletion = Stopwatch.StartNew();
        CompletionList completion = await client.Rpc.InvokeWithParameterObjectAsync<CompletionList>(
            "textDocument/completion",
            new TextDocumentPositionParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = fixture.MarkupUri },
                Position = PositionAt(fixture.Markup, completionOffset)
            },
            timeout.Token);
        firstCompletion.Stop();

        Assert.Contains(completion.Items, item => item.Label == "ImmediateProperty");
        Assert.True(
            firstCompletion.Elapsed < TimeSpan.FromSeconds(2),
            "First semantic completion took " + firstCompletion.Elapsed.TotalMilliseconds + " ms.");
        using (FileStream output = File.Open(
            fixture.OutputAssemblyPath,
            FileMode.Open,
            FileAccess.ReadWrite,
            FileShare.None))
        {
            Assert.True(output.CanWrite);
        }

        Assert.Equal(0, await client.StopAsync(timeout.Token));
    }

    [Fact]
    public async Task CompletionResolveAndSignatureHelpRunThroughTheRealProtocol()
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromMinutes(2));
        await using ProtocolTestClient client = ProtocolTestClient.Start();
        string repositoryRoot = FindRepositoryRoot();
        string solutionPath = Path.Combine(repositoryRoot, "Cerneala.slnx");
        InitializeResult initialized = await client.InitializeAsync(timeout.Token, solutionPath);
        await client.Rpc.NotifyWithParameterObjectAsync("initialized", new { });

        Assert.True(initialized.Capabilities.CompletionProvider?.ResolveProvider);
        Assert.Contains(" ", initialized.Capabilities.CompletionProvider!.TriggerCharacters);
        Assert.NotNull(initialized.Capabilities.SignatureHelpProvider);
        Assert.Contains("\"", initialized.Capabilities.SignatureHelpProvider!.TriggerCharacters);
        Assert.Contains("'", initialized.Capabilities.SignatureHelpProvider.TriggerCharacters);

        string path = Path.Combine(repositoryRoot, "CernealaPresentation", "OpeningView.crn");
        string uri = new Uri(path).AbsoluteUri;
        string markup = "<UserControl DataType=\"Cerneala.Presentation.PrismStudioModel\"><TextBlock Text=\"$DataContext.\" /></UserControl>";
        await client.Rpc.NotifyWithParameterObjectAsync(
            "textDocument/didOpen",
            new DidOpenTextDocumentParams
            {
                TextDocument = new TextDocumentItem
                {
                    Uri = uri,
                    LanguageId = "cerneala",
                    Version = 1,
                    Text = markup
                }
            });

        int completionOffset = markup.IndexOf("$DataContext.", StringComparison.Ordinal) + "$DataContext.".Length;
        CompletionList completion = await client.Rpc.InvokeWithParameterObjectAsync<CompletionList>(
            "textDocument/completion",
            new TextDocumentPositionParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = uri },
                Position = PositionAt(markup, completionOffset)
            },
            timeout.Token);
        LspCompletionItem target = Assert.Single(completion.Items.Where(item => item.Label == "Target"));
        string applied = Apply(markup, target.TextEdit);
        _ = XDocument.Parse(applied);
        Assert.Contains("$DataContext.Target", applied, StringComparison.Ordinal);

        LspCompletionItem resolved = await client.Rpc.InvokeWithParameterObjectAsync<LspCompletionItem>(
            "completionItem/resolve",
            target,
            timeout.Token);
        Assert.Contains("Target", resolved.Detail, StringComparison.Ordinal);
        Assert.Contains("CernealaPresentation", resolved.Detail, StringComparison.Ordinal);

        string signatureMarkup = "<UserControl>Tween(100ms, )</UserControl>";
        await client.Rpc.NotifyWithParameterObjectAsync(
            "textDocument/didChange",
            new DidChangeTextDocumentParams
            {
                TextDocument = new VersionedTextDocumentIdentifier { Uri = uri, Version = 2 },
                ContentChanges = [new TextDocumentContentChangeEvent { Text = signatureMarkup }]
            });
        int signatureOffset = signatureMarkup.IndexOf(')');
        LspSignatureHelp signature = await client.Rpc.InvokeWithParameterObjectAsync<LspSignatureHelp>(
            "textDocument/signatureHelp",
            new TextDocumentPositionParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = uri },
                Position = PositionAt(signatureMarkup, signatureOffset)
            },
            timeout.Token);

        Assert.Equal(1, signature.ActiveParameter);
        Assert.Contains("Tween", Assert.Single(signature.Signatures).Label, StringComparison.Ordinal);

        string motionCompletionMarkup = "<UserControl>Tween(680ms, Ea)</UserControl>";
        await client.Rpc.NotifyWithParameterObjectAsync(
            "textDocument/didChange",
            new DidChangeTextDocumentParams
            {
                TextDocument = new VersionedTextDocumentIdentifier { Uri = uri, Version = 3 },
                ContentChanges = [new TextDocumentContentChangeEvent { Text = motionCompletionMarkup }]
            });
        int motionCompletionOffset = motionCompletionMarkup.IndexOf(')');
        CompletionList motionCompletion = await client.Rpc.InvokeWithParameterObjectAsync<CompletionList>(
            "textDocument/completion",
            new TextDocumentPositionParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = uri },
                Position = PositionAt(motionCompletionMarkup, motionCompletionOffset)
            },
            timeout.Token);
        LspCompletionItem easing = Assert.Single(motionCompletion.Items.Where(item => item.Label == "EaseIn"));

        Assert.Equal("easing", easing.Detail);
        Assert.Contains("Tween(680ms, EaseIn)", Apply(motionCompletionMarkup, easing.TextEdit), StringComparison.Ordinal);

        string referenceMarkup = "<UserControl><UserControl.Resources>" +
            "<Tween Name=\"Quick\" Duration=\"100ms\" />" +
            "<MotionClip Name=\"Pulse\" TargetType=\"Button\" />" +
            "<Aspect Name=\"Animated\" TargetType=\"Button\">@on Loaded { @run $; }</Aspect>" +
            "</UserControl.Resources><Button Name=\"Action\" /></UserControl>";
        await client.Rpc.NotifyWithParameterObjectAsync(
            "textDocument/didChange",
            new DidChangeTextDocumentParams
            {
                TextDocument = new VersionedTextDocumentIdentifier { Uri = uri, Version = 4 },
                ContentChanges = [new TextDocumentContentChangeEvent { Text = referenceMarkup }]
            });
        int referenceOffset = referenceMarkup.IndexOf("$;", StringComparison.Ordinal) + 1;
        CompletionList referenceCompletion = await client.Rpc.InvokeWithParameterObjectAsync<CompletionList>(
            "textDocument/completion",
            new TextDocumentPositionParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = uri },
                Position = PositionAt(referenceMarkup, referenceOffset)
            },
            timeout.Token);
        LspCompletionItem clip = Assert.Single(referenceCompletion.Items.Where(item => item.Label == "$Pulse"));

        Assert.Contains(referenceCompletion.Items, item => item.Label == "$DataContext");
        Assert.Contains(referenceCompletion.Items, item => item.Label == "$Quick");
        Assert.Contains(referenceCompletion.Items, item => item.Label == "$Action");
        string appliedReference = Apply(referenceMarkup, clip.TextEdit);
        Assert.Contains("@run $Pulse;", appliedReference, StringComparison.Ordinal);
        Assert.DoesNotContain("@run $$Pulse;", appliedReference, StringComparison.Ordinal);

        string memberMarkup = "<UserControl><UserControl.Resources>" +
            "<Aspect Name=\"Animated\" TargetType=\"Button\">@animate { @from { $Action. } }</Aspect>" +
            "</UserControl.Resources><Button Name=\"Action\" /></UserControl>";
        await client.Rpc.NotifyWithParameterObjectAsync(
            "textDocument/didChange",
            new DidChangeTextDocumentParams
            {
                TextDocument = new VersionedTextDocumentIdentifier { Uri = uri, Version = 5 },
                ContentChanges = [new TextDocumentContentChangeEvent { Text = memberMarkup }]
            });
        int memberOffset = memberMarkup.IndexOf("$Action.", StringComparison.Ordinal) + "$Action.".Length;
        CompletionList memberCompletion = await client.Rpc.InvokeWithParameterObjectAsync<CompletionList>(
            "textDocument/completion",
            new TextDocumentPositionParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = uri },
                Position = PositionAt(memberMarkup, memberOffset)
            },
            timeout.Token);
        LspCompletionItem opacity = Assert.Single(memberCompletion.Items.Where(item => item.Label == "Opacity"));

        Assert.Equal(10, opacity.Kind);
        Assert.Contains(memberCompletion.Items, item => item.Label == "Click" && item.Kind == 23);
        Assert.Contains("$Action.Opacity", Apply(memberMarkup, opacity.TextEdit), StringComparison.Ordinal);

        string handleMarkup = "<UserControl><UserControl.Resources>" +
            "<MotionClip Name=\"Pulse\" TargetType=\"Button\" />" +
            "<Aspect Name=\"Animated\" TargetType=\"Button\">" +
            "@handle Loading; @on Loaded { @run $Pulse as ; }</Aspect>" +
            "</UserControl.Resources></UserControl>";
        await client.Rpc.NotifyWithParameterObjectAsync(
            "textDocument/didChange",
            new DidChangeTextDocumentParams
            {
                TextDocument = new VersionedTextDocumentIdentifier { Uri = uri, Version = 6 },
                ContentChanges = [new TextDocumentContentChangeEvent { Text = handleMarkup }]
            });
        int handleOffset = handleMarkup.IndexOf("as ;", StringComparison.Ordinal) + "as ".Length;
        CompletionList handleCompletion = await client.Rpc.InvokeWithParameterObjectAsync<CompletionList>(
            "textDocument/completion",
            new TextDocumentPositionParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = uri },
                Position = PositionAt(handleMarkup, handleOffset)
            },
            timeout.Token);
        LspCompletionItem loading = Assert.Single(handleCompletion.Items.Where(item => item.Label == "Loading"));

        Assert.Equal("Motion handle", loading.Detail);
        Assert.Contains("as Loading;", Apply(handleMarkup, loading.TextEdit), StringComparison.Ordinal);

        string targetPropertyMarkup = "<UserControl><UserControl.Resources>" +
            "<Aspect TargetType=\"Button\">@default { Op }</Aspect>" +
            "</UserControl.Resources></UserControl>";
        await client.Rpc.NotifyWithParameterObjectAsync(
            "textDocument/didChange",
            new DidChangeTextDocumentParams
            {
                TextDocument = new VersionedTextDocumentIdentifier { Uri = uri, Version = 7 },
                ContentChanges = [new TextDocumentContentChangeEvent { Text = targetPropertyMarkup }]
            });
        int targetPropertyOffset = targetPropertyMarkup.IndexOf("Op }", StringComparison.Ordinal) + 2;
        CompletionList targetPropertyCompletion = await client.Rpc.InvokeWithParameterObjectAsync<CompletionList>(
            "textDocument/completion",
            new TextDocumentPositionParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = uri },
                Position = PositionAt(targetPropertyMarkup, targetPropertyOffset)
            },
            timeout.Token);
        LspCompletionItem targetOpacity = Assert.Single(
            targetPropertyCompletion.Items.Where(item => item.Label == "Opacity"));

        Assert.Equal(10, targetOpacity.Kind);
        Assert.Contains("@default { Opacity =  }", Apply(targetPropertyMarkup, targetOpacity.TextEdit), StringComparison.Ordinal);

        string nestedDirectiveMarkup = "<UserControl><UserControl.Resources>" +
            "<Aspect TargetType=\"Button\">@when IsMouseOver { @i }</Aspect>" +
            "</UserControl.Resources></UserControl>";
        await client.Rpc.NotifyWithParameterObjectAsync(
            "textDocument/didChange",
            new DidChangeTextDocumentParams
            {
                TextDocument = new VersionedTextDocumentIdentifier { Uri = uri, Version = 8 },
                ContentChanges = [new TextDocumentContentChangeEvent { Text = nestedDirectiveMarkup }]
            });
        int nestedDirectiveOffset = nestedDirectiveMarkup.IndexOf("@i }", StringComparison.Ordinal) + 2;
        CompletionList nestedDirectiveCompletion = await client.Rpc.InvokeWithParameterObjectAsync<CompletionList>(
            "textDocument/completion",
            new TextDocumentPositionParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = uri },
                Position = PositionAt(nestedDirectiveMarkup, nestedDirectiveOffset)
            },
            timeout.Token);

        Assert.Contains(nestedDirectiveCompletion.Items, item => item.Label == "@if");
        Assert.DoesNotContain(nestedDirectiveCompletion.Items, item => item.Kind == 10);

        string reactiveExpressionMarkup = "<UserControl><UserControl.Resources>" +
            "<Aspect TargetType=\"Button\">@when  { Opacity = 1; }</Aspect>" +
            "</UserControl.Resources></UserControl>";
        await client.Rpc.NotifyWithParameterObjectAsync(
            "textDocument/didChange",
            new DidChangeTextDocumentParams
            {
                TextDocument = new VersionedTextDocumentIdentifier { Uri = uri, Version = 9 },
                ContentChanges = [new TextDocumentContentChangeEvent { Text = reactiveExpressionMarkup }]
            });
        int reactiveExpressionOffset =
            reactiveExpressionMarkup.IndexOf("@when  ", StringComparison.Ordinal) + "@when ".Length;
        CompletionList reactiveExpressionCompletion = await client.Rpc.InvokeWithParameterObjectAsync<CompletionList>(
            "textDocument/completion",
            new TextDocumentPositionParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = uri },
                Position = PositionAt(reactiveExpressionMarkup, reactiveExpressionOffset)
            },
            timeout.Token);
        LspCompletionItem mouseOver = Assert.Single(
            reactiveExpressionCompletion.Items.Where(item => item.Label == "IsMouseOver"));

        Assert.Equal(10, mouseOver.Kind);
        Assert.Contains("@when IsMouseOver {", Apply(reactiveExpressionMarkup, mouseOver.TextEdit), StringComparison.Ordinal);

        string prismMarkup = "<UserControl><UserControl.Resources>" +
            "<PrismComposition Name=\"Fx\">@layer Neon { @style OuterGlow { Si } }</PrismComposition>" +
            "</UserControl.Resources></UserControl>";
        await client.Rpc.NotifyWithParameterObjectAsync(
            "textDocument/didChange",
            new DidChangeTextDocumentParams
            {
                TextDocument = new VersionedTextDocumentIdentifier { Uri = uri, Version = 10 },
                ContentChanges = [new TextDocumentContentChangeEvent { Text = prismMarkup }]
            });
        int prismOffset = prismMarkup.IndexOf("Si }", StringComparison.Ordinal) + 2;
        CompletionList prismCompletion = await client.Rpc.InvokeWithParameterObjectAsync<CompletionList>(
            "textDocument/completion",
            new TextDocumentPositionParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = uri },
                Position = PositionAt(prismMarkup, prismOffset)
            },
            timeout.Token);
        LspCompletionItem size = Assert.Single(prismCompletion.Items.Where(item => item.Label == "Size"));

        Assert.Equal(10, size.Kind);
        Assert.Contains("@style OuterGlow { Size =  }", Apply(prismMarkup, size.TextEdit), StringComparison.Ordinal);

        string thicknessMarkup = "<UserControl><Button Margin=\"0,-12,18,\" /></UserControl>";
        await client.Rpc.NotifyWithParameterObjectAsync(
            "textDocument/didChange",
            new DidChangeTextDocumentParams
            {
                TextDocument = new VersionedTextDocumentIdentifier { Uri = uri, Version = 11 },
                ContentChanges = [new TextDocumentContentChangeEvent { Text = thicknessMarkup }]
            });
        int thicknessOffset = thicknessMarkup.IndexOf("\" />", StringComparison.Ordinal);
        LspSignatureHelp thickness = await client.Rpc.InvokeWithParameterObjectAsync<LspSignatureHelp>(
            "textDocument/signatureHelp",
            new TextDocumentPositionParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = uri },
                Position = PositionAt(thicknessMarkup, thicknessOffset)
            },
            timeout.Token);

        Assert.Equal(3, thickness.ActiveParameter);
        Assert.Equal(1, thickness.ActiveSignature);
        Assert.Equal("Thickness(left, top, right, bottom)", thickness.Signatures[1].Label);
        Assert.Equal(0, await client.StopAsync(timeout.Token));
    }

    private static LspPosition PositionAt(string text, int offset)
    {
        int line = 0;
        int lineStart = 0;
        for (int index = 0; index < offset; index++)
        {
            if (text[index] == '\n')
            {
                line++;
                lineStart = index + 1;
            }
        }

        return new LspPosition { Line = line, Character = offset - lineStart };
    }

    private static string Apply(string source, LspTextEdit edit)
    {
        Assert.Equal(edit.Range.Start.Line, edit.Range.End.Line);
        Assert.Equal(0, edit.Range.Start.Line);
        return source.Substring(0, edit.Range.Start.Character) +
            edit.NewText +
            source.Substring(edit.Range.End.Character);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Cerneala.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the Cerneala repository root.");
    }

    private sealed class ImmediateSemanticWorkspace : IDisposable
    {
        private ImmediateSemanticWorkspace(
            string rootPath,
            string solutionPath,
            string markupPath,
            string outputAssemblyPath,
            string markup)
        {
            RootPath = rootPath;
            SolutionPath = solutionPath;
            MarkupPath = markupPath;
            OutputAssemblyPath = outputAssemblyPath;
            Markup = markup;
        }

        public string RootPath { get; }

        public string SolutionPath { get; }

        public string MarkupPath { get; }

        public string OutputAssemblyPath { get; }

        public string MarkupUri => new Uri(MarkupPath).AbsoluteUri;

        public string Markup { get; }

        public static ImmediateSemanticWorkspace Create()
        {
            string root = Path.Combine(Path.GetTempPath(), $"cerneala-lsp-bootstrap-{Guid.NewGuid():N}");
            Directory.CreateDirectory(root);
            string projectPath = Path.Combine(root, "Fixture.csproj");
            string solutionPath = Path.Combine(root, "Fixture.slnx");
            string markupPath = Path.Combine(root, "View.crn");
            string markup =
                "<Window DataType=\"Fixture.ViewModel\"><TextBlock Text=\"$DataContext.\" /></Window>";
            File.WriteAllText(projectPath, """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <AssemblyName>Fixture</AssemblyName>
                  </PropertyGroup>
                  <ItemGroup>
                    <AdditionalFiles Include="View.crn" />
                  </ItemGroup>
                </Project>
                """);
            File.WriteAllText(solutionPath, """
                <Solution>
                  <Project Path="Fixture.csproj" />
                </Solution>
                """);
            File.WriteAllText(markupPath, markup);
            string outputAssemblyPath = Path.Combine(root, "bin", "Debug", "net10.0", "Fixture.dll");
            EmitBuiltProject(outputAssemblyPath);
            return new ImmediateSemanticWorkspace(
                root,
                solutionPath,
                markupPath,
                outputAssemblyPath,
                markup);
        }

        public void Dispose()
        {
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, recursive: true);
            }
        }

        private static void EmitBuiltProject(string outputPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            string[] trustedAssemblies = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))?
                .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries) ?? [];
            CSharpCompilation compilation = CSharpCompilation.Create(
                "Fixture",
                [CSharpSyntaxTree.ParseText("namespace Fixture; public sealed class ViewModel { public string ImmediateProperty { get; set; } = string.Empty; }")],
                trustedAssemblies.Select(path => MetadataReference.CreateFromFile(path)),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            Microsoft.CodeAnalysis.Emit.EmitResult result = compilation.Emit(outputPath);
            Assert.True(result.Success, string.Join(Environment.NewLine, result.Diagnostics));
        }
    }
}
