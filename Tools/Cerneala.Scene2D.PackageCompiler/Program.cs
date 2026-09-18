using Cerneala.Scene2D.Importers;
using Cerneala.Scene2D.Packages;

if (args.Length != 4 || args[0] is not ("tiled" or "ldtk"))
{
    Console.Error.WriteLine("Usage: Cerneala.Scene2D.PackageCompiler <tiled|ldtk> <input-map> <asset-root> <new-output-directory>");
    return 2;
}

using CancellationTokenSource cancellation = new();
ConsoleCancelEventHandler cancel = (_, eventArgs) => { eventArgs.Cancel = true; cancellation.Cancel(); };
Console.CancelKeyPress += cancel;
try
{
    string input = Path.GetFullPath(args[1]);
    string root = Path.GetFullPath(args[2]);
    string output = Path.GetFullPath(args[3]);
    Scene2DImportOptions options = new() { AssetRootDirectory = root };
    Scene2DImportResult imported = args[0] == "tiled"
        ? TiledScene2DImporter.Import(input, options) : LdtkScene2DImporter.Import(input, options);
    foreach (var diagnostic in imported.Diagnostics)
    {
        Console.Error.WriteLine($"{diagnostic.Code}: {diagnostic.Message} ({diagnostic.FilePath}, {diagnostic.JsonPath})");
    }
    if (!imported.Success) { return 1; }
    await Scene2DPackageWriter.WriteAsync(output, imported.Document!, imported.AssetRootDirectory!, imported.ReferencedFiles, cancellation.Token);
    Console.WriteLine($"Prepared '{input}' -> '{output}' ({imported.Document!.Levels.Count} levels, {imported.ReferencedFiles.Count} files).");
    return 0;
}
catch (OperationCanceledException)
{
    Console.Error.WriteLine("Package preparation canceled.");
    return 130;
}
catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
{
    Console.Error.WriteLine(error.Message);
    return 1;
}
finally { Console.CancelKeyPress -= cancel; }
