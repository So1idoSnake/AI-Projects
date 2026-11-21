# Display Switcher development context

Read README.md, BASELINE_BACKUP.md and ../REBUILD.md first.
The actual implementation is src/DisplayController/DisplayController.cs; build.ps1 builds both EXEs.
The icon generation source is tools/New-TrayIcon.ps1 and recovery tests are in src plus test-recovery.ps1.
Keep backups/2026-11-20 intact. Runtime deployment is C:/StreamingTools/DisplayController.
Source recognizes old AW3423DW DELA1E4. AW3926QW DELA2F9 profiles are pending, not implemented.
Native 5120x2160/165 HDR/cap162 is the requested standard monitor selection; optional manual
2560x1080/330/cap327 and 3840x2160/165 black bars/cap162 are planned. DSC is accepted.
TV/streaming/audio/startup/recovery defaults must stay unchanged unless separately requested.
The temporary new-monitor session has the old tray stopped and saved preference TV.
Do not start it during that session without arranging a valid monitor profile or returning to TV.
Never move the TVs' HDMI cable while Windows is running; full shutdown first.
Building is not deployment. Do not call validate/stream-start/set or restart services during a
documentation/import task. Follow project-specific verification only for authorized implementation.
