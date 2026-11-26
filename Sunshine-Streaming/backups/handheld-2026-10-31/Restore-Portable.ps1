$ErrorActionPreference = 'Stop'
if (Get-Process -Name StreamLight -ErrorAction SilentlyContinue) {
    throw 'Close StreamLight completely before restoring the Portable profile.'
}
$root = 'HKCU:\Software\FoggyBytes\StreamLight\hostprofiles'
$profiles = @(Get-ChildItem -LiteralPath $root -Recurse | Where-Object {
    (Get-ItemProperty -LiteralPath $_.PSPath).name -eq 'Portable'
})
if ($profiles.Count -ne 1) { throw 'Expected exactly one original Portable profile.' }
foreach ($valueName in @('width','height','bitrate','codec','displaymode','framepacing','vsync','fractionalvsync','vrr','yuv444','hdr')) {
    Remove-ItemProperty -LiteralPath $profiles[0].PSPath -Name $valueName -ErrorAction SilentlyContinue
}
New-ItemProperty -LiteralPath $profiles[0].PSPath -Name fps -Value 116 -PropertyType DWord -Force | Out-Null
Write-Output 'Original Portable overrides restored. Docked and pairing were preserved.'
