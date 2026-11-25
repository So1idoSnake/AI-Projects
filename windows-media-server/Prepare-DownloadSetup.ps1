[CmdletBinding(SupportsShouldProcess)]
param(
    [ValidateNotNullOrEmpty()]
    [string]$MediaRoot,

    [switch]$CreateFolders
)

$ErrorActionPreference = 'Stop'
$plan = Import-PowerShellDataFile -LiteralPath (Join-Path $PSScriptRoot 'DownloadPlan.psd1')
if (!$PSBoundParameters.ContainsKey('MediaRoot')) { $MediaRoot = $plan.MediaRoot }

function Assert-DirectoryName([string]$Name) {
    if ([string]::IsNullOrWhiteSpace($Name) -or $Name.EndsWith('.') -or $Name.EndsWith(' ') -or
        $Name.IndexOfAny([IO.Path]::GetInvalidFileNameChars()) -ge 0 -or
        $Name -match '^(CON|PRN|AUX|NUL|CONIN\$|CONOUT\$|LPT[1-9\u00B9\u00B2\u00B3]|COM[1-9\u00B9\u00B2\u00B3])(\.|$)') {
        throw "Invalid Windows directory name: $Name"
    }
}

function Test-SafeDirectoryPath([string]$Path) {
    $current = [IO.Path]::GetPathRoot($Path)
    foreach ($component in $Path.Substring($current.Length).Split('\')) {
        $current = [IO.Path]::Combine($current, $component)
        try {
            $item = Get-Item -LiteralPath $current -Force
        } catch [System.Management.Automation.ItemNotFoundException] {
            return $false
        }
        if ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) {
            throw "A reparse point is in the planned path: $current"
        }
        if (!$item.PSIsContainer) { throw "A file is in the planned directory path: $current" }
    }
    return $true
}

$MediaRoot = $MediaRoot.TrimEnd('\')
if ($MediaRoot -notmatch '^[A-Za-z]:\\.+') { throw 'Specify an absolute local Windows directory below a drive root.' }
foreach ($component in $MediaRoot.Substring(3).Split('\')) { Assert-DirectoryName $component }
$MediaRoot = [IO.Path]::GetFullPath($MediaRoot)
$driveRoot = [IO.Path]::GetPathRoot($MediaRoot)
$drive = [IO.DriveInfo]::new($driveRoot)
if (!$drive.IsReady -or $drive.DriveType -ne 'Fixed' -or $drive.DriveFormat -ne 'NTFS') {
    throw 'The planned root needs a ready local fixed NTFS drive.'
}
$mediaParent = [IO.Path]::GetDirectoryName($MediaRoot)
if ($mediaParent -ne $driveRoot -and !(Test-SafeDirectoryPath $mediaParent)) {
    throw 'The folder above the planned media root must already exist.'
}

$categories = @('Incomplete', 'manual') + @($plan.Routes.Category)
$clientFolders = @($plan.DownloadClients.Folder)
$libraryFolders = @($plan.Routes.Library)
foreach ($names in @($categories, $clientFolders, $libraryFolders)) {
    foreach ($name in $names) { Assert-DirectoryName $name }
    if ($names | Group-Object | Where-Object Count -gt 1) { throw 'Download plan contains duplicate folder names in one parent directory.' }
}

$downloadRoot = Join-Path $MediaRoot 'Downloads'
$libraryRoot = Join-Path $MediaRoot 'Library'
$directories = @($MediaRoot, $downloadRoot, $libraryRoot)
foreach ($client in $plan.DownloadClients) {
    $clientRoot = Join-Path $downloadRoot $client.Folder
    $directories += $clientRoot
    foreach ($category in $categories) { $directories += Join-Path $clientRoot $category }
}
foreach ($library in $libraryFolders) { $directories += Join-Path $libraryRoot $library }

$listeners = [Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties().GetActiveTcpListeners()
$applications = @(@{ Name = 'Prowlarr'; Port = $plan.ProwlarrPort }) + $plan.DownloadClients + $plan.Routes
foreach ($app in $applications) {
    if ($app.Port -isnot [int] -or $app.Port -lt 1 -or $app.Port -gt 65535) { throw "Invalid port for $($app.Name)." }
}
if ($applications.Port | Group-Object | Where-Object Count -gt 1) { throw 'Download plan contains duplicate admin ports.' }

$folders = foreach ($directory in $directories) {
    [pscustomobject]@{ Path = $directory; Exists = (Test-SafeDirectoryPath $directory) }
}

$create = $CreateFolders -and ($folders | Where-Object { !$_.Exists }) -and
    $PSCmdlet.ShouldProcess($MediaRoot, 'Create missing folders in the planned layout')
$mode = if ($create) { 'folder creation' } else { 'preview' }
"Download setup $mode. Storage is NTFS with $([math]::Round($drive.AvailableFreeSpace / 1GB)) GiB available."
foreach ($route in $plan.Routes) {
    "ROUTE $($route.Name) | $($route.Category) | Library\$($route.Library)"
}
foreach ($app in $applications) {
    $bound = @($listeners | Where-Object Port -eq $app.Port)
    $status = if (!$bound.Count) { 'no TCP listener observed' }
        elseif ($bound | Where-Object { ![Net.IPAddress]::IsLoopback($_.Address) }) { 'review non-loopback listener' }
        else { 'in use on loopback; verify app' }
    "ADMIN $($app.Name) | $($plan.AdminAddress):$($app.Port) | $status"
}

foreach ($folder in $folders) {
    if ($folder.Exists) {
        "PRESENT $($folder.Path)"
    } elseif ($create) {
        if (!(Test-SafeDirectoryPath $folder.Path)) {
            $parent = [IO.Path]::GetDirectoryName($folder.Path)
            if ($parent -ne $driveRoot -and !(Test-SafeDirectoryPath $parent)) {
                throw "A planned parent folder is missing: $parent"
            }
            [IO.Directory]::CreateDirectory($folder.Path) | Out-Null
        }
        "READY   $($folder.Path)"
    } else {
        "PLANNED $($folder.Path)"
    }
}
