param(
    [Parameter(Mandatory = $true)]
    [string[]] $Path
)

function Get-UInt16([byte[]] $Bytes, [int] $Offset) {
    if ($Offset -lt 0 -or $Offset + 2 -gt $Bytes.Length) { throw "Invalid PE offset: $Offset" }
    [BitConverter]::ToUInt16($Bytes, $Offset)
}

function Get-UInt32([byte[]] $Bytes, [int] $Offset) {
    if ($Offset -lt 0 -or $Offset + 4 -gt $Bytes.Length) { throw "Invalid PE offset: $Offset" }
    [BitConverter]::ToUInt32($Bytes, $Offset)
}

function Get-AsciiZ([byte[]] $Bytes, [int] $Offset) {
    if ($Offset -lt 0 -or $Offset -ge $Bytes.Length) { return $null }
    $end = $Offset
    $limit = [Math]::Min($Bytes.Length, $Offset + 512)
    while ($end -lt $limit -and $Bytes[$end] -ne 0) { $end++ }
    if ($end -eq $Offset) { return $null }
    [Text.Encoding]::ASCII.GetString($Bytes, $Offset, $end - $Offset)
}

function Get-PeMetadata([string] $FilePath) {
    $bytes = [IO.File]::ReadAllBytes($FilePath)
    if ($bytes.Length -lt 256 -or (Get-UInt16 $bytes 0) -ne 0x5A4D) { throw "Not a PE file" }

    $peOffset = [int](Get-UInt32 $bytes 0x3C)
    if ((Get-UInt32 $bytes $peOffset) -ne 0x00004550) { throw "Invalid PE signature" }

    $machine = Get-UInt16 $bytes ($peOffset + 4)
    $sectionCount = Get-UInt16 $bytes ($peOffset + 6)
    $optionalSize = Get-UInt16 $bytes ($peOffset + 20)
    $optionalOffset = $peOffset + 24
    $magic = Get-UInt16 $bytes $optionalOffset
    $dataDirectoryOffset = if ($magic -eq 0x10B) { $optionalOffset + 96 } elseif ($magic -eq 0x20B) { $optionalOffset + 112 } else { throw "Unknown optional-header format" }
    $sectionOffset = $optionalOffset + $optionalSize

    $sections = for ($i = 0; $i -lt $sectionCount; $i++) {
        $base = $sectionOffset + ($i * 40)
        [pscustomobject]@{
            VirtualSize = Get-UInt32 $bytes ($base + 8)
            VirtualAddress = Get-UInt32 $bytes ($base + 12)
            RawSize = Get-UInt32 $bytes ($base + 16)
            RawAddress = Get-UInt32 $bytes ($base + 20)
        }
    }

    function Convert-Rva([uint32] $Rva) {
        foreach ($section in $sections) {
            $span = [Math]::Max([uint32]$section.VirtualSize, [uint32]$section.RawSize)
            if ($Rva -ge $section.VirtualAddress -and $Rva -lt ($section.VirtualAddress + $span)) {
                return [int]($section.RawAddress + ($Rva - $section.VirtualAddress))
            }
        }
        return $null
    }

    $importRva = Get-UInt32 $bytes ($dataDirectoryOffset + 8)
    $imports = [Collections.Generic.List[string]]::new()
    if ($importRva -ne 0) {
        $descriptorOffset = Convert-Rva $importRva
        for ($i = 0; $null -ne $descriptorOffset -and $i -lt 4096; $i++) {
            $entry = $descriptorOffset + ($i * 20)
            if ($entry + 20 -gt $bytes.Length) { break }
            $originalThunk = Get-UInt32 $bytes $entry
            $timeStamp = Get-UInt32 $bytes ($entry + 4)
            $forwarder = Get-UInt32 $bytes ($entry + 8)
            $nameRva = Get-UInt32 $bytes ($entry + 12)
            $firstThunk = Get-UInt32 $bytes ($entry + 16)
            if (($originalThunk -bor $timeStamp -bor $forwarder -bor $nameRva -bor $firstThunk) -eq 0) { break }
            $nameOffset = Convert-Rva $nameRva
            if ($null -ne $nameOffset) {
                $name = Get-AsciiZ $bytes $nameOffset
                if ($name) { $imports.Add($name) }
            }
        }
    }

    $architecture = switch ($machine) {
        0x014C { 'x86' }
        0x8664 { 'x64' }
        0xAA64 { 'ARM64' }
        default { ('0x{0:X4}' -f $machine) }
    }

    [pscustomobject]@{
        Path = $FilePath
        Architecture = $architecture
        Imports = @($imports | Sort-Object -Unique)
    }
}

foreach ($item in $Path) {
    try {
        Get-PeMetadata -FilePath $item
    }
    catch {
        [pscustomobject]@{
            Path = $item
            Architecture = 'Unknown'
            Imports = @()
            Error = $_.Exception.Message
        }
    }
}
