using Cerneala.LanguageServer.Protocol;
using StreamJsonRpc;

namespace Cerneala.Tests.LanguageServer;

public sealed class FormattingProtocolTests
{
    [Fact]
    public async Task FormattingAndClosingTagCodeActionRunThroughTheRealProtocol()
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(30));
        await using ProtocolTestClient client = ProtocolTestClient.Start();
        InitializeResult initialized = await client.InitializeAsync(timeout.Token);
        await client.Rpc.NotifyWithParameterObjectAsync("initialized", new { });

        Assert.True(initialized.Capabilities.DocumentFormattingProvider);
        Assert.True(initialized.Capabilities.DocumentRangeFormattingProvider);
        Assert.NotNull(initialized.Capabilities.DocumentOnTypeFormattingProvider);
        Assert.NotNull(initialized.Capabilities.CodeActionProvider);

        string uri = new Uri(Path.GetFullPath("FormattingProtocol.crn")).AbsoluteUri;
        const string unformatted = "<Window>\n<Button />\n</Window>";
        string text = unformatted;
        await OpenAsync(client, uri, text);

        FormattingOptions options = new() { TabSize = 2, InsertSpaces = true };
        LspTextEdit[] documentEdits = await client.Rpc.InvokeWithParameterObjectAsync<LspTextEdit[]>(
            "textDocument/formatting",
            new DocumentFormattingParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = uri },
                Options = options
            },
            timeout.Token);
        Assert.NotEmpty(documentEdits);
        text = Apply(text, documentEdits);
        Assert.Equal("<Window>\n  <Button />\n</Window>", text);
        await ChangeAsync(client, uri, version: 2, text);

        Assert.Empty(await client.Rpc.InvokeWithParameterObjectAsync<LspTextEdit[]>(
            "textDocument/formatting",
            new DocumentFormattingParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = uri },
                Options = options
            },
            timeout.Token));

        text = unformatted;
        await ChangeAsync(client, uri, version: 3, text);
        LspTextEdit[] rangeEdits = await client.Rpc.InvokeWithParameterObjectAsync<LspTextEdit[]>(
            "textDocument/rangeFormatting",
            new DocumentRangeFormattingParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = uri },
                Range = new LspRange
                {
                    Start = new LspPosition { Line = 1, Character = 0 },
                    End = new LspPosition { Line = 1, Character = "<Button />".Length }
                },
                Options = options
            },
            timeout.Token);
        Assert.Equal("<Window>\n  <Button />\n</Window>", Apply(text, rangeEdits));

        LspTextEdit[] onTypeEdits = await client.Rpc.InvokeWithParameterObjectAsync<LspTextEdit[]>(
            "textDocument/onTypeFormatting",
            new DocumentOnTypeFormattingParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = uri },
                Position = new LspPosition { Line = 1, Character = "<Button />".Length },
                Character = ">",
                Options = options
            },
            timeout.Token);
        Assert.Equal("<Window>\n  <Button />\n</Window>", Apply(text, onTypeEdits));

        text = "<Window>\n  <Button />";
        await ChangeAsync(client, uri, version: 4, text);
        FullDocumentDiagnosticReport broken = await DiagnosticsAsync(client, uri, timeout.Token);
        LspDiagnostic target = Assert.Single(broken.Items, diagnostic => diagnostic.Code == "CERNEALAUI001");
        LspCodeAction closing = Assert.Single(
            await client.Rpc.InvokeWithParameterObjectAsync<LspCodeAction[]>(
                "textDocument/codeAction",
                new CodeActionParams
                {
                    TextDocument = new TextDocumentIdentifier { Uri = uri },
                    Range = FullRange(text),
                    Context = new CodeActionContext
                    {
                        Diagnostics = [target],
                        Only = ["quickfix"]
                    }
                },
                timeout.Token),
            action => action.Title == "Add closing tag </Window>");
        Assert.True(closing.IsPreferred);
        text = Apply(text, Assert.Single(closing.Edit.Changes, pair => pair.Key == uri).Value);
        await ChangeAsync(client, uri, version: 5, text);

        FullDocumentDiagnosticReport repaired = await DiagnosticsAsync(client, uri, timeout.Token);
        Assert.DoesNotContain(repaired.Items, diagnostic => diagnostic.Code == "CERNEALAUI001");
        Assert.Equal("<Window>\n  <Button /></Window>", text);
        Assert.Equal(0, await client.StopAsync(timeout.Token));
    }

    private static Task OpenAsync(ProtocolTestClient client, string uri, string text) =>
        client.Rpc.NotifyWithParameterObjectAsync(
            "textDocument/didOpen",
            new DidOpenTextDocumentParams
            {
                TextDocument = new TextDocumentItem
                {
                    Uri = uri,
                    LanguageId = "cerneala",
                    Version = 1,
                    Text = text
                }
            });

    private static Task ChangeAsync(ProtocolTestClient client, string uri, int version, string text) =>
        client.Rpc.NotifyWithParameterObjectAsync(
            "textDocument/didChange",
            new DidChangeTextDocumentParams
            {
                TextDocument = new VersionedTextDocumentIdentifier { Uri = uri, Version = version },
                ContentChanges = [new TextDocumentContentChangeEvent { Text = text }]
            });

    private static Task<FullDocumentDiagnosticReport> DiagnosticsAsync(
        ProtocolTestClient client,
        string uri,
        CancellationToken cancellationToken) => client.Rpc.InvokeWithParameterObjectAsync<FullDocumentDiagnosticReport>(
            "textDocument/diagnostic",
            new DocumentDiagnosticParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = uri }
            },
            cancellationToken);

    private static LspRange FullRange(string text) => new()
    {
        Start = new LspPosition { Line = 0, Character = 0 },
        End = PositionAt(text, text.Length)
    };

    private static string Apply(string source, IReadOnlyList<LspTextEdit> edits)
    {
        foreach (LspTextEdit edit in edits.OrderByDescending(edit => OffsetAt(source, edit.Range.Start)))
        {
            int start = OffsetAt(source, edit.Range.Start);
            int end = OffsetAt(source, edit.Range.End);
            source = source.Substring(0, start) + edit.NewText + source.Substring(end);
        }

        return source;
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
}
