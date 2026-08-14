using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;
using Cerneala.LanguageServer.Protocol;

namespace Cerneala.Tests.LanguageServer;

public sealed class StructureProtocolTests
{
    [Fact]
    public async Task StructureFeaturesUseUnsavedOverlayAndRecoverAfterLocalError()
    {
        using TemporaryStructureWorkspace fixture = TemporaryStructureWorkspace.Create();
        using CancellationTokenSource timeout = new(TimeSpan.FromMinutes(2));
        await using ProtocolTestClient client = ProtocolTestClient.Start();
        InitializeResult initialized = await client.InitializeAsync(timeout.Token, fixture.ProjectPath);
        await client.Rpc.NotifyWithParameterObjectAsync("initialized", new { });

        Assert.NotNull(initialized.Capabilities.SemanticTokensProvider);
        Assert.True(initialized.Capabilities.DocumentSymbolProvider);
        Assert.True(initialized.Capabilities.WorkspaceSymbolProvider);
        Assert.True(initialized.Capabilities.FoldingRangeProvider);
        Assert.True(initialized.Capabilities.SelectionRangeProvider);

        string markup = fixture.UnsavedMarkup;
        await client.Rpc.NotifyWithParameterObjectAsync(
            "textDocument/didOpen",
            new DidOpenTextDocumentParams
            {
                TextDocument = new TextDocumentItem
                {
                    Uri = fixture.MarkupUri,
                    LanguageId = "cerneala",
                    Version = 1,
                    Text = markup
                }
            });

        LspSemanticTokens full = await client.Rpc.InvokeWithParameterObjectAsync<LspSemanticTokens>(
            "textDocument/semanticTokens/full",
            new SemanticTokensParams { TextDocument = new TextDocumentIdentifier { Uri = fixture.MarkupUri } },
            timeout.Token);
        Assert.NotEmpty(full.Data);
        Assert.Equal(0, full.Data.Length % 5);

        LspDocumentSymbol[] symbols = await DocumentSymbolsAsync(client, fixture.MarkupUri, timeout.Token);
        Assert.Contains(Flatten(symbols), symbol => symbol.Name == "UnsavedAction");
        LspSymbolInformation[] workspaceSymbols = await client.Rpc.InvokeWithParameterObjectAsync<LspSymbolInformation[]>(
            "workspace/symbol",
            new WorkspaceSymbolParams { Query = "Unsaved" },
            timeout.Token);
        Assert.Contains(workspaceSymbols, symbol => symbol.Name == "UnsavedAction" && symbol.Location.Uri == fixture.MarkupUri);

        LspFoldingRange[] folding = await FoldingAsync(client, fixture.MarkupUri, timeout.Token);
        Assert.NotEmpty(folding);
        LspSelectionRange selection = Assert.Single(await client.Rpc.InvokeWithParameterObjectAsync<LspSelectionRange[]>(
            "textDocument/selectionRange",
            new SelectionRangeParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = fixture.MarkupUri },
                Positions = [PositionAt(markup, markup.IndexOf("UnsavedAction", StringComparison.Ordinal) + 2)]
            },
            timeout.Token));
        Assert.NotNull(selection.Parent);

        string edited = markup.Replace(
            "<Button Name=\"UnsavedAction\"",
            "<Button Bogus=\"1\" Name=\"UnsavedAction\"",
            StringComparison.Ordinal);
        await ChangeAsync(client, fixture.MarkupUri, 2, edited);
        JsonElement delta = await client.Rpc.InvokeWithParameterObjectAsync<JsonElement>(
            "textDocument/semanticTokens/full/delta",
            new SemanticTokensDeltaParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = fixture.MarkupUri },
                PreviousResultId = full.ResultId
            },
            timeout.Token);
        Assert.True(delta.TryGetProperty("edits", out JsonElement edits));
        Assert.True(edits.GetArrayLength() > 0);

        Assert.Contains(Flatten(await DocumentSymbolsAsync(client, fixture.MarkupUri, timeout.Token)),
            symbol => symbol.Name == "UnsavedAction");
        Assert.NotEmpty(await FoldingAsync(client, fixture.MarkupUri, timeout.Token));
        Assert.Equal(0, await client.StopAsync(timeout.Token));
    }

    private static Task<LspDocumentSymbol[]> DocumentSymbolsAsync(
        ProtocolTestClient client,
        string uri,
        CancellationToken cancellationToken) => client.Rpc.InvokeWithParameterObjectAsync<LspDocumentSymbol[]>(
        "textDocument/documentSymbol",
        new DocumentSymbolParams { TextDocument = new TextDocumentIdentifier { Uri = uri } },
        cancellationToken);

    private static Task<LspFoldingRange[]> FoldingAsync(
        ProtocolTestClient client,
        string uri,
        CancellationToken cancellationToken) => client.Rpc.InvokeWithParameterObjectAsync<LspFoldingRange[]>(
        "textDocument/foldingRange",
        new FoldingRangeParams { TextDocument = new TextDocumentIdentifier { Uri = uri } },
        cancellationToken);

    private static Task ChangeAsync(ProtocolTestClient client, string uri, int version, string text) =>
        client.Rpc.NotifyWithParameterObjectAsync(
            "textDocument/didChange",
            new DidChangeTextDocumentParams
            {
                TextDocument = new VersionedTextDocumentIdentifier { Uri = uri, Version = version },
                ContentChanges = [new TextDocumentContentChangeEvent { Text = text }]
            });

    private static IEnumerable<LspDocumentSymbol> Flatten(IEnumerable<LspDocumentSymbol> symbols) =>
        symbols.SelectMany(symbol => new[] { symbol }.Concat(Flatten(symbol.Children)));

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

    private sealed class TemporaryStructureWorkspace : IDisposable
    {
        private TemporaryStructureWorkspace(string rootPath, string projectPath, string markupPath)
        {
            RootPath = rootPath;
            ProjectPath = projectPath;
            MarkupPath = markupPath;
        }

        public string RootPath { get; }

        public string ProjectPath { get; }

        public string MarkupPath { get; }

        public string MarkupUri => new Uri(MarkupPath).AbsoluteUri;

        public string UnsavedMarkup => """
            <Window>
              <!-- keep this fold -->
              <Window.Resources>
                <SolidColorBrush Name="Accent" />
              </Window.Resources>
              <StackPanel>
                <Button Name="UnsavedAction" Background="$Accent" />
              </StackPanel>
            </Window>
            """;

        public static TemporaryStructureWorkspace Create()
        {
            string repositoryRoot = FindRepositoryRoot();
            string root = Path.Combine(Path.GetTempPath(), "cerneala-structure-protocol-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            string project = Path.Combine(root, "Fixture.csproj");
            string markup = Path.Combine(root, "View.cui.xml");
            XDocument projectDocument = new(
                new XElement("Project",
                    new XAttribute("Sdk", "Microsoft.NET.Sdk"),
                    new XElement("PropertyGroup",
                        new XElement("TargetFramework", "net10.0-windows"),
                        new XElement("ImplicitUsings", "enable"),
                        new XElement("Nullable", "enable")),
                    new XElement("ItemGroup",
                        new XElement("ProjectReference", new XAttribute("Include", Path.Combine(repositoryRoot, "Cerneala.csproj"))),
                        new XElement("AdditionalFiles", new XAttribute("Include", "View.cui.xml")))));
            projectDocument.Save(project);
            File.WriteAllText(Path.Combine(root, "View.cui.xml.cs"), """
                using Cerneala.UI.Controls;
                namespace Fixture;
                public sealed partial class View : Window { }
                """);
            File.WriteAllText(markup, "<Window><Button Name=\"SavedAction\" /></Window>");
            TemporaryStructureWorkspace fixture = new(root, project, markup);
            fixture.Build();
            return fixture;
        }

        public void Dispose()
        {
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, recursive: true);
            }
        }

        private void Build()
        {
            ProcessStartInfo start = new("dotnet")
            {
                WorkingDirectory = RootPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            start.ArgumentList.Add("build");
            start.ArgumentList.Add(ProjectPath);
            start.ArgumentList.Add("--nologo");
            start.ArgumentList.Add("--verbosity");
            start.ArgumentList.Add("quiet");
            using Process process = Process.Start(start) ?? throw new InvalidOperationException("Could not start fixture build.");
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            Assert.True(process.WaitForExit(60000), "Fixture build timed out.");
            Assert.True(process.ExitCode == 0, output + Environment.NewLine + error);
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
    }
}
