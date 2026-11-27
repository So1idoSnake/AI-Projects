#requires -Version 7.0
[CmdletBinding()]
param(
    [string]$PrivateBackupRoot = (Join-Path $env:LOCALAPPDATA 'WindowsMediaServer\Backups'),
    [Parameter(Mandatory)][string]$SnapshotDirectory,
    [switch]$UseExistingNativeBackups,
    [switch]$SkipPlexArtwork
)
$ErrorActionPreference = 'Stop'
$base = Join-Path $env:LOCALAPPDATA 'WindowsMediaServer'
$stamp = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ')
$private = Join-Path $PrivateBackupRoot ('BeforeProfiles-' + $stamp)
$cursor = [IO.DirectoryInfo]::new([IO.Path]::GetFullPath($private))
while ($null -ne $cursor) {
    if (Test-Path -LiteralPath (Join-Path $cursor.FullName '.git')) { throw 'Private backups must stay outside every Git worktree.' }
    $cursor = $cursor.Parent
}
$null = New-Item -ItemType Directory -Path $private -Force
$null = New-Item -ItemType Directory -Path $SnapshotDirectory -Force
$utf8 = [Text.UTF8Encoding]::new($false)
$secrets = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$secretNames = '(?i)(password|passphrase|api.?key|token|secret|cookie|passkey|username|email|identifier|machine.?id|client.?id|fingerprint)'
function Write-Json([string]$Path, $Value) {
    $null = New-Item -ItemType Directory -Path (Split-Path $Path) -Force
    [IO.File]::WriteAllText($Path, (ConvertTo-Json -InputObject $Value -Depth 60), $utf8)
}
function Remember-Secret($Value) {
    if ($Value -is [string] -and $Value.Length -ge 12 -and $Value -notmatch '^\*+$') { $null = $secrets.Add($Value) }
}
function Protect-Value($Value) {
    if ($null -eq $Value) { return $null }
    if ($Value -is [string]) {
        $text = $Value.Replace($env:USERPROFILE, '%USERPROFILE%')
        return [regex]::Replace($text, '(?i)([?&](?:apikey|api_key|token|passkey|password|r|i)=)[^&\s]+', '$1[REDACTED]')
    }
    if ($Value -is [Collections.IDictionary]) {
        $safe = [ordered]@{}
        foreach ($key in $Value.Keys) {
            if ([string]$key -match $secretNames) { Remember-Secret $Value[$key]; $safe[$key] = '[REDACTED]' }
            else { $safe[$key] = Protect-Value $Value[$key] }
        }
        return $safe
    }
    if ($Value -is [Collections.IEnumerable]) {
        $items = @()
        foreach ($item in $Value) { $items += ,(Protect-Value $item) }
        return ,$items
    }
    if ($Value -is [pscustomobject]) {
        $safe = [ordered]@{}
        $fieldName = [string]$Value.name
        foreach ($property in $Value.PSObject.Properties) {
            if ($property.Name -match $secretNames -or ($property.Name -eq 'value' -and $fieldName -match $secretNames)) {
                Remember-Secret $property.Value
                $safe[$property.Name] = '[REDACTED]'
            } elseif ($property.Name -eq 'fields') {
                $safe[$property.Name] = @(foreach ($field in $property.Value) {
                    Protect-Value ([pscustomobject]@{ name = $field.name; value = $field.value })
                })
            } elseif ($property.Name -in @('presets', 'infoLink')) {
                continue
            } else { $safe[$property.Name] = Protect-Value $property.Value }
        }
        return $safe
    }
    return $Value
}
function Copy-Tree([string]$Source, [string]$Destination) {
    if (!(Test-Path -LiteralPath $Source)) { return }
    $null = New-Item -ItemType Directory -Path $Destination -Force
    & robocopy.exe $Source $Destination /E /COPY:DAT /DCOPY:DAT /R:1 /W:1 /XJ /NFL /NDL /NP /NJH /NJS | Out-Null
    if ($LASTEXITCODE -ge 8) { throw ('Copy failed for ' + (Split-Path $Source -Leaf)) }
}
$records = @()
foreach ($app in @('Prowlarr', 'Sonarr', 'Radarr', 'Radarr Small Movies')) {
    [xml]$config = Get-Content -LiteralPath (Join-Path $base ('Data/' + $app + '/config.xml'))
    Remember-Secret ([string]$config.Config.ApiKey)
    $headers = @{ 'X-Api-Key' = [string]$config.Config.ApiKey }
    $version = if ($app -eq 'Prowlarr') { 'v1' } else { 'v3' }
    $api = 'http://127.0.0.1:' + $config.Config.Port + '/api/' + $version
    if (!$UseExistingNativeBackups) {
        $command = Invoke-RestMethod ($api + '/command') -Method Post -Headers $headers -ContentType application/json -Body '{"name":"Backup"}'
        for ($attempt = 0; $attempt -lt 120; $attempt++) {
            $state = Invoke-RestMethod ($api + '/command/' + $command.id) -Headers $headers
            if ($state.status -eq 'completed') { break }
            if ($state.status -eq 'failed') { throw ($app + ' native backup failed.') }
            Start-Sleep -Milliseconds 500
        }
        if ($state.status -ne 'completed') { throw ($app + ' backup did not complete in time.') }
    }
    $archive = Get-ChildItem -LiteralPath (Join-Path $base ('Data/' + $app + '/Backups')) -Filter '*.zip' -Recurse |
        Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
    if (!$archive -or $archive.Length -eq 0 -or $archive.LastWriteTimeUtc -lt [DateTime]::UtcNow.AddMinutes(-30)) { throw ($app + ' needs a fresh native backup.') }
    $appPrivate = Join-Path $private $app
    $null = New-Item -ItemType Directory -Path $appPrivate -Force
    Copy-Item -LiteralPath $archive.FullName -Destination $appPrivate
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [IO.Compression.ZipFile]::OpenRead((Join-Path $appPrivate $archive.Name))
    try {
        $entries = @($zip.Entries | ForEach-Object FullName)
        if (!($entries -match '\.db$') -or !($entries -match 'config\.xml$')) { throw ($app + ' backup is missing its database or config.') }
    } finally { $zip.Dispose() }
    $safeApp = Join-Path $SnapshotDirectory ($app.Replace(' ', '-'))
    $null = New-Item -ItemType Directory -Path $safeApp -Force
    $safeXml = [xml]$config.OuterXml
    foreach ($node in @($safeXml.Config.ChildNodes)) {
        if ($node.Name -match $secretNames) { Remember-Secret $node.InnerText; $node.InnerText = '[REDACTED]' }
    }
    [IO.File]::WriteAllText((Join-Path $safeApp 'config.redacted.xml'), $safeXml.OuterXml.Replace($env:USERPROFILE, '%USERPROFILE%'), $utf8)
    $endpoints = if ($app -eq 'Prowlarr') {
        @('appprofile', 'applications', 'indexer', 'indexerproxy', 'downloadclient')
    } else {
        @('qualityprofile', 'qualitydefinition', 'customformat', 'delayprofile', 'config/mediamanagement', 'config/naming', 'config/indexer', 'config/downloadclient', 'downloadclient', 'indexer')
    }
    foreach ($endpoint in $endpoints) {
        $data = Invoke-RestMethod ($api + '/' + $endpoint) -Headers $headers
        $safe = Protect-Value $data
        Write-Json (Join-Path $safeApp ($endpoint.Replace('/', '-') + '.json')) $safe
    }
    if ($app -ne 'Prowlarr') {
        $roots = Invoke-RestMethod ($api + '/rootfolder') -Headers $headers
        Write-Json (Join-Path $safeApp 'rootfolder.json') @($roots | Select-Object path)
    }
    $records += [pscustomobject]@{ App = $app; NativeBackupVerified = $true; File = $archive.Name }
    Write-Output ($app + ': native backup verified and sanitized settings exported.')
}
Copy-Tree (Join-Path $base 'Private') (Join-Path $private 'Private')
Copy-Tree (Join-Path $base 'Data/NZBGet') (Join-Path $private 'NZBGet')
$nzbLines = Get-Content -LiteralPath (Join-Path $base 'Data/NZBGet/nzbget.conf')
$safeNzb = foreach ($line in $nzbLines) {
    if ($line -match '^([^#=]+)=(.*)$') {
        $key = $Matches[1]; $value = $Matches[2]
        if ($key -match $secretNames) { Remember-Secret $value; $key + '=[REDACTED]' }
        elseif ($key -match '(?i)Feed\d+\.(URL|Filter)') { $key + '=[REDACTED]' }
        else { $key + '=' + (Protect-Value $value) }
    }
}
$null = New-Item -ItemType Directory -Path (Join-Path $SnapshotDirectory 'NZBGet') -Force
[IO.File]::WriteAllLines((Join-Path $SnapshotDirectory 'NZBGet/nzbget.redacted.conf'), [string[]]$safeNzb, $utf8)
Copy-Tree (Join-Path $env:APPDATA 'qBittorrent') (Join-Path $private 'qBittorrent/Roaming')
Copy-Tree (Join-Path $env:LOCALAPPDATA 'qBittorrent/BT_backup') (Join-Path $private 'qBittorrent/BT_backup')
Copy-Tree (Join-Path $env:LOCALAPPDATA 'qBittorrent/rss') (Join-Path $private 'qBittorrent/LocalRSS')
$qbLines = Get-Content -LiteralPath (Join-Path $env:APPDATA 'qBittorrent/qBittorrent.ini')
$qbAllow = '^(Session\\|WebUI\\|Connection\\|Downloads\\|General\\Locale$|PortForwardingEnabled$)'
$safeQb = foreach ($line in $qbLines) {
    if ($line -match '^([^=]+)=(.*)$') {
        $key = $Matches[1]; $value = $Matches[2]
        if ($key -notmatch $qbAllow -or $key -match '(?i)history|recent|cookie|tracker|rss') { continue }
        if ($key -match ($secretNames + '|(?i:session\\interface(?:address)?$)')) { Remember-Secret $value; $key + '=[REDACTED]' }
        else { $key + '=' + (Protect-Value $value) }
    } elseif ($line -match '^\[') { $line }
}
$null = New-Item -ItemType Directory -Path (Join-Path $SnapshotDirectory 'qBittorrent') -Force
[IO.File]::WriteAllLines((Join-Path $SnapshotDirectory 'qBittorrent/qBittorrent.redacted.ini'), [string[]]$safeQb, $utf8)
$categories = Get-Content -LiteralPath (Join-Path $env:APPDATA 'qBittorrent/categories.json') -Raw | ConvertFrom-Json
Write-Json (Join-Path $SnapshotDirectory 'qBittorrent/categories.json') (Protect-Value $categories)
Write-Output 'NZBGet, private login store, and qBittorrent configuration/resume files copied privately.'
Copy-Tree (Join-Path $env:APPDATA 'WindowsMediaServer/Watchdog') (Join-Path $private 'Watchdog')
$null = New-Item -ItemType Directory -Path (Join-Path $private 'Startup') -Force
Get-ChildItem -LiteralPath ([Environment]::GetFolderPath('Startup')) -Filter '*.lnk' |
    Where-Object Name -match 'Windows Media Server|Plex Media Server' |
    Copy-Item -Destination (Join-Path $private 'Startup')
Copy-Tree (Join-Path ([Environment]::GetFolderPath('Desktop')) 'Media Server') (Join-Path $private 'Desktop-MediaServer')
Copy-Item -LiteralPath (Join-Path $base 'Open-MediaServerLogin.ps1') -Destination $private
$taskXml = Export-ScheduledTask -TaskName 'Windows Media Server Watchdog'
[IO.File]::WriteAllText((Join-Path $private 'Windows-Media-Server-Watchdog.xml'), $taskXml, $utf8)
$plexBase = Join-Path $env:LOCALAPPDATA 'Plex Media Server'
$plexPrivate = Join-Path $private 'Plex'
$null = New-Item -ItemType Directory -Path (Join-Path $plexPrivate 'Databases') -Force
& reg.exe export 'HKCU\Software\Plex, Inc.\Plex Media Server' (Join-Path $plexPrivate 'Plex-settings.reg') /y | Out-Null
if ($LASTEXITCODE -ne 0) { throw 'Plex preferences export failed.' }
$plexProcess = Get-CimInstance Win32_Process -Filter "Name='Plex Media Server.exe'" | Select-Object -First 1
$sqlite = Join-Path (Split-Path $plexProcess.ExecutablePath) 'Plex SQLite.exe'
if (!(Test-Path -LiteralPath $sqlite)) { throw 'Plex SQLite is needed for a consistent live database backup.' }
foreach ($name in @('com.plexapp.plugins.library.db', 'com.plexapp.plugins.library.blobs.db')) {
    $source = Join-Path $plexBase ('Plug-in Support/Databases/' + $name)
    $destination = Join-Path $plexPrivate ('Databases/' + $name)
    & $sqlite -readonly -batch -noinit $source (".timeout 30000") (".backup '" + $destination.Replace("'", "''") + "'") | Out-Null
    if ($LASTEXITCODE -ne 0 -or !(Test-Path -LiteralPath $destination)) { throw ('Plex SQLite backup failed: ' + $name) }
    $check = & $sqlite -readonly -batch -noinit $destination 'PRAGMA quick_check;'
    if ($LASTEXITCODE -ne 0 -or ($check -join '').Trim() -ne 'ok') { throw ('Plex database quick_check failed: ' + $name) }
    Write-Output ($name + ': consistent SQLite backup passed quick_check.')
}
if (!$SkipPlexArtwork) {
    foreach ($folder in @('Metadata', 'Media')) {
        Write-Output ('Copying Plex ' + $folder + '...')
        Copy-Tree (Join-Path $plexBase $folder) (Join-Path $plexPrivate $folder)
    }
}
foreach ($folder in @('Plug-ins', 'Scanners', 'Plug-in Support/Preferences', 'Plug-in Support/Data')) {
    Copy-Tree (Join-Path $plexBase $folder) (Join-Path $plexPrivate $folder)
}
if (Test-Path -LiteralPath (Join-Path $plexBase '.LocalAdminToken')) {
    Copy-Item -LiteralPath (Join-Path $plexBase '.LocalAdminToken') -Destination $plexPrivate
}
$plexSettings = Get-ItemProperty -LiteralPath 'HKCU:\Software\Plex, Inc.\Plex Media Server'
foreach ($property in $plexSettings.PSObject.Properties) {
    if ($property.Name -match $secretNames) { Remember-Secret ([string]$property.Value) }
}
$plexSafe = [ordered]@{}
foreach ($name in @('TranscoderVideo', 'ManualPortMappingMode', 'ManualPortMappingPort', 'DisableFSEvents', 'EnableIPv6', 'DlnaEnabled', 'sendCrashReports')) {
    $property = $plexSettings.PSObject.Properties[$name]
    if ($property) { $plexSafe[$name] = $property.Value }
}
Write-Json (Join-Path $SnapshotDirectory 'Plex/settings.selected.json') $plexSafe
$hashes = @(Get-ChildItem -LiteralPath $private -Recurse -File | ForEach-Object {
    [pscustomobject]@{ Path = [IO.Path]::GetRelativePath($private, $_.FullName); Bytes = $_.Length; SHA256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
})
Write-Json (Join-Path $private 'manifest.json') @{
    CreatedUtc = [DateTime]::UtcNow.ToString('o')
    Scope = 'Application state, settings, Plex databases/artwork, native arr archives, qBittorrent resume state; media files and installers excluded.'
    Consistency = 'arr native backup and Plex SQLite online backup; other runtime files/artwork are live copies. No services stopped.'
    NativeBackups = $records
    Files = $hashes
    ArtworkIncluded = !$SkipPlexArtwork
}
foreach ($file in Get-ChildItem -LiteralPath $SnapshotDirectory -Recurse -File) {
    $content = Get-Content -LiteralPath $file.FullName -Raw
    foreach ($secret in $secrets) {
        if ($content.Contains($secret)) { throw ('Secret scan failed in ' + $file.Name + '; do not stage this snapshot.') }
    }
}
Write-Json (Join-Path $SnapshotDirectory 'snapshot.json') @{
    CreatedUtc = [DateTime]::UtcNow.ToString('o')
    Purpose = 'Current configuration before quality profile changes'
    ApplicationBackups = $records
    SecretScanPassed = $true
    PrivateBackup = '%LOCALAPPDATA%\WindowsMediaServer\Backups\BeforeProfiles-' + $stamp
    Limitations = @('Credentials and personal databases are local only.', 'Sanitized JSON files are reference exports, not drop-in native restore archives.', 'Media content and third-party installations are not included.')
}
Write-Output ('PRIVATE_BACKUP=' + $private)
Write-Output ('SAFE_SNAPSHOT=' + $SnapshotDirectory)
Write-Output 'Secret scan passed. Profiles and application settings were not modified.'
