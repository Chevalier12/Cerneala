# Timbre decoding stage 0: selecție, corpus și contract

Data: 2026-10-07. Plan: `docs/plans/2026-10-03-timbre-decoding-streaming.md`, etapa 0. Contract canonic: index §1.1 și `../2026-10-07-timbre-core-stage0/stage0-contract-and-gates.md`. Host: Windows 11 Pro 10.0.26200, .NET SDK 10.0.400, runtime 8.0 (net8.0).

## 1. Matricea aprobată, formalizată

| Format | Acceptat | Exclus (eroare) |
| --- | --- | --- |
| WAV | RIFF little-endian; PCM 8 (unsigned)/16/24/32-bit și IEEE float32; `WAVE_FORMAT_EXTENSIBLE` cu subformat PCM/float; mono/stereo; 8–192 kHz | RIFX, RF64/BW64, alte format tags (ADPCM etc.), float64, >2 canale, rate în afara 8–192 kHz → `UnsupportedFormat`; date trunchiate, fmt/data lipsă, frame parțial → `InvalidData` |
| MP3 | MPEG-1/2/2.5 Layer III, CBR și VBR, mono/stereo; ID3v2 la început, ID3v1/APEv2 la final; tag Xing/Info (LAME/Lavc) → lungime și trimming gapless (encoder delay + 529, padding); fără tag: fără trimming, lungime necunoscută | Layer I/II, free-format, schimbarea versiunii/ratei/canalelor în flux → `UnsupportedFormat`; sync pierdut în flux, frame final trunchiat → `InvalidData` |
| Ogg Vorbis | un singur stream logic, mono/stereo, 8–192 kHz, end trimming prin granule final | chained, multiplexed, >2 canale → `UnsupportedFormat`; CRC greșit, secvență de pagini ruptă, pagină/pachet trunchiat → `InvalidData` |
| Ogg Opus | un singur stream logic, channel mapping family 0 (mono/stereo), pre-skip, end trimming, output gain; ieșire 48 kHz | mapping family ≠ 0 (surround), chained/multiplexed → `UnsupportedFormat`; aceleași erori de container → `InvalidData` |

Comun: formatul se detectează după conținut, nu după extensie; sursă = fișier local (relativ la `BaseDirectory ?? AppContext.BaseDirectory`) sau factory de stream seekable; HTTP/non-seekable excluse (core). Domeniul de ieșire rămâne cel din core: stereo float32 48 kHz, mono copiat pe ambele canale, fără downmix. Un decoder nedisponibil (assembly lipsă la runtime) se raportează `UnsupportedFormat` cu excepția interioară, nu `InvalidData`.

## 2. Corpus

Generat determinist de `tests/Fixtures/TimbreCorpusGenerator` (task-local, nu e în `Cerneala.slnx`, nu e dependență runtime):

```powershell
dotnet run --project tests/Fixtures/TimbreCorpusGenerator -c Release -- tests/Cerneala.Tests.Timbre/Corpus
```

`tests/Cerneala.Tests.Timbre/Corpus/corpus-manifest.json` înregistrează pentru fiecare fișier SHA-256, dimensiune, encoder și parametri. Encodere: LAME 3.100 (`libmp3lame.64.dll` din NAudio.Lame 2.1.0, SHA-256 `71dba147…bce3`, LGPL — numai unealtă locală), OggVorbisEncoder 1.2.2 (MIT), encoderul managed Concentus 2.2.2 (BSD-3-Clause), writer Ogg propriu. Două generări consecutive ale configurației finale produc manifest identic (verificat prin hash).

Oracle independent de decoder: `CorpusSignal` (comun generatorului și testelor) — canal 0 chirp 200→1200 Hz amplitudine 0.5, canal 1 chirp 1500→400 Hz amplitudine 0.3, periodic (2 s pentru fișiere scurte, 10 s pentru cele lungi), deci fiecare poziție dintr-o perioadă e unică (seek/lag), iar canalele se disting (ordine L/R). WAV-urile pozitive se sintetizează în teste (cuantizare exactă).

Două proprietăți ale encoderelor, stabilite din container, nu din PCM decodat:
- OggVorbisEncoder 1.2.2 începe numărarea granule-urilor după prima jumătate de bloc: fluxul începe `leadingFramesDropped` frame-uri în semnal (256–1024 după rată, în manifest); ambele căi NVorbis (containerul NVorbis și cel Timbre) dau aceeași lungime = granule final. Oracle-ul se decalează cu această valoare. Rata 22.05 kHz stereo și unele calități eșuează în encoder (excepții interne); s-au ales configurațiile care funcționează.
- LAME omite tag-ul Info când primul frame e prea mic (MPEG-2.5 8 kHz 16 kbps); câmpul `gapless` din manifest o marchează. LAME re-eșantiona silențios la rate mici (48 kHz→24 kHz la 64 kbps stereo) până la fixarea `OutputSampleRate`; ratele din antete sunt verificate egale cu numele.

| Grup | Fișiere | Scop |
| --- | --- | --- |
| MP3 pozitiv | `mp3-mpeg1-44100-stereo-cbr128`, `mp3-mpeg1-48000-mono-cbr64`, `mp3-mpeg1-32000-stereo-vbr`, `mp3-mpeg2-22050-mono-vbr`, `mp3-mpeg2-16000-stereo-cbr48`, `mp3-mpeg25-8000-mono-cbr16` (fără tag), `mp3-long-22050-mono-cbr64` (300 s), `mp3-reservoir-wrap-48000-stereo-cbr64` (55 s, vezi §4), `mp3-no-info-tag-44100-stereo`, `mp3-id3v2-id3v1-44100-stereo` | versiuni/rate/CBR/VBR, gapless prezent/absent, tag-uri, lung, regresie NLayer |
| MP3 negativ | `mp3-truncated`, `mp3-corrupt-sync`, `mpeg-layer2-44100-stereo.mp2` | trunchiat, sync pierdut, variantă exclusă |
| Vorbis pozitiv | 8k/16k/22.05k mono, 44.1k/48k/96k/192k stereo, `vorbis-long-22050-mono-q2` (300 s) | rate 8–192 kHz, lung |
| Vorbis negativ | `vorbis-truncated`, `vorbis-corrupt-crc`, `ogg-chained-vorbis`, `ogg-multiplexed-vorbis-opus`, `ogg-flac` | container corupt/trunchiat, chained/multiplexed, codec Ogg nesuportat |
| Opus pozitiv | `opus-stereo-48000-96k`, `opus-mono-16000-24k`, `opus-stereo-preskip-trim` (pre-skip 312+3840, 1.2345 s), `opus-long-mono-32k` (300 s) | pre-skip/end trim, lung |
| Opus negativ | `opus-truncated`, `opus-surround-6ch` | trunchiat, surround |
| WAV | sintetizat în teste: 8/16/24/32/f32 × 8k–192k × mono/stereo, extensible, RIFX/RF64, ADPCM, 3 canale, trunchiat, fără data | matrice și negative |

Conținut versus extensie: testele copiază fixture-uri sub extensii greșite (MP3 ca `.wav`, Ogg ca `.mp3`, WAV fără extensie) și text/fișier gol cu extensie audio.

## 3. Politica de loading (core, neschimbată)

`SoundLoading.Auto/Preload/Streaming` (implicit pe `SoundClip`), `SoundRuntime.PrepareAsync` și bugetele `AutoPreloadMaxBytes` 1 MiB, `MaxPreloadBytes` 16 MiB, `MaxCacheBytes` 64 MiB sunt deja publice și aplicate de core. Adaptoarele raportează `LengthFrames` canonic (48 kHz) când e cunoscut la deschidere — WAV din antet, MP3 din tag-ul Xing/Info/VBRI, Ogg din granule-ul ultimei pagini — altfel `null`, iar Auto alege streaming. Nicio politică nouă de loading.

## 4. Candidați și probe

Probe izolate în `Tools/TimbreDecoderProbe` (în afara glob-urilor de producție), fiecare caz într-un proces separat cu timeout 120 s (`Invoke-DecoderProbe.ps1`); rezultate brute `probe-results.jsonl`, tabel `probe-summary.md` (`Format-ProbeSummary.ps1`). Fiecare candidat e ambalat în aceeași formă `DecodedSource` și convertit de același `CanonicalConverter`; măsurători: deschidere (timp, alocări, octeți citiți), primul output (octeți citiți / fișier), decodare completă (frame-uri vs așteptat, xRT, lag și SNR vs oracle, scor de canal inversat), EOF stabil, seek la 10/50/90% (diferența față de decodarea continuă, lag), trei bucle (seek 0 + pas complet), heap live.

```powershell
pwsh Tools/TimbreDecoderProbe/Invoke-DecoderProbe.ps1 -ResultFile benchmarks/Cerneala.Benchmarks/results/2026-10-07-timbre-decoding-stage0/probe-results.jsonl
dotnet run --project Tools/TimbreDecoderProbe -c Release -- tests/Cerneala.Tests.Timbre/Corpus benchmarks/Cerneala.Benchmarks/results/2026-10-07-timbre-decoding-stage0/footprint.json --footprint
```

| Format | Candidat | Rezultat (din `probe-summary.md`) | Verdict |
| --- | --- | --- | --- |
| WAV | RIFF propriu | toate cazurile: lungime exactă, lag 0, SNR = cuantizarea formatului, seek/loop diferență 0 | **selectat** |
| WAV | NAudio.Core 2.4.0 `WaveFileReader` | PCM identic | respins: dependență în plus pentru parsarea unui chunk RIFF; conversia și gating-ul matricei rămân oricum în adaptor |
| MP3 | demux Timbre + NLayer 3.0.0 `MpegFrameDecoder` | lag 0, lungime gapless exactă (inclusiv 300 s), seek bit-exact (diferență 0), loop 0, primul output după 0.1–12% din fișier, heap independent de durată | **selectat** |
| MP3 | NLayer 3.0.0 `MpegFile` | ignoră delay-ul decoderului (lag 529–1587), seek decalat 337–1600 frame-uri, citește fișierul întreg la deschidere pentru fișiere mici/fără tag, păstrează lista tuturor frame-urilor (`MpegStreamReader`), are `Task.Delay(500).Wait()` în bucla de citire, pierde un frame la wrap-ul rezervorului | respins |
| Vorbis | Ogg Timbre + NVorbis 0.10.5 `StreamDecoder` (provider propriu) | lag 0, lungime = granule final, seek bit-exact, loop 0, primul output după 7.5% pe 300 s | **selectat** |
| Vorbis | NVorbis 0.10.5 `VorbisReader` | seek decalat 32–768 frame-uri, citește întreg fișierul la deschidere (`TotalSamples` indexează toate paginile), **se blochează** pe `vorbis-8000-mono-q2` (timeout 120 s) | respins |
| Vorbis | NVorbis 1.0.0-rc.2 | prerelease (2026-07-13) | neprobat: pin stabil preferat; candidat de upgrade |
| Opus | Ogg Timbre + Concentus 2.2.2 `OpusDecoder` (managed) | lag 0 (−1 pe 300 s la 32 kbps), pre-skip/end trim exacte, seek cu pre-roll 80 ms (diferență ≤ 0.011 față de continuu, decoder resetat), loop 0 | **selectat** |
| Opus | Concentus.OggFile 1.0.7 `OpusOggReadStream` | nu aplică pre-skip (lag 312) și nici end trimming, fără lungime, seek imprecis (diferență 0.55 la buclă), returnează PCM16 | respins |
| Resampling | Concentus 2.2.2 `SpeexResampler` (managed, calitate 5) | lungime ceil(N·48000/rate), aliniere exactă (lag 0) la toate ratele, seek repornit pe perioada rațională → identic cu continuul | **selectat** |

Excluse fără probă: MP3Sharp (LGPL), NAudio `Mp3FileReader` (ACM, numai Windows), libopus/libvorbis/libmpg123 native (managed satisface matricea; native ar cere asset-uri pe șase RID-uri), SDL_mixer (exclus de plan).

**Defect NLayer 3.0.0 găsit de probe.** `BitReservoir.AddBits` folosește `_end == -1` atât pentru reset, cât și pentru wrap-ul ring-ului de 8192 octeți; când un frame se termină exact pe ultimul octet, frame-ul următor e tratat ca început de stream și decodat la 0 sample-uri. Reprodus: `mp3-long-22050-mono-cbr32` (generare anterioară) eșua la frame-ul 4477; `mp3-reservoir-wrap-48000-stereo-cbr64` atinge condiția exact la frame-ul 2048 (192 B/frame, 156 B de rezervor), unde `MpegFile` livrează 2 639 808 din 2 640 000 frame-uri. Adaptorul Timbre prezice condiția din octeții alimentați și re-amorsează decodorul pe pre-roll-ul anterior (același mecanism ca seek-ul, verificat bit-exact); rezultatul e exact. Fără fork al bibliotecii.

## 5. Selecția, pin-uri, licențe și distribuție

| Pachet | Versiune | Licență | Rol | Dependențe tranzitive (net8.0) | Mentenanță |
| --- | --- | --- | --- | --- | --- |
| NLayer | 3.0.0 (commit `046c7ce4`, 2026-08-27) | MIT („based on the MPEG specifications”, LICENSE la commit) | decodor frame Layer III | niciuna | activ (release 2026) |
| NVorbis | 0.10.5 (2022-10-16) | MIT | decodor Vorbis (`StreamDecoder`) | System.Memory 4.5.3, System.ValueTuple 4.5.0 — fără asset-uri pe net8.0 | repo activ (1.0.0-rc.2 în 2026) |
| Concentus | 2.2.2 (commit `6c2328dc`, 2024-05-26) | BSD-3-Clause (Xiph/Opus) | decodor Opus managed + resampler Speex | System.Memory 4.5.4, System.Numerics.Vectors 4.5.0 — fără asset-uri pe net8.0 | menținut |

Toate trei sunt managed, AnyCPU. Constructorii managed `OpusDecoder`/`SpeexResampler` sunt marcați `[Obsolete]` în favoarea factory-urilor care pot încărca libopus/speexdsp nativ; Timbre îi folosește explicit (suprimare locală documentată) ca decodarea să fie deterministă și fără cod nativ.

Verificare host-side (`Tools/scripts/Test-TimbreDecoderAssets.ps1` pe `Tools/TimbreDecoderSelection`, publicare framework-dependent): `asset-check-six-rids.json` — pentru win-x64, win-arm64, linux-x64, linux-arm64, osx-x64, osx-arm64 aceleași trei assembly-uri cu hash identic (NLayer `2cb48f87…`, NVorbis `d4fd7a6a…`, Concentus `3a1dcfcf…`), 8 fișiere, zero biblioteci native de codec. Execuția runtime pe non-Windows: N/A conform politicii de platformă (index §7), nu PASS.

Notice: textele de licență MIT (NLayer, NVorbis) și BSD-3-Clause (Concentus) se livrează într-un fișier THIRD-PARTY-NOTICES al pachetului Cerneala la adăugarea `PackageReference` (etapa 1), verificat de același script cu `-NoticeFile` în etapa 3.

## 6. Streaming incremental și memorie observată

Instrumentare: `CountingStream` (apeluri/octeți citiți, seek-uri, cel mai îndepărtat octet, disposal) în jurul fluxului dat oricărui candidat, deci și citirile din interiorul bibliotecii; heap live măsurat prin GC complet cu numai convertorul ținut (`footprint.json`, pas „warm” după inițializarea statică).

| Codec | Fișier 2 s: heap live peak | Fișier 300 s: heap live peak | Octeți citiți la 1 s de output (300 s) |
| --- | ---: | ---: | ---: |
| WAV 22.05 kHz mono | 137 944 B | 137 944 B | 47 148 din 13 230 044 |
| MP3 22.05 kHz mono | 177 592 B (VBR) | 181 624 B (CBR 64k) | 9 419 din 2 400 756 |
| Vorbis 22.05 kHz mono | 644 728 B | 644 728 B | 72 881 din 970 526 |
| Opus mono | 121 912 B | 121 912 B | 77 882 din 1 238 611 |

Starea live nu crește cu durata (nici după seek-uri și bucle), iar primul output apare după o fracțiune mică a fișierului. Demux-urile Timbre nu păstrează index: MP3 caută prin parcurgerea antetelor cu un inel de 64 offset-uri; Ogg prin bisecție pe octeți, cu o singură pagină și un pachet în memorie. Fișierele de 2 s sub 64 KiB sunt citite integral la deschidere de scanarea paginii finale Ogg (fereastră 64 KiB); adaptorul de producție scanează coada incremental.

Scope: heap managed atribuibil readerului (stream, demux, decodor, convertor). Nu include tabelele statice ale bibliotecilor (o dată pe proces), garbage-ul necolectat, memoria CLR/OS/nativă sau stiva; nu e un bound RAM/RSS. Pentru fișierele scurte valoarea „afterDispose” rămâne ridicată din cauza vieții variabilelor locale în codul JIT tier-0 (pe fișierele de 300 s scade la ~4 KiB).

Rezervări cooperative propuse pentru adaptoare (≥ 2× peak-ul măsurat, inclusiv convertorul): WAV 256 KiB, MP3 512 KiB, Opus 512 KiB, Vorbis 2 MiB de bază plus completare cu alocările măsurate la parsarea antetelor când le depășesc (codebook-urile depind de fișier). Bufferul de fișier (64 KiB) și ring-ul streaming (64 KiB) se rezervă separat.

## 7. Buget cooperativ (suprafață aditivă implementată în această etapă)

`SoundMemoryBudget`/`SoundMemoryReservation` și overload-urile `SoundSource.FromStream/FromReader(Func<SoundMemoryBudget, …>)`; pool-ul runtime (`StreamingMemoryLimit`, implicit `long.MaxValue`, fără alocare) e comun ring-urilor streaming și rezervărilor; `FileSource` rezervă bufferul de 64 KiB înaintea deschiderii; runtime-ul închide bugetul după `Dispose`-ul readerului; diagnostic intern separat `ReaderMemoryBytes`. Docs: paginile `SoundMemoryBudget`, `SoundMemoryReservation`, `SoundSource`, `SoundRuntimeOptions`, manifest; exemple compilate în `tests/Fixtures/TimbreConsumer/DocumentationExamples.cs`.

- `stage0-budget-red.trx`: 9 eșecuri — 8 `NotImplementedException` din suprafața staged (nu RED de comportament) și 1 RED fidel: `FileSourceReservesItsIoBufferBeforeOpeningTheFile` (fișierul era deschis înaintea oricărei rezervări: `SourceUnavailable` în loc de `ResourceLimitExceeded`).
- `stage0-budget-green.trx`: 15/15 (8 `MemoryBudgetTests` + `SoundDefinitionTests`).

Notă de compatibilitate: overload-urile noi fac ambiguu numai apelul cu literal `null` (`FromReader(null!)`); lambdas și method groups rămân neambigue. Binar compatibil (aditiv).
