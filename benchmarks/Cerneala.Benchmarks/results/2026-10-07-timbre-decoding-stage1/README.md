# Timbre decoding stage 1: adaptoare și PCM conformance

Data: 2026-10-07. Plan: `docs/plans/2026-10-03-timbre-decoding-streaming.md`, etapa 1. Selecția și corpusul: `../2026-10-07-timbre-decoding-stage0/README.md`.

## RED → GREEN

| Artefact | Conținut | Rezultat |
| --- | --- | --- |
| `stage1-red.trx` | testele `SoundDecoder*` noi, compilate contra seam-ului existent `SoundDecoders.Open` (gol în core) | 100 failed / 16 passed. Eșecurile sunt comportament absent, nu tipuri lipsă: fișiere valide → `UnsupportedFormat` („No available decoder recognizes the content”), decodorul indisponibil nu era mapat, notice-ul nu era livrat. Cele 16 trecute sunt contracte deja adevărate (conținut nerecunoscut → `UnsupportedFormat`). |
| `stage1-timbre-full.trx` | întregul `Cerneala.Tests.Timbre` după implementare | 250/250 |

Primul GREEN cu `NLayer 3.0.0` ca pachet a lăsat un eșec real: `mp3-mpeg25-8000-mono-cbr16.mp3` la 7.59 dB (prag 20 dB), cu aliniere corectă.

## Defecte NLayer și decizia de vendoring

Experimente (scripturi csi locale, aceleași fișiere din corpus, oracle `CorpusSignal`, lag căutat larg):

| Decodor | `mp3-mpeg25-8000-mono-cbr16` | `mp3-mpeg2-16000-stereo-cbr48` |
| --- | --- | --- |
| Windows Media Foundation (`MediaFoundationReader`) | 26.01 dB | 25.83 dB |
| NLayer 3.0.0 `MpegFile` (pachet) | 7.89 dB, lag 1105 = 576 + 529 | 26.01 dB |
| NLayer la commit-ul pin-uit, compilat local cu `region1Start = _sfBandIndexS[3] * 3` | 26.02 dB | — |

Cauza: `LayerIIIDecoder.ReadSamples` fixează `region1Start = 36` pentru blocuri scurte; frontiera e banda scurtă 3 × 3, adică 36 la toate ratele cu excepția MPEG-2.5 8 kHz (72). Al doilea defect (stage 0): `BitReservoir.AddBits` confundă wrap-ul ring-ului terminat pe ultimul octet cu un reset și pierde frame-ul următor.

Decizie a utilizatorului (2026-10-07): vendoring NLayer 3.0.0 (MIT) cu cele două fix-uri. Implementare: `Timbre/Decoding/NLayer/` conține numai decodorul de frame (`MpegFrameDecoder`, `IMpegFrame`, enum-uri, `BitReservoir`, `Huffman`, decodoarele de layer), cu namespace `Cerneala.Timbre.Decoding.NLayer`, tipuri `internal` (nu intră în API-ul public și nu intră în conflict cu un pachet NLayer al consumatorului), `#nullable disable`, antet de proveniență și modificări marcate `Cerneala:`; helper-ele CRC nefolosite (dependente de cititorul NLayer nevendorizat) au fost eliminate. `PackageReference` NLayer a fost scos; workaround-ul de re-amorsare din adaptor a fost eliminat, fiindcă defectul e reparat la sursă. Testul `Mp3FrameAtTheNLayerReservoirWrapIsNotDropped` (fixture `mp3-reservoir-wrap-48000-stereo-cbr64`, wrap exact la frame-ul 2048) și conformitatea MPEG-2.5 8 kHz protejează ambele fix-uri.

## Implementare

- `Timbre/Decoding/`: `WavSource` (RIFF propriu), `Mp3Source` (demux fără index peste NLayer vendorizat), `OggStreamReader` (demux Ogg mărginit: o pagină + un pachet, CRC, secvență, bisecție, coadă scanată incremental), `VorbisSource` (NVorbis 0.10.5 `StreamDecoder` cu provider propriu), `OpusSource` (Concentus 2.2.2 managed, pre-skip/end-trim/gain/pre-roll 80 ms), `CanonicalConverter` (mono→stereo fără downmix, Speex resampler managed calitate 5, lungime ceil, seek pe perioada rațională), `DecodedSoundReader` (contractul `SoundReader`), `SoundFormats` (detecție după conținut + rezervări de buget).
- `SoundDecoders`: registrul real; primul decodor care recunoaște conținutul îl deține (fără fallback); assembly de decodor lipsă → `UnsupportedFormat` cu excepția interioară, nu `InvalidData`.
- Rezervări cooperative înaintea construirii: WAV 256 KiB, MP3 512 KiB, Opus 512 KiB, Vorbis 2 MiB + completare cu octeții alocați la construirea tabelelor dacă îi depășesc (§6 din stage 0); `FileSource` 64 KiB separat.
- `Cerneala.csproj`: `NVorbis 0.10.5`, `Concentus 2.2.2`; `Timbre/Decoding/THIRD-PARTY-NOTICES.txt` publicat ca `Cerneala.Timbre.THIRD-PARTY-NOTICES.txt` (copiat în output, inclus în pachet) cu licențele MIT (NLayer, NVorbis) și BSD-3-Clause (Concentus).
- Docs: `SoundSource` (matricea, erorile, gapless, notice), `SoundErrorKind` (`UnsupportedFormat` include decodorul nedisponibil).

## Corpus de conformitate (tests/Cerneala.Tests.Timbre/Decoding)

- `SoundDecoderConformanceTests`: toate fișierele pozitive din corpus și matricea WAV sintetizată (PCM 8/16/24/32, float32, extensible; 8–192 kHz; mono/stereo) — lungime declarată și decodată egale cu ceil(N·48000/rată) (MP3 fără tag: interval delay + semnal + < 2 frame-uri, lungimea devine cunoscută la final), lag 0 (±1 frame), SNR per canal ≥ 20 dB lossy / 55 dB WAV ≥16-bit / 35 dB WAV 8-bit (sub cel mai slab caz al probelor), ordinea canalelor (dreapta nu corespunde canalului sursă stâng), mono identic pe ambele canale, DC |medie| ≤ 0.01, RMS în ±15% față de oracle, sample-uri finite.
- `SoundDecoderPartitionTests`: partiții aleatoare 1–3000 frame-uri identice bit cu bit cu citiri mari; EOF raportat o dată, apoi `(0, true)` stabil; pre-skip 312+3840 și end-trim Opus; lungimea MP3 VBR din tag înaintea decodării; frame-ul de la wrap-ul rezervorului; două readere ale aceluiași fișier intercalate, fără stare comună.
- `SoundDecoderErrorTests`: detecție după conținut (Vorbis ca `.mp3`, Opus ca `.wav`, MP3 ca `.ogg`, fără extensie, WAV ca `.mp3`); negative din corpus (Layer II, chained, multiplexed, Ogg FLAC, Opus surround → `UnsupportedFormat`; MP3 trunchiat/sync pierdut, Vorbis trunchiat/CRC, Opus trunchiat → `InvalidData`); WAV RIFX/RF64/ADPCM/float64/3 canale/4 kHz/384 kHz/non-WAVE → `UnsupportedFormat`, trunchiat/frame parțial/fără data → `InvalidData`; text/gol/zgomot → `UnsupportedFormat`; decodor indisponibil → `UnsupportedFormat` cu stream închis; un decodor care eșuează nu e urmat de altul; rezervarea refuzată înaintea construirii decodorului.
- `SoundDecoderRuntimeTests`: fișiere MP3/Vorbis/Opus/WAV redate preload prin runtime-ul real egal cu PCM-ul decodat (toleranță 0); Auto preîncarcă fișierul de 2 s și nu pe cel de 300 s; limita explicită refuză rezervarea decodorului (`ResourceLimitExceeded`, readerele și rezervările revin la 0); notice-ul e copiat lângă assembly.

`LifecycleTests.FileSourcesFailExplicitlyWithoutADecoderOrFile` folosea un stub de 8 octeți `RIFF` pentru „niciun decodor”; cu decodoarele înregistrate stub-ul devenea RIFF trunchiat. Intenția (conținut nerecunoscut → `UnsupportedFormat`) e păstrată cu un fișier text `.wav`; RIFF cu alt form type decât WAVE e acum `UnsupportedFormat` explicit.

Nu se pretinde output fizic: testele observă PCM-ul readerului și sink-ul determinist.
