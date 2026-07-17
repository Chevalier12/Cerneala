# AGENTS.md

## Engineering Principles (MANDATORY)

- Keep it simple.
- Use clean architecture.
- Follow DRY: avoid unnecessary duplication.
- Follow YAGNI: do not build what is not needed.
- Follow SOLID principles.
- Fix bugs in the layer that owns the broken invariant. Do not add app- or view-level workarounds for framework defects unless the user explicitly approves a temporary workaround; remove existing workarounds when implementing the root fix.

## Repository Search and Indexing (MANDATORY)

- Codex must use the RoslynIndexer CLI as the default and primary search/navigation tool for this repository.
- Invoke it with `dotnet run --no-build --project .\Tools\RoslynRepoIndexer\src\RoslynRepoIndexer.Cli\RoslynRepoIndexer.Cli.csproj -- <command>` from the repository root.
- Use the CLI for indexing, status, search, read, partial read, go-to-definition, references, symbols, and doctor whenever it can answer the task.
- Do not use `rg`, `grep`, shell directory scans, IDE search, or other search/navigation tools unless the RoslynIndexer CLI cannot answer that specific scenario.
- Valid exceptions include CLI failure, non-indexable files, binary/generated artifacts where RoslynIndexer has no useful coverage, or troubleshooting RoslynIndexer itself.
- Before inspecting or reasoning about repository structure, run `.\Tools\scripts\New-FileTree.ps1` from the repository root, then read `FileTree.md` first.
- After every code or project-file modification, re-index with `dotnet run --no-build --project .\Tools\RoslynRepoIndexer\src\RoslynRepoIndexer.Cli\RoslynRepoIndexer.Cli.csproj -- index .\Cerneala.slnx --json` so Codex stays current.
- Before editing a C# file, use `ri read <filePath>` through the CLI to read the full file. Use `ri pread` only for targeted partial reads after the full context is known.
- RoslynIndexer is read/search/index only. Do not expect shell execution or write-file capabilities from it.

## API Documentation (MANDATORY)

- The single source of truth for API documentation is `docs-site/documentation/classes/`.
- Keep the documentation in sync with every public API change so it does not become stale. Update the corresponding class/member page in the same change, and keep `docs-site/documentation/manifest.json` in sync when pages are added or renamed.
- Put API documentation only in `docs-site/documentation/classes/`; do not add API documentation under `docs/documentation/`.
- Create and update API documentation with the [`writing-api-documentation`](C:\Users\Shadow\.codex\skills\writing-api-documentation\SKILL.md) skill.

## Local Tooling

- `csi` is available as a local C# scripting/REPL command via the globally installed `dotnet-csi` tool. Use it for small C# experiments when a focused script is faster than adding throwaway project code.
- When using `csi`, prefer running a temporary `.csx` file instead of piping script text into stdin, always use a short timeout, clean up the temp file, and check/kill any stuck `csi` process after suspicious runs. Do not leave interactive `csi` sessions or long-running scripts in the background; they can leak or balloon memory badly.

## Principles

1. Ask, don't assume. If something is unclear, ask before writing a single line. Never make silent assumptions about intent, architecture, or requirements. When running unattended, pick the most reasonable interpretation, proceed, and record the assumption rather than blocking.

2. Implement the simplest solution for simple problems, better solutions for harder problems. Do not over-engineer or add flexibility that isn't needed yet. 

3. Don't touch unrelated code but please do surface bad code or design smells you discover with me so we can address them as a separate issue.

4. Flag uncertainty explicitly. If you're unsure about something, see point 1 above. If it makes sense to do so, conduct a small, localised and low-risk experiment and bring the hypothesis and results to me to discuss. Confidence without certainty causes more damage than admitting a gap.
