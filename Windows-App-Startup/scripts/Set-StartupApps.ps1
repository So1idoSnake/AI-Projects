# Reusable adaptation of the original successful shortcut procedure.
# This is a one-time setup script, not a persistent launcher.
[CmdletBinding(SupportsShouldProcess=$true)]
param(
    [ValidateSet('ChatGPT','Xbox','Gemini')]
    [string[]]$Apps = @('ChatGPT','Xbox','Gemini'),
    [switch]$Apply
)
$ErrorActionPreference = 'Stop'
$installed = @(Get-StartApps)
$startupFolder = [Environment]::GetFolderPath('Startup')
$explorerPath = Join-Path $env:SystemRoot 'explorer.exe'
if (-not (Test-Path -LiteralPath $startupFolder -PathType Container)) {
    throw 'The current user Startup folder is unavailable.'
}
$wsh = New-Object -ComObject WScript.Shell
$plans = foreach ($name in ($Apps | Select-Object -Unique)) {
    $matches = @($installed | Where-Object { $_.Name -ieq $name })
    if ($matches.Count -ne 1) {
        throw "Expected one installed Start app named $name; found $($matches.Count). Resolve the name/ID before applying."
    }
    $shortcutPath = Join-Path $startupFolder ($name + '.lnk')
    $arguments = 'shell:AppsFolder\' + $matches[0].AppID
    $exists = Test-Path -LiteralPath $shortcutPath
    if ($exists) {
        $existing = $wsh.CreateShortcut($shortcutPath)
        if ($existing.TargetPath -ine $explorerPath -or $existing.Arguments -ine $arguments) {
            throw "Conflicting existing shortcut: $shortcutPath. Review it manually; nothing was overwritten."
        }
    }
    [pscustomobject]@{
        Name=$name; AppId=$matches[0].AppID; Shortcut=$shortcutPath
        Target=$explorerPath; Arguments=$arguments; AlreadyConfigured=$exists
    }
}
# Build and validate all plans before writing any shortcut.
$plans | Select-Object Name,AppId,Shortcut,AlreadyConfigured
if (-not $Apply) {
    Write-Output 'Preview only. Add -Apply to create missing shortcuts; -WhatIf is also supported.'
    return
}
foreach ($plan in $plans) {
    if ($plan.AlreadyConfigured) { continue }
    if ($PSCmdlet.ShouldProcess($plan.Shortcut, 'Create per-user Startup shortcut')) {
        $link = $wsh.CreateShortcut($plan.Shortcut)
        $link.TargetPath = $plan.Target
        $link.Arguments = $plan.Arguments
        $link.WorkingDirectory = $env:SystemRoot
        $link.Description = 'Launch ' + $plan.Name + ' at Windows sign-in'
        $link.Save()
        $verified = $wsh.CreateShortcut($plan.Shortcut)
        if ($verified.TargetPath -ine $plan.Target -or $verified.Arguments -ine $plan.Arguments) {
            throw "Shortcut verification failed: $($plan.Shortcut)"
        }
        Write-Output ('Created: ' + $plan.Shortcut)
    }
}
