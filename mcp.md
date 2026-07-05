## MCP server for Roslyn Indexer

- [ ] Implement an MCP server as a separate project, for example `src/Ri.Mcp`.
- [ ] The MCP server must be a thin adapter over the existing Roslyn Indexer core.
- [ ] Do not duplicate indexing, search, parsing, storage, ranking, or Roslyn logic inside the MCP project.
- [ ] Reuse the same services used by the CLI commands.
- [ ] The CLI and MCP server must return the same core result DTOs.
- [ ] The MCP server must not use AI, embeddings, vector DBs, LLMs, external search engines, or network calls for repo analysis.
- [ ] Prefer stdio transport first, because it is the simplest local integration path for coding agents.
- [ ] Add HTTP transport only later if explicitly needed.
- [ ] Use the official MCP C# SDK unless there is a strong reason not to.
- [ ] Package the MCP server as a runnable .NET console app.
- [ ] Add `ri-mcp --version`.
- [ ] Add `ri-mcp --help`.
- [ ] Add tests for tool registration.
- [ ] Add tests for tool input validation.
- [ ] Add tests that every MCP tool calls the same application service as the matching CLI command.
- [ ] Add tests that MCP outputs match CLI JSON contracts where applicable.

## MCP tools

- [ ] Expose MCP tool `roslyn_doctor`.
  - Inputs:
    - `repoRoot?: string`
  - Calls the same logic as `ri doctor --json`.
  - Returns diagnostics for repo discovery, SDK/MSBuild/Roslyn availability, solution/project loading, skipped files, and index health.

- [ ] Expose MCP tool `roslyn_index`.
  - Inputs:
    - `repoRoot?: string`
    - `force?: boolean`
  - Calls the same logic as `ri index --json`.
  - Returns indexed file/project/symbol counts, elapsed time, warnings, and errors.

- [ ] Expose MCP tool `roslyn_status`.
  - Inputs:
    - `repoRoot?: string`
  - Calls the same logic as `ri status --json`.
  - Returns index existence, staleness, schema version, last indexed time, counts, and skipped files.

- [ ] Expose MCP tool `roslyn_search`.
  - Inputs:
    - `repoRoot?: string`
    - `query: string`
    - `mode?: "all" | "symbol" | "text" | "path"`
    - `fromFile?: string`
    - `fromProject?: string`
    - `includeTests?: boolean`
    - `excludeTests?: boolean`
    - `includeGenerated?: boolean`
    - `maxResults?: number`
  - Calls the same logic as `ri search --json`.
  - Returns ranked results with `filePath`, line/column range, kind, symbolName, score, matchReason, snippet, and `suggestedReadCommand`.

- [ ] Expose MCP tool `roslyn_read`.
  - Inputs:
    - `repoRoot?: string`
    - `filePath: string`
    - `maxBytes?: number`
  - Calls the same logic as `ri read <filePath> --json`.
  - Returns the entire text content of one repository file.
  - Rejects files outside repo root, binary files, directories, missing files, and oversized files.

- [ ] Expose MCP tool `roslyn_pread`.
  - Inputs:
    - `repoRoot?: string`
    - `filePath: string`
    - `range?: string`
    - `around?: number`
    - `context?: number`
    - `maxBytes?: number`
  - Calls the same logic as `ri pread <filePath> --json`.
  - Returns a partial text slice from one repository file.

- [ ] Expose MCP tool `roslyn_goto`.
  - Inputs:
    - `repoRoot?: string`
    - `symbol: string`
    - `fromFile?: string`
    - `maxResults?: number`
  - Calls the same logic as `ri goto --json`.
  - Returns definitions/declarations for the symbol.

- [ ] Expose MCP tool `roslyn_refs`.
  - Inputs:
    - `repoRoot?: string`
    - `symbol: string`
    - `exact?: boolean`
    - `fromFile?: string`
    - `maxResults?: number`
    - `timeoutMs?: number`
  - Calls the same logic as `ri refs --json`.
  - Uses approximate indexed references by default.
  - Uses Roslyn exact reference search only when `exact` is true.

- [ ] Expose MCP tool `roslyn_suggest`.
  - Inputs:
    - `repoRoot?: string`
    - `question: string`
    - `maxSuggestions?: number`
  - Calls the same logic as `ri suggest --json`.
  - Returns deterministic suggested search/goto/refs/read commands for AI agents.

## MCP output contracts

- [ ] Every MCP tool must return structured JSON-compatible output.
- [ ] Every MCP tool result must include:
  - `success`
  - `tool`
  - `repoRoot`
  - `elapsedMs`
  - `warnings`
  - `errors`
- [ ] MCP errors must be actionable for the LLM.
- [ ] Validation errors must explain how the model can retry with corrected arguments.
- [ ] Do not return stack traces by default.
- [ ] Include stack traces only behind an explicit debug option.
- [ ] Tool descriptions must be concise and accurate.
- [ ] Tool descriptions must explain when to use `roslyn_read` versus `roslyn_pread`.
- [ ] Tool descriptions must tell the model to prefer full-file `roslyn_read` before editing.
- [ ] Tool list order must be deterministic.

## MCP safety and repo boundary

- [ ] The MCP server must only operate inside the selected repository root.
- [ ] Reject path traversal attempts.
- [ ] Reject absolute paths outside the repository root.
- [ ] Do not expose arbitrary shell execution.
- [ ] Do not expose arbitrary file write operations.
- [ ] Do not modify source files through MCP tools.
- [ ] MCP tools are read/index/search only.
- [ ] Index writes are allowed only inside the index storage directory.
- [ ] Do not send repository content over the network.
- [ ] Do not log full file contents by default.
- [ ] Redact or truncate large content in logs.

