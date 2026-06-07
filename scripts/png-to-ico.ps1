# Convert a PNG -> multi-size ICO (PNG-encoded, Windows Vista+).
param(
    [string]$SourcePath,
    [string]$DestPath
)
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

if (-not $SourcePath) { $SourcePath = Join-Path $PSScriptRoot "..\icon.png" }
if (-not $DestPath)   { $DestPath   = Join-Path $PSScriptRoot "..\icon.ico" }

$sizes = @(16, 32, 48, 64, 128, 256)
$original = [System.Drawing.Image]::FromFile($SourcePath)

$entries = @()
foreach ($s in $sizes) {
    $bmp = New-Object System.Drawing.Bitmap($s, $s)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.DrawImage($original, (New-Object System.Drawing.Rectangle(0, 0, $s, $s)))
    $g.Dispose()
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    $entries += [pscustomobject]@{ Size = $s; Bytes = $ms.ToArray() }
    $ms.Dispose()
}
$original.Dispose()

$fs = [System.IO.File]::Create($DestPath)
$bw = New-Object System.IO.BinaryWriter($fs)
$bw.Write([UInt16]0)               # reserved
$bw.Write([UInt16]1)               # type = icon
$bw.Write([UInt16]$entries.Count)

$offset = 6 + (16 * $entries.Count)
foreach ($e in $entries) {
    $dim = if ($e.Size -ge 256) { 0 } else { $e.Size }
    $bw.Write([Byte]$dim)
    $bw.Write([Byte]$dim)
    $bw.Write([Byte]0)
    $bw.Write([Byte]0)
    $bw.Write([UInt16]1)
    $bw.Write([UInt16]32)
    $bw.Write([UInt32]$e.Bytes.Length)
    $bw.Write([UInt32]$offset)
    $offset += $e.Bytes.Length
}
foreach ($e in $entries) { $bw.Write($e.Bytes) }
$bw.Flush(); $bw.Close(); $fs.Close()
Write-Host "Created $DestPath ($($entries.Count) sizes: $($sizes -join ', '))"
