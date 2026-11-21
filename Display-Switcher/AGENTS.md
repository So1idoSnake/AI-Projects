# Display Switcher development context

Source is prepared for attended validation: read [WORK_IN_PROGRESS.md](WORK_IN_PROGRESS.md),
README.md and [the test plan](docs/AW3926QW-TEST-PLAN.md) first.
Do not deploy or run the new application against live displays while the user is away.

Read README.md, BASELINE_BACKUP.md and ../REBUILD.md first.
The actual implementation is src/DisplayController/DisplayController.cs; build.ps1 builds both EXEs.
The icon generation source is tools/New-TrayIcon.ps1 and recovery tests are in src plus test-recovery.ps1.
Keep backups/2026-11-20 intact. Runtime deployment is C:/StreamingTools/DisplayController.
The installed baseline recognizes AW3423DW DELA1E4. This branch's draft source adds
AW3926QW DELA2F9 profiles; they are compiled/offline-checked, not deployed or verified on hardware.
Native 5120x2160/165 HDR/cap162 is the requested standard monitor selection; optional manual
2560x1080/330/cap327 and 3840x2160/165 black bars/cap162 are coded. DSC is accepted.
4K is intended to use NVIDIA/GPU centering through the Windows driver, not Dell Other Modes.
330 Hz still needs the monitor's advertised hardware mode. Neither OSD nor NVIDIA global
settings are automated. The user will disable DV manually and keep HDR10; do not add DV automation.
TV/streaming/audio/startup/recovery defaults must stay unchanged unless separately requested.
At the last manual switch the old tray was stopped and saved preference was TV.
Recheck actual state later. Do not start the old tray during a temporary monitor session
without arranging a valid monitor profile or returning to TV.
Never move the TVs' HDMI cable while Windows is running; full shutdown first.
Building is not deployment. Do not call validate/stream-start/set or restart services during a
documentation/import task. Follow project-specific verification only for authorized implementation.
