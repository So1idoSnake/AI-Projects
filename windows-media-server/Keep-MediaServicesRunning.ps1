[CmdletBinding(DefaultParameterSetName = 'Watch')]
param(
    [Parameter(Mandatory, ParameterSetName = 'Install')]
    [switch]$InstallTask,
    [Parameter(ParameterSetName = 'Install')]
    [ValidateSet('Plex Media Server', 'Prowlarr', 'Sonarr', 'Radarr', 'Radarr Small Movies', 'qBittorrent')]
    [string[]]$HoldApps,
    [Parameter(ParameterSetName = 'Watch')]
    [switch]$CheckOnly
)

$ErrorActionPreference = 'Stop'
$taskName = 'Windows Media Server Watchdog'
$watchdogDirectory = Join-Path $env:APPDATA 'WindowsMediaServer\Watchdog'
$holdFile = Join-Path $watchdogDirectory 'held-apps.txt'

if ($InstallTask) {
    $installedScript = Join-Path $watchdogDirectory 'Keep-MediaServicesRunning.ps1'
    [void][IO.Directory]::CreateDirectory($watchdogDirectory)
    if ($PSCommandPath -ne $installedScript) {
        Copy-Item -LiteralPath $PSCommandPath -Destination $installedScript
    }
    if ($PSBoundParameters.ContainsKey('HoldApps')) {
        Set-Content -LiteralPath $holdFile -Value $HoldApps -Encoding UTF8
    }

    $user = [Security.Principal.WindowsIdentity]::GetCurrent().Name
    $principal = New-ScheduledTaskPrincipal -UserId $user -LogonType Interactive -RunLevel Limited
    $powershell = Join-Path $env:WINDIR 'System32\WindowsPowerShell\v1.0\powershell.exe'
    $arguments = '-NoProfile -NonInteractive -WindowStyle Hidden -ExecutionPolicy RemoteSigned -File "' + $installedScript + '"'
    $action = New-ScheduledTaskAction -Execute $powershell -Argument $arguments -WorkingDirectory $watchdogDirectory
    $atLogon = New-ScheduledTaskTrigger -AtLogOn -User $user
    $atLogon.Delay = 'PT45S'
    $repeating = New-ScheduledTaskTrigger -Once -At (Get-Date).AddMinutes(1) -RepetitionInterval (New-TimeSpan -Minutes 1)
    $settings = New-ScheduledTaskSettingsSet -MultipleInstances IgnoreNew -ExecutionTimeLimit (New-TimeSpan -Seconds 45) `
        -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries
    Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $atLogon, $repeating -Principal $principal `
        -Settings $settings -Description 'Restarts stopped media applications in their normal Windows user session.' -Force | Out-Null
    Start-ScheduledTask -TaskName $taskName
    Write-Output "Registered and started $taskName."
    return
}

function Write-WatchdogEvent([string]$Message) {
    if (!$CheckOnly) {
        [void][IO.Directory]::CreateDirectory($watchdogDirectory)
        $file = Join-Path $watchdogDirectory 'watchdog.log'
        if ((Test-Path -LiteralPath $file) -and (Get-Item -LiteralPath $file).Length -ge 256KB) {
            Move-Item -LiteralPath $file -Destination ($file + '.previous') -Force
        }
        Add-Content -LiteralPath $file -Value ('{0:o} {1}' -f [DateTimeOffset]::Now, $Message) -Encoding UTF8
    }
    Write-Output $Message
}

function Test-TorrentVpn {
    $mullvad = Join-Path $env:ProgramFiles 'Mullvad VPN\resources\mullvad.exe'
    if (!(Test-Path -LiteralPath $mullvad -PathType Leaf)) { return $false }
    $status = & $mullvad status 2>$null | Select-Object -First 1
    if ($LASTEXITCODE -ne 0 -or $status -notmatch '^Connected\b') { return $false }

    $profile = Join-Path $env:APPDATA 'qBittorrent\qBittorrent.ini'
    if (!(Test-Path -LiteralPath $profile -PathType Leaf)) { return $false }
    $adapters = @(Get-NetAdapter -Name 'Mullvad' -ErrorAction SilentlyContinue | Where-Object Status -eq 'Up')
    if ($adapters.Count -ne 1 -or !$adapters[0].InterfaceName) { return $false }
    $lines = Get-Content -LiteralPath $profile
    return ('Session\InterfaceName=Mullvad' -in $lines -and ('Session\Interface=' + $adapters[0].InterfaceName) -in $lines)
}

$apps = @(
    @{ Name = 'Plex Media Server'; Shortcut = 'Plex Media Server.lnk'; Port = 32400 },
    @{ Name = 'Prowlarr'; Shortcut = 'Windows Media Server - Prowlarr.lnk'; Port = 9696 },
    @{ Name = 'Sonarr'; Shortcut = 'Windows Media Server - Sonarr.lnk'; Port = 8989 },
    @{ Name = 'Radarr'; Shortcut = 'Windows Media Server - Radarr.lnk'; Port = 7878 },
    @{ Name = 'Radarr Small Movies'; Shortcut = 'Windows Media Server - Radarr Small Movies.lnk'; Port = 7879 },
    @{ Name = 'qBittorrent'; Shortcut = 'Windows Media Server - qBittorrent.lnk'; Port = 8080 }
)

$startupDirectory = [Environment]::GetFolderPath('Startup')
$shell = New-Object -ComObject WScript.Shell
$sessionId = (Get-Process -Id $PID).SessionId
$processes = @(Get-CimInstance Win32_Process -Filter "SessionId = $sessionId")
$listeners = [Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties().GetActiveTcpListeners()
$problems = $false
$heldApps = if (Test-Path -LiteralPath $holdFile) { Get-Content -LiteralPath $holdFile }

foreach ($app in $apps) {
    $name = $app.Name
    if ($name -in $heldApps) {
        if ($CheckOnly) { Write-Output "$name is on hold." }
        continue
    }
    try {
        $path = Join-Path $startupDirectory $app.Shortcut
        if (!(Test-Path -LiteralPath $path -PathType Leaf)) { throw 'The Startup shortcut is missing.' }
        $shortcut = $shell.CreateShortcut($path)
        if (!(Test-Path -LiteralPath $shortcut.TargetPath -PathType Leaf)) { throw 'The Startup shortcut target is missing.' }

        $expectedArguments = $shortcut.Arguments
        $running = $processes | Where-Object {
            $_.ExecutablePath -eq $shortcut.TargetPath -and (!$expectedArguments -or
                ($_.CommandLine -and $_.CommandLine.IndexOf($expectedArguments, [StringComparison]::OrdinalIgnoreCase) -ge 0))
        } | Select-Object -First 1
        # The home LAN port proxy shares the media app ports. Only the app listener counts here.
        $listener = $listeners | Where-Object {
            $_.Port -eq $app.Port -and ($name -eq 'Plex Media Server' -or [Net.IPAddress]::IsLoopback($_.Address))
        } | Select-Object -First 1

        if ($running -and $listener) {
            if ($CheckOnly) { Write-Output "$name is running on port $($app.Port)." }
            continue
        }
        if ($running) { throw "The process is running, but port $($app.Port) is unavailable." }
        if ($listener) { throw "Port $($app.Port) is already in use." }
        if ($name -eq 'qBittorrent' -and !(Test-TorrentVpn)) {
            throw 'Waiting for a connected Mullvad tunnel and the matching qBittorrent interface setting.'
        }
        if ($CheckOnly) {
            Write-Output "$name is stopped and can be restarted."
            $problems = $true
            continue
        }

        Invoke-Item -LiteralPath $path
        Write-WatchdogEvent "Requested restart for $name."
    } catch {
        Write-WatchdogEvent "$name needs attention. $($_.Exception.Message)"
        $problems = $true
    }
}

if ($problems) { exit 1 }
