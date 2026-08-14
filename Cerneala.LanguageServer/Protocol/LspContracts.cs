using System.Text.Json.Serialization;

namespace Cerneala.LanguageServer.Protocol;

internal sealed class InitializeParams
{
    [JsonPropertyName("processId")]
    public int? ProcessId { get; init; }

    [JsonPropertyName("rootUri")]
    public string? RootUri { get; init; }

    [JsonPropertyName("capabilities")]
    public object? Capabilities { get; init; }

    [JsonPropertyName("initializationOptions")]
    public CernealaInitializationOptions? InitializationOptions { get; init; }
}

internal sealed class CernealaInitializationOptions
{
    [JsonPropertyName("solutionPath")]
    public string? SolutionPath { get; init; }

    [JsonPropertyName("activeTargetFramework")]
    public string? ActiveTargetFramework { get; init; }

    [JsonPropertyName("configuration")]
    public string? Configuration { get; init; }
}

internal sealed class InitializeResult
{
    [JsonPropertyName("capabilities")]
    public required ServerCapabilities Capabilities { get; init; }

    [JsonPropertyName("serverInfo")]
    public required ServerInfo ServerInfo { get; init; }
}

internal sealed class ServerCapabilities
{
    [JsonPropertyName("textDocumentSync")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TextDocumentSyncOptions? TextDocumentSync { get; init; }

    [JsonPropertyName("completionProvider")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CompletionOptions? CompletionProvider { get; init; }

    [JsonPropertyName("signatureHelpProvider")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SignatureHelpOptions? SignatureHelpProvider { get; init; }

    [JsonPropertyName("hoverProvider")]
    public bool HoverProvider { get; init; }

    [JsonPropertyName("definitionProvider")]
    public bool DefinitionProvider { get; init; }

    [JsonPropertyName("referencesProvider")]
    public bool ReferencesProvider { get; init; }

    [JsonPropertyName("documentHighlightProvider")]
    public bool DocumentHighlightProvider { get; init; }

    [JsonPropertyName("renameProvider")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RenameOptions? RenameProvider { get; init; }

    [JsonPropertyName("diagnosticProvider")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DiagnosticOptions? DiagnosticProvider { get; init; }

    [JsonPropertyName("semanticTokensProvider")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SemanticTokensOptions? SemanticTokensProvider { get; init; }

    [JsonPropertyName("documentSymbolProvider")]
    public bool DocumentSymbolProvider { get; init; }

    [JsonPropertyName("workspaceSymbolProvider")]
    public bool WorkspaceSymbolProvider { get; init; }

    [JsonPropertyName("foldingRangeProvider")]
    public bool FoldingRangeProvider { get; init; }

    [JsonPropertyName("selectionRangeProvider")]
    public bool SelectionRangeProvider { get; init; }

    [JsonPropertyName("documentFormattingProvider")]
    public bool DocumentFormattingProvider { get; init; }

    [JsonPropertyName("documentRangeFormattingProvider")]
    public bool DocumentRangeFormattingProvider { get; init; }

    [JsonPropertyName("documentOnTypeFormattingProvider")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public DocumentOnTypeFormattingOptions? DocumentOnTypeFormattingProvider { get; init; }

    [JsonPropertyName("codeActionProvider")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CodeActionOptions? CodeActionProvider { get; init; }
}

internal sealed class DocumentOnTypeFormattingOptions
{
    [JsonPropertyName("firstTriggerCharacter")]
    public required string FirstTriggerCharacter { get; init; }

    [JsonPropertyName("moreTriggerCharacter")]
    public required string[] MoreTriggerCharacter { get; init; }
}

internal sealed class CodeActionOptions
{
    [JsonPropertyName("codeActionKinds")]
    public required string[] CodeActionKinds { get; init; }

    [JsonPropertyName("resolveProvider")]
    public bool ResolveProvider { get; init; }
}

internal sealed class SemanticTokensOptions
{
    [JsonPropertyName("legend")]
    public required SemanticTokensLegend Legend { get; init; }

    [JsonPropertyName("full")]
    public required SemanticTokensFullOptions Full { get; init; }
}

internal sealed class SemanticTokensLegend
{
    [JsonPropertyName("tokenTypes")]
    public required string[] TokenTypes { get; init; }

    [JsonPropertyName("tokenModifiers")]
    public required string[] TokenModifiers { get; init; }
}

internal sealed class SemanticTokensFullOptions
{
    [JsonPropertyName("delta")]
    public bool Delta { get; init; }
}

internal sealed class RenameOptions
{
    [JsonPropertyName("prepareProvider")]
    public bool PrepareProvider { get; init; }
}

internal sealed class CompletionOptions
{
    [JsonPropertyName("resolveProvider")]
    public bool ResolveProvider { get; init; }

    [JsonPropertyName("triggerCharacters")]
    public required string[] TriggerCharacters { get; init; }
}

internal sealed class SignatureHelpOptions
{
    [JsonPropertyName("triggerCharacters")]
    public required string[] TriggerCharacters { get; init; }

    [JsonPropertyName("retriggerCharacters")]
    public required string[] RetriggerCharacters { get; init; }
}

internal sealed class DiagnosticOptions
{
    [JsonPropertyName("identifier")]
    public required string Identifier { get; init; }

    [JsonPropertyName("interFileDependencies")]
    public bool InterFileDependencies { get; init; }

    [JsonPropertyName("workspaceDiagnostics")]
    public bool WorkspaceDiagnostics { get; init; }
}

internal sealed class TextDocumentSyncOptions
{
    [JsonPropertyName("openClose")]
    public bool OpenClose { get; init; }

    [JsonPropertyName("change")]
    public int Change { get; init; }

    [JsonPropertyName("save")]
    public required SaveOptions Save { get; init; }
}

internal sealed class SaveOptions
{
    [JsonPropertyName("includeText")]
    public bool IncludeText { get; init; }
}

internal sealed class ServerInfo
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("version")]
    public required string Version { get; init; }
}

internal sealed class DidOpenTextDocumentParams
{
    [JsonPropertyName("textDocument")]
    public required TextDocumentItem TextDocument { get; init; }
}

internal sealed class TextDocumentItem
{
    [JsonPropertyName("uri")]
    public required string Uri { get; init; }

    [JsonPropertyName("languageId")]
    public required string LanguageId { get; init; }

    [JsonPropertyName("version")]
    public int Version { get; init; }

    [JsonPropertyName("text")]
    public required string Text { get; init; }
}

internal sealed class DidChangeTextDocumentParams
{
    [JsonPropertyName("textDocument")]
    public required VersionedTextDocumentIdentifier TextDocument { get; init; }

    [JsonPropertyName("contentChanges")]
    public required TextDocumentContentChangeEvent[] ContentChanges { get; init; }
}

internal sealed class DidCloseTextDocumentParams
{
    [JsonPropertyName("textDocument")]
    public required TextDocumentIdentifier TextDocument { get; init; }
}

internal sealed class DidSaveTextDocumentParams
{
    [JsonPropertyName("textDocument")]
    public required TextDocumentIdentifier TextDocument { get; init; }
}

internal sealed class VersionedTextDocumentIdentifier
{
    [JsonPropertyName("uri")]
    public required string Uri { get; init; }

    [JsonPropertyName("version")]
    public int Version { get; init; }
}

internal sealed class TextDocumentIdentifier
{
    [JsonPropertyName("uri")]
    public required string Uri { get; init; }
}

internal sealed class TextDocumentContentChangeEvent
{
    [JsonPropertyName("range")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public LspRange? Range { get; init; }

    [JsonPropertyName("text")]
    public required string Text { get; init; }
}

internal sealed class LspRange
{
    [JsonPropertyName("start")]
    public required LspPosition Start { get; init; }

    [JsonPropertyName("end")]
    public required LspPosition End { get; init; }
}

internal sealed class LspPosition
{
    [JsonPropertyName("line")]
    public int Line { get; init; }

    [JsonPropertyName("character")]
    public int Character { get; init; }
}

internal sealed class TextDocumentPositionParams
{
    [JsonPropertyName("textDocument")]
    public required TextDocumentIdentifier TextDocument { get; init; }

    [JsonPropertyName("position")]
    public required LspPosition Position { get; init; }
}

internal sealed class SemanticTokensParams
{
    [JsonPropertyName("textDocument")]
    public required TextDocumentIdentifier TextDocument { get; init; }
}

internal sealed class SemanticTokensDeltaParams
{
    [JsonPropertyName("textDocument")]
    public required TextDocumentIdentifier TextDocument { get; init; }

    [JsonPropertyName("previousResultId")]
    public required string PreviousResultId { get; init; }
}

internal sealed class LspSemanticTokens
{
    [JsonPropertyName("resultId")]
    public required string ResultId { get; init; }

    [JsonPropertyName("data")]
    public required int[] Data { get; init; }
}

internal sealed class LspSemanticTokensDelta
{
    [JsonPropertyName("resultId")]
    public required string ResultId { get; init; }

    [JsonPropertyName("edits")]
    public required LspSemanticTokensEdit[] Edits { get; init; }
}

internal sealed class LspSemanticTokensEdit
{
    [JsonPropertyName("start")]
    public int Start { get; init; }

    [JsonPropertyName("deleteCount")]
    public int DeleteCount { get; init; }

    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int[]? Data { get; init; }
}

internal sealed class DocumentSymbolParams
{
    [JsonPropertyName("textDocument")]
    public required TextDocumentIdentifier TextDocument { get; init; }
}

internal sealed class LspDocumentSymbol
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("detail")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Detail { get; init; }

    [JsonPropertyName("kind")]
    public int Kind { get; init; }

    [JsonPropertyName("range")]
    public required LspRange Range { get; init; }

    [JsonPropertyName("selectionRange")]
    public required LspRange SelectionRange { get; init; }

    [JsonPropertyName("children")]
    public required LspDocumentSymbol[] Children { get; init; }
}

internal sealed class WorkspaceSymbolParams
{
    [JsonPropertyName("query")]
    public string Query { get; init; } = string.Empty;
}

internal sealed class LspSymbolInformation
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("kind")]
    public int Kind { get; init; }

    [JsonPropertyName("location")]
    public required LspLocation Location { get; init; }

    [JsonPropertyName("containerName")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ContainerName { get; init; }
}

internal sealed class FoldingRangeParams
{
    [JsonPropertyName("textDocument")]
    public required TextDocumentIdentifier TextDocument { get; init; }
}

internal sealed class LspFoldingRange
{
    [JsonPropertyName("startLine")]
    public int StartLine { get; init; }

    [JsonPropertyName("startCharacter")]
    public int StartCharacter { get; init; }

    [JsonPropertyName("endLine")]
    public int EndLine { get; init; }

    [JsonPropertyName("endCharacter")]
    public int EndCharacter { get; init; }

    [JsonPropertyName("kind")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Kind { get; init; }
}

internal sealed class SelectionRangeParams
{
    [JsonPropertyName("textDocument")]
    public required TextDocumentIdentifier TextDocument { get; init; }

    [JsonPropertyName("positions")]
    public required LspPosition[] Positions { get; init; }
}

internal sealed class LspSelectionRange
{
    [JsonPropertyName("range")]
    public required LspRange Range { get; init; }

    [JsonPropertyName("parent")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public LspSelectionRange? Parent { get; init; }
}

internal sealed class FormattingOptions
{
    [JsonPropertyName("tabSize")]
    public int TabSize { get; init; }

    [JsonPropertyName("insertSpaces")]
    public bool InsertSpaces { get; init; }
}

internal sealed class DocumentFormattingParams
{
    [JsonPropertyName("textDocument")]
    public required TextDocumentIdentifier TextDocument { get; init; }

    [JsonPropertyName("options")]
    public required FormattingOptions Options { get; init; }
}

internal sealed class DocumentRangeFormattingParams
{
    [JsonPropertyName("textDocument")]
    public required TextDocumentIdentifier TextDocument { get; init; }

    [JsonPropertyName("range")]
    public required LspRange Range { get; init; }

    [JsonPropertyName("options")]
    public required FormattingOptions Options { get; init; }
}

internal sealed class DocumentOnTypeFormattingParams
{
    [JsonPropertyName("textDocument")]
    public required TextDocumentIdentifier TextDocument { get; init; }

    [JsonPropertyName("position")]
    public required LspPosition Position { get; init; }

    [JsonPropertyName("ch")]
    public required string Character { get; init; }

    [JsonPropertyName("options")]
    public required FormattingOptions Options { get; init; }
}

internal sealed class CodeActionParams
{
    [JsonPropertyName("textDocument")]
    public required TextDocumentIdentifier TextDocument { get; init; }

    [JsonPropertyName("range")]
    public required LspRange Range { get; init; }

    [JsonPropertyName("context")]
    public required CodeActionContext Context { get; init; }
}

internal sealed class CodeActionContext
{
    [JsonPropertyName("diagnostics")]
    public required LspDiagnostic[] Diagnostics { get; init; }

    [JsonPropertyName("only")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string[]? Only { get; init; }
}

internal sealed class LspCodeAction
{
    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("kind")]
    public required string Kind { get; init; }

    [JsonPropertyName("diagnostics")]
    public required LspDiagnostic[] Diagnostics { get; init; }

    [JsonPropertyName("isPreferred")]
    public bool IsPreferred { get; init; }

    [JsonPropertyName("edit")]
    public required LspWorkspaceEdit Edit { get; init; }
}

internal sealed class ReferenceParams
{
    [JsonPropertyName("textDocument")]
    public required TextDocumentIdentifier TextDocument { get; init; }

    [JsonPropertyName("position")]
    public required LspPosition Position { get; init; }

    [JsonPropertyName("context")]
    public required ReferenceContext Context { get; init; }
}

internal sealed class ReferenceContext
{
    [JsonPropertyName("includeDeclaration")]
    public bool IncludeDeclaration { get; init; }
}

internal sealed class RenameParams
{
    [JsonPropertyName("textDocument")]
    public required TextDocumentIdentifier TextDocument { get; init; }

    [JsonPropertyName("position")]
    public required LspPosition Position { get; init; }

    [JsonPropertyName("newName")]
    public required string NewName { get; init; }
}

internal sealed class LspHover
{
    [JsonPropertyName("contents")]
    public required MarkupContent Contents { get; init; }

    [JsonPropertyName("range")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public LspRange? Range { get; init; }
}

internal sealed class LspLocation
{
    [JsonPropertyName("uri")]
    public required string Uri { get; init; }

    [JsonPropertyName("range")]
    public required LspRange Range { get; init; }
}

internal sealed class LspDocumentHighlight
{
    [JsonPropertyName("range")]
    public required LspRange Range { get; init; }

    [JsonPropertyName("kind")]
    public int Kind { get; init; }
}

internal sealed class LspPrepareRenameResult
{
    [JsonPropertyName("range")]
    public required LspRange Range { get; init; }

    [JsonPropertyName("placeholder")]
    public required string Placeholder { get; init; }
}

internal sealed class LspWorkspaceEdit
{
    [JsonPropertyName("changes")]
    public required Dictionary<string, LspTextEdit[]> Changes { get; init; }
}

internal sealed class CompletionList
{
    [JsonPropertyName("isIncomplete")]
    public bool IsIncomplete { get; init; }

    [JsonPropertyName("items")]
    public required LspCompletionItem[] Items { get; init; }
}

internal sealed class LspCompletionItem
{
    [JsonPropertyName("label")]
    public required string Label { get; init; }

    [JsonPropertyName("kind")]
    public int Kind { get; init; }

    [JsonPropertyName("detail")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Detail { get; init; }

    [JsonPropertyName("documentation")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MarkupContent? Documentation { get; init; }

    [JsonPropertyName("sortText")]
    public required string SortText { get; init; }

    [JsonPropertyName("filterText")]
    public required string FilterText { get; init; }

    [JsonPropertyName("textEdit")]
    public required LspTextEdit TextEdit { get; init; }

    [JsonPropertyName("deprecated")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Deprecated { get; init; }

    [JsonPropertyName("tags")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int[]? Tags { get; init; }

    [JsonPropertyName("data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CompletionItemData? Data { get; init; }
}

internal sealed class CompletionItemData
{
    [JsonPropertyName("uri")]
    public required string Uri { get; init; }

    [JsonPropertyName("version")]
    public long Version { get; init; }

    [JsonPropertyName("type")]
    public required string TypeMetadataName { get; init; }

    [JsonPropertyName("member")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MemberName { get; init; }
}

internal sealed class LspTextEdit
{
    [JsonPropertyName("range")]
    public required LspRange Range { get; init; }

    [JsonPropertyName("newText")]
    public required string NewText { get; init; }
}

internal sealed class MarkupContent
{
    [JsonPropertyName("kind")]
    public string Kind { get; init; } = "markdown";

    [JsonPropertyName("value")]
    public required string Value { get; init; }
}

internal sealed class LspSignatureHelp
{
    [JsonPropertyName("signatures")]
    public required LspSignatureInformation[] Signatures { get; init; }

    [JsonPropertyName("activeSignature")]
    public int ActiveSignature { get; init; }

    [JsonPropertyName("activeParameter")]
    public int ActiveParameter { get; init; }
}

internal sealed class LspSignatureInformation
{
    [JsonPropertyName("label")]
    public required string Label { get; init; }

    [JsonPropertyName("documentation")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MarkupContent? Documentation { get; init; }

    [JsonPropertyName("parameters")]
    public required LspParameterInformation[] Parameters { get; init; }
}

internal sealed class LspParameterInformation
{
    [JsonPropertyName("label")]
    public required string Label { get; init; }

    [JsonPropertyName("documentation")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MarkupContent? Documentation { get; init; }
}

internal sealed class DocumentDiagnosticParams
{
    [JsonPropertyName("textDocument")]
    public required TextDocumentIdentifier TextDocument { get; init; }
}

internal sealed class FullDocumentDiagnosticReport
{
    [JsonPropertyName("kind")]
    public string Kind { get; init; } = "full";

    [JsonPropertyName("resultId")]
    public required string ResultId { get; init; }

    [JsonPropertyName("items")]
    public required LspDiagnostic[] Items { get; init; }
}

internal sealed class PublishDiagnosticsParams
{
    [JsonPropertyName("uri")]
    public required string Uri { get; init; }

    [JsonPropertyName("version")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? Version { get; init; }

    [JsonPropertyName("diagnostics")]
    public required LspDiagnostic[] Diagnostics { get; init; }
}

internal sealed class LspDiagnostic
{
    [JsonPropertyName("range")]
    public required LspRange Range { get; init; }

    [JsonPropertyName("severity")]
    public int Severity { get; init; }

    [JsonPropertyName("code")]
    public required string Code { get; init; }

    [JsonPropertyName("source")]
    public string Source { get; init; } = "cerneala";

    [JsonPropertyName("message")]
    public required string Message { get; init; }
}

internal sealed class BuildDiagnosticsParams
{
    [JsonPropertyName("items")]
    public required BuildDiagnosticItem[] Items { get; init; }
}

internal sealed class BuildDiagnosticItem
{
    [JsonPropertyName("uri")]
    public required string Uri { get; init; }

    [JsonPropertyName("code")]
    public required string Code { get; init; }

    [JsonPropertyName("range")]
    public required LspRange Range { get; init; }

    [JsonPropertyName("message")]
    public required string Message { get; init; }
}

internal sealed class SetTraceParams
{
    [JsonPropertyName("value")]
    public required string Value { get; init; }
}
