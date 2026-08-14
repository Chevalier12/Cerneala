using Cerneala.LanguageServer.Protocol;
using System.Xml.Linq;

namespace Cerneala.Tests.LanguageServer;

public sealed class CompletionProtocolTests
{
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
        Assert.NotNull(initialized.Capabilities.SignatureHelpProvider);

        string path = Path.Combine(repositoryRoot, "CernealaPresentation", "OpeningView.cui.xml");
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
}
