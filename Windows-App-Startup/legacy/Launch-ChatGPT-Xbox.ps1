$ErrorActionPreference = 'Stop'

$logPath = 'C:\Users\micha\AppData\Local\CodexStartup\ChatGPT-Xbox-Startup.log'

function Write-LauncherLog {
    param([string]$Message)
    Add-Content -LiteralPath $logPath -Value "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') | $Message"
}

try {
    Write-LauncherLog 'Scheduled launcher began'

    Start-Sleep -Seconds 10
    Start-Process -FilePath 'C:\Windows\explorer.exe' -ArgumentList 'shell:AppsFolder\OpenAI.Codex_2p2nqsd0c76g0!App'
    Write-LauncherLog 'ChatGPT launch requested successfully'

    Start-Sleep -Seconds 3
    Start-Process -FilePath 'C:\Windows\explorer.exe' -ArgumentList 'shell:AppsFolder\Microsoft.GamingApp_8wekyb3d8bbwe!Microsoft.Xbox.App'
    Write-LauncherLog 'Xbox launch requested successfully'

    Write-LauncherLog 'Scheduled launcher finished'
    exit 0
}
catch {
    Write-LauncherLog "Scheduled launcher failed: $($_.Exception.Message)"
    exit 1
}
