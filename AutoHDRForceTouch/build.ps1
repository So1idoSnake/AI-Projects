[CmdletBinding()]
param(
    [switch]$SkipTests,
    [string]$Destination
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($Destination)) {
    $Destination = Join-Path $PSScriptRoot 'dist'
}

$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) {
    throw "The required .NET Framework C# compiler was not found at $compiler"
}

$sourceDirectory = Join-Path $PSScriptRoot 'src'
$assetDirectory = Join-Path $PSScriptRoot 'assets'
$testDirectory = Join-Path $PSScriptRoot 'tests'
$buildDirectory = Join-Path $PSScriptRoot 'build'
New-Item -ItemType Directory -Force -Path $buildDirectory, $Destination | Out-Null

$iconPngPath = Join-Path $assetDirectory 'AutoHDRForceTouch.png'
$iconPath = Join-Path $assetDirectory 'AutoHDRForceTouch.ico'
if (-not (Test-Path -LiteralPath $iconPngPath)) {
    throw "The application icon source was not found at $iconPngPath"
}

# Regenerate the ICO on every build so the embedded executable icon and the
# separately supplied Xbox/library artwork cannot drift apart.
& (Join-Path $PSScriptRoot 'tools\Convert-PngToIco.ps1') -InputPath $iconPngPath -OutputPath $iconPath

$applicationPath = Join-Path $buildDirectory 'AutoHDRForceTouch.exe'
$applicationArguments = @(
    '/nologo',
    '/warn:4',
    '/warnaserror+',
    '/optimize+',
    '/langversion:5',
    '/target:winexe',
    '/platform:anycpu',
    ('/win32manifest:' + (Join-Path $sourceDirectory 'app.manifest')),
    ('/win32icon:' + $iconPath),
    '/reference:System.dll',
    '/reference:System.Core.dll',
    '/reference:System.Drawing.dll',
    '/reference:System.Windows.Forms.dll',
    ('/out:' + $applicationPath),
    (Join-Path $sourceDirectory 'Program.cs'),
    (Join-Path $sourceDirectory 'MainForm.cs'),
    (Join-Path $sourceDirectory 'RegistryBackupService.cs'),
    (Join-Path $sourceDirectory 'RegistryService.cs')
)

& $compiler @applicationArguments
if ($LASTEXITCODE -ne 0) {
    throw "Application compilation failed with exit code $LASTEXITCODE."
}

$testPath = Join-Path $buildDirectory 'RegistryServiceTests.exe'
if (-not $SkipTests) {
    $testArguments = @(
        '/nologo',
        '/warn:4',
        '/warnaserror+',
        '/optimize+',
        '/langversion:5',
        '/target:exe',
        '/platform:anycpu',
        '/reference:System.dll',
        '/reference:System.Core.dll',
        ('/out:' + $testPath),
        '/main:AutoHDRForceTouchTests.RegistryServiceTests',
        (Join-Path $sourceDirectory 'RegistryBackupService.cs'),
        (Join-Path $sourceDirectory 'RegistryService.cs'),
        (Join-Path $testDirectory 'RegistryServiceTests.cs')
    )

    & $compiler @testArguments
    if ($LASTEXITCODE -ne 0) {
        throw "Test compilation failed with exit code $LASTEXITCODE."
    }

    & $testPath
    if ($LASTEXITCODE -ne 0) {
        throw "Registry tests failed with exit code $LASTEXITCODE."
    }
}

$configurationSource = Join-Path $sourceDirectory 'AutoHDRForceTouch.exe.config'
Copy-Item -LiteralPath $applicationPath -Destination (Join-Path $Destination 'AutoHDRForceTouch.exe') -Force
Copy-Item -LiteralPath $configurationSource -Destination (Join-Path $Destination 'AutoHDRForceTouch.exe.config') -Force
Copy-Item -LiteralPath $iconPngPath -Destination (Join-Path $Destination 'AutoHDRForceTouch.png') -Force
Copy-Item -LiteralPath $iconPath -Destination (Join-Path $Destination 'AutoHDRForceTouch.ico') -Force

Write-Host "Built: $(Join-Path $Destination 'AutoHDRForceTouch.exe')"
