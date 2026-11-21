$ErrorActionPreference = 'Stop'
try {
    Restart-Service -Name SunshineService
    'Sunshine service restarted.' | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'sunshine-restart-result.txt')
} catch {
    $_.Exception.Message | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'sunshine-restart-result.txt')
    exit 1
}
