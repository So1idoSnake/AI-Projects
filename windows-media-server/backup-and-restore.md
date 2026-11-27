# Recover the media server configuration

The [before-profile snapshot](backups/2026-11-27-before-profiles) records settings before the quality-profile overhaul. It includes current Sonarr and both Radarr profiles, quality definitions, custom formats, delay profiles, naming, size/import settings, library roots, download-client connections, Prowlarr app/indexer configuration, and redacted NZBGet/qBittorrent settings.

## GitHub and private recovery files

The GitHub snapshot is a sanitized reference export. It excludes passwords, API keys, Plex identity tokens, personal databases, torrent resume files, logs, and artwork. Resource IDs and schemas belong to the captured application versions; these exports are not native restore archives and must not be blindly posted to another instance.

The complete private recovery set is under the PrivateBackup location in the snapshot's snapshot.json, inside %LOCALAPPDATA%\WindowsMediaServer\Backups\BeforeProfiles-<UTC timestamp>. Only a directory with a completed manifest.json is a finished backup. A partial directory from a failed attempt is not a recovery set.

The private set contains:

- Native Prowlarr, Sonarr, Radarr, and Radarr Small Movies ZIP backups with their configuration and databases.
- NZBGet configuration/queue state and the local encrypted media-app login store.
- qBittorrent preferences, categories, RSS configuration, and BT_backup resume files.
- Plex registry settings, consistent SQLite copies of its library/blobs databases, copied metadata/artwork, plugin preferences/data, plugins, and scanners.
- Startup shortcuts, watchdog task/script, desktop media links, and the local login helper.
- File sizes and SHA-256 hashes in manifest.json.

Media files under library/download folders, third-party installations, installers, OS images, router configuration, and VPN/Tailscale account or device keys are outside this application-configuration backup. Raw torrent state and app databases stay out of GitHub under the repository privacy rules. The private set is local only; it does not protect against loss of the server or its drive.

The Windows DPAPI-encrypted login store depends on the original Windows user context. Do not assume it decrypts on a fresh computer. Native app backups also contain sensitive state and must remain private.

## Capture a new backup

Use PowerShell 7 on the Windows account that runs the applications, with the existing per-user installation paths and all applications running. The script does not stop/restart apps, change profiles, install software, or configure schedules.

From this project directory:

~~~powershell
.\Backup-Setup.ps1 -SnapshotDirectory .\backups\<new-snapshot-name>
~~~

The script creates native backups sequentially. Both Radarr instances share a temporary backup directory, so their backup jobs must not run simultaneously. It copies the private recovery set outside Git, exports sanitized settings, validates ZIP structure, checks copied Plex databases with SQLite quick_check, creates a hash manifest, and scans exports for known credentials.

Plex databases use SQLite online backup instead of ordinary copies of open databases/WAL files. Artwork and qBittorrent/NZBGet runtime files are live copies, not an atomic system image. A migration requiring exact torrent/session state needs a separately authorized quiet maintenance window and a backup after clean shutdown.

-SkipPlexArtwork excludes Plex Metadata and Media and is not equivalent to the full snapshot. -UseExistingNativeBackups is for an attended run with fresh native backups already completed within 30 minutes; their archives are still validated.

Before committing, inspect intended changed paths and credential exclusions. Keep raw ZIP/DB/private exports out of the staged set. Push a non-main branch and verify its remote commit SHA. A local commit is not a GitHub backup.

## Restore only when requested

No restore, shutdown, or profile change was used to validate this backup.

1. Locate the finished private set and verify the relevant file hashes against manifest.json.
2. Preserve newer settings before restoring. Explicitly authorize a maintenance window and hold the watchdog before any shutdown or file replacement.
3. Restore each Sonarr, Radarr, Small Movies Radarr, and Prowlarr archive through that app's System > Backup > Restore. Keep the two Radarr data directories separate and use the matching captured version where practical.
4. Restore NZBGet configuration and queue state to its app-data layout. Check drive letters, categories, TLS/certificate validation, and provider authentication before resuming downloads.
5. Restore qBittorrent configuration and resume state with qBittorrent cleanly stopped. Preserve file locations and verify Mullvad interface binding before starting torrents.
6. Restore Plex databases/preferences with Plex cleanly stopped, following Plex's current procedure. Registry preferences contain identity/authentication information. Review paths/ownership before importing on a different Windows account or machine.
7. Recreate startup/task registration only when deployment is explicitly requested. Re-establish local login handling for the target Windows account.
8. Verify provider/indexer authentication, application sync, category routing, library roots, a representative authorized download/import, and Plex playback before ordinary use.

For a profile-only rollback, compare the sanitized JSON's qualities, cutoff, upgrade policy, custom-format scores, and size rules with the live app. Re-create them through its UI or a reviewed API operation that maps IDs correctly. Restoring a full native database rolls back other application state too.

Official references: [Sonarr restore](https://wiki.servarr.com/sonarr/faq#how-do-i-backuprestore-my-sonarr), [Radarr restore](https://wiki.servarr.com/radarr/faq#how-do-i-backuprestore-my-radarr), [Plex backup](https://support.plex.tv/articles/201539237-backing-up-plex-media-server-data/), [qBittorrent profiles](https://github.com/qbittorrent/qBittorrent/wiki/Frequently-Asked-Questions).
