# Repository migration record

Date: 2026-11-20. Local organization only; no remote push, app installation, startup changes,
service restart, game/save changes or display-profile implementation.

The existing Display-Switcher baseline commit 149d2a2 remains in history. Its source and binary
backup were not rewritten. AGENTS.md and SOURCE_GUIDE.md add navigation/context only.

## Relocated inactive code

These original files are moved byte-for-byte after verifying the copied content.
Their current Startup shortcuts do not reference these paths.

| Original workspace file | New repository location |
|---|---|
| 2026-09-06/somewhere-you-created/work/Install-ChatGPT-Xbox-StartupTask.ps1 | Windows-App-Startup/legacy/Install-ChatGPT-Xbox-StartupTask.ps1 |
| 2026-09-06/somewhere-you-created/work/Launch-ChatGPT-Xbox.ps1 | Windows-App-Startup/legacy/Launch-ChatGPT-Xbox.ps1 |
| 2026-09-06/somewhere-you-created/work/Launch ChatGPT at Logon.vbs | Windows-App-Startup/legacy/Launch ChatGPT at Logon.vbs |
| 2026-10-31/why-isn-t-moonlight-working-from/restart-sunshine.ps1 | Sunshine-Streaming/scripts/restart-sunshine.ps1 |

Original workspace paths are relative to C:/Users/micha/Documents/Codex.
The startup scripts are superseded attempts, not the recommended implementation.
The restart helper is manual and has no background scheduled invocation established in this review.

## Copied context and private backups

- Original startup README -> Windows-App-Startup/docs/ORIGINAL_GUIDE.md.
- HOST-FALLBACK-CHANGE.md -> Sunshine-Streaming/docs/.
- Townfall downloaded staging -> Townfall-Mods/local-private/vendor-staging (ignored).
- UE4SS supplied license -> Townfall-Mods/notices/UE4SS-LICENSE.txt.
- Hogwarts work/backups -> Hogwarts-Save-Migration/local-private/original-work (ignored).
- Original and final game-history data and final handoff MD -> Game-History-Inventory/local-private/ (ignored).
- Personal Gemini memory handoff -> Gemini-Memory-Import/local-private/ (ignored).
- TV EDID binaries and diagnostic text -> Display-HDR-Research/local-private/original-evidence (ignored).
- Partial recovered session-command histories -> each relevant local-private/ directory (ignored).

Private backups supplement rather than delete their original data files. They are deliberately
absent from Git commits and from a fresh clone. A separate private backup is needed for them.
No generated game/mod binaries were classified as first-party source.
