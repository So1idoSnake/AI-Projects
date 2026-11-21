$ErrorActionPreference = 'Stop'

$taskName = 'Launch ChatGPT and Xbox'
$launcherPath = 'C:\Users\micha\AppData\Local\CodexStartup\Launch-ChatGPT-Xbox.ps1'
$userId = "$env:USERDOMAIN\$env:USERNAME"

if (-not (Test-Path -LiteralPath $launcherPath)) {
    throw "Launcher not found: $launcherPath"
}

$action = New-ScheduledTaskAction `
    -Execute "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe" `
    -Argument "-NoProfile -NonInteractive -WindowStyle Hidden -ExecutionPolicy Bypass -File `"$launcherPath`""

$trigger = New-ScheduledTaskTrigger -AtLogOn -User $userId
$principal = New-ScheduledTaskPrincipal `
    -UserId $userId `
    -LogonType Interactive `
    -RunLevel Limited

$settings = New-ScheduledTaskSettingsSet `
    -AllowStartIfOnBatteries `
    -DontStopIfGoingOnBatteries `
    -StartWhenAvailable `
    -ExecutionTimeLimit (New-TimeSpan -Minutes 2) `
    -MultipleInstances IgnoreNew

Register-ScheduledTask `
    -TaskName $taskName `
    -Action $action `
    -Trigger $trigger `
    -Principal $principal `
    -Settings $settings `
    -Description 'Starts ChatGPT 10 seconds after logon, then Xbox 3 seconds later.' `
    -Force | Out-Null

$installedTask = Get-ScheduledTask -TaskName $taskName
if ($installedTask.State -eq 'Disabled') {
    throw 'The scheduled task was created but is disabled.'
}

exit 0
