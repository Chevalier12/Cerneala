using Cerneala.LanguageServer.Features;
using Cerneala.LanguageServer.Logging;
using Cerneala.LanguageServer.Workspace;
using StreamJsonRpc;

namespace Cerneala.LanguageServer.Protocol;

internal sealed class LanguageServerEndpoint(IServerLogger logger) : IAsyncDisposable
{
    private readonly TaskCompletionSource<int> exitCompletion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int state;
    private CernealaWorkspace? workspace;
    private BuildDiagnosticStore? buildDiagnostics;
    private DiagnosticService? diagnosticService;
    private DiagnosticPublisher? diagnosticPublisher;
    private CompletionService? completionService;
    private NavigationService? navigationService;
    private StructureService? structureService;
    private FormattingService? formattingService;
    private JsonRpc? client;

    public Task<int> ExitTask => exitCompletion.Task;

    public void AttachClient(JsonRpc rpc) => client = rpc ?? throw new ArgumentNullException(nameof(rpc));

    [JsonRpcMethod("initialize", UseSingleObjectParameterDeserialization = true)]
    public async Task<InitializeResult> InitializeAsync(InitializeParams request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (Interlocked.CompareExchange(ref state, 1, 0) != 0)
        {
            throw new LocalRpcException("The server has already been initialized.") { ErrorCode = -32600 };
        }

        CernealaInitializationOptions? options = request.InitializationOptions;
        WorkspaceConfiguration configuration = WorkspaceConfiguration.Create(
            request.RootUri,
            options?.SolutionPath,
            options?.ActiveTargetFramework,
            options?.Configuration);
        workspace = await CernealaWorkspace.CreateAsync(configuration, logger, cancellationToken).ConfigureAwait(false);
        buildDiagnostics = new BuildDiagnosticStore();
        diagnosticService = new DiagnosticService(workspace, buildDiagnostics);
        diagnosticPublisher = new DiagnosticPublisher(diagnosticService, PublishDiagnosticsAsync, logger);
        completionService = new CompletionService(workspace);
        navigationService = new NavigationService(workspace);
        structureService = new StructureService(workspace);
        formattingService = new FormattingService(workspace);

        logger.Info("lifecycle.initialized", ("clientProcessId", request.ProcessId));
        return new InitializeResult
        {
            Capabilities = new ServerCapabilities
            {
                TextDocumentSync = new TextDocumentSyncOptions
                {
                    OpenClose = true,
                    Change = 2,
                    Save = new SaveOptions { IncludeText = false }
                },
                DiagnosticProvider = new DiagnosticOptions
                {
                    Identifier = "cerneala",
                    InterFileDependencies = true,
                    WorkspaceDiagnostics = false
                },
                CompletionProvider = new CompletionOptions
                {
                    ResolveProvider = true,
                    TriggerCharacters = ["<", " ", "=", "\"", "'", ".", "$", "@", ":", "(", ","]
                },
                SignatureHelpProvider = new SignatureHelpOptions
                {
                    TriggerCharacters = ["(", ","],
                    RetriggerCharacters = [","]
                },
                HoverProvider = true,
                DefinitionProvider = true,
                ReferencesProvider = true,
                DocumentHighlightProvider = true,
                RenameProvider = new RenameOptions { PrepareProvider = true },
                SemanticTokensProvider = new SemanticTokensOptions
                {
                    Legend = new SemanticTokensLegend
                    {
                        TokenTypes = StructureService.TokenTypes,
                        TokenModifiers = StructureService.TokenModifiers
                    },
                    Full = new SemanticTokensFullOptions { Delta = true }
                },
                DocumentSymbolProvider = true,
                WorkspaceSymbolProvider = true,
                FoldingRangeProvider = true,
                SelectionRangeProvider = true,
                DocumentFormattingProvider = true,
                DocumentRangeFormattingProvider = true,
                DocumentOnTypeFormattingProvider = new DocumentOnTypeFormattingOptions
                {
                    FirstTriggerCharacter = ">",
                    MoreTriggerCharacter = ["\n", "}"]
                },
                CodeActionProvider = new CodeActionOptions
                {
                    CodeActionKinds = ["quickfix", "refactor.rewrite", "source.fixAll.cerneala"],
                    ResolveProvider = false
                }
            },
            ServerInfo = new ServerInfo
            {
                Name = "Cerneala Language Server",
                Version = typeof(LanguageServerEndpoint).Assembly.GetName().Version?.ToString() ?? "0.0.0"
            }
        };
    }

    [JsonRpcMethod("textDocument/hover", UseSingleObjectParameterDeserialization = true)]
    public async Task<LspHover?> GetHoverAsync(
        TextDocumentPositionParams request,
        CancellationToken cancellationToken)
    {
        EnsureInitialized();
        VersionedDocumentResult<LspHover?>? result = await MeasureRequestAsync(
            "navigation",
            () => GetNavigationService().GetHoverAsync(request.TextDocument.Uri, request.Position, cancellationToken))
            .ConfigureAwait(false);
        return result?.Value ?? (result is null ? throw SupersededRequest("hover") : null);
    }

    [JsonRpcMethod("textDocument/definition", UseSingleObjectParameterDeserialization = true)]
    public async Task<LspLocation[]> GetDefinitionsAsync(
        TextDocumentPositionParams request,
        CancellationToken cancellationToken)
    {
        EnsureInitialized();
        VersionedDocumentResult<LspLocation[]>? result = await MeasureRequestAsync(
            "navigation",
            () => GetNavigationService().GetDefinitionsAsync(request.TextDocument.Uri, request.Position, cancellationToken))
            .ConfigureAwait(false);
        return result?.Value ?? throw SupersededRequest("definition");
    }

    [JsonRpcMethod("textDocument/references", UseSingleObjectParameterDeserialization = true)]
    public async Task<LspLocation[]> GetReferencesAsync(
        ReferenceParams request,
        CancellationToken cancellationToken)
    {
        EnsureInitialized();
        VersionedDocumentResult<LspLocation[]>? result = await MeasureRequestAsync(
            "navigation",
            () => GetNavigationService().GetReferencesAsync(
                request.TextDocument.Uri,
                request.Position,
                request.Context.IncludeDeclaration,
                cancellationToken))
            .ConfigureAwait(false);
        return result?.Value ?? throw SupersededRequest("references");
    }

    [JsonRpcMethod("textDocument/documentHighlight", UseSingleObjectParameterDeserialization = true)]
    public async Task<LspDocumentHighlight[]> GetDocumentHighlightsAsync(
        TextDocumentPositionParams request,
        CancellationToken cancellationToken)
    {
        EnsureInitialized();
        VersionedDocumentResult<LspDocumentHighlight[]>? result = await MeasureRequestAsync(
            "navigation",
            () => GetNavigationService().GetDocumentHighlightsAsync(
                request.TextDocument.Uri,
                request.Position,
                cancellationToken))
            .ConfigureAwait(false);
        return result?.Value ?? throw SupersededRequest("document highlight");
    }

    [JsonRpcMethod("textDocument/prepareRename", UseSingleObjectParameterDeserialization = true)]
    public async Task<LspPrepareRenameResult> PrepareRenameAsync(
        TextDocumentPositionParams request,
        CancellationToken cancellationToken)
    {
        EnsureInitialized();
        try
        {
            VersionedDocumentResult<LspPrepareRenameResult>? result = await MeasureRequestAsync(
                "navigation",
                () => GetNavigationService().PrepareRenameAsync(
                    request.TextDocument.Uri,
                    request.Position,
                    cancellationToken))
                .ConfigureAwait(false);
            return result?.Value ?? throw SupersededRequest("prepare rename");
        }
        catch (InvalidOperationException exception)
        {
            throw RenameRejected(exception.Message);
        }
    }

    [JsonRpcMethod("textDocument/rename", UseSingleObjectParameterDeserialization = true)]
    public async Task<LspWorkspaceEdit> RenameAsync(
        RenameParams request,
        CancellationToken cancellationToken)
    {
        EnsureInitialized();
        try
        {
            VersionedDocumentResult<LspWorkspaceEdit>? result = await MeasureRequestAsync(
                "navigation",
                () => GetNavigationService().RenameAsync(
                    request.TextDocument.Uri,
                    request.Position,
                    request.NewName,
                    cancellationToken))
                .ConfigureAwait(false);
            return result?.Value ?? throw SupersededRequest("rename");
        }
        catch (InvalidOperationException exception)
        {
            throw RenameRejected(exception.Message);
        }
    }

    [JsonRpcMethod("textDocument/completion", UseSingleObjectParameterDeserialization = true)]
    public async Task<CompletionList> GetCompletionsAsync(
        TextDocumentPositionParams request,
        CancellationToken cancellationToken)
    {
        EnsureInitialized();
        VersionedDocumentResult<CompletionList>? result = await MeasureRequestAsync(
            "completion",
            () => GetCompletionService().GetCompletionsAsync(
                request.TextDocument.Uri,
                request.Position,
                cancellationToken))
            .ConfigureAwait(false);
        return result?.Value ?? throw SupersededRequest("completion");
    }

    [JsonRpcMethod("completionItem/resolve", UseSingleObjectParameterDeserialization = true)]
    public async Task<LspCompletionItem> ResolveCompletionAsync(
        LspCompletionItem request,
        CancellationToken cancellationToken)
    {
        EnsureInitialized();
        VersionedDocumentResult<LspCompletionItem>? result = await MeasureRequestAsync(
            "completion",
            () => GetCompletionService().ResolveAsync(request, cancellationToken))
            .ConfigureAwait(false);
        return result?.Value ?? throw SupersededRequest("completion resolve");
    }

    [JsonRpcMethod("textDocument/signatureHelp", UseSingleObjectParameterDeserialization = true)]
    public async Task<LspSignatureHelp?> GetSignatureHelpAsync(
        TextDocumentPositionParams request,
        CancellationToken cancellationToken)
    {
        EnsureInitialized();
        VersionedDocumentResult<LspSignatureHelp?>? result = await MeasureRequestAsync(
            "completion",
            () => GetCompletionService().GetSignatureHelpAsync(
                request.TextDocument.Uri,
                request.Position,
                cancellationToken))
            .ConfigureAwait(false);
        return result?.Value ?? (result is null ? throw SupersededRequest("signature help") : null);
    }

    [JsonRpcMethod("textDocument/semanticTokens/full", UseSingleObjectParameterDeserialization = true)]
    public async Task<LspSemanticTokens> GetSemanticTokensAsync(
        SemanticTokensParams request,
        CancellationToken cancellationToken)
    {
        EnsureInitialized();
        VersionedDocumentResult<LspSemanticTokens>? result = await GetStructureService()
            .GetSemanticTokensAsync(request.TextDocument.Uri, cancellationToken)
            .ConfigureAwait(false);
        return result?.Value ?? throw SupersededRequest("semantic tokens");
    }

    [JsonRpcMethod("textDocument/semanticTokens/full/delta", UseSingleObjectParameterDeserialization = true)]
    public async Task<object> GetSemanticTokensDeltaAsync(
        SemanticTokensDeltaParams request,
        CancellationToken cancellationToken)
    {
        EnsureInitialized();
        VersionedDocumentResult<object>? result = await GetStructureService()
            .GetSemanticTokensDeltaAsync(
                request.TextDocument.Uri,
                request.PreviousResultId,
                cancellationToken)
            .ConfigureAwait(false);
        return result?.Value ?? throw SupersededRequest("semantic token delta");
    }

    [JsonRpcMethod("textDocument/documentSymbol", UseSingleObjectParameterDeserialization = true)]
    public async Task<LspDocumentSymbol[]> GetDocumentSymbolsAsync(
        DocumentSymbolParams request,
        CancellationToken cancellationToken)
    {
        EnsureInitialized();
        VersionedDocumentResult<LspDocumentSymbol[]>? result = await GetStructureService()
            .GetDocumentSymbolsAsync(request.TextDocument.Uri, cancellationToken)
            .ConfigureAwait(false);
        return result?.Value ?? throw SupersededRequest("document symbols");
    }

    [JsonRpcMethod("workspace/symbol", UseSingleObjectParameterDeserialization = true)]
    public async Task<LspSymbolInformation[]> GetWorkspaceSymbolsAsync(
        WorkspaceSymbolParams request,
        CancellationToken cancellationToken)
    {
        EnsureInitialized();
        VersionedWorkspaceResult<LspSymbolInformation[]>? result = await GetStructureService()
            .GetWorkspaceSymbolsAsync(request.Query, cancellationToken)
            .ConfigureAwait(false);
        return result?.Value ?? throw SupersededRequest("workspace symbols");
    }

    [JsonRpcMethod("textDocument/foldingRange", UseSingleObjectParameterDeserialization = true)]
    public async Task<LspFoldingRange[]> GetFoldingRangesAsync(
        FoldingRangeParams request,
        CancellationToken cancellationToken)
    {
        EnsureInitialized();
        VersionedDocumentResult<LspFoldingRange[]>? result = await GetStructureService()
            .GetFoldingRangesAsync(request.TextDocument.Uri, cancellationToken)
            .ConfigureAwait(false);
        return result?.Value ?? throw SupersededRequest("folding ranges");
    }

    [JsonRpcMethod("textDocument/selectionRange", UseSingleObjectParameterDeserialization = true)]
    public async Task<LspSelectionRange[]> GetSelectionRangesAsync(
        SelectionRangeParams request,
        CancellationToken cancellationToken)
    {
        EnsureInitialized();
        VersionedDocumentResult<LspSelectionRange[]>? result = await GetStructureService()
            .GetSelectionRangesAsync(request.TextDocument.Uri, request.Positions, cancellationToken)
            .ConfigureAwait(false);
        return result?.Value ?? throw SupersededRequest("selection ranges");
    }

    [JsonRpcMethod("textDocument/formatting", UseSingleObjectParameterDeserialization = true)]
    public async Task<LspTextEdit[]> FormatDocumentAsync(
        DocumentFormattingParams request,
        CancellationToken cancellationToken)
    {
        EnsureInitialized();
        VersionedDocumentResult<LspTextEdit[]>? result = await GetFormattingService()
            .FormatDocumentAsync(request.TextDocument.Uri, request.Options, cancellationToken)
            .ConfigureAwait(false);
        return result?.Value ?? throw SupersededRequest("document formatting");
    }

    [JsonRpcMethod("textDocument/rangeFormatting", UseSingleObjectParameterDeserialization = true)]
    public async Task<LspTextEdit[]> FormatRangeAsync(
        DocumentRangeFormattingParams request,
        CancellationToken cancellationToken)
    {
        EnsureInitialized();
        VersionedDocumentResult<LspTextEdit[]>? result = await GetFormattingService()
            .FormatRangeAsync(request.TextDocument.Uri, request.Range, request.Options, cancellationToken)
            .ConfigureAwait(false);
        return result?.Value ?? throw SupersededRequest("range formatting");
    }

    [JsonRpcMethod("textDocument/onTypeFormatting", UseSingleObjectParameterDeserialization = true)]
    public async Task<LspTextEdit[]> FormatOnTypeAsync(
        DocumentOnTypeFormattingParams request,
        CancellationToken cancellationToken)
    {
        EnsureInitialized();
        VersionedDocumentResult<LspTextEdit[]>? result = await GetFormattingService()
            .FormatOnTypeAsync(request.TextDocument.Uri, request.Position, request.Options, cancellationToken)
            .ConfigureAwait(false);
        return result?.Value ?? throw SupersededRequest("on-type formatting");
    }

    [JsonRpcMethod("textDocument/codeAction", UseSingleObjectParameterDeserialization = true)]
    public async Task<LspCodeAction[]> GetCodeActionsAsync(
        CodeActionParams request,
        CancellationToken cancellationToken)
    {
        EnsureInitialized();
        VersionedDocumentResult<LspCodeAction[]>? result = await GetFormattingService()
            .GetCodeActionsAsync(request, cancellationToken)
            .ConfigureAwait(false);
        return result?.Value ?? throw SupersededRequest("code actions");
    }

    [JsonRpcMethod("initialized", UseSingleObjectParameterDeserialization = true)]
    public void Initialized(object? request)
    {
        EnsureInitialized();
        logger.Info("lifecycle.ready");
    }

    [JsonRpcMethod("textDocument/didOpen", UseSingleObjectParameterDeserialization = true)]
    public void DidOpen(DidOpenTextDocumentParams request)
    {
        EnsureInitialized();
        ArgumentNullException.ThrowIfNull(request.TextDocument);
        bool accepted = GetWorkspace().OpenDocument(
            request.TextDocument.Uri,
            request.TextDocument.Text,
            request.TextDocument.Version);
        if (accepted)
        {
            GetDiagnosticPublisher().Schedule(request.TextDocument.Uri);
        }

        logger.Info("document.opened", ("version", request.TextDocument.Version), ("accepted", accepted));
    }

    [JsonRpcMethod("textDocument/didChange", UseSingleObjectParameterDeserialization = true)]
    public void DidChange(DidChangeTextDocumentParams request)
    {
        EnsureInitialized();
        ArgumentNullException.ThrowIfNull(request.TextDocument);
        ArgumentNullException.ThrowIfNull(request.ContentChanges);
        bool accepted = GetWorkspace().ApplyChanges(
            request.TextDocument.Uri,
            request.TextDocument.Version,
            request.ContentChanges);
        if (accepted)
        {
            GetDiagnosticPublisher().Schedule(request.TextDocument.Uri);
        }

        logger.Info("document.changed", ("version", request.TextDocument.Version), ("accepted", accepted));
    }

    [JsonRpcMethod("textDocument/didClose", UseSingleObjectParameterDeserialization = true)]
    public void DidClose(DidCloseTextDocumentParams request)
    {
        EnsureInitialized();
        GetWorkspace().CloseDocument(request.TextDocument.Uri);
        GetDiagnosticPublisher().Clear(request.TextDocument.Uri);
        GetStructureService().Clear(request.TextDocument.Uri);
        logger.Info("document.closed");
    }

    [JsonRpcMethod("textDocument/didSave", UseSingleObjectParameterDeserialization = true)]
    public async Task DidSaveAsync(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
    {
        EnsureInitialized();
        await GetWorkspace().ReloadAsync(cancellationToken).ConfigureAwait(false);
        GetDiagnosticPublisher().Schedule(request.TextDocument.Uri);
        logger.Info("document.saved");
    }

    [JsonRpcMethod("textDocument/diagnostic", UseSingleObjectParameterDeserialization = true)]
    public async Task<FullDocumentDiagnosticReport> GetDiagnosticsAsync(
        DocumentDiagnosticParams request,
        CancellationToken cancellationToken)
    {
        EnsureInitialized();
        VersionedDocumentResult<IReadOnlyList<LspDiagnostic>>? result = await MeasureRequestAsync(
            "diagnostics",
            () => GetDiagnosticService().AnalyzeAsync(request.TextDocument.Uri, cancellationToken))
            .ConfigureAwait(false);
        if (result is null)
        {
            throw new LocalRpcException("The diagnostic request was superseded by a newer document version.")
            {
                ErrorCode = -32800
            };
        }

        return new FullDocumentDiagnosticReport
        {
            ResultId = result.Version.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Items = result.Value.ToArray()
        };
    }

    [JsonRpcMethod("cerneala/buildDiagnostics", UseSingleObjectParameterDeserialization = true)]
    public void SetBuildDiagnostics(BuildDiagnosticsParams request)
    {
        EnsureInitialized();
        IReadOnlyList<string> affectedUris = GetBuildDiagnostics().Replace(request.Items);
        foreach (string uri in affectedUris)
        {
            GetDiagnosticPublisher().Schedule(uri);
        }
    }

    [JsonRpcMethod("$/setTrace", UseSingleObjectParameterDeserialization = true)]
    public void SetTrace(SetTraceParams request)
    {
        ServerTraceLevel level = request.Value switch
        {
            "off" => ServerTraceLevel.Off,
            "verbose" => ServerTraceLevel.Verbose,
            _ => ServerTraceLevel.Messages
        };
        logger.SetTraceLevel(level);
    }

    [JsonRpcMethod("shutdown")]
    public async Task<object?> ShutdownAsync()
    {
        EnsureInitialized();
        Interlocked.Exchange(ref state, 2);
        await DisposeWorkspaceAsync().ConfigureAwait(false);
        logger.Info("lifecycle.shutdown");
        return null;
    }

    public ValueTask DisposeAsync() => DisposeWorkspaceAsync();

    [JsonRpcMethod("exit")]
    public void Exit()
    {
        int exitCode = Volatile.Read(ref state) == 2 ? 0 : 1;
        logger.Info("lifecycle.exit", ("exitCode", exitCode));
        exitCompletion.TrySetResult(exitCode);
    }

    private void EnsureInitialized()
    {
        if (Volatile.Read(ref state) == 0)
        {
            throw new LocalRpcException("The server is not initialized.") { ErrorCode = -32002 };
        }
    }

    private CernealaWorkspace GetWorkspace() =>
        workspace ?? throw new LocalRpcException("The workspace is not available.") { ErrorCode = -32002 };

    private DiagnosticService GetDiagnosticService() =>
        diagnosticService ?? throw new LocalRpcException("Diagnostics are not available.") { ErrorCode = -32002 };

    private DiagnosticPublisher GetDiagnosticPublisher() =>
        diagnosticPublisher ?? throw new LocalRpcException("Diagnostics are not available.") { ErrorCode = -32002 };

    private BuildDiagnosticStore GetBuildDiagnostics() =>
        buildDiagnostics ?? throw new LocalRpcException("Diagnostics are not available.") { ErrorCode = -32002 };

    private CompletionService GetCompletionService() =>
        completionService ?? throw new LocalRpcException("Completion is not available.") { ErrorCode = -32002 };

    private NavigationService GetNavigationService() =>
        navigationService ?? throw new LocalRpcException("Navigation is not available.") { ErrorCode = -32002 };

    private StructureService GetStructureService() =>
        structureService ?? throw new LocalRpcException("Structure features are not available.") { ErrorCode = -32002 };

    private FormattingService GetFormattingService() =>
        formattingService ?? throw new LocalRpcException("Formatting is not available.") { ErrorCode = -32002 };

    private Task<T> MeasureRequestAsync<T>(string operation, Func<Task<T>> action) =>
        GetWorkspace().Telemetry.MeasureAsync(operation, action);

    private static LocalRpcException SupersededRequest(string feature) => new(
        "The " + feature + " request was superseded by a newer document version.")
    {
        ErrorCode = -32800
    };

    private static LocalRpcException RenameRejected(string message) => new(message)
    {
        ErrorCode = -32803
    };

    private Task PublishDiagnosticsAsync(PublishDiagnosticsParams notification)
    {
        JsonRpc rpc = client ?? throw new InvalidOperationException("The language client is not attached.");
        return rpc.NotifyWithParameterObjectAsync("textDocument/publishDiagnostics", notification);
    }

    private async ValueTask DisposeWorkspaceAsync()
    {
        DiagnosticPublisher? currentPublisher = Interlocked.Exchange(ref diagnosticPublisher, null);
        if (currentPublisher is not null)
        {
            await currentPublisher.DisposeAsync().ConfigureAwait(false);
        }

        diagnosticService = null;
        buildDiagnostics = null;
        completionService = null;
        navigationService = null;
        StructureService? currentStructure = Interlocked.Exchange(ref structureService, null);
        currentStructure?.Dispose();
        formattingService = null;
        CernealaWorkspace? current = Interlocked.Exchange(ref workspace, null);
        if (current is not null)
        {
            await current.DisposeAsync().ConfigureAwait(false);
        }
    }
}
