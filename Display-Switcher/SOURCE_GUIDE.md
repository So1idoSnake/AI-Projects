# Source and fresh-machine build guide

This folder contains the actual application source, not just a backup executable.

| Path | Role |
|---|---|
| src/DisplayController/DisplayController.cs | Console commands, tray UI, state persistence, Windows display/HDR APIs, Sunshine request handling and RTSS integration |
| src/DisplayController/RecoveryTests.cs | Isolated recovery-policy checks |
| build.ps1 | Builds console DisplayController.exe and GUI DisplayControllerTray.exe from the same source |
| test-recovery.ps1 | Compiles/runs the isolated policy checks; not a live display test |
| tools/New-TrayIcon.ps1 | Icon asset generation/conversion source |
| assets/ | Source/final artwork and ICO embedded in the application |
| deployment/ | Sunshine integration examples and historical config |
| backups/2026-11-20/ | Exact deployed binary/config snapshots; not the working source |

## Building elsewhere

Use Windows and PowerShell with the Windows .NET Framework compiler expected by build.ps1.
Its original compiler path is C:/Windows/Microsoft.NET/Framework64/v4.0.30319/csc.exe.
It targets x86 and RTSS_X86 to match RTSSHooks.dll, even on 64-bit Windows.
Review source Paths.RtssDirectory, the independent NativeMethods.RtssHooksLibrary DLL path,
and machine-specific DisplayTarget.Identity before using the executable on another PC.
Changing Paths.RtssDirectory alone does not update the P/Invoke DLL path.
The compiler references System, System.Core, System.Drawing,
and System.Windows.Forms. There is no NuGet restore step in the current build.

From this directory, run ./build.ps1 only when building is intended. Output is written under
outputs/DisplayController, ignored by Git. Review/test the result before a separate deployment.
Installed RTSS is required for caps, VDD for virtual output, and Sunshine for streaming.
Do not run the old executable against a different monitor's IDs without adapting the source.

The current source still targets the AW3423DW. See BASELINE_BACKUP.md for AW3926QW pending work.
The supplied raster artwork was AI-assisted; the ICO conversion source does not reproduce the
original image-generation model. It reuses the preserved image asset.

To regenerate artwork into ignored work/ files, from this project directory:

```powershell
New-Item -ItemType Directory -Path .\work -Force
& .\tools\New-TrayIcon.ps1 -InputPath .\assets\gaming-display-controller-generated.png -OutputPng .\work\gaming-display-controller.png -OutputIco .\work\gaming-display-controller.ico
```

Inspect the generated result before replacing the committed assets; this import did not regenerate them.
