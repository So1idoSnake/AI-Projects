$ErrorActionPreference = 'Stop'
if (Get-Process -Name StreamLight -ErrorAction SilentlyContinue) {
    throw 'Close StreamLight completely, then run this restore script again.'
}
$configPath = 'HKCU:\Software\FoggyBytes\StreamLight'
$dockProfiles = @(Get-ChildItem -LiteralPath ($configPath + '\hostprofiles') -Recurse |
    Where-Object { (Get-ItemProperty -LiteralPath $_.PSPath).name -eq 'Docked' })
if ($dockProfiles.Count -ne 1) { throw 'Could not uniquely identify the original Docked profile.' }
# These overrides did not exist before optimization. Remove only these added values.
foreach ($valueName in @('bitrate','codec','displaymode','vsync','framepacing','fractionalvsync','vrr','yuv444','hdr')) {
    Remove-ItemProperty -LiteralPath $dockProfiles[0].PSPath -Name $valueName -ErrorAction SilentlyContinue
}
# Restore the four global values changed by this task. Pairing data is untouched.
New-ItemProperty -LiteralPath $configPath -Name 'videodec' -Value 0 -PropertyType DWord -Force | Out-Null
New-ItemProperty -LiteralPath $configPath -Name 'yuv444' -Value 'true' -PropertyType String -Force | Out-Null
New-ItemProperty -LiteralPath $configPath -Name 'showperfoverlay' -Value 'false' -PropertyType String -Force | Out-Null
New-ItemProperty -LiteralPath $configPath -Name 'overlayitems' -Value 4579 -PropertyType DWord -Force | Out-Null
Write-Output 'Original StreamLight settings restored. Pairing information was preserved.'
