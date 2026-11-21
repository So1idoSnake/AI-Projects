param(
    [Parameter(Mandatory = $true)]
    [string]$InputPath,

    [Parameter(Mandatory = $true)]
    [string]$OutputPng,

    [Parameter(Mandatory = $true)]
    [string]$OutputIco,

    [string]$PreviewPath
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

function New-TransparentBitmap([int]$width, [int]$height) {
    return New-Object System.Drawing.Bitmap($width, $height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
}

function Draw-ScaledImage($source, [int]$width, [int]$height) {
    $target = New-TransparentBitmap $width $height
    $graphics = [System.Drawing.Graphics]::FromImage($target)
    try {
        $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
        $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $graphics.DrawImage($source, 0, 0, $width, $height)
    }
    finally {
        $graphics.Dispose()
    }
    return $target
}

$source = [System.Drawing.Bitmap]::FromFile((Resolve-Path -LiteralPath $InputPath))
try {
    $cleaned = New-TransparentBitmap $source.Width $source.Height
    try {
        $graphics = [System.Drawing.Graphics]::FromImage($cleaned)
        try {
            $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
            $graphics.DrawImageUnscaled($source, 0, 0)
        }
        finally {
            $graphics.Dispose()
        }

        $rect = New-Object System.Drawing.Rectangle(0, 0, $cleaned.Width, $cleaned.Height)
        $data = $cleaned.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::ReadWrite, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        try {
            $bytes = New-Object byte[] ($data.Stride * $data.Height)
            [Runtime.InteropServices.Marshal]::Copy($data.Scan0, $bytes, 0, $bytes.Length)

            $left = $cleaned.Width
            $top = $cleaned.Height
            $right = -1
            $bottom = -1

            for ($y = 0; $y -lt $data.Height; $y++) {
                for ($x = 0; $x -lt $data.Width; $x++) {
                    $offset = ($y * $data.Stride) + ($x * 4)
                    $blue = [int]$bytes[$offset]
                    $green = [int]$bytes[$offset + 1]
                    $red = [int]$bytes[$offset + 2]
                    $maximum = [Math]::Max($red, [Math]::Max($green, $blue))
                    $minimum = [Math]::Min($red, [Math]::Min($green, $blue))

                    # The generator rendered its transparency preview as neutral
                    # gray squares. The pet is entirely blue/cyan/navy, so removing
                    # bright low-chroma pixels isolates it without changing its art.
                    if (($maximum - $minimum) -le 18 -and $maximum -ge 120) {
                        $bytes[$offset + 3] = 0
                    }
                    else {
                        $bytes[$offset + 3] = 255
                        if ($x -lt $left) { $left = $x }
                        if ($x -gt $right) { $right = $x }
                        if ($y -lt $top) { $top = $y }
                        if ($y -gt $bottom) { $bottom = $y }
                    }
                }
            }

            [Runtime.InteropServices.Marshal]::Copy($bytes, 0, $data.Scan0, $bytes.Length)
        }
        finally {
            $cleaned.UnlockBits($data)
        }

        if ($right -lt $left -or $bottom -lt $top) {
            throw 'No foreground pixels remained after removing the generated checkerboard.'
        }

        $foregroundWidth = $right - $left + 1
        $foregroundHeight = $bottom - $top + 1
        $cropSize = [int][Math]::Ceiling([Math]::Max($foregroundWidth, $foregroundHeight) * 1.10)
        $cropSize = [Math]::Min($cropSize, [Math]::Min($cleaned.Width, $cleaned.Height))
        $centerX = ($left + $right) / 2.0
        $centerY = ($top + $bottom) / 2.0
        $cropLeft = [int][Math]::Round($centerX - ($cropSize / 2.0))
        $cropTop = [int][Math]::Round($centerY - ($cropSize / 2.0))
        $cropLeft = [Math]::Max(0, [Math]::Min($cropLeft, $cleaned.Width - $cropSize))
        $cropTop = [Math]::Max(0, [Math]::Min($cropTop, $cleaned.Height - $cropSize))

        $master = New-TransparentBitmap 1024 1024
        try {
            $graphics = [System.Drawing.Graphics]::FromImage($master)
            try {
                $graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
                $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
                $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
                $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
                $sourceRect = New-Object System.Drawing.Rectangle($cropLeft, $cropTop, $cropSize, $cropSize)
                $destinationRect = New-Object System.Drawing.Rectangle(0, 0, 1024, 1024)
                $graphics.DrawImage($cleaned, $destinationRect, $sourceRect, [System.Drawing.GraphicsUnit]::Pixel)
            }
            finally {
                $graphics.Dispose()
            }

            $pngDirectory = Split-Path -Parent $OutputPng
            $icoDirectory = Split-Path -Parent $OutputIco
            if ($pngDirectory) { New-Item -ItemType Directory -Path $pngDirectory -Force | Out-Null }
            if ($icoDirectory) { New-Item -ItemType Directory -Path $icoDirectory -Force | Out-Null }
            $master.Save($OutputPng, [System.Drawing.Imaging.ImageFormat]::Png)

            $sizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)
            $images = @()
            foreach ($size in $sizes) {
                $resized = Draw-ScaledImage $master $size $size
                try {
                    $stream = New-Object System.IO.MemoryStream
                    $resized.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
                    $images += ,$stream.ToArray()
                    $stream.Dispose()
                }
                finally {
                    $resized.Dispose()
                }
            }

            $file = [System.IO.File]::Open($OutputIco, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write)
            $writer = New-Object System.IO.BinaryWriter($file)
            try {
                $writer.Write([UInt16]0)
                $writer.Write([UInt16]1)
                $writer.Write([UInt16]$sizes.Count)
                $offset = 6 + (16 * $sizes.Count)
                for ($index = 0; $index -lt $sizes.Count; $index++) {
                    $size = $sizes[$index]
                    $encodedSize = if ($size -eq 256) { 0 } else { $size }
                    $writer.Write([byte]$encodedSize)
                    $writer.Write([byte]$encodedSize)
                    $writer.Write([byte]0)
                    $writer.Write([byte]0)
                    $writer.Write([UInt16]1)
                    $writer.Write([UInt16]32)
                    $writer.Write([UInt32]$images[$index].Length)
                    $writer.Write([UInt32]$offset)
                    $offset += $images[$index].Length
                }
                foreach ($imageBytes in $images) {
                    $writer.Write($imageBytes)
                }
            }
            finally {
                $writer.Dispose()
                $file.Dispose()
            }

            if ($PreviewPath) {
                $preview = New-Object System.Drawing.Bitmap(512, 256, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
                try {
                    $graphics = [System.Drawing.Graphics]::FromImage($preview)
                    try {
                        $graphics.Clear([System.Drawing.Color]::FromArgb(255, 30, 30, 30))
                        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
                        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
                        foreach ($sample in @(@(16, 32, 48), @(24, 208, 40), @(32, 352, 24))) {
                            $size = [int]$sample[0]
                            $x = [int]$sample[1]
                            $y = [int]$sample[2]
                            $tiny = Draw-ScaledImage $master $size $size
                            try {
                                $scale = [int](192 / $size)
                                $graphics.DrawImage($tiny, $x, $y, $size * $scale, $size * $scale)
                            }
                            finally {
                                $tiny.Dispose()
                            }
                        }
                    }
                    finally {
                        $graphics.Dispose()
                    }
                    $previewDirectory = Split-Path -Parent $PreviewPath
                    if ($previewDirectory) { New-Item -ItemType Directory -Path $previewDirectory -Force | Out-Null }
                    $preview.Save($PreviewPath, [System.Drawing.Imaging.ImageFormat]::Png)
                }
                finally {
                    $preview.Dispose()
                }
            }
        }
        finally {
            $master.Dispose()
        }
    }
    finally {
        $cleaned.Dispose()
    }
}
finally {
    $source.Dispose()
}

Write-Output $OutputPng
Write-Output $OutputIco
