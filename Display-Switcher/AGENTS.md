# Display Switcher development context

Attended validation is in progress: read [the results](docs/AW3926QW-ATTENDED-RESULTS.md),
[WORK_IN_PROGRESS.md](WORK_IN_PROGRESS.md), README.md and the test plan first.
Do not deploy or run the new application against live displays while the user is away.

Read README.md, BASELINE_BACKUP.md and ../REBUILD.md first.
The actual implementation is src/DisplayController/DisplayController.cs; build.ps1 builds both EXEs.
The icon generation source is tools/New-TrayIcon.ps1 and recovery tests are in src plus test-recovery.ps1.
Keep backups/2026-11-20 intact. Runtime deployment is C:/StreamingTools/DisplayController.
The historical baseline recognizes AW3423DW DELA1E4. The installed attended-test build
recognizes AW3926QW DELA2F9; native/4K round-trip testing passed on 2026-11-21.
Native 5120x2160/165 HDR/cap162 is the requested standard monitor selection; optional manual
2560x1080/330/cap327 and 3840x2160/165 black bars/cap162 are coded. DSC is accepted.
4K uses NVIDIA/GPU centering through Windows CCD, not Dell Other Modes. Where CCD
reports identity for the smaller desktop, read-only NVAPI must confirm GPU no-scaling.
330 Hz still needs the monitor's advertised hardware mode. Neither OSD nor NVIDIA global
settings are automated. The user will disable DV manually and keep HDR10; do not add DV automation.
TV/streaming/audio/startup/recovery defaults must stay unchanged unless separately requested.
The old binaries were privately backed up before deployment. The new tray/watchdog
is still stopped for manual testing. Saved state was aligned to native Alienware;
recheck live state before any further transition. Do not start an old binary against
the new monitor or optional state names.
Never move the TVs' HDMI cable while Windows is running; full shutdown first.
Building is not deployment. Do not call validate/stream-start/set or restart services during a
documentation/import task. Follow project-specific verification only for authorized implementation.
