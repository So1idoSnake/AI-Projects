# Source and fresh-machine build guide

This folder contains the actual application source, not just a backup executable.

| Path | Role |
|---|---|
| src/DisplayController/DisplayController.cs | Console commands, tray UI, state persistence, Windows display/HDR APIs, Sunshine request handling and RTSS integration |
| src/DisplayController/RecoveryTests.cs | Isolated recovery, profile, snapshot/scaling and settling checks |
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

The current branch's source adds AW3926QW native, manual 330 Hz and centered-4K profiles.
It is deployed for attended testing; complete hardware validation remains pending. See
[WORK_IN_PROGRESS.md](WORK_IN_PROGRESS.md) and [the attended test plan](docs/AW3926QW-TEST-PLAN.md).
[BASELINE_BACKUP.md](BASELINE_BACKUP.md) describes the historical installed baseline.
See [attended results](docs/AW3926QW-ATTENDED-RESULTS.md) for the current installation.

The NVIDIA identity-scaling compatibility check uses read-only public NVAPI queries
from the installed driver (`nvapi.dll` for x86). No SDK package or new driver is required.
It resolves the active GDI display to a vendor display ID and confirms scaling mode 3,
GPU-centered/no-scaling, via GetDisplayConfig. It never calls NVAPI display setters.
ABI references are NVIDIA's MIT-licensed [header](https://github.com/NVIDIA/nvapi/blob/main/nvapi.h)
and [interface IDs](https://github.com/NVIDIA/nvapi/blob/main/nvapi_interface.h).
The experimental scanout-rectangle query exposed only part of the scanout;
it is not used by production verification.
The supplied raster artwork was AI-assisted; the ICO conversion source does not reproduce the
original image-generation model. It reuses the preserved image asset.

To regenerate artwork into ignored work/ files, from this project directory:

```powershell
New-Item -ItemType Directory -Path .\work -Force
& .\tools\New-TrayIcon.ps1 -InputPath .\assets\gaming-display-controller-generated.png -OutputPng .\work\gaming-display-controller.png -OutputIco .\work\gaming-display-controller.ico
```

Inspect the generated result before replacing the committed assets; this import did not regenerate them.
