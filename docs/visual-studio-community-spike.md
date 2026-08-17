# Spike Visual Studio Community 2026 18.9

> Data verificarii: 2026-08-14
> IDE: Visual Studio Community `18.9.12105.275`, Experimental Instance `Exp`
> Decizie aprobata: host VSSDK clasic (`ILanguageClient` + MEF), nu
> `Microsoft.VisualStudio.Extensibility.LanguageServerProvider`

## Rezultat

Spike-ul a confirmat o cale completa pentru extensia Cerneala:

- VSIX clasic `net472`, incarcat lazy prin content type-ul exact `.crn`;
- server `win-x64` self-contained, inclus in VSIX si pornit din install root;
- conexiune LSP pe stdin/stdout, cu `initialize`, `initialized`, `shutdown` si
  disparitia procesului la inchiderea Visual Studio;
- TextMate disponibil imediat si semantic tokens aplicate dupa initializare;
- diagnostics push afisate o singura data in editor si Error List;
- XML-ul normal si extensiile asemanatoare nu sunt preluate.

Modelul out-of-process s-a incarcat si a executat o comanda minima, dar
`LanguageServerProvider.CreateServerConnectionAsync` nu a fost apelat pentru
documentul asociat. Acelasi rezultat a aparut cu sample-ul Rust oficial, nu doar
cu codul Cerneala. Problema corespunde issue-ului Microsoft inca deschis
[VSExtensibility #472](https://github.com/microsoft/VSExtensibility/issues/472).
Cum activarea serverului este un contract obligatoriu, fallback-ul VSSDK clasic
a fost aprobat explicit.

## Proiectele de spike

Au fost testate doua hosturi temporare:

| Host | Pachete | Rezultat |
|---|---|---|
| Out-of-process | `Microsoft.VisualStudio.Extensibility.Sdk` si `.Build` `17.14.40608` | Extensia se incarca; providerul LSP nu se activeaza in 18.9. |
| VSSDK clasic | `Microsoft.VisualStudio.LanguageServer.Client` `17.14.60`, Shell `17.14.40264`, BuildTools `18.9.820` | `ILanguageClient` se activeaza, porneste serverul si schimba mesaje LSP. |

Configuratia clasica foloseste un `ContentTypeDefinition` derivat din
`code-languageserver-preview`, un `FileExtensionToContentTypeDefinition` numai
pentru `.crn` si exportul `ILanguageClient` decorat cu acel content type.
Inregistrarea editorului atribuie extensiei `crn` editorul built-in Source Code
(Text) Editor cu prioritatea `0x64`; astfel editorul XML nu castiga asocierea.

Comanda de reproducere pentru hostul clasic a fost:

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe' `
  .\Tools\spikes\Cerneala.VisualStudio.CrnSpike\Container\Container.csproj `
  /t:Rebuild /p:Configuration=Debug

& 'C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\devenv.exe' `
  /RootSuffix Exp `
  .\Tools\spikes\Cerneala.VisualStudio.CrnSpike\Fixture\Fixture.sln
```

Codul temporar indicat de comenzi a fost sters dupa capturarea rezultatelor, asa
cum cere planul. Etapa 1 recreeaza numai hostul clasic aprobat.

## Activare documente

Probe-ul `IWpfTextViewCreationListener`, content type registry si logul procesului
au produs matricea urmatoare:

| Document | Content type/editor | Client Cerneala | Server Cerneala |
|---|---|---:|---:|
| `View.crn` | `cerneala-crn-spike`, Source Code (Text) Editor | da | da |
| `app.config` | `XML` | nu | nu |
| `foo.xml` | `XML` | nu | nu |
| `View.crn.cs` | `CSharp` | nu | nu |
| `View.cui.xml` | `XML` | nu | nu |

Pentru toate cele patru controale negative nu au aparut `client.loaded`, o
conexiune sau un proces server. Asocierea `.crn` nu schimba editorul XML.

## Lifecycle si packaging

Serverul a fost publicat `win-x64` self-contained. VSIX-ul l-a instalat sub
directorul versiunii extensiei si l-a pornit exclusiv relativ la acel install
root. Nu a fost necesar un SDK sau runtime instalat separat.

Logul unei sesiuni complete a confirmat, in ordine:

```text
client.loaded
connection.started
server.started
workspace.loaded
lifecycle.initialized
lifecycle.ready
client.initialized
document.opened
lifecycle.shutdown
```

Procesul server si procesul Experimental Instance nu mai existau dupa quit.

Prima incercare de packaging a aplatizat accidental fisierele din
`BuildHost-net472` si `BuildHost-netcore`, suprascriind dependinte din radacina
serverului. Target-ul final de spike a pastrat `RecursiveDir` pentru fiecare
fisier. Inspectia VSIX a confirmat `System.Text.Json.dll` de `1,890,088` bytes in
radacina, varianta de `777,480` bytes in `BuildHost-net472` si 23 de fisiere in
subdirectorul BuildHost, fara coliziuni.

## Colorizare si diagnostics

Inainte ca serverul sa fie ready, classifier API a raportat deja scope-urile
TextMate `markup attribute`, `markup node` si `string`. Trace-ul clientului a
raportat ulterior `textDocument/semanticTokens/full` si
`textDocument/semanticTokens/full/delta`. Grammar-ul ramane fallback, iar
semantic tokens pot rafina clasificarea dupa handshake.

Visual Studio 18.9 a expus un gap reproductibil cand serverul anunta simultan
diagnostics push si pull:

1. stdout-ul brut al serverului continea un singur
   `textDocument/publishDiagnostics`, cu un singur `CERNEALAUI002`;
2. editorul si Error List afisau initial cate o intrare;
3. dupa cererea `textDocument/diagnostic`, ambele ajungeau la doua intrari
   identice.

Un run controlat in care serverul a anuntat numai diagnostics push a ramas la
exact un `IErrorTag` si exact o intrare Error List dupa ciclul complet de refresh:

```text
error-tags count=1
error-list total=1 count=1 entries=CERNEALAUI002:...
```

Contractul hostului final este deci: Visual Studio primeste modul diagnostics
push prin initialization options, iar serverul nu anunta `diagnosticProvider`
pentru acea sesiune. Alte hosturi pot continua sa foloseasca pull. Selectia unui
singur canal apartine adaptorului protocol/host; nu se deduplica artificial in
view sau in Error List.

## Capabilitati client 18.9

Tabelul oficial Microsoft, actualizat la 2026-06-01, confirma suportul pentru
`initialize`, `initialized`, `shutdown`, `exit`, cancellation, diagnostics push,
completion, completion resolve, hover, signature help, definition, references,
rename, document/range formatting si code actions. Sursa este
[Add a Language Server Protocol extension](https://learn.microsoft.com/en-us/visualstudio/extensibility/adding-an-lsp-extension?view=visualstudio).

| Contract Cerneala | Suport 18.9 | Dovada Stage 0 |
|---|---:|---|
| Completion | da | Tabel oficial si capability handshake. |
| Completion resolve | da | Tabel oficial si resolve support in client capabilities. |
| Diagnostics | da | Push, squiggle si Error List verificate; hostul selecteaza push-only. |
| Hover | da | Tabel oficial si capability handshake. |
| Definition | da | Tabel oficial si capability handshake. |
| References | da | Tabel oficial si capability handshake. |
| Rename | da | Tabel oficial si prepare/rename capability handshake. |
| Formatting | da | Document si range formatting in tabelul oficial. |
| Semantic tokens | da | Cereri live `full` si `full/delta` in trace-ul 18.9. |
| Code actions | da | Tabel oficial si code-action capability handshake. |

`textDocument/onTypeFormatting` nu apare ca suportat in tabelul oficial si nu
este folosit ca dovada pentru formatting-ul obligatoriu. Matricea functionala
end-to-end pentru comenzile editorului ramane gate-ul Etapei 4.

## Decizia pentru implementare

Etapele urmatoare folosesc VSSDK clasic, content type MEF exact `.crn`, editorul
text built-in, server self-contained bundled si diagnostics push-only pentru
sesiunea Visual Studio. Modelul out-of-process poate fi reevaluat numai dupa ce
Microsoft rezolva activarea `LanguageServerProvider` in instalarea target si
aceeasi matrice trece fara exceptii.
