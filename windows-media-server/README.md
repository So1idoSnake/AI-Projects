# Windows home media server

The server runs on the existing Windows desktop and NTFS drives. Plex is the only service published through the router. File shares stay on the home network. The owner can use Sonarr and regular Radarr from the home network or over private Tailscale HTTPS. The download pipeline uses native Windows apps and manual release selection.

| Area | Status |
| --- | --- |
| Windows file sharing | `Transfer` and `Game Archive` use a named non-admin account. The SMB firewall rule is scoped to the wired home network. The custom drive-root shares were retired. |
| Plex | Direct remote playback was verified from an iPhone on cellular with Mullvad connected. Video transcoding is disabled. The tested iPhone client needed Force Direct Play. |
| Torrent workflow | Phone access to regular Radarr, Knaben interactive search, qBittorrent downloads, Radarr imports, and Plex indexing were verified with 1080p and 4K HDR movies. Sonarr and Small Movies are configured. Their full download and import checks are pending. |
| Owner access over cellular | The owner's iPhone reached both private Tailscale sign-in pages over cellular with Wi-Fi off. Both app logins were verified through Tailscale HTTPS. |
| Usenet and family requests | NZBGet provider setup, stock Seerr review, and family access are planned. |

## Team and access preflight

- Assign an orchestrator to track dependencies and readiness, a UI designer to review Seerr later, and Windows, media-integration, and verification agents as needed. Choose models and reasoning effort based on each role.
- Before building, each agent reports the tools, exact resources and operations, minimum access, user approvals, verification steps, and fallbacks they need. The orchestrator deduplicates the responses and bundles required sign-ins, approvals, and device checks into one attended window where possible.
- Verify GitHub repository write access, Windows elevation, router admin access, Plex and Mullvad sessions, indexer and Usenet access, and suitable test devices. Identify optional family accounts and private access separately. Use provider sign-in, a connected app, or a local secret store for credentials. Keep passwords and tokens out of the README, commits, and agent handoffs.

## 1. Secure Windows and verify device access

- Keep `Transfer` writable and `Game Archive` read-only for the existing non-admin `ShareUser` account. Limit both share and NTFS permissions and preserve local owner, administrator, and system access. Back up the original configuration. Validate the intended shares before retiring the custom drive-root shares, `Users`, and the duplicate `All` share. Disable the built-in Guest account.
- Use the desktop for whole-drive maintenance. For work from another device, create a temporary, home-network-only share for a named maintenance account and remove it after the job. Use the smallest folder that covers the work, or the data-drive root when needed.
- Reserve the server's current LAN IP on the router. Scope the Windows SMB rule on TCP `445` to the home subnet and keep Remote Desktop disabled. Confirm a single explicit router mapping to Plex TCP `32400`, with no public SMB or admin app ports. Keep Windows Firewall, Defender, and Plex two-factor authentication enabled. [Microsoft SMB guidance](https://learn.microsoft.com/en-us/windows-server/storage/file-server/smb-secure-traffic) · [Plex Remote Access](https://support.plex.tv/articles/200289506-remote-access/)
- Confirm the named user can write to `Transfer` and read `Game Archive`, while guest access and retired shares are denied. The owner has already confirmed baseline LAN file access. Test Plex on a home device and a phone using cellular data, and verify that the remote Plex connection is direct with Mullvad connected. Preserve Mullvad LAN sharing and the Plex split-tunnel exclusion.
- Keep Plex video transcoding disabled. Use Direct Play or Direct Stream with the original video track, and allow Plex to convert audio when the client requires it. For iPhone cellular checks, start at Original quality with a compatible title and subtitles off. Force Direct Play if the app still asks for conversion. This worked in the initial 5G test with Mullvad connected. [Plex Direct Play and Direct Stream](https://support.plex.tv/articles/200250387-streaming-media-direct-play-and-direct-stream/)

### Check the Windows setup

Run [Test-Setup.ps1](Test-Setup.ps1) on the server under the Windows account that runs Plex. Supply the wired adapter name and the network address of the home subnet in CIDR notation.

```powershell
.\Test-Setup.ps1 -LanInterface '<wired adapter name>' -LanSubnet '<home subnet in CIDR>'
```

The script reads the SMB share and NTFS root permissions, the active SMB firewall rule and other rules that explicitly include TCP `445`, Guest and Remote Desktop settings, and local Plex configuration and health. It returns exit code `1` when a check needs review and `0` when the local checks pass. It leaves all settings as they are and uses no credentials. A separate firewall audit is needed to assess wildcard and dynamic rules.

Repeat the device checks after changes to the router, firewall, or VPN. On a LAN client signed in as `ShareUser`, open `Game Archive`, then create and delete a test file in `Transfer`. Confirm the archive rejects writes. With Wi-Fi off on the iPhone, play a compatible title from the personal Plex library and check the Plex dashboard for a direct connection. In Deco, confirm the wired adapter's address reservation, the single TCP rule to Plex, and any IPv6 exceptions for the server.

## 2. Build and verify the download pipeline

Use the [download setup runbook](download-setup.md) for phone access, manual release selection, and import troubleshooting. The [download plan](DownloadPlan.psd1) holds the routes, folders, and ports for setup and recovery. Preview the layout from the project folder in PowerShell.

```powershell
.\Prepare-DownloadSetup.ps1
```

The preview checks the local NTFS path and reports observed TCP listeners. Once live setup is authorized, `-CreateFolders -WhatIf` previews the creation and `-CreateFolders` creates missing directories. Add `-TorrentOnly` to stage the torrent routes for the Knaben trial. If you override `-MediaRoot`, use an existing parent on a fixed local NTFS drive. [Torrent trial workflow](download-setup.md#torrent-only-trial)

Prowlarr syncs Knaben to Sonarr and both Radarr instances. These apps send selected releases to qBittorrent, then import finished files. NZBGet is the planned Usenet client. Staging and library data use local paths under `E:\MediaServer`. [Prowlarr quick start](https://wiki.servarr.com/en/prowlarr/quick-start-guide) · [Radarr quick start](https://wiki.servarr.com/en/radarr/quick-start-guide)

| App | Local port | Download category | Import destination |
| --- | --- | --- | --- |
| Sonarr | `8989` | `tv` | `Library\TV` |
| Radarr | `7878` | `movies` | `Library\Movies` |
| Radarr Small Movies | `7879` | `movies-small` | `Library\Small Movies` |

- Stage files under `Downloads\Torrents` and `Downloads\Usenet`, with category subfolders and separate incomplete folders. Keep staging and imports on the same NTFS volume for hardlinks and moves. Preserve the existing qBittorrent queue and Plex paths. Start with completed-download handling on and automatic cleanup off.
- Keep the apps bound to `127.0.0.1` with authentication required. Windows forwards the three picker ports in the table only on the reserved home LAN address, with firewall rules scoped to the wired adapter and home subnet. Prowlarr on `9696` and qBittorrent on `8080` remain desktop-only. The reserved NZBGet port is `6789`.
- Keep Mullvad auto-connect enabled, bind qBittorrent to the Mullvad interface, and disable qBittorrent automatic port mapping. Set **Tools → On Downloads Done → Do Nothing** so Radarr and Sonarr can reach qBittorrent after transfers finish. Use certificate-verified TLS for Usenet when configured. The torrent apps use per-user startup shortcuts and start without elevation after Windows sign-in. [qBittorrent VPN binding](https://github.com/qbittorrent/wiki/blob/master/How-to-bind-your-vpn-to-prevent-ip-leaks.md) · [NZBGet certificate verification](https://nzbget.com/documentation/certificate-verification/)
- Keep Small Movies opt-in. Give the second Radarr instance separate app data, a dedicated category, and a starter 720p/1080p profile capped at 3 GB per release. Use interactive search for individual requests. Keep library mirroring, automatic grabs, and upgrades off. Keep `E:\SmallMovies` in its existing Plex library and add the new import root as **Travel Movies**. [Radarr multiple instances](https://wiki.servarr.com/radarr/installation/multiple-instances) · [Radarr profiles](https://wiki.servarr.com/radarr/settings)
- Choose small copies that are already encoded for travel devices. Start with SDR H.264 video and stereo AAC in an MP4 container where available. Confirm the imported file's codecs and test playback on the travel device at Original quality before relying on it offline.
- Finish the [verification checklist](download-setup.md#4-verify-before-enabling-ordinary-use) before treating the remaining routes as ready. Back up working configuration separately from writable shares.

### Private Sonarr and Radarr access

Tailscale Serve proxies HTTPS port `443` to Sonarr on Windows loopback port `8989` and HTTPS port `8443` to regular Radarr on loopback port `7878`. Both routes are tailnet-only. The Windows server and the owner's iPhone use the same tailnet. Small Movies stays on the home LAN, and Plex remote playback uses its existing router rule.

See [iPhone access and password changes](download-setup.md#open-sonarr-and-radarr-on-the-iphone) for finding the actual URLs, checking cellular access and keeping separate logins in a password manager. After changing VPN settings, repeat the cellular login checks and verify that qBittorrent is still bound to Mullvad and Plex still plays directly over cellular. [Tailscale Serve](https://tailscale.com/docs/features/tailscale-serve) · [VPN compatibility](https://tailscale.com/docs/reference/faq/other-vpns)

### Keep the Windows apps running

[Keep-MediaServicesRunning.ps1](Keep-MediaServicesRunning.ps1) uses the existing per-user Startup shortcuts. Its scheduled task checks the apps at sign-in and every minute while the Windows account is signed in. It restarts stopped apps and logs port conflicts or running processes that lack a listener. A qBittorrent launch requires an active Mullvad connection and its matching interface setting. Tailscale and Mullvad run as separate Windows services with their own crash recovery.

Run from the project folder under the Windows account that owns the Startup shortcuts.

```powershell
.\Keep-MediaServicesRunning.ps1 -InstallTask
.\Keep-MediaServicesRunning.ps1 -CheckOnly
```

The installed copy and local log are under `%APPDATA%\WindowsMediaServer\Watchdog`. Add `-HoldApps Prowlarr` when installing if Windows is blocking Prowlarr. The hold is stored in `held-apps.txt`. Once the app can start, remove its line to resume its checks. Use Task Scheduler to disable **Windows Media Server Watchdog** before intentionally stopping the apps for maintenance.

## 3. Review the request interface and family access later

- Pilot stock Seerr on Windows at `127.0.0.1:5055` after the pipeline passes. Connect it to Plex, Sonarr, and regular Radarr. Keep travel requests owner-only and require approval before family requests enter a download queue. [Seerr Windows setup](https://docs.seerr.dev/getting-started/buildfromsource/#windows)
- Review the stock Seerr look and workflow on desktop and phone. Test title search, availability, request submission, and status. Keep stock Seerr if it meets those needs. Reserve AI-assisted mockups for a specific usability problem, and build an extension only if the mockup solves it. Keep credentials and approval controls server-side. [Request interface review brief](request-interface.md)
- Keep texting as the fallback. An optional Plex Watchlist pilot lets each parent make requests from the Plex app after a one-time Seerr login and opt-in. Keep auto-approval disabled. If a separate request page is preferred, test invite-only Tailscale Serve with Mullvad before enabling it. Limit access to the request page and verify that an uninvited device cannot open it. [Seerr Watchlist requests](https://docs.seerr.dev/using-seerr/plex/watchlist-auto-request/) · [Tailscale Serve](https://tailscale.com/docs/features/tailscale-serve)
