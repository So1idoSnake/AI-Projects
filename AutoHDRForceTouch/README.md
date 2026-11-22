# Auto HDR Force Touch

A minimal, touch-friendly Windows utility for setting the per-application Auto HDR compatibility override used by Direct3D.

## Use

1. Run `AutoHDRForceTouch.exe`.
2. Choose the game's `.exe` in the standard Windows file picker.
3. Choose whether to enable Auto HDR and 10-bit color.
4. Select **Apply**, then **Done** after the registry result is verified.

The application does not launch the game, change global Windows HDR settings, require administrator access, or run in the background.

Keep `AutoHDRForceTouch.exe.config` in the same folder as `AutoHDRForceTouch.exe` so the .NET Framework runtime and PerMonitorV2 DPI settings are applied.

`AutoHDRForceTouch.png` is the square library artwork for launchers such as the Xbox app. `AutoHDRForceTouch.ico` contains 16, 24, 32, 48, 64, 128, and 256-pixel Windows icon frames; the same ICO is embedded in the executable for Explorer, the title bar, and the taskbar.

## Registry behavior

Settings are stored below `HKEY_CURRENT_USER\SOFTWARE\Microsoft\Direct3D` using the existing `ApplicationN`, `Name`, and `D3DBehaviors` convention.

- Auto HDR with 10-bit color writes `BufferUpgradeOverride=1;BufferUpgradeEnable10Bit=1`.
- Auto HDR without 10-bit color writes `BufferUpgradeOverride=1`.
- Selecting No for Auto HDR removes `D3DBehaviors` from a matching entry. If no matching entry exists, nothing is created.

Before a real registry change, the current Direct3D branch is exported to `%LOCALAPPDATA%\AutoHDRForceTouch\Backups`. If the branch does not yet exist, the backup folder receives a timestamped marker recording that fact. A failed backup prevents the registry write.

## Build and test

Requires Windows, .NET Framework 4.x, its 64-bit C# compiler at
`C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`, and PowerShell.
No NuGet packages or third-party executable are required.
From this project folder, run:

```powershell
.\build.ps1
```

The script treats compiler warnings as errors, runs the isolated registry tests,
and copies the executable, runtime configuration and artwork into `dist/`.
Use `-Destination <folder>` for a different output folder, or `-SkipTests` to build
without executing tests. Build products are ignored by Git.
Tests create and delete unique temporary branches below
`HKCU\Software\AutoHDRForceTouch.Tests`; they do not access the production
Direct3D branch. Launching `dist/AutoHDRForceTouch.exe` is a separate manual step.

`src/` holds the original C# implementation, application manifest and runtime
configuration; `tests/` holds its original test suite. `assets/` holds the
original icon artwork and `tools/Convert-PngToIco.ps1` rebuilds its ICO frames.

## Behavioral references

This is a fresh implementation. No source code was copied from these projects:

- `ledoge/autohdr_force`: https://github.com/ledoge/autohdr_force
- `emmagine79/AutoHDRForce`: https://github.com/emmagine79/AutoHDRForce
- `7gxycn08/ForceAutoHDR`: https://github.com/7gxycn08/ForceAutoHDR

## Verification

The following results were recorded in the original October 2026 workspace:

- Application and tests compile with warnings treated as errors.
- All 12 isolated registry tests pass.
- The picker, option dependency, touch layout, Cancel, success, and Done flows were visually verified.
- A controlled production-registry round trip created and verified a test entry, produced a restorable `.reg` backup, and then removed only that exact test entry. Pre-existing Direct3D entries were preserved.

The November 21 source import preserves the original application, tests and
icon tooling. The build script's default output folder was adapted to `dist/`.
The import does not establish current UI or game behavior. Registry exports and
built executables are excluded from this source backup. Keep any rollback
exports separately; review an export's contents before manually restoring it.
