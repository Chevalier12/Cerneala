# Stage 2: bounded, strict compression

The permanent `TruncatedCompressionContainerDoesNotBecomeAValidMap` regression demonstrated that both .NET 8 `GZipStream` and `ZLibStream` accepted a payload missing its trailer while still producing the declared 16 bytes. The RED run is `scene-import-stage2-coverage-red.trx` (two compression failures; four separate layer-disposition failures).

The [.NET 8 implementation](https://github.com/dotnet/runtime/blob/v8.0.0/src/libraries/System.IO.Compression/src/System/IO/Compression/DeflateZLib/DeflateStream.cs) gates incomplete-input rejection on a cached process-global `System.IO.Compression.UseStrictValidation` switch. An optional scene importer must not change application-wide decompression semantics, depend on initialization order, or require a host workaround.

Repository project/dependency inspection found no existing strict inflater dependency; the Roslyn `Inflater` search had no results. The optional importer therefore takes [SharpZipLib 1.4.2](https://www.nuget.org/packages/SharpZipLib/1.4.2): MIT, managed, no additional runtime dependencies for the selected net6.0 asset. No dependency is added to core or either backend.

Only its established `Inflater` and CRC implementation are used. Explicit `IsFinished` and `RemainingInput` separate a complete stream from exhausted input without inventing a DEFLATE implementation. Output is bounded by the already-validated cell count, with a one-byte overflow probe. Incomplete syntax/checksums are `SCN2D002`; decoded length disagreement is `SCN2D005`.

The importer owns strict [RFC 1952](https://www.rfc-editor.org/rfc/rfc1952) gzip framing (including concatenated members, optional headers, header CRC, per-member CRC and size) and rejects trailing garbage. It does not use the library's tolerant gzip stream wrapper. [RFC 1950](https://www.rfc-editor.org/rfc/rfc1950) zlib completion/checksum is verified by the inflater; preset dictionaries are explicitly unsupported. Filename/comment bytes are never executed or used as filesystem paths.

This decision implements the existing hostile-input gate. It does not add archive extraction, alternative compression formats, a global compatibility switch, or a new public codec API. Verification remains recorded separately at the stage checkpoint.
