| File | Candidate | Frames / expected | Lag | SNR L / R dB | Open read | First output read | xRT | Seek max diff | Seek lag | Loop diff | Live heap after decode (KiB) | Result |
| --- | --- | --- | ---: | --- | --- | --- | ---: | ---: | --- | ---: | ---: | --- |
| mp3-mpeg1-44100-stereo-cbr128.mp3 | Timbre demux + NLayer 3.0.0 MpegFrameDecoder | 96000 (96000) / 96000 | 0 | 26.02 / 26.02 | 1% | 5.2% | 14.7 | 0 | 0,0,0 | 0 | 584 | ok |
| mp3-mpeg1-44100-stereo-cbr128.mp3 | NLayer 3.0.0 MpegFile | 96000 (96000) / 96000 | 576 | -2.76 / -2.75 | 12% | 12.4% | 14.5 | 0.95 | 452,-19,765 | 0.000723 | 600 | ok |
| mp3-mpeg1-48000-mono-cbr64.mp3 | Timbre demux + NLayer 3.0.0 MpegFrameDecoder | 96000 (96000) / 96000 | 0 | 26.02 / 26.02 | 1% | 5.0% | 32 | 0 | 0,0,0 | 0 | 511 | ok |
| mp3-mpeg1-48000-mono-cbr64.mp3 | NLayer 3.0.0 MpegFile | 96000 (96000) / 96000 | 529 | -2.78 / -2.78 | 25% | 24.8% | 37.1 | 0.95 | 337,721,-5 | 0 | 512 | ok |
| mp3-mpeg2-16000-stereo-cbr48.mp3 | Timbre demux + NLayer 3.0.0 MpegFrameDecoder | 96000 (96000) / 96000 | 0 | 25.84 / 26 | 2% | 9.0% | 23 | 0 | 0,0,0 | 0 | 566 | ok |
| mp3-mpeg2-16000-stereo-cbr48.mp3 | NLayer 3.0.0 MpegFile | 96000 (96000) / 96000 | 1587 | -2.73 / -2.74 | 32% | 32.1% | 18.8 | 0.95 | 1540,1559,1305 | 0.00929 | 579 | ok |
| mp3-mpeg25-8000-mono-cbr16.mp3 | Timbre demux + NLayer 3.0.0 MpegFrameDecoder | 103680 (?) / 96000 | 1382 | -2.85 / -2.85 | 5% | 11.4% | 35.2 |  |  | 0 | 320 | ok |
| mp3-mpeg25-8000-mono-cbr16.mp3 | NLayer 3.0.0 MpegFile | 103680 (103680) / 96000 | 1382 | -2.85 / -2.85 | 100% | 100.0% | 29.4 | 3.52 | 1289,1505,1479 | 0 | 557 | ok |
| vorbis-44100-stereo-q4.ogg | Timbre Ogg + NVorbis 0.10.5 StreamDecoder | 94886 (94886) / 94886 | 0 | 39.41 / 37.02 | 136% | 169.7% | 16.7 | 0 | 0,0,0 | 0 | 1,245 | ok |
| vorbis-44100-stereo-q4.ogg | NVorbis 0.10.5 VorbisReader | 94886 (94886) / 94886 | 0 | 39.41 / 37.02 | 100% | 134.5% | 14.5 | 0.88 | 139,139,0 | 0 | 1,188 | ok |
| vorbis-48000-stereo-q2.ogg | Timbre Ogg + NVorbis 0.10.5 StreamDecoder | 94976 (94976) / 94976 | 0 | 36.28 / 32.88 | 133% | 166.0% | 23 | 0 | 0,0,0 | 0 | 1,136 | ok |
| vorbis-48000-stereo-q2.ogg | NVorbis 0.10.5 VorbisReader | 94976 (94976) / 94976 | 0 | 36.28 / 32.88 | 100% | 134.0% | 22.2 | 0.619 | 128,128,0 | 0 | 1,078 | ok |
| vorbis-22050-mono-q2.ogg | Timbre Ogg + NVorbis 0.10.5 StreamDecoder | 94886 (94886) / 94886 | 0 | 26.22 / 26.22 | 135% | 176.9% | 16.3 | 0 | 0,0,0 | 0 | 991 | ok |
| vorbis-22050-mono-q2.ogg | NVorbis 0.10.5 VorbisReader | 94886 (94886) / 94886 | 0 | 26.22 / 26.22 | 100% | 142.9% | 21.3 | 1.08 | 557,557,0 | 0 | 932 | ok |
| vorbis-8000-mono-q2.ogg | Timbre Ogg + NVorbis 0.10.5 StreamDecoder | 94464 (94464) / 94464 | 0 | 27.42 / 27.42 | 168% | 201.5% | 32.4 | 0 | 0,0,0 | 0 | 766 | ok |
| vorbis-8000-mono-q2.ogg | NVorbis 0.10.5 VorbisReader | | | | | | | | | | | timeout: no result after 120 s (process killed) |
| vorbis-96000-stereo-q4.ogg | Timbre Ogg + NVorbis 0.10.5 StreamDecoder | 95488 (95488) / 95488 | 0 | 38.72 / 38.06 | 123% | 145.3% | 7.3 | 0 | 0,0,0 | 0 | 1,184 | ok |
| vorbis-96000-stereo-q4.ogg | NVorbis 0.10.5 VorbisReader | 95488 (95488) / 95488 | 0 | 38.72 / 38.06 | 100% | 122.5% | 9 | 1.02 | 64,0,0 | 0 | 1,132 | ok |
| vorbis-192000-stereo-q2.ogg | Timbre Ogg + NVorbis 0.10.5 StreamDecoder | 95744 (95744) / 95744 | 0 | 33.81 / 32.76 | 116% | 132.8% | 6.9 | 0 | 0,0,0 | 0 | 1,145 | ok |
| vorbis-192000-stereo-q2.ogg | NVorbis 0.10.5 VorbisReader | 95744 (95744) / 95744 | 0 | 33.81 / 32.76 | 100% | 117.2% | 7.1 | 0.755 | 32,0,0 | 0 | 1,088 | ok |
| vorbis-16000-mono-q2.ogg | Timbre Ogg + NVorbis 0.10.5 StreamDecoder | 94464 (94464) / 94464 | 0 | 25.53 / 25.53 | 142% | 193.1% | 27.3 | 0 | 0,0,0 | 0 | 895 | ok |
| vorbis-16000-mono-q2.ogg | NVorbis 0.10.5 VorbisReader | 94464 (94464) / 94464 | 0 | 25.53 / 25.53 | 100% | 151.8% | 25.5 | 1.06 | 768,768,0 | 0 | 834 | ok |
| vorbis-long-22050-mono-q2.ogg | Timbre Ogg + NVorbis 0.10.5 StreamDecoder | 14398886 (14398886) / 14398886 | 0 | 26.63 / 26.63 | 7% | 7.5% | 153.1 | 0 | 0,0,0 | 0 | 991 | ok |
| vorbis-long-22050-mono-q2.ogg | NVorbis 0.10.5 VorbisReader | 14398886 (14398886) / 14398886 | 0 | 26.63 / 26.63 | 100% | 100.4% | 164.2 | 0 | 0,0,0 | 0 | 943 | ok |
| opus-stereo-48000-96k.opus | Timbre Ogg + Concentus 2.2.2 OpusDecoder | 96000 (96000) / 96000 | 0 | 29.62 / 29.31 | 117% | 133.5% | 11.5 | 0.0043 | 0,0,0 | 0 | 537 | ok |
| opus-stereo-48000-96k.opus | Concentus.OggFile 1.0.7 OpusOggReadStream | 96960 (?) / 96000 | 312 | -3.19 / -3.02 | 200% | 200.0% | 15.1 |  |  | 0.549 | 310 | ok |
| opus-mono-16000-24k.opus | Timbre Ogg + Concentus 2.2.2 OpusDecoder | 96000 (96000) / 96000 | 0 | 30.88 / 30.88 | 151% | 198.7% | 22.3 | 0 | 0,0,0 | 0 | 502 | ok |
| opus-mono-16000-24k.opus | Concentus.OggFile 1.0.7 OpusOggReadStream | 96960 (?) / 96000 | 312 | -3.19 / -3.19 | 200% | 200.0% | 27.9 |  |  | 0.537 | 296 | ok |
| opus-stereo-preskip-trim.opus | Timbre Ogg + Concentus 2.2.2 OpusDecoder | 59256 (59256) / 59256 | 0 | 27.44 / 27.32 | 140% | 178.3% | 16.6 | 0.011 | 0,0,-60 | 0 | 537 | ok |
| opus-stereo-preskip-trim.opus | Concentus.OggFile 1.0.7 OpusOggReadStream | 64320 (?) / 59256 | 1552 | -2.84 / -2.63 | 200% | 200.0% | 15.3 |  |  | 0.545 | 307 | ok |
| opus-long-mono-32k.opus | Timbre Ogg + Concentus 2.2.2 OpusDecoder | 14400000 (14400000) / 14400000 | -1 | 33.57 / 33.57 | 6% | 5.9% | 179.9 | 0 | -1,-1,-1 | 0 | 502 | ok |
| opus-long-mono-32k.opus | Concentus.OggFile 1.0.7 OpusOggReadStream | 14400960 (?) / 14400000 | 311 | -3.21 / -3.21 | 115% | 114.7% | 127.2 |  |  | 0.549 | 2,149 | ok |
| wav-44100-stereo-s8-2s.wav | Timbre RIFF reader | 96000 (96000) / 96000 | 0 | 44.35 / 39.79 | 0% | 2.4% | 19.7 | 0 | 0,0,0 | 0 | 457 | ok |
| wav-44100-stereo-s8-2s.wav | NAudio.Core 2.4.0 WaveFileReader | 96000 (96000) / 96000 | 0 | 44.35 / 39.79 | 0% | 2.4% | 27.6 | 0 | 0,0,0 | 0 | 451 | ok |
| wav-44100-stereo-s16-2s.wav | Timbre RIFF reader | 96000 (96000) / 96000 | 0 | 84.17 / 81.97 | 0% | 2.3% | 25.3 | 0 | 0,0,0 | 0 | 457 | ok |
| wav-44100-stereo-s16-2s.wav | NAudio.Core 2.4.0 WaveFileReader | 96000 (96000) / 96000 | 0 | 84.17 / 81.97 | 0% | 2.3% | 24.9 | 0 | 0,0,0 | 0 | 453 | ok |
| wav-44100-stereo-s24-2s.wav | Timbre RIFF reader | 96000 (96000) / 96000 | 0 | 86.79 / 84.41 | 0% | 2.3% | 17.5 | 0 | 0,0,0 | 0 | 460 | ok |
| wav-44100-stereo-s24-2s.wav | NAudio.Core 2.4.0 WaveFileReader | 96000 (96000) / 96000 | 0 | 86.79 / 84.41 | 0% | 2.3% | 11.4 | 0 | 0,0,0 | 0 | 459 | ok |
| wav-44100-stereo-s32-2s.wav | Timbre RIFF reader | 96000 (96000) / 96000 | 0 | 86.79 / 84.41 | 0% | 2.3% | 20.2 | 0 | 0,0,0 | 0 | 457 | ok |
| wav-44100-stereo-s32-2s.wav | NAudio.Core 2.4.0 WaveFileReader | 96000 (96000) / 96000 | 0 | 86.79 / 84.41 | 0% | 2.3% | 29.4 | 0 | 0,0,0 | 0 | 457 | ok |
| wav-44100-stereo-f32-2s.wav | Timbre RIFF reader | 96000 (96000) / 96000 | 0 | 86.79 / 84.41 | 0% | 2.3% | 27 | 0 | 0,0,0 | 0 | 457 | ok |
| wav-44100-stereo-f32-2s.wav | NAudio.Core 2.4.0 WaveFileReader | 96000 (96000) / 96000 | 0 | 86.79 / 84.41 | 0% | 2.3% | 27.8 | 0 | 0,0,0 | 0 | 457 | ok |
| wav-8000-mono-s16-2s.wav | Timbre RIFF reader | 96000 (96000) / 96000 | 0 | 61.08 / 61.08 | 0% | 6.5% | 57.5 | 0 | 0,0,0 | 0 | 419 | ok |
| wav-8000-mono-s16-2s.wav | NAudio.Core 2.4.0 WaveFileReader | 96000 (96000) / 96000 | 0 | 61.08 / 61.08 | 0% | 6.5% | 45.5 | 0 | 0,0,0 | 0 | 409 | ok |
| wav-8000-stereo-s24-2s.wav | Timbre RIFF reader | 96000 (96000) / 96000 | 0 | 61.09 / 58.77 | 0% | 6.4% | 25.3 | 0 | 0,0,0 | 0 | 448 | ok |
| wav-8000-stereo-s24-2s.wav | NAudio.Core 2.4.0 WaveFileReader | 96000 (96000) / 96000 | 0 | 61.09 / 58.77 | 0% | 6.4% | 54.4 | 0 | 0,0,0 | 0 | 446 | ok |
| wav-22050-mono-s16-2s.wav | Timbre RIFF reader | 96000 (96000) / 96000 | 0 | 74.39 / 74.39 | 0% | 2.4% | 46.7 | 0 | 0,0,0 | 0 | 502 | ok |
| wav-22050-mono-s16-2s.wav | NAudio.Core 2.4.0 WaveFileReader | 96000 (96000) / 96000 | 0 | 74.39 / 74.39 | 0% | 2.4% | 44.2 | 0 | 0,0,0 | 0 | 492 | ok |
| wav-22050-stereo-s24-2s.wav | Timbre RIFF reader | 96000 (96000) / 96000 | 0 | 74.6 / 72.4 | 0% | 2.3% | 31.1 | 0 | 0,0,0 | 0 | 515 | ok |
| wav-22050-stereo-s24-2s.wav | NAudio.Core 2.4.0 WaveFileReader | 96000 (96000) / 96000 | 0 | 74.6 / 72.4 | 0% | 2.3% | 38.1 | 0 | 0,0,0 | 0 | 518 | ok |
| wav-48000-mono-s16-2s.wav | Timbre RIFF reader | 96000 (96000) / 96000 | 0 | 88.07 / 88.07 | 0% | 2.2% | 29.1 | 0 | 0,0,0 | 0 | 389 | ok |
| wav-48000-mono-s16-2s.wav | NAudio.Core 2.4.0 WaveFileReader | 96000 (96000) / 96000 | 0 | 88.07 / 88.07 | 0% | 2.2% | 96.9 | 0 | 0,0,0 | 0 | 380 | ok |
| wav-48000-stereo-s24-2s.wav | Timbre RIFF reader | 96000 (96000) / 96000 | 0 | 136.81 / 133.27 | 0% | 2.1% | 72.9 | 0 | 0,0,0 | 0 | 393 | ok |
| wav-48000-stereo-s24-2s.wav | NAudio.Core 2.4.0 WaveFileReader | 96000 (96000) / 96000 | 0 | 136.81 / 133.27 | 0% | 2.1% | 85.8 | 0 | 0,0,0 | 0 | 392 | ok |
| wav-96000-mono-s16-2s.wav | Timbre RIFF reader | 96000 (96000) / 96000 | 0 | 86.84 / 86.84 | 0% | 2.7% | 14.9 | 0 | 0,0,0 | 0 | 396 | ok |
| wav-96000-mono-s16-2s.wav | NAudio.Core 2.4.0 WaveFileReader | 96000 (96000) / 96000 | 0 | 86.84 / 86.84 | 0% | 2.7% | 26 | 0 | 0,0,0 | 0 | 386 | ok |
| wav-96000-stereo-s24-2s.wav | Timbre RIFF reader | 96000 (96000) / 96000 | 0 | 91.66 / 81.27 | 0% | 2.7% | 9.8 | 0 | 0,0,0 | 0 | 403 | ok |
| wav-96000-stereo-s24-2s.wav | NAudio.Core 2.4.0 WaveFileReader | 96000 (96000) / 96000 | 0 | 91.66 / 81.27 | 0% | 2.7% | 12.7 | 0 | 0,0,0 | 0 | 402 | ok |
| wav-192000-mono-s16-2s.wav | Timbre RIFF reader | 96000 (96000) / 96000 | 0 | 87.1 / 87.1 | 0% | 2.4% | 13.5 | 0 | 0,0,0 | 0 | 396 | ok |
| wav-192000-mono-s16-2s.wav | NAudio.Core 2.4.0 WaveFileReader | 96000 (96000) / 96000 | 0 | 87.1 / 87.1 | 0% | 2.4% | 16.9 | 0 | 0,0,0 | 0 | 386 | ok |
| wav-192000-stereo-s24-2s.wav | Timbre RIFF reader | 96000 (96000) / 96000 | 0 | 91.88 / 80.13 | 0% | 2.4% | 11.5 | 0 | 0,0,0 | 0 | 406 | ok |
| wav-192000-stereo-s24-2s.wav | NAudio.Core 2.4.0 WaveFileReader | 96000 (96000) / 96000 | 0 | 91.88 / 80.13 | 0% | 2.4% | 8.1 | 0 | 0,0,0 | 0 | 401 | ok |
| wav-22050-mono-s16-300s.wav | Timbre RIFF reader | 14400000 (14400000) / 14400000 | 0 | 79.54 / 79.54 | 0% | 0.0% | 198.7 | 0 | 0,0,0 | 0 | 502 | ok |
| wav-22050-mono-s16-300s.wav | NAudio.Core 2.4.0 WaveFileReader | 14400000 (14400000) / 14400000 | 0 | 79.54 / 79.54 | 0% | 0.0% | 223.2 | 0 | 0,0,0 | 0 | 492 | ok |
| mp3-long-22050-mono-cbr64.mp3 | Timbre demux + NLayer 3.0.0 MpegFrameDecoder | 14400000 (14400000) / 14400000 | 0 | 26.01 / 26.01 | 0% | 0.1% | 109.6 | 0 | 0,0,0 | 0 | 643 | ok |
| mp3-long-22050-mono-cbr64.mp3 | NLayer 3.0.0 MpegFile | 14400000 (14400000) / 14400000 | 1152 | -2.8 / -2.8 | 0% | 0.2% | 122.4 | 0.974 | 1496,1411,1560 | 0 | 1,636 | ok |
| mp3-mpeg1-32000-stereo-vbr.mp3 | Timbre demux + NLayer 3.0.0 MpegFrameDecoder | 96000 (96000) / 96000 | 0 | 67.66 / 59.46 | 3% | 11.7% | 16.7 | 0 | 0,0,0 | 0 | 554 | ok |
| mp3-mpeg1-32000-stereo-vbr.mp3 | NLayer 3.0.0 MpegFile | 96000 (96000) / 96000 | 793 | -3.06 / -2.99 | 21% | 21.5% | 17.6 | 1 | 766,1150,1535 | 0.0349 | 568 | ok |
| mp3-mpeg2-22050-mono-vbr.mp3 | Timbre demux + NLayer 3.0.0 MpegFrameDecoder | 96000 (96000) / 96000 | 0 | 67.53 / 67.53 | 3% | 11.9% | 22.7 | 0 | 0,0,0 | 0 | 624 | ok |
| mp3-mpeg2-22050-mono-vbr.mp3 | NLayer 3.0.0 MpegFile | 96000 (96000) / 96000 | 1152 | -3 / -3 | 46% | 46.2% | 27.3 | 4.51 | 1334,1600,1564 | 0 | 624 | ok |
| mp3-reservoir-wrap-48000-stereo-cbr64.mp3 | Timbre demux + NLayer 3.0.0 MpegFrameDecoder | 2640000 (2640000) / 2640000 | 0 | 26.01 / 26 | 0% | 0.2% | 51.4 | 0 | 0,0,0 | 0 | 547 | ok |
| mp3-reservoir-wrap-48000-stereo-cbr64.mp3 | NLayer 3.0.0 MpegFile | 2639808 (2640000) / 2640000 | 529 | -2.79 / -2.83 | 1% | 0.9% | 68.9 | 0.963 | 208,913,529 | 0 | 750 | ok |
