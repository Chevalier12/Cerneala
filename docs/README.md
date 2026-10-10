# Cerneala documentation

This folder holds the repository documentation, sorted by document type. Each
folder answers one question.

| Folder | What it contains | Example |
| --- | --- | --- |
| [`guides/`](guides/) | How to use a part of Cerneala, step by step. | [`guides/getting-started.md`](guides/getting-started.md) |
| [`reference/`](reference/) | Exact contracts, syntax, lists, and generated reports. | [`reference/motion-markup-syntax.md`](reference/motion-markup-syntax.md) |
| [`architecture/`](architecture/) | How a subsystem works inside: ownership, data flow, lifecycle. | [`architecture/overview.md`](architecture/overview.md) |
| [`plans/`](plans/) | Dated checklist plans and their evidence. See the [plan index](plans/README.md). | [`plans/2026-08-27-unify-aspect-runtime.md`](plans/2026-08-27-unify-aspect-runtime.md) |
| [`audits/`](audits/) | Dated audits of existing code or documents. | [`audits/2026-09-02-prism-audit.md`](audits/2026-09-02-prism-audit.md) |
| [`archive/`](archive/) | Historical or replaced documents. Each starts with a one-line banner that names the current source. | [`archive/architecture-v2.md`](archive/architecture-v2.md) |
| [`assets/`](assets/) | Images used by the documents. | [`assets/cerneala-architecture.png`](assets/cerneala-architecture.png) |

No Markdown document sits directly in `docs/` except this index.

## API documentation

Public API documentation lives only in
[`docs-site/documentation/classes/`](../docs-site/documentation/classes/), with
one page per type. Every page is listed in
[`docs-site/documentation/manifest.json`](../docs-site/documentation/manifest.json).
Do not write API pages under `docs/`.

## Naming

- New or moved files use lowercase words separated by `-`, for example
  `markup-data-bindings.md`.
- Dated documents start with the date as `YYYY-MM-DD-`, for example
  `2026-09-02-prism-audit.md`.
- Generated files end with `.generated.md`, for example
  `reference/prism-filter-reference.generated.md`. Do not edit them by hand;
  regenerate them with the tool named at the top of the file.

## Where new documents go

| You are writing | Put it at |
| --- | --- |
| A new plan | `docs/plans/YYYY-MM-DD-subject.md`, then add a row to [`plans/README.md`](plans/README.md) |
| A new audit | `docs/audits/YYYY-MM-DD-subject.md` |
| A how-to for users | `docs/guides/subject.md` |
| An exact syntax or contract | `docs/reference/subject.md` |
| How a subsystem works inside | `docs/architecture/subject.md` |
| A document that is replaced | `docs/archive/`, with the banner `> Arhivat la YYYY-MM-DD: <reason>. Sursa actuală: [<name>](<relative path>).` under the title |
