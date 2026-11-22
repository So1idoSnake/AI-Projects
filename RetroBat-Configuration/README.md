# RetroBat handheld configuration

Reference for the RetroBat setup on the Xbox Ally handheld, plus the small
configuration files and inspection tool written during that work. The notes
record October 2026 changes; they are not a fresh inspection of the installed
configuration. Auto HDR's completed application is in
[AutoHDRForceTouch](../AutoHDRForceTouch/README.md).

## What was done

- Pointed RetroBat at the SD card's `D:\roms` collection.
- Used Xbox face-button labels where supported, including Nintendo systems.
- Set up uncommon rear-button hotkeys for menu/pause and fast-forward.
- Added selectable Retro Game Corps shader presets; global shaders stayed off.
- Tuned RetroBat and standalone emulator graphics for the handheld display.
- Moved saves into iCloud Drive and kept the normal RetroBat save path as a
  junction, with a separate rollback copy of the originals.

The reported display target was 1920x1080 at 120 Hz with 150% Windows scaling,
HDR enabled, 1400-nit maximum and 200-nit paper white. Those are historical
configuration values; discover the actual panel and supported modes on a new PC.

## Original files preserved

- `config/rgc-shader-configs/`: 15 custom `rendering-defaults.yml` selector files.
  They refer to shaders that must already be installed. The upstream shader code,
  overlay artwork and palettes are not included.
- `tools/Get-PEImports.ps1`: original read-only Windows PE import/architecture
  inspection script used when checking executable dependencies.

Example inspection, from this folder:

```powershell
.\tools\Get-PEImports.ps1 -Path 'C:\path\to\emulator.exe'
```

It reports binary metadata/imports; it does not install missing runtime DLLs or
prove that every dynamically loaded dependency is available. Use official
Microsoft runtime installers and upstream emulator releases when needed.

## Configuration locations and controls

| Item | Original location or setting |
|---|---|
| RetroBat | `C:\RetroBat` |
| ROMs | `D:\roms` |
| System settings | `emulationstation\.emulationstation\es_settings.cfg` below RetroBat |
| RetroArch | `emulators\retroarch\retroarch.cfg` below RetroBat |
| Keyboard hotkeys | `user\inputmapping\kb_hotkeys.yml` below RetroBat |
| M2 / right rear | `Ctrl+Alt+Shift+Scroll Lock` for fast-forward; Armoury Crate `Macro01` |
| M1 / left rear | `Ctrl+Alt+Shift+Insert` for menu/pause; Armoury Crate `Macro02` |

Recorded button overrides included `3ds.gamepadbuttons=1`,
`gamecube.gamecube_buttons=xbox`, `gb.xbox_layout=1`, `gba.xbox_layout=1`,
`gbc.xbox_layout=1`, `n64.lr_n64_buttons=xbox`, `nds.buttonsInvert=1`,
`nes.xbox_layout=1`, and `snes.buttonsInvert=1`. Recheck actual button behavior
after generator/profile updates. Standalone hotkey support varies by emulator.

## Recorded graphics baseline

Global: D3D11, HDR, 120 Hz, VSync and VRR runloop enabled; bezels, ReShade and
global shaders disabled. GB/GBC/GBA/NES/SNES used integer scaling without smoothing.

| Emulator/system | Recorded choices |
|---|---|
| Dreamcast | 1440x1080, per-triangle alpha sorting, no frame skip, threaded rendering, 8x AF |
| N64 | 1440x1080, 3-point filtering, no MSAA, threaded rendering, widescreen off |
| Beetle PSX HW | Vulkan, 4x resolution, asynchronous CD, PGXP memory-only, 3-point filtering, widescreen off |
| Dolphin | Vulkan, 3x resolution, 16x AF, no AA, Hybrid Ubershaders, native HDR, VSync |
| melonDS DS | OpenGL Core override, threaded rendering, JIT, 4x resolution, touch screen layout |
| Azahar | Vulkan, 4x resolution, Fast HLE, VSync; asynchronous shader compilation off |
| PCSX2 | Vulkan, 3x resolution, 16x AF, MTVU, widescreen patches, VSync |
| PPSSPP | Vulkan, 4x resolution, 16x AF, Smart 2D filtering, VSync |
| Xenia Canary | D3D12, 1280x720 at 1x, CAS, asynchronous shaders, XInput, 60 FPS cap |

Dolphin's GameCube aspect was 4:3 and Wii 16:9. Direct fallback settings were
also written to Dolphin, Azahar, PCSX2 and Xenia configuration files; a separate
PPSSPP INI had not yet been generated. This README records the choices instead
of copying complete machine-generated configurations.

## Recreate and verify

Install [RetroBat](https://www.retrobat.org/) and needed emulators, choose the
current ROM location, then compare supported settings in that version. Install
the relevant upstream shaders before adding these selectors through RetroBat's
custom shader-set mechanism; the original dependency was the
[Retro Game Corps overlay/shader pack](https://github.com/retrogamecorps/rgc-overlay-pack).
Keep its attribution and follow its own instructions. The YAML is configuration,
not a shader installer. Check each referenced shader path on the new installation.

Test one title per configured system, face-button labels, rear hotkeys, rendering
and save writes. No emulator/game was launched during this repository import.

## Saves and rollback

On October 27, `C:\RetroBat\saves` was redirected to
`%USERPROFILE%\iCloudDrive\RomSaves`. The original migration recorded 1,857 files,
149,263,872 bytes, verified with SHA-256. The original tree was retained under
`C:\RetroBat\user\backups\icloud-saves-migration-20261027-165025\saves-original`.
This is historical migration evidence, not a current sync check.

Use one device at a time and allow synchronization to finish before playing on
another. Inspect the current junction and both save locations before changing
them; do not replace the link with a normal folder blindly. Restore into a separate
location for comparison before switching a live save path. Save files are not
part of this repository.

Original graphics and hotkey backups were under `user\backups\graphics-x20-20261017`
and `user\backups\rear-hotkeys-20261016`. They remain on the original machine;
this source backup does not copy those installations or imply they are current.

## Provenance

Source workspace: `2026-10-16/can-you-configure-retrobat-to-point` under the
original Documents/Codex folder. Configuration/notes were imported November 21,
2026. See [PROJECT_INDEX.md](../PROJECT_INDEX.md) for the related task.
