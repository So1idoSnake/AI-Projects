$ErrorActionPreference = 'Stop'

$source = Join-Path $PSScriptRoot 'src\DisplayController\DisplayController.cs'
$icon = Join-Path $PSScriptRoot 'assets\gaming-display-controller.ico'
$outputDirectory = Join-Path $PSScriptRoot 'outputs\DisplayController'
$output = Join-Path $outputDirectory 'DisplayController.exe'
$trayOutput = Join-Path $outputDirectory 'DisplayControllerTray.exe'
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$iconSwitch = "/win32icon:$icon"
$iconResourceSwitch = "/resource:$icon,GamingDisplayController.TrayIcon"

New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null

& $compiler /nologo /target:exe /platform:x86 /define:RTSS_X86 /optimize+ $iconSwitch $iconResourceSwitch /out:$output /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll $source
if ($LASTEXITCODE -ne 0) {
    throw "C# compilation failed with exit code $LASTEXITCODE"
}

& $compiler /nologo /target:winexe /platform:x86 /define:RTSS_X86 /optimize+ $iconSwitch $iconResourceSwitch /out:$trayOutput /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll $source
if ($LASTEXITCODE -ne 0) {
    throw "C# tray compilation failed with exit code $LASTEXITCODE"
}

Write-Output $output
Write-Output $trayOutput
