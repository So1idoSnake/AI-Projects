# Windows home media server

**Draft for review.** The rollout uses the existing Windows desktop and NTFS drives. Plex is the only service intended for off-network access. File sharing stays on the home network. Complete Windows, router, and device setup before configuring download providers or building a request interface.

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

## 2. Build and verify the download pipeline

Prowlarr syncs indexers to Sonarr and both Radarr instances. These apps send jobs to qBittorrent for torrents or NZBGet for Usenet, then import finished files. Use local paths under `E:\MediaServer` for all new staging and library data. [Prowlarr quick start](https://wiki.servarr.com/en/prowlarr/quick-start-guide) · [Radarr quick start](https://wiki.servarr.com/en/radarr/quick-start-guide)

| App | Local port | Download category | Import destination |
| --- | --- | --- | --- |
| Sonarr | `8989` | `tv` | `Library\TV` |
| Radarr | `7878` | `movies` | `Library\Movies` |
| Radarr Small Movies | `7879` | `movies-small` | `Library\Small Movies` |

- Stage files under `Downloads\Torrents` and `Downloads\Usenet`, with category subfolders and separate incomplete folders. Keep staging and imports on the same NTFS volume for hardlinks and moves. Preserve the existing qBittorrent queue and Plex paths. Start with completed-download handling on and automatic cleanup off.
- Bind all admin UIs to `127.0.0.1` and require authentication. Prowlarr uses `9696`, qBittorrent `8080`, and NZBGet `6789`. Enable Mullvad auto-connect, bind qBittorrent to the Mullvad interface, and disable qBittorrent automatic port mapping. Use certificate-verified TLS for Usenet. Start apps without elevation after Windows sign-in, with Windows auto-login off and sleep disabled while serving. [qBittorrent VPN binding](https://github.com/qbittorrent/wiki/blob/master/How-to-bind-your-vpn-to-prevent-ip-leaks.md) · [NZBGet certificate verification](https://nzbget.com/documentation/certificate-verification/)
- Keep Small Movies opt-in. Give the second Radarr instance separate app data, a dedicated category, and a starter 720p/1080p profile capped at 3 GB per release. Use interactive search for individual requests. Keep library mirroring, automatic grabs, and upgrades off. Keep `E:\SmallMovies` in its existing Plex library and add the new import root as **Travel Movies**. [Radarr multiple instances](https://wiki.servarr.com/radarr/installation/multiple-instances) · [Radarr profiles](https://wiki.servarr.com/radarr/settings)
- Choose small copies that are already encoded for travel devices. Start with SDR H.264 video and stereo AAC in an MP4 container where available. Confirm the imported file's codecs and test playback on the travel device at Original quality before relying on it offline.
- Test both download protocols and all three import paths. Verify that a seeding torrent is hardlinked, Plex sees new files, and a travel copy plays offline. Confirm qBittorrent stops transferring when Mullvad disconnects and resumes after reconnecting. Reboot, sign in, and check startup and localhost-only admin access. Back up working configuration separately from writable shares.

## 3. Review the request interface and family access later

- Pilot stock Seerr on Windows at `127.0.0.1:5055` after the pipeline passes. Connect it to Plex, Sonarr, and regular Radarr. Keep travel requests owner-only and require approval before family requests enter a download queue. [Seerr Windows setup](https://docs.seerr.dev/getting-started/buildfromsource/#windows)
- Review stock Seerr on desktop and phone alongside one or two original AI-assisted mockups. Test title search, availability, request submission, and status. Keep stock Seerr if it meets those needs. If an original design solves a clear usability problem, build the smallest useful extension and keep credentials and approval controls server-side.
- Keep texting as the fallback. An optional Plex Watchlist pilot lets each parent make requests from the Plex app after a one-time Seerr login and opt-in. Keep auto-approval disabled. If a separate request page is preferred, test invite-only Tailscale Serve with Mullvad before enabling it. Limit access to the request page and verify that an uninvited device cannot open it. [Seerr Watchlist requests](https://docs.seerr.dev/using-seerr/plex/watchlist-auto-request/) · [Tailscale Serve](https://tailscale.com/docs/features/tailscale-serve)
