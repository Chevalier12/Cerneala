using Cerneala.Drawing;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: Cerneala.SvgAssetCompiler <input.svg> <output.svg.cerneala.png>");
    return 2;
}

string inputPath = Path.GetFullPath(args[0]);
string outputPath = Path.GetFullPath(args[1]);
if (!File.Exists(inputPath))
{
    Console.Error.WriteLine($"SVG input '{inputPath}' does not exist.");
    return 2;
}

string? outputDirectory = Path.GetDirectoryName(outputPath);
if (!string.IsNullOrEmpty(outputDirectory))
{
    Directory.CreateDirectory(outputDirectory);
}

byte[] png = SvgRasterizer.Compile(inputPath);
string signature = SvgRasterizer.ComputeSourceSignature(inputPath);
string temporaryPath = outputPath + ".tmp";
string signaturePath = outputPath + ".sha256";
string temporarySignaturePath = signaturePath + ".tmp";
try
{
    File.WriteAllBytes(temporaryPath, png);
    File.WriteAllText(temporarySignaturePath, signature);
    File.Move(temporaryPath, outputPath, overwrite: true);
    File.Move(temporarySignaturePath, signaturePath, overwrite: true);
}
finally
{
    File.Delete(temporaryPath);
    File.Delete(temporarySignaturePath);
}

Console.WriteLine($"Compiled '{inputPath}' -> '{outputPath}' ({png.Length:N0} bytes).");
return 0;
