# Configuration before profile changes

Captured from the running Windows media server on November 27, 2026, before the quality-profile overhaul.

- Sonarr: existing standard profiles with upgrades disabled.
- Radarr: standard profiles plus the existing 4K HDR (manual) profile.
- Radarr Small Movies: standard profiles plus Small Movies (720p-1080p).
- Prowlarr: DrunkenSlug, altHUB, and NinjaCentral enabled and synced to Sonarr/both Radarr instances. Knaben keeps its manual trial sync profile.
- NZBGet: Newshosting over TLS port 563 with strict certificate verification, loopback-only web control, and category-specific E: download paths.
- qBittorrent: existing preferences/categories with credentials and device identifiers redacted.

See [recovery instructions](../../backup-and-restore.md) for private native backups and restore limits. snapshot.json records the private recovery-set location and verification status. Values marked [REDACTED] require local credentials; these are reference exports, not drop-in restore files.
