---
name: writing-api-documentation
description: Use when Codex must create or update Markdown API documentation from a source file, class, member, component, or natural-language instruction, especially WPF/Microsoft Learn-style class and member docs that must be written under docs/documentation/.
---

# Writing API Documentation

## Overview

Produce clean Markdown API documentation from either a user instruction or a specific source file. Always write the final `.md` file under the current repository's `docs/documentation/` directory unless the user explicitly asks for a different destination.

## Workflow

1. Resolve the documentation target.
   - If the user gives a file path, document the primary class/member in that file.
   - If the user gives an instruction, inspect the smallest relevant source surface needed to document it accurately.
   - Honor repository navigation rules, generated file rules, and local AGENTS.md instructions before reading code.
2. Read `references/wpf-api-doc-formula.md` before drafting WPF-style class or member documentation.
3. Extract facts from source, tests, and existing docs. Do not invent behavior. Mark unknowns briefly or omit sections that have no useful content.
4. Create `docs/documentation/` if it does not exist.
5. Write one focused Markdown file:
   - Class/component: `ClassName.md`
   - Member: `ClassName.MemberName.md`
   - Instruction-only topic: concise kebab-case slug, e.g. `layout-system.md`
6. Verify the produced file before finishing:
   - It is under `docs/documentation/`.
   - It has no placeholder markers.
   - It includes a Definition section and at least one Examples or Remarks section.
   - Public members are grouped into scan-friendly tables when documenting a class.

## Output Rules

- Prefer factual API documentation over tutorial prose.
- Keep examples minimal and compilable-looking; use real project APIs.
- Use local naming and terminology from the codebase.
- Include inherited members only when they matter for users; do not dump noise.
- Link related local docs or source files when useful.
- Do not change production code while documenting unless the user explicitly asks.

## Common Mistakes

| Mistake | Fix |
| --- | --- |
| Writing docs beside the source file | Always write to `docs/documentation/`. |
| Copying WPF sections blindly | Keep the formula, omit empty or irrelevant sections. |
| Guessing semantics from names | Check implementation, tests, samples, and existing docs. |
| Making a tutorial instead of API docs | Lead with definition, behavior, members, and examples. |
