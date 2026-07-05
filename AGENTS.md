# AGENTS.md

## Engineering Principles (MANDATORY)

- Keep it simple.
- Use clean architecture.
- Follow DRY: avoid unnecessary duplication.
- Follow YAGNI: do not build what is not needed.
- Follow SOLID principles.

## Repository Search and Indexing (MANDATORY)

- Codex must use RoslynIndexer as the default and primary search/navigation tool for this repository.
- Use RoslynIndexer tools for search, status, read, partial read, go-to-definition, references, suggestions, doctor, and indexing whenever they can answer the task.
- Do not use `rg`, `grep`, shell directory scans, IDE search, or other search/navigation tools unless RoslynIndexer cannot help with that specific scenario.
- Valid exceptions include missing MCP availability, RoslynIndexer failure, non-indexable files, binary/generated artifacts where RoslynIndexer has no useful coverage, or troubleshooting RoslynIndexer itself.
- Before inspecting or reasoning about repository structure, run `.\Tools\scripts\New-FileTree.ps1` from the repository root, then read `FileTree.md` first.
- After every code or project-file modification, re-index `Cerneala.slnx` with RoslynIndexer so Codex stays current.
- For Codex/MCP indexing, prefer C#-only indexing unless the task explicitly needs non-C# text search.
- Before editing a C# file, prefer `roslyn_read` to read the full file. Use `roslyn_pread` only for targeted partial reads.
- RoslynIndexer is read/search/index only. Do not add or expect shell execution or write-file capabilities through MCP.

## Principles

1. Ask, don't assume. If something is unclear, ask before writing a single line. Never make silent assumptions about intent, architecture, or requirements. When running unattended, pick the most reasonable interpretation, proceed, and record the assumption rather than blocking.

2. Implement the simplest solution for simple problems, better solutions for harder problems. Do not over-engineer or add flexibility that isn't needed yet. 

3. Don't touch unrelated code but please do surface bad code or design smells you discover with me so we can address them as a separate issue.

4. Flag uncertainty explicitly. If you're unsure about something, see point 1 above. If it makes sense to do so, conduct a small, localised and low-risk experiment and bring the hypothesis and results to me to discuss. Confidence without certainty causes more damage than admitting a gap.
