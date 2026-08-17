using System.Text.Json;
using Cerneala.LanguageServer.Protocol;
using StreamJsonRpc;

namespace Cerneala.Tests.LanguageServer;

public sealed class ProtocolContractTests
{
    [Fact]
    public async Task LifecycleNegotiatesAndStopsCleanlyOverInMemoryTransport()
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
        await using ProtocolTestClient client = ProtocolTestClient.Start();

        InitializeResult result = await client.InitializeAsync(timeout.Token);
        await client.Rpc.NotifyWithParameterObjectAsync("initialized", new { });

        Assert.Equal("Cerneala Language Server", result.ServerInfo.Name);
        Assert.NotNull(result.Capabilities.TextDocumentSync);
        Assert.True(result.Capabilities.TextDocumentSync.OpenClose);
        Assert.Equal(2, result.Capabilities.TextDocumentSync.Change);
        Assert.False(result.Capabilities.TextDocumentSync.Save.IncludeText);
        Assert.NotNull(result.Capabilities.CompletionProvider);
        Assert.True(result.Capabilities.CompletionProvider.ResolveProvider);
        Assert.Contains("<", result.Capabilities.CompletionProvider.TriggerCharacters);
        Assert.NotNull(result.Capabilities.SignatureHelpProvider);
        Assert.Contains("(", result.Capabilities.SignatureHelpProvider.TriggerCharacters);
        Assert.True(result.Capabilities.HoverProvider);
        Assert.True(result.Capabilities.DefinitionProvider);
        Assert.True(result.Capabilities.ReferencesProvider);
        Assert.True(result.Capabilities.DocumentHighlightProvider);
        Assert.True(result.Capabilities.RenameProvider?.PrepareProvider);
        Assert.NotNull(result.Capabilities.DiagnosticProvider);
        Assert.Equal("cerneala", result.Capabilities.DiagnosticProvider.Identifier);
        Assert.True(result.Capabilities.DiagnosticProvider.InterFileDependencies);
        Assert.False(result.Capabilities.DiagnosticProvider.WorkspaceDiagnostics);
        Assert.NotNull(result.Capabilities.SemanticTokensProvider);
        Assert.True(result.Capabilities.SemanticTokensProvider.Full.Delta);
        Assert.Equal(
            ["type", "property", "event", "namespace", "variable", "keyword", "function", "parameter", "enumMember", "label", "property"],
            result.Capabilities.SemanticTokensProvider.Legend.TokenTypes);
        Assert.Equal(["declaration"], result.Capabilities.SemanticTokensProvider.Legend.TokenModifiers);
        Assert.True(result.Capabilities.DocumentSymbolProvider);
        Assert.True(result.Capabilities.WorkspaceSymbolProvider);
        Assert.True(result.Capabilities.FoldingRangeProvider);
        Assert.True(result.Capabilities.SelectionRangeProvider);
        Assert.True(result.Capabilities.DocumentFormattingProvider);
        Assert.True(result.Capabilities.DocumentRangeFormattingProvider);
        Assert.Equal(">", result.Capabilities.DocumentOnTypeFormattingProvider?.FirstTriggerCharacter);
        Assert.Equal(["\n", "}"], result.Capabilities.DocumentOnTypeFormattingProvider?.MoreTriggerCharacter);
        Assert.Equal(
            ["quickfix", "refactor.rewrite", "source.fixAll.cerneala"],
            result.Capabilities.CodeActionProvider?.CodeActionKinds);
        Assert.False(result.Capabilities.CodeActionProvider?.ResolveProvider);
        Assert.Equal(0, await client.StopAsync(timeout.Token));
    }

    [Fact]
    public async Task VisualStudioPushModeDoesNotAdvertisePullDiagnostics()
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
        await using ProtocolTestClient client = ProtocolTestClient.Start();

        InitializeResult result = await client.InitializeAsync(
            timeout.Token,
            diagnosticsMode: "push");

        Assert.Null(result.Capabilities.DiagnosticProvider);
        Assert.Equal(0, await client.StopAsync(timeout.Token));
    }

    [Fact]
    public async Task VisualStudioUsesThemeDistinctSemanticTokenClassifications()
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
        await using ProtocolTestClient client = ProtocolTestClient.Start();

        InitializeResult result = await client.InitializeAsync(timeout.Token, host: "visualStudio");

        Assert.Equal(
            ["type", "property name", "event", "namespace", "type", "keyword", "function", "parameter", "enumMember", "keyword - control", "method name"],
            result.Capabilities.SemanticTokensProvider!.Legend.TokenTypes);
        Assert.Equal(0, await client.StopAsync(timeout.Token));
    }

    [Fact]
    public async Task DiagnosticsAndCompletionAreGreenAfterIncrementalChange()
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
        await using ProtocolTestClient client = ProtocolTestClient.Start();
        await client.InitializeAsync(timeout.Token);
        await client.Rpc.NotifyWithParameterObjectAsync("initialized", new { });

        string uri = new Uri(Path.GetFullPath("View.crn")).AbsoluteUri;
        await client.Rpc.NotifyWithParameterObjectAsync(
            "textDocument/didOpen",
            new DidOpenTextDocumentParams
            {
                TextDocument = new TextDocumentItem
                {
                    Uri = uri,
                    LanguageId = "cerneala",
                    Version = 1,
                    Text = "<Window />"
                }
            });
        await client.Rpc.NotifyWithParameterObjectAsync(
            "textDocument/didChange",
            new DidChangeTextDocumentParams
            {
                TextDocument = new VersionedTextDocumentIdentifier { Uri = uri, Version = 2 },
                ContentChanges =
                [
                    new TextDocumentContentChangeEvent
                    {
                        Range = new LspRange
                        {
                            Start = new LspPosition { Line = 0, Character = 8 },
                            End = new LspPosition { Line = 0, Character = 8 }
                        },
                        Text = " Title=\"Demo\""
                    }
                ]
            });

        FullDocumentDiagnosticReport diagnostics = await client.Rpc.InvokeWithParameterObjectAsync<FullDocumentDiagnosticReport>(
                "textDocument/diagnostic",
                new DocumentDiagnosticParams
                {
                    TextDocument = new TextDocumentIdentifier { Uri = uri }
                },
                timeout.Token);
        CompletionList completion = await client.Rpc.InvokeWithParameterObjectAsync<CompletionList>(
                "textDocument/completion",
                new TextDocumentPositionParams
                {
                    TextDocument = new TextDocumentIdentifier { Uri = uri },
                    Position = new LspPosition { Line = 0, Character = 8 }
                },
                timeout.Token);

        Assert.Equal("full", diagnostics.Kind);
        Assert.Equal("2", diagnostics.ResultId);
        Assert.Equal("CERNEALAWORKSPACE001", Assert.Single(diagnostics.Items).Code);
        Assert.Contains(completion.Items, item => item.Label == "Width");
        Assert.Equal(0, await client.StopAsync(timeout.Token));
    }

    [Fact]
    public async Task PushDiagnosticsUseCurrentVersionAndRetractAfterRepairAndClose()
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
        await using ProtocolTestClient client = ProtocolTestClient.Start();
        await client.InitializeAsync(timeout.Token);
        await client.Rpc.NotifyWithParameterObjectAsync("initialized", new { });

        string uri = new Uri(Path.GetFullPath("Recovery.crn")).AbsoluteUri;
        await client.Rpc.NotifyWithParameterObjectAsync(
            "textDocument/didOpen",
            new DidOpenTextDocumentParams
            {
                TextDocument = new TextDocumentItem
                {
                    Uri = uri,
                    LanguageId = "cerneala",
                    Version = 1,
                    Text = "<"
                }
            });

        PublishDiagnosticsParams broken = await client.WaitForDiagnosticsAsync(
            notification => notification.Uri == uri && notification.Version == 1,
            timeout.Token);
        Assert.Contains(broken.Diagnostics, diagnostic => diagnostic.Code == "CERNEALAUI001");

        await client.Rpc.NotifyWithParameterObjectAsync(
            "textDocument/didChange",
            new DidChangeTextDocumentParams
            {
                TextDocument = new VersionedTextDocumentIdentifier { Uri = uri, Version = 2 },
                ContentChanges = [new TextDocumentContentChangeEvent { Text = "<Window />" }]
            });

        PublishDiagnosticsParams repaired = await client.WaitForDiagnosticsAsync(
            notification => notification.Uri == uri && notification.Version == 2,
            timeout.Token);
        Assert.DoesNotContain(repaired.Diagnostics, diagnostic => diagnostic.Code == "CERNEALAUI001");
        Assert.Equal("CERNEALAWORKSPACE001", Assert.Single(repaired.Diagnostics).Code);

        await client.Rpc.NotifyWithParameterObjectAsync(
            "textDocument/didClose",
            new DidCloseTextDocumentParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = uri }
            });
        PublishDiagnosticsParams closed = await client.WaitForDiagnosticsAsync(
            notification => notification.Uri == uri && notification.Version is null,
            timeout.Token);
        Assert.Empty(closed.Diagnostics);
        Assert.Equal(0, await client.StopAsync(timeout.Token));
    }

    [Fact]
    public async Task IncrementalPushDiagnosticsReportEmbeddedSyntaxBeforeDeferredWorkspaceLoads()
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
        await using ProtocolTestClient client = ProtocolTestClient.Start();
        await client.InitializeAsync(
            timeout.Token,
            diagnosticsMode: "push",
            deferWorkspaceLoad: true);

        string uri = new Uri(Path.GetFullPath("DeferredMotion.crn")).AbsoluteUri;
        const string validText = """
            <Window>
              <Window.Resources>
                <MotionClip Name="LoadingSequence" TargetType="Window">
                  @run $LoadingSequence as Loading;
                </MotionClip>
              </Window.Resources>
            </Window>
            """;
        await client.Rpc.NotifyWithParameterObjectAsync(
            "textDocument/didOpen",
            new DidOpenTextDocumentParams
            {
                TextDocument = new TextDocumentItem
                {
                    Uri = uri,
                    LanguageId = "cerneala",
                    Version = 1,
                    Text = validText
                }
            });
        await client.WaitForDiagnosticsAsync(
            notification => notification.Uri == uri && notification.Version == 1,
            timeout.Token);

        long started = System.Diagnostics.Stopwatch.GetTimestamp();
        await client.Rpc.NotifyWithParameterObjectAsync(
            "textDocument/didChange",
            new DidChangeTextDocumentParams
            {
                TextDocument = new VersionedTextDocumentIdentifier { Uri = uri, Version = 2 },
                ContentChanges =
                [
                    new TextDocumentContentChangeEvent
                    {
                        Text = validText.Replace(" as Loading;", " as Loading", StringComparison.Ordinal)
                    }
                ]
            });

        using CancellationTokenSource diagnosticTimeout = new(TimeSpan.FromSeconds(2));
        PublishDiagnosticsParams diagnostics = await client.WaitForDiagnosticsAsync(
            notification => notification.Uri == uri &&
                notification.Version == 2 &&
                notification.Diagnostics.Any(diagnostic => diagnostic.Code == "CERNEALAUI020"),
            diagnosticTimeout.Token);
        TimeSpan elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(started);

        Assert.Contains(diagnostics.Diagnostics, diagnostic => diagnostic.Code == "CERNEALAUI020");
        Assert.True(elapsed < TimeSpan.FromSeconds(1), $"Embedded diagnostics took {elapsed.TotalMilliseconds:F0} ms.");
        Assert.Equal(0, await client.StopAsync(timeout.Token));
    }

    [Fact]
    public async Task BuildDiagnosticsSuppressOnlyTheMatchingLspIdentity()
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
        await using ProtocolTestClient client = ProtocolTestClient.Start();
        await client.InitializeAsync(timeout.Token);

        string uri = new Uri(Path.GetFullPath("BuildDedupe.crn")).AbsoluteUri;
        await client.Rpc.NotifyWithParameterObjectAsync(
            "textDocument/didOpen",
            new DidOpenTextDocumentParams
            {
                TextDocument = new TextDocumentItem
                {
                    Uri = uri,
                    LanguageId = "cerneala",
                    Version = 1,
                    Text = "<"
                }
            });
        FullDocumentDiagnosticReport initial = await client.Rpc.InvokeWithParameterObjectAsync<FullDocumentDiagnosticReport>(
            "textDocument/diagnostic",
            new DocumentDiagnosticParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = uri }
            },
            timeout.Token);
        LspDiagnostic duplicate = Assert.Single(initial.Items, diagnostic => diagnostic.Code == "CERNEALAUI001");

        await client.Rpc.InvokeWithParameterObjectAsync<object?>(
            "cerneala/buildDiagnostics",
            new BuildDiagnosticsParams
            {
                Items =
                [
                    new BuildDiagnosticItem
                    {
                        Uri = uri,
                        Code = duplicate.Code,
                        Range = duplicate.Range,
                        Message = duplicate.Message
                    }
                ]
            },
            timeout.Token);
        FullDocumentDiagnosticReport deduplicated = await client.Rpc.InvokeWithParameterObjectAsync<FullDocumentDiagnosticReport>(
            "textDocument/diagnostic",
            new DocumentDiagnosticParams
            {
                TextDocument = new TextDocumentIdentifier { Uri = uri }
            },
            timeout.Token);

        Assert.DoesNotContain(deduplicated.Items, diagnostic => diagnostic.Code == "CERNEALAUI001");
        Assert.Equal("CERNEALAWORKSPACE001", Assert.Single(deduplicated.Items).Code);
        Assert.Equal(0, await client.StopAsync(timeout.Token));
    }

    [Fact]
    public async Task VerboseTraceNeverWritesDocumentContent()
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(10));
        using StringWriter logs = new();
        await using ProtocolTestClient client = ProtocolTestClient.Start(logs);
        await client.InitializeAsync(timeout.Token);
        await client.Rpc.NotifyWithParameterObjectAsync("$/setTrace", new { value = "verbose" });
        await client.Rpc.NotifyWithParameterObjectAsync(
            "textDocument/didOpen",
            new DidOpenTextDocumentParams
            {
                TextDocument = new TextDocumentItem
                {
                    Uri = "file:///private/View.crn",
                    LanguageId = "cerneala",
                    Version = 1,
                    Text = "private-document-secret"
                }
            });
        Assert.Equal(0, await client.StopAsync(timeout.Token));

        string output = logs.ToString();
        Assert.DoesNotContain("private-document-secret", output, StringComparison.Ordinal);
        Assert.DoesNotContain("file:///private/View.crn", output, StringComparison.Ordinal);
    }
}
