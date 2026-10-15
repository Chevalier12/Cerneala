#requires -Version 7.0
<#
.SYNOPSIS
Turns the PNG screenshots of a reproduction app's check mode into one looping GIF.

.DESCRIPTION
cerneala-fix-bug runs the reproduction app with `--check <report> --frames <folder>`; the app saves a
screenshot through Cerneala's own screenshot API after each Servo action. This script reads those PNG files
in name order, scales them down to -MaxWidth, reduces each one to its own 256-color palette with WPF
(built into Windows), and writes an animated GIF that loops forever. The last frame stays longer, so the
final verdict can be read.

.EXAMPLE
pwsh -NoProfile -File Tools/scripts/New-ReproGif.ps1 -FramesDirectory $env:TEMP/repro-268/frames-before -OutputPath $env:TEMP/repro-268/Issue268-before.gif
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $FramesDirectory,
    [Parameter(Mandatory)][string] $OutputPath,
    [int] $FrameDelayMs = 900,
    [int] $LastFrameDelayMs = 3500,
    [int] $MaxWidth = 960
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationCore, WindowsBase
Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.IO;

public static class CernealaGifWriter
{
    // GIF89a with one local 256-color table per frame, a delay per frame, and the NETSCAPE loop extension.
    public static void Write(string path, int width, int height, byte[][] frames, byte[][] palettes, int[] delaysCs)
    {
        using var stream = File.Create(path);
        using var w = new BinaryWriter(stream);
        w.Write(System.Text.Encoding.ASCII.GetBytes("GIF89a"));
        w.Write((ushort)width);
        w.Write((ushort)height);
        w.Write((byte)0x70); // no global color table, 8-bit color resolution
        w.Write((byte)0);
        w.Write((byte)0);
        w.Write(new byte[] { 0x21, 0xFF, 0x0B });
        w.Write(System.Text.Encoding.ASCII.GetBytes("NETSCAPE2.0"));
        w.Write(new byte[] { 3, 1, 0, 0, 0 }); // loop forever
        for (int i = 0; i < frames.Length; i++)
        {
            w.Write(new byte[] { 0x21, 0xF9, 4, 0x04 }); // graphic control: keep the frame, no transparency
            w.Write((ushort)delaysCs[i]);
            w.Write((byte)0);
            w.Write((byte)0);
            w.Write((byte)0x2C);
            w.Write((ushort)0);
            w.Write((ushort)0);
            w.Write((ushort)width);
            w.Write((ushort)height);
            w.Write((byte)0x87); // local color table of 256 entries
            w.Write(palettes[i]);
            w.Write((byte)8);
            WriteLzw(w, frames[i]);
            w.Write((byte)0);
        }
        w.Write((byte)0x3B);
    }

    // Variable-width LZW as GIF expects it, following the classic encoder: a code is written with the current
    // width, and the width grows after a write once the next free code no longer fits.
    private static void WriteLzw(BinaryWriter w, byte[] pixels)
    {
        const int Clear = 256, End = 257, MaxCodes = 4096;
        var table = new Dictionary<int, int>();
        int next = End + 1, size = 9, bits = 0, bitCount = 0;
        var block = new List<byte>(255);

        void Flush()
        {
            if (block.Count == 0) return;
            w.Write((byte)block.Count);
            w.Write(block.ToArray());
            block.Clear();
        }

        void Emit(int code)
        {
            bits |= code << bitCount;
            bitCount += size;
            while (bitCount >= 8)
            {
                block.Add((byte)bits);
                bits >>= 8;
                bitCount -= 8;
                if (block.Count == 255) Flush();
            }
            if (next > (1 << size) - 1 && size < 12) size++;
        }

        Emit(Clear);
        int prefix = pixels[0];
        for (int i = 1; i < pixels.Length; i++)
        {
            int pixel = pixels[i];
            int key = (prefix << 8) | pixel;
            if (table.TryGetValue(key, out var code))
            {
                prefix = code;
                continue;
            }
            Emit(prefix);
            if (next < MaxCodes)
            {
                table[key] = next++;
            }
            else
            {
                Emit(Clear);
                table.Clear();
                next = End + 1;
                size = 9;
            }
            prefix = pixel;
        }
        Emit(prefix);
        Emit(End);
        if (bitCount > 0) block.Add((byte)bits);
        Flush();
    }
}
'@

$files = @(Get-ChildItem -LiteralPath $FramesDirectory -Filter *.png | Sort-Object Name)
if ($files.Count -eq 0) { throw "No PNG files in $FramesDirectory." }

$frames = [System.Collections.Generic.List[byte[]]]::new()
$palettes = [System.Collections.Generic.List[byte[]]]::new()
$width = 0
$height = 0
foreach ($file in $files) {
    $decoder = [System.Windows.Media.Imaging.BitmapDecoder]::Create(
        [Uri]$file.FullName, [System.Windows.Media.Imaging.BitmapCreateOptions]::PreservePixelFormat,
        [System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad)
    [System.Windows.Media.Imaging.BitmapSource]$source = $decoder.Frames[0]
    if ($source.PixelWidth -gt $MaxWidth) {
        $scale = $MaxWidth / $source.PixelWidth
        $source = [System.Windows.Media.Imaging.TransformedBitmap]::new($source, [System.Windows.Media.ScaleTransform]::new($scale, $scale))
    }
    $bgr = [System.Windows.Media.Imaging.FormatConvertedBitmap]::new($source, [System.Windows.Media.PixelFormats]::Bgr32, $null, 0)
    $palette = [System.Windows.Media.Imaging.BitmapPalette]::new($bgr, 256)
    $indexed = [System.Windows.Media.Imaging.FormatConvertedBitmap]::new($bgr, [System.Windows.Media.PixelFormats]::Indexed8, $palette, 0)
    if ($width -eq 0) { $width = $indexed.PixelWidth; $height = $indexed.PixelHeight }
    elseif ($indexed.PixelWidth -ne $width -or $indexed.PixelHeight -ne $height) {
        throw "$($file.Name) is $($indexed.PixelWidth)x$($indexed.PixelHeight) after scaling; the first frame is ${width}x$height."
    }

    $stride = ($width + 3) -band -4
    $raw = [byte[]]::new($stride * $height)
    $indexed.CopyPixels($raw, $stride, 0)
    $pixels = [byte[]]::new($width * $height)
    for ($row = 0; $row -lt $height; $row++) { [Array]::Copy($raw, $row * $stride, $pixels, $row * $width, $width) }
    $frames.Add($pixels)

    $colors = [byte[]]::new(768)
    $index = 0
    foreach ($color in $palette.Colors) {
        $colors[$index * 3] = $color.R
        $colors[$index * 3 + 1] = $color.G
        $colors[$index * 3 + 2] = $color.B
        $index++
    }
    $palettes.Add($colors)
}

$delays = [int[]]::new($frames.Count)
for ($i = 0; $i -lt $frames.Count; $i++) { $delays[$i] = [int][Math]::Round($FrameDelayMs / 10) }
$delays[-1] = [int][Math]::Round($LastFrameDelayMs / 10)

$output = [IO.Path]::GetFullPath($OutputPath)
[CernealaGifWriter]::Write($output, $width, $height, $frames.ToArray(), $palettes.ToArray(), $delays)

# Read the file back with WPF's GIF decoder, so a broken encoding fails here and not in the pull request.
$check = [System.Windows.Media.Imaging.GifBitmapDecoder]::new(
    [Uri]$output, [System.Windows.Media.Imaging.BitmapCreateOptions]::None, [System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad)
if ($check.Frames.Count -ne $frames.Count) { throw "The GIF decodes to $($check.Frames.Count) frames, expected $($frames.Count)." }
'{0}: {1} frames, {2}x{3}, {4:N0} KB' -f $output, $frames.Count, $width, $height, ((Get-Item -LiteralPath $output).Length / 1KB)
