# Display Switcher development context

Attended validation is in progress: read [the results](docs/AW3926QW-ATTENDED-RESULTS.md),
[WORK_IN_PROGRESS.md](WORK_IN_PROGRESS.md), README.md and the test plan first.
Do not deploy or run the new application against live displays while the user is away.

Read README.md, BASELINE_BACKUP.md and ../REBUILD.md first.
The actual implementation is src/DisplayController/DisplayController.cs; build.ps1 builds both EXEs.
The icon generation source is tools/New-TrayIcon.ps1 and recovery tests are in src plus test-recovery.ps1.
Keep backups/2026-11-20 intact. Runtime deployment is C:/StreamingTools/DisplayController.
The historical baseline recognizes AW3423DW DELA1E4. The installed attended-test build
recognizes AW3926QW native DELA2F9 and DisplayPort DUAL DELA2FA. Native/4K and
office-TV and hardware330/native round trips passed on 2026-11-21, including HDR and reapply.
Native 5120x2160/165 HDR/cap162 is the requested standard monitor selection; optional manual
2560x1080/330/cap327 and 3840x2160/165 black bars/cap162 are coded. DSC is accepted.
4K uses NVIDIA/GPU centering through Windows CCD, not Dell Other Modes. Where CCD
reports identity for the smaller desktop, read-only NVAPI must confirm GPU no-scaling.
330 Hz needs the monitor's hardware mode. At the user's request, the running tray now
follows native/DUAL hardware changes while a monitor profile is selected. Hardware-only
round-trip tests passed with HDR/cap327 and HDR/cap162. It waits for stable observations,
rechecks under the operation lock, and skips resolution resets when already correct.
It never follows these changes from TV/Stream/VDD state, and native EDID does not cancel
explicit centered4K. User subsequently changed all automatic recovery, including Stop
Stream, to native Alienware -> temporary VDD. Never automatically select TV during recovery.
`AwaitingMonitor=True` in Vdd state preserves native-return intent across restart; the tray
retries when native is available. Manual selection or a new stream cancels that intent.
Native timing must be advertised; hardware330 is not an automatic recovery candidate.
164 isolated checks pass. Physical off/on return is pending attended confirmation.
Neither OSD nor NVIDIA global
settings are automated. The user will disable DV manually and keep HDR10; do not add DV automation.
Client streaming modes/audio/startup must stay unchanged unless separately requested.
The new recovery destination was explicitly requested for every recovery path, including
stream-end; the previous TV-first policy is superseded. The two TV connections use different
HDMI routes (soundbar passthrough versus direct-TV with eARC audio), so EDID availability
must not be treated as TV/soundbar power or usable-picture detection.
The old binaries were privately backed up before deployment. The new tray/watchdog
is now running as one instance; initial observation found no unwanted transitions.
Saved state is native Alienware;
recheck live state before any further transition. Do not start an old binary against
the new monitor or optional state names.
Never move the TVs' HDMI cable while Windows is running; full shutdown first.
Building is not deployment. Do not call validate/stream-start/set or restart services during a
documentation/import task. Follow project-specific verification only for authorized implementation.
