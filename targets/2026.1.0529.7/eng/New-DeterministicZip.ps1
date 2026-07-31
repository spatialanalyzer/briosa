[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Source,

    [Parameter(Mandatory)]
    [string]$Destination,

    [Parameter(Mandatory)]
    [ValidatePattern('^[^/\\]+$')]
    [string]$RootName
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$sourceRoot = [IO.Path]::GetFullPath($Source)
$destinationPath = [IO.Path]::GetFullPath($Destination)
if (-not (Test-Path -LiteralPath $sourceRoot -PathType Container)) {
    throw "The deterministic ZIP source directory does not exist."
}

$sourcePrefix = $sourceRoot
if (-not $sourcePrefix.EndsWith([string][IO.Path]::DirectorySeparatorChar) -and
    -not $sourcePrefix.EndsWith([string][IO.Path]::AltDirectorySeparatorChar)) {
    $sourcePrefix += [IO.Path]::DirectorySeparatorChar
}
if ($destinationPath.StartsWith(
        $sourcePrefix,
        [StringComparison]::OrdinalIgnoreCase)) {
    throw "The deterministic ZIP destination must be outside the source directory."
}

$destinationDirectory = Split-Path -Parent $destinationPath
[IO.Directory]::CreateDirectory($destinationDirectory) | Out-Null

[string[]]$relativePaths = @(
    Get-ChildItem -LiteralPath $sourceRoot -File -Recurse |
        ForEach-Object {
            if (-not $_.FullName.StartsWith(
                    $sourcePrefix,
                    [StringComparison]::OrdinalIgnoreCase)) {
                throw "A deterministic ZIP input resolved outside the source directory."
            }

            $_.FullName.Substring($sourcePrefix.Length).Replace('\', '/')
        })
[Array]::Sort($relativePaths, [StringComparer]::Ordinal)

if ($relativePaths.Count -gt [UInt16]::MaxValue) {
    throw "The deterministic ZIP writer does not support more than 65,535 files."
}
if (Test-Path -LiteralPath $destinationPath) {
    throw "The deterministic ZIP destination already exists."
}

if ($null -eq ("Briosa.Build.Crc32" -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.IO;

namespace Briosa.Build
{
    public static class Crc32
    {
        private static readonly uint[] Table = CreateTable();

        public static uint Compute(string path)
        {
            uint crc = UInt32.MaxValue;
            byte[] buffer = new byte[65536];
            using (FileStream stream = File.OpenRead(path))
            {
                int read;
                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    for (int index = 0; index < read; index++)
                    {
                        crc = Table[(crc ^ buffer[index]) & 0xff] ^ (crc >> 8);
                    }
                }
            }

            return ~crc;
        }

        private static uint[] CreateTable()
        {
            uint[] table = new uint[256];
            for (uint index = 0; index < table.Length; index++)
            {
                uint value = index;
                for (int bit = 0; bit < 8; bit++)
                {
                    value = (value & 1) == 1
                        ? 0xedb88320U ^ (value >> 1)
                        : value >> 1;
                }

                table[index] = value;
            }

            return table;
        }
    }
}
'@
}

# Write the ZIP32 records directly. ZipArchive chooses different stored/Deflate
# encodings across .NET implementations even for CompressionLevel.NoCompression.
$utf8 = [Text.UTF8Encoding]::new($false)
$storedMethod = [UInt16]0
$utf8Flag = [UInt16]0x0800
$fixedDosTime = [UInt16]0
$fixedDosDate = [UInt16]0x0021
$version20 = [UInt16]20
$centralRecords = New-Object 'System.Collections.Generic.List[object]'
$stream = [IO.FileStream]::new(
    $destinationPath,
    [IO.FileMode]::CreateNew,
    [IO.FileAccess]::Write,
    [IO.FileShare]::None)
try {
    $writer = [IO.BinaryWriter]::new($stream, $utf8, $true)
    try {
        foreach ($relativePath in $relativePaths) {
            $entryName = "$RootName/$relativePath"
            $entryNameBytes = $utf8.GetBytes($entryName)
            if ($entryNameBytes.Length -gt [UInt16]::MaxValue) {
                throw "A deterministic ZIP entry name is too long."
            }

            $inputPath = Join-Path $sourceRoot $relativePath.Replace(
                '/',
                [IO.Path]::DirectorySeparatorChar)
            $fileLength = (Get-Item -LiteralPath $inputPath).Length
            if ($fileLength -gt [UInt32]::MaxValue) {
                throw "The deterministic ZIP writer does not support files larger than 4 GiB."
            }
            if ($stream.Position -gt [UInt32]::MaxValue) {
                throw "The deterministic ZIP writer does not support ZIP64 offsets."
            }

            $size = [UInt32]$fileLength
            $crc32 = [Briosa.Build.Crc32]::Compute($inputPath)
            $localHeaderOffset = [UInt32]$stream.Position

            $writer.Write([UInt32]0x04034b50)
            $writer.Write($version20)
            $writer.Write($utf8Flag)
            $writer.Write($storedMethod)
            $writer.Write($fixedDosTime)
            $writer.Write($fixedDosDate)
            $writer.Write($crc32)
            $writer.Write($size)
            $writer.Write($size)
            $writer.Write([UInt16]$entryNameBytes.Length)
            $writer.Write([UInt16]0)
            $writer.Write($entryNameBytes)

            $input = [IO.File]::OpenRead($inputPath)
            try {
                $input.CopyTo($stream)
            }
            finally {
                $input.Dispose()
            }

            $centralRecords.Add([pscustomobject]@{
                NameBytes = $entryNameBytes
                Crc32 = $crc32
                Size = $size
                LocalHeaderOffset = $localHeaderOffset
            })
        }

        if ($stream.Position -gt [UInt32]::MaxValue) {
            throw "The deterministic ZIP writer does not support ZIP64 offsets."
        }
        $centralDirectoryOffset = [UInt32]$stream.Position
        foreach ($record in $centralRecords) {
            $writer.Write([UInt32]0x02014b50)
            $writer.Write($version20)
            $writer.Write($version20)
            $writer.Write($utf8Flag)
            $writer.Write($storedMethod)
            $writer.Write($fixedDosTime)
            $writer.Write($fixedDosDate)
            $writer.Write([UInt32]$record.Crc32)
            $writer.Write([UInt32]$record.Size)
            $writer.Write([UInt32]$record.Size)
            $writer.Write([UInt16]$record.NameBytes.Length)
            $writer.Write([UInt16]0)
            $writer.Write([UInt16]0)
            $writer.Write([UInt16]0)
            $writer.Write([UInt16]0)
            $writer.Write([UInt32]0)
            $writer.Write([UInt32]$record.LocalHeaderOffset)
            $writer.Write([byte[]]$record.NameBytes)
        }

        $centralDirectoryLength = $stream.Position - $centralDirectoryOffset
        if ($centralDirectoryLength -gt [UInt32]::MaxValue) {
            throw "The deterministic ZIP writer does not support ZIP64 central directories."
        }

        $entryCount = [UInt16]$centralRecords.Count
        $writer.Write([UInt32]0x06054b50)
        $writer.Write([UInt16]0)
        $writer.Write([UInt16]0)
        $writer.Write($entryCount)
        $writer.Write($entryCount)
        $writer.Write([UInt32]$centralDirectoryLength)
        $writer.Write($centralDirectoryOffset)
        $writer.Write([UInt16]0)
        $writer.Flush()
    }
    finally {
        $writer.Dispose()
    }
}
finally {
    $stream.Dispose()
}
