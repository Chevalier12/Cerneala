// Touches one type of each selected decoder package so the published output
// carries exactly the assemblies a Timbre consumer receives.
Console.WriteLine(typeof(NLayer.MpegFrameDecoder).Assembly.GetName());
Console.WriteLine(typeof(NVorbis.StreamDecoder).Assembly.GetName());
Console.WriteLine(typeof(Concentus.Structs.OpusDecoder).Assembly.GetName());
