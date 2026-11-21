$ErrorActionPreference = 'Stop'
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$source = Join-Path $PSScriptRoot 'src\DisplayController\DisplayController.cs'
$tests = Join-Path $PSScriptRoot 'src\DisplayController\RecoveryTests.cs'
$outputDirectory = Join-Path $PSScriptRoot 'work\recovery-tests'
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
$output = Join-Path $outputDirectory 'RecoveryTests.exe'
& $compiler /nologo /target:exe /platform:x86 /define:RTSS_X86 /main:GamingDisplayController.RecoveryTests /out:$output /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll $source $tests
if ($LASTEXITCODE -ne 0) { throw 'Recovery test compilation failed.' }
& $output
if ($LASTEXITCODE -ne 0) { throw 'Recovery checks failed.' }
