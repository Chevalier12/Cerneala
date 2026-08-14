using System.Diagnostics;
using System.Xml.Linq;
using Cerneala.LanguageServer.Protocol;
using StreamJsonRpc;

namespace Cerneala.Tests.LanguageServer;

public sealed class NavigationProtocolTests
{
    [Fact]
    public async Task HoverNavigationReferencesHighlightsAndSafeRenameRunThroughTheRealProtocol()
    {
        using TemporaryNavigationWorkspace fixture = TemporaryNavigationWorkspace.Create();
        using CancellationTokenSource timeout = new(TimeSpan.FromMinutes(2));
        await using ProtocolTestClient client = ProtocolTestClient.Start();
        InitializeResult initialized = await client.InitializeAsync(timeout.Token, fixture.ProjectPath);
        await client.Rpc.NotifyWithParameterObjectAsync("initialized", new { });

        Assert.True(initialized.Capabilities.HoverProvider);
        Assert.True(initialized.Capabilities.DefinitionProvider);
        Assert.True(initialized.Capabilities.ReferencesProvider);
        Assert.True(initialized.Capabilities.DocumentHighlightProvider);
        Assert.True(initialized.Capabilities.RenameProvider?.PrepareProvider);

        string markup = File.ReadAllText(fixture.ViewMarkupPath);
        await client.Rpc.NotifyWithParameterObjectAsync(
            "textDocument/didOpen",
            new DidOpenTextDocumentParams
            {
                TextDocument = new TextDocumentItem
                {
                    Uri = fixture.ViewMarkupUri,
                    LanguageId = "cerneala",
                    Version = 1,
                    Text = markup
                }
            });

        int titleOffset = markup.IndexOf("$DataContext.Title", StringComparison.Ordinal) + "$DataContext.".Length + 1;
        TextDocumentPositionParams titleRequest = PositionRequest(fixture.ViewMarkupUri, markup, titleOffset);
        LspHover hover = await client.Rpc.InvokeWithParameterObjectAsync<LspHover>(
            "textDocument/hover",
            titleRequest,
            timeout.Token);
        Assert.Contains("Visible title", hover.Contents.Value, StringComparison.Ordinal);
        Assert.Contains("Default: `\"Untitled\"`", hover.Contents.Value, StringComparison.Ordinal);
        Assert.Contains("Declared by `Fixture.ViewModel`", hover.Contents.Value, StringComparison.Ordinal);

        LspLocation titleDefinition = Assert.Single(await client.Rpc.InvokeWithParameterObjectAsync<LspLocation[]>(
            "textDocument/definition",
            titleRequest,
            timeout.Token));
        Assert.Equal(Normalize(fixture.CodePath), Normalize(new Uri(titleDefinition.Uri).LocalPath));

        int rootOffset = markup.IndexOf("Window", StringComparison.Ordinal) + 1;
        LspLocation rootDefinition = Assert.Single(await client.Rpc.InvokeWithParameterObjectAsync<LspLocation[]>(
            "textDocument/definition",
            PositionRequest(fixture.ViewMarkupUri, markup, rootOffset),
            timeout.Token));
        Assert.Equal(Normalize(fixture.CodePath), Normalize(new Uri(rootDefinition.Uri).LocalPath));
        Assert.DoesNotContain("Generated", rootDefinition.Uri, StringComparison.OrdinalIgnoreCase);

        int accentOffset = markup.IndexOf("$Accent", StringComparison.Ordinal) + 1;
        LspLocation accentDefinition = Assert.Single(await client.Rpc.InvokeWithParameterObjectAsync<LspLocation[]>(
            "textDocument/definition",
            PositionRequest(fixture.ViewMarkupUri, markup, accentOffset),
            timeout.Token));
        Assert.Equal(Normalize(fixture.ApplicationMarkupPath), Normalize(new Uri(accentDefinition.Uri).LocalPath));

        int actionOffset = markup.LastIndexOf("$Action", StringComparison.Ordinal) + 1;
        TextDocumentPositionParams actionRequest = PositionRequest(fixture.ViewMarkupUri, markup, actionOffset);
        LspLocation[] actionReferences = await client.Rpc.InvokeWithParameterObjectAsync<LspLocation[]>(
            "textDocument/references",
            new ReferenceParams
            {
                TextDocument = actionRequest.TextDocument,
                Position = actionRequest.Position,
                Context = new ReferenceContext { IncludeDeclaration = true }
            },
            timeout.Token);
        Assert.Equal(2, actionReferences.Length);
        Assert.All(actionReferences, location => Assert.Equal(fixture.ViewMarkupUri, location.Uri));

        LspDocumentHighlight[] highlights = await client.Rpc.InvokeWithParameterObjectAsync<LspDocumentHighlight[]>(
            "textDocument/documentHighlight",
            actionRequest,
            timeout.Token);
        Assert.Equal(2, highlights.Length);

        LspPrepareRenameResult prepared = await client.Rpc.InvokeWithParameterObjectAsync<LspPrepareRenameResult>(
            "textDocument/prepareRename",
            actionRequest,
            timeout.Token);
        Assert.Equal("Action", prepared.Placeholder);

        LspLocation[] titleReferences = await client.Rpc.InvokeWithParameterObjectAsync<LspLocation[]>(
            "textDocument/references",
            new ReferenceParams
            {
                TextDocument = titleRequest.TextDocument,
                Position = titleRequest.Position,
                Context = new ReferenceContext { IncludeDeclaration = true }
            },
            timeout.Token);
        Assert.Contains(titleReferences, location => Normalize(new Uri(location.Uri).LocalPath) == Normalize(fixture.CodePath));
        Assert.Contains(titleReferences, location => location.Uri == fixture.ViewMarkupUri);

        LspWorkspaceEdit rename = await client.Rpc.InvokeWithParameterObjectAsync<LspWorkspaceEdit>(
            "textDocument/rename",
            new RenameParams
            {
                TextDocument = titleRequest.TextDocument,
                Position = titleRequest.Position,
                NewName = "Heading"
            },
            timeout.Token);
        Assert.Contains(rename.Changes.Keys, uri => Normalize(new Uri(uri).LocalPath) == Normalize(fixture.CodePath));
        Assert.Contains(rename.Changes.Keys, uri => uri == fixture.ViewMarkupUri);
        Assert.DoesNotContain(rename.Changes.Keys, uri => uri.Contains("Generated", StringComparison.OrdinalIgnoreCase));

        string partial = markup.Replace(
            "</StackPanel>",
            "<Button Bogus=\"1\" /></StackPanel>",
            StringComparison.Ordinal);
        await ChangeDocumentAsync(client, fixture.ViewMarkupUri, version: 2, partial);
        int partialActionOffset = partial.LastIndexOf("$Action", StringComparison.Ordinal) + 1;
        Assert.Single(await client.Rpc.InvokeWithParameterObjectAsync<LspLocation[]>(
            "textDocument/definition",
            PositionRequest(fixture.ViewMarkupUri, partial, partialActionOffset),
            timeout.Token));

        string duplicate = partial.Replace(
            "<Button Name=\"Action\"",
            "<Button Name=\"Action\" /><Button Name=\"Action\"",
            StringComparison.Ordinal);
        await ChangeDocumentAsync(client, fixture.ViewMarkupUri, version: 3, duplicate);
        int duplicateOffset = duplicate.IndexOf("Name=\"Action\"", StringComparison.Ordinal) + "Name=\"".Length + 1;
        RemoteInvocationException rejection = await Assert.ThrowsAnyAsync<RemoteInvocationException>(() =>
            client.Rpc.InvokeWithParameterObjectAsync<LspWorkspaceEdit>(
                "textDocument/rename",
                new RenameParams
                {
                    TextDocument = new TextDocumentIdentifier { Uri = fixture.ViewMarkupUri },
                    Position = PositionAt(duplicate, duplicateOffset),
                    NewName = "RenamedAction"
                },
                timeout.Token));
        Assert.Contains("duplicate", rejection.Message, StringComparison.OrdinalIgnoreCase);

        Assert.Equal(0, await client.StopAsync(timeout.Token));
        fixture.Apply(rename);
        Assert.Contains("TitleText", File.ReadAllText(fixture.CodePath), StringComparison.Ordinal);
        Assert.Contains("\"Title\"", File.ReadAllText(fixture.CodePath), StringComparison.Ordinal);
        Assert.Contains("$DataContext.Heading", File.ReadAllText(fixture.ViewMarkupPath), StringComparison.Ordinal);
        _ = XDocument.Load(fixture.ViewMarkupPath);
        fixture.Build(noRestore: true);
    }

    private static Task ChangeDocumentAsync(
        ProtocolTestClient client,
        string uri,
        int version,
        string text) => client.Rpc.NotifyWithParameterObjectAsync(
            "textDocument/didChange",
            new DidChangeTextDocumentParams
            {
                TextDocument = new VersionedTextDocumentIdentifier { Uri = uri, Version = version },
                ContentChanges = [new TextDocumentContentChangeEvent { Text = text }]
            });

    private static TextDocumentPositionParams PositionRequest(string uri, string text, int offset) => new()
    {
        TextDocument = new TextDocumentIdentifier { Uri = uri },
        Position = PositionAt(text, offset)
    };

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

    private static int OffsetAt(string text, LspPosition position)
    {
        int line = 0;
        int offset = 0;
        while (line < position.Line)
        {
            int next = text.IndexOf('\n', offset);
            if (next < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(position));
            }

            offset = next + 1;
            line++;
        }

        return offset + position.Character;
    }

    private static string Normalize(string path) => Path.GetFullPath(path)
        .Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

    private sealed class TemporaryNavigationWorkspace : IDisposable
    {
        private TemporaryNavigationWorkspace(
            string rootPath,
            string projectPath,
            string codePath,
            string viewMarkupPath,
            string applicationMarkupPath)
        {
            RootPath = rootPath;
            ProjectPath = projectPath;
            CodePath = codePath;
            ViewMarkupPath = viewMarkupPath;
            ApplicationMarkupPath = applicationMarkupPath;
        }

        public string RootPath { get; }

        public string ProjectPath { get; }

        public string CodePath { get; }

        public string ViewMarkupPath { get; }

        public string ApplicationMarkupPath { get; }

        public string ViewMarkupUri => new Uri(ViewMarkupPath).AbsoluteUri;

        public static TemporaryNavigationWorkspace Create()
        {
            string repositoryRoot = FindRepositoryRoot();
            string root = Path.Combine(Path.GetTempPath(), "cerneala-navigation-protocol-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            Directory.CreateDirectory(Path.Combine(root, "Generated"));
            string project = Path.Combine(root, "Fixture.csproj");
            string code = Path.Combine(root, "View.cui.xml.cs");
            string view = Path.Combine(root, "View.cui.xml");
            string application = Path.Combine(root, "App.cui.xml");
            XDocument projectDocument = new(
                new XElement("Project",
                    new XAttribute("Sdk", "Microsoft.NET.Sdk"),
                    new XElement("PropertyGroup",
                        new XElement("TargetFramework", "net10.0-windows"),
                        new XElement("ImplicitUsings", "enable"),
                        new XElement("Nullable", "enable"),
                        new XElement("AssemblyName", "NavigationProtocolFixture")),
                    new XElement("ItemGroup",
                        new XElement("ProjectReference", new XAttribute("Include", Path.Combine(repositoryRoot, "Cerneala.csproj"))),
                        new XElement("AdditionalFiles", new XAttribute("Include", "View.cui.xml")),
                        new XElement("AdditionalFiles", new XAttribute("Include", "App.cui.xml")))));
            projectDocument.Save(project);
            File.WriteAllText(code, """
                using System.ComponentModel;
                using Cerneala.UI.Controls;

                namespace Fixture;

                public sealed partial class View : Window
                {
                }

                public sealed class ViewModel : INotifyPropertyChanged
                {
                    /// <summary>Visible title from the protocol fixture.</summary>
                    [DefaultValue("Untitled")]
                    public string Title { get; set; } = string.Empty;
                    public string Echo() => Title;
                    public string TitleText => "Title";
                    public event PropertyChangedEventHandler? PropertyChanged;
                }
                """);
            File.WriteAllText(Path.Combine(root, "Generated", "View.g.cs"), """
                namespace Fixture;
                public sealed partial class View
                {
                }
                """);
            File.WriteAllText(application, """
                <Application>
                  <Application.Resources>
                    <SolidColorBrush Name="Accent" />
                  </Application.Resources>
                </Application>
                """);
            File.WriteAllText(view, """
                <Window DataType="Fixture.ViewModel">
                  <StackPanel>
                    <Button Name="Action" Background="$Accent" />
                    <TextBlock Text="$DataContext.Title" />
                    <TextBlock Text="$Action.Width" />
                    <TextBlock Text="Action" />
                  </StackPanel>
                </Window>
                """);
            TemporaryNavigationWorkspace fixture = new(root, project, code, view, application);
            fixture.Build(noRestore: false);
            return fixture;
        }

        public void Apply(LspWorkspaceEdit edit)
        {
            foreach ((string uri, LspTextEdit[] edits) in edit.Changes)
            {
                string path = new Uri(uri).LocalPath;
                string text = File.ReadAllText(path);
                foreach (LspTextEdit change in edits
                    .OrderByDescending(change => OffsetAt(text, change.Range.Start)))
                {
                    int start = OffsetAt(text, change.Range.Start);
                    int end = OffsetAt(text, change.Range.End);
                    text = text.Substring(0, start) + change.NewText + text.Substring(end);
                }

                File.WriteAllText(path, text);
            }
        }

        public void Build(bool noRestore)
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
            if (noRestore)
            {
                start.ArgumentList.Add("--no-restore");
            }

            using Process process = Process.Start(start) ?? throw new InvalidOperationException("Could not start dotnet build.");
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            Assert.True(process.WaitForExit(60000), "Fixture build timed out.");
            Assert.True(process.ExitCode == 0, output + Environment.NewLine + error);
        }

        public void Dispose()
        {
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, recursive: true);
            }
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
