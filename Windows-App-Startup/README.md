# Windows app startup

Creates ordinary per-user Windows Startup-folder shortcuts. This is the successful approach
used for ChatGPT, Xbox and later Gemini, replacing the earlier scheduled-task/VBS/PowerShell
launch chain. The setup script is run once, not persistently at sign-in.

## Reuse on a fresh PC

1. Install the apps and run them normally once.
2. Review scripts/Set-StartupApps.ps1 and docs/ORIGINAL_GUIDE.md.
3. Preview: `powershell -NoProfile -File .\scripts\Set-StartupApps.ps1`.
4. From an existing PowerShell session, apply the selected apps:
   `& .\scripts\Set-StartupApps.ps1 -Apps @('ChatGPT','Xbox','Gemini') -Apply`.
5. Test the shortcuts separately, then verify the next actual Windows sign-in yourself.
   Windows controls timing; no precise launch delay is promised.

The reusable script was extracted/adapted from the successful guide during repository import.
It discovers installed app IDs, refuses ambiguous app matches, and does not remove unrelated
startup items, stop apps, or silently overwrite a conflicting shortcut. It is preview-only
without -Apply and supports -WhatIf.

## Original applications and observed shortcuts, 2026-11-20

| Application | Shortcut target | Arguments |
|---|---|---|
| ChatGPT | Windows explorer.exe | shell:AppsFolder\OpenAI.Codex_2p2nqsd0c76g0!App |
| Xbox full UI | Windows explorer.exe | shell:AppsFolder\Microsoft.GamingApp_8wekyb3d8bbwe!Microsoft.Xbox.App |
| Gemini | Windows explorer.exe | shell:AppsFolder\com.google.gemini.windows |

Gemini's ID resembles a desktop app ID rather than an MSIX family; treat it as the installed
Start-app identifier, not proof that Gemini is packaged.
RTSS and Gaming Display Controller also have independent startup shortcuts, but are owned
by the display/streaming setup and are outside this generic script's selected-app scope.

Xbox's native startup registration historically launched its tray component rather than the
full UI; the explicit app shortcut solved the desired behavior. Windows may delay Startup items.

## Historical code

legacy/ contains the three original launcher/installer files recovered from the old workspace.
They are superseded, contain original-machine paths, and must not be run as the recommended setup.
They were moved here without changing their contents; no current shortcut targeted those files.
The full prior procedure is preserved in docs/ORIGINAL_GUIDE.md.
