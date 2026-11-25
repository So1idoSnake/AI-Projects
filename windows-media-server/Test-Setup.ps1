[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$LanInterface,

    [Parameter(Mandatory)]
    [ValidatePattern('^\d{1,3}(\.\d{1,3}){3}/([1-9]|[12][0-9]|3[0-2])$')]
    [string]$LanSubnet,

    [ValidateNotNullOrEmpty()]
    [string]$ShareAccount = 'ShareUser',

    [ValidateNotNullOrEmpty()]
    [string]$SharesRoot = 'F:\',

    [ValidateRange(1, 65535)]
    [int]$PlexPublicPort = 32400
)

$ErrorActionPreference = 'Stop'
$smbRule = 'WindowsMediaServer-SMB-HomeLAN'

function Invoke-SetupCheck([string]$Name, [scriptblock]$Check) {
    try {
        $detail = & $Check
        [pscustomobject]@{ Name = $Name; Passed = $true; Detail = $detail }
    } catch {
        [pscustomobject]@{ Name = $Name; Passed = $false; Detail = $_.Exception.Message }
    }
}

$checks = @(
    Invoke-SetupCheck 'SMB shares' {
        $user = Get-LocalUser -Name $ShareAccount
        if (!$user.Enabled) { throw 'The intended local share account is disabled.' }
        if (Get-LocalGroupMember -SID 'S-1-5-32-544' | Where-Object SID -eq $user.SID) {
            throw 'The intended share account is a local administrator.'
        }

        $accountName = $user.SID.Translate([Security.Principal.NTAccount]).Value
        $allowedPrincipals = @($accountName)
        foreach ($sid in 'S-1-5-18', 'S-1-5-32-544') {
            $allowedPrincipals += ([Security.Principal.SecurityIdentifier]$sid).Translate([Security.Principal.NTAccount]).Value
        }
        $shares = @(Get-SmbShare | Where-Object { !$_.Special })
        if ($shares.Count -ne 2 -or $shares.Name -notcontains 'Transfer' -or $shares.Name -notcontains 'Game Archive') {
            throw 'Expected Transfer and Game Archive to be the only custom shares.'
        }

        foreach ($share in $shares) {
            if ($share.Path -ne (Join-Path $SharesRoot $share.Name)) {
                throw "$($share.Name) points to an unexpected folder."
            }
            $isTransfer = $share.Name -eq 'Transfer'
            $shareRight = if ($isTransfer) { 'Change' } else { 'Read' }
            $fileRight = if ($isTransfer) { 'Modify, Synchronize' } else { 'ReadAndExecute, Synchronize' }
            $access = @(Get-SmbShareAccess -Name $share.Name)
            $accountAccess = @($access | Where-Object AccountName -eq $accountName)
            if ($accountAccess.Count -ne 1 -or $accountAccess[0].AccessControlType -ne 'Allow' -or $accountAccess[0].AccessRight -ne $shareRight) {
                throw "Unexpected share access for the intended account on $($share.Name)."
            }
            if ($access | Where-Object { $_.AccessControlType -eq 'Allow' -and $_.AccountName -notin $allowedPrincipals }) {
                throw "Unexpected allowed SMB principal on $($share.Name)."
            }

            $acl = Get-Acl -LiteralPath $share.Path
            $entries = $acl.GetAccessRules($true, $true, [Security.Principal.SecurityIdentifier])
            $accountEntries = @($entries | Where-Object { $_.IdentityReference -eq $user.SID })
            if ($accountEntries.Count -ne 1 -or $accountEntries[0].AccessControlType -ne 'Allow' -or
                $accountEntries[0].FileSystemRights -ne [Security.AccessControl.FileSystemRights]$fileRight -or
                $accountEntries[0].InheritanceFlags -ne [Security.AccessControl.InheritanceFlags]'ContainerInherit, ObjectInherit' -or
                $accountEntries[0].PropagationFlags -ne 'None') {
                throw "Unexpected NTFS root access for the intended account on $($share.Name)."
            }
            if ($entries | Where-Object { $_.IdentityReference.Value -eq 'S-1-1-0' -and $_.AccessControlType -eq 'Allow' }) {
                throw "The Everyone group has NTFS access on the root of $($share.Name)."
            }
        }
        'Transfer grants Change, and Game Archive grants Read, with matching NTFS root entries.'
    }

    Invoke-SetupCheck 'Windows access' {
        $guest = Get-LocalUser | Where-Object { $_.SID.Value -match '-501$' } | Select-Object -First 1
        if (!$guest -or $guest.Enabled) { throw 'The built-in Guest account is enabled or could not be verified.' }
        $smb = Get-SmbServerConfiguration
        if ($smb.EnableSMB1Protocol -or !$smb.EnableSMB2Protocol -or !$smb.RequireSecuritySignature) {
            throw 'Expected SMB1 off, SMB2/3 enabled, and SMB signing required.'
        }
        $rdp = 'HKLM:\SYSTEM\CurrentControlSet\Control\Terminal Server'
        if ((Get-ItemPropertyValue -LiteralPath $rdp -Name fDenyTSConnections) -ne 1) {
            throw 'Remote Desktop is enabled in Windows.'
        }
        $rdpPort = Get-ItemPropertyValue -LiteralPath "$rdp\WinStations\RDP-Tcp" -Name PortNumber
        if ([Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties().GetActiveTcpListeners().Port -contains $rdpPort) {
            throw 'A TCP listener is present on the configured Remote Desktop port.'
        }
        'Guest and Remote Desktop are disabled. SMB signing is required.'
    }

    Invoke-SetupCheck 'SMB firewall' {
        $profiles = @(Get-NetFirewallProfile -PolicyStore ActiveStore)
        if ($profiles.Count -ne 3 -or ($profiles | Where-Object { $_.Enabled -ne 'True' -or $_.DefaultInboundAction -ne 'Block' })) {
            throw 'Expected all three firewall profiles enabled with inbound traffic blocked by default.'
        }
        $address, $prefix = $LanSubnet.Split('/')
        $network = [Net.IPAddress]::Parse($address).GetAddressBytes()
        $mask = for ($i = 0; $i -lt 4; $i++) {
            $bits = [Math]::Max(0, [Math]::Min(8, [int]$prefix - 8 * $i))
            [int](256 - [Math]::Pow(2, 8 - $bits))
        }
        for ($i = 0; $i -lt 4; $i++) {
            if (($network[$i] -band $mask[$i]) -ne $network[$i]) {
                throw 'Specify the network address of the home subnet in CIDR notation.'
            }
        }
        $lanAddresses = Get-NetIPAddress -InterfaceAlias $LanInterface -AddressFamily IPv4 | Where-Object PrefixLength -eq ([int]$prefix)
        $onHomeSubnet = foreach ($lanAddress in $lanAddresses) {
            $bytes = [Net.IPAddress]::Parse($lanAddress.IPAddress).GetAddressBytes()
            $adapterNetwork = for ($i = 0; $i -lt 4; $i++) { $bytes[$i] -band $mask[$i] }
            if (($adapterNetwork -join '.') -eq $address) { $lanAddress }
        }
        if (!$onHomeSubnet) { throw 'The LAN adapter does not currently have an address on the expected subnet.' }
        $expected = @($LanSubnet, "$address/$($mask -join '.')")
        $rules = @(Get-NetFirewallRule -PolicyStore ActiveStore -Enabled True -Direction Inbound -Action Allow)
        $rule = $rules | Where-Object Name -eq $smbRule
        if (!$rule -or $rule.Profile -ne 'Any' -or $rule.EdgeTraversalPolicy -ne 'Block') {
            throw 'Expected an enabled home-LAN SMB rule on all profiles with edge traversal blocked.'
        }
        $portFilter = Get-NetFirewallPortFilter -AssociatedNetFirewallRule $rule
        $addresses = @((Get-NetFirewallAddressFilter -AssociatedNetFirewallRule $rule).RemoteAddress)
        $interfaces = @((Get-NetFirewallInterfaceFilter -AssociatedNetFirewallRule $rule).InterfaceAlias)
        if ($portFilter.Protocol -notin @('TCP', '6') -or @($portFilter.LocalPort).Count -ne 1 -or $portFilter.LocalPort -ne '445' -or
            $addresses.Count -ne 1 -or $addresses[0] -notin $expected -or
            $interfaces.Count -ne 1 -or $interfaces[0] -ne $LanInterface) {
            throw 'The home SMB rule does not match the expected interface, subnet, and TCP port.'
        }

        foreach ($other in ($rules | Where-Object Name -ne $smbRule)) {
            $ports = Get-NetFirewallPortFilter -AssociatedNetFirewallRule $other
            if ($ports.Protocol -notin @('TCP', '6', 'Any', '256')) { continue }
            $includesSmb = $ports.LocalPort | Where-Object {
                $_ -eq '445' -or ($_ -match '^(\d+)-(\d+)$' -and [int]$Matches[1] -le 445 -and [int]$Matches[2] -ge 445)
            }
            if (!$includesSmb) { continue }
            $app = Get-NetFirewallApplicationFilter -AssociatedNetFirewallRule $other
            $service = Get-NetFirewallServiceFilter -AssociatedNetFirewallRule $other
            if ($app.Program -in @('', 'Any', 'System') -and $app.Package -in @('', 'Any', $null) -and
                $service.Service -in @('', 'Any', 'LanmanServer')) {
                throw "Review the additional inbound rule that could allow SMB: $($other.DisplayName)."
            }
        }
        'The SMB rule is limited to the intended adapter and subnet. No other explicit TCP 445 allow for SMB was found.'
    }

    Invoke-SetupCheck 'Plex local setup' {
        $plex = Get-ItemProperty -LiteralPath 'HKCU:\Software\Plex, Inc.\Plex Media Server'
        if ($plex.PublishServerOnPlexOnlineKey -ne 1 -or $plex.ManualPortMappingMode -ne 1 -or
            $plex.ManualPortMappingPort -ne $PlexPublicPort -or $plex.TranscoderCanOnlyRemuxVideo -ne 1) {
            throw 'Expected remote access enabled with the configured manual port and video transcoding disabled.'
        }
        $response = Invoke-WebRequest -Uri 'http://127.0.0.1:32400/identity' -UseBasicParsing -TimeoutSec 5 -MaximumRedirection 0
        if (([xml]$response.Content).MediaContainer.claimed -ne '1') { throw 'The local Plex server is not responding as a claimed server.' }
        'Plex responds locally, has manual remote access configured, and keeps video transcoding disabled.'
    }
)

foreach ($check in $checks) {
    $status = if ($check.Passed) { 'PASS' } else { 'REVIEW' }
    "$status $($check.Name) - $($check.Detail)"
}
'MANUAL - Verify actual SMB access from a LAN device and direct Plex playback from outside the home network.'
'MANUAL - Verify DHCP reservation, port forwarding, and IPv6 rules in the router.'

if ($checks | Where-Object { !$_.Passed }) { exit 1 }
exit 0
