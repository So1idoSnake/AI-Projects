# Gaming Display Controller

Status: attended testing began on 2026-11-21 on branch `projects/aw3926qw-profiles`.
The AW3926QW build is deployed. One tray/watchdog instance was restarted after manual
native/4K, office-TV and hardware330/native round trips passed; further tray tests remain.
Native -> centered 4K -> native passed controller checks and user visual confirmation.
The remaining attended checks are tracked in
[the results](docs/AW3926QW-ATTENDED-RESULTS.md); the full setup is not yet validated.

Read [WORK_IN_PROGRESS.md](WORK_IN_PROGRESS.md) for the source checkpoint and
[the attended test plan](docs/AW3926QW-TEST-PLAN.md) before continuing validation.

## Source profiles

| Selection | Desktop mode | HDR intent | RTSS cap | Selection policy |
|---|---|---|---|---|
| TV | 3840x2160 / 120 Hz | On | 117 | Existing default; CX or C2/soundbar |
| Monitor / Alienware | 5120x2160 / 165 Hz | On | 162 | Standard monitor selection and monitor recovery candidate |
| Monitor 330 | 2560x1080 / 330 Hz | On | 327 | Explicit manual selection only |
| Monitor 4K | 3840x2160 / 165 Hz, centered | On | 162 | Explicit manual selection only |
| Manual/safety VDD | 1920x1080 / 120 Hz | Off | 120 | Existing behavior |
| Sunshine stream | Client-requested 1080p or 4K / 60, 90, 120 Hz | Client request | Requested FPS | Existing behavior |

AW3926QW uses `DELA2F9` in native mode and `DELA2FA` in DisplayPort DUAL/330 mode.
Both are recognized as the same physical monitor. The old `DELA1E4` monitor is not
eligible for these new timings. TV identity rules remain LG/GSM and exact Samsung
soundbar `SAM721B`; the virtual display remains `MTT1337`.

## Commands for the deployed attended-test build

```text
DisplayController.exe status [--json]
DisplayController.exe set monitor
DisplayController.exe set monitor-330
DisplayController.exe set monitor-4k
DisplayController.exe set tv
DisplayController.exe set vdd
DisplayController.exe reapply
DisplayController.exe stream-start
DisplayController.exe stream-end
DisplayController.exe restore-lg
DisplayController.exe set-cap <fps>
DisplayController.exe validate-stream-request
DisplayController.exe validate
DisplayController.exe tray
```

`alienware`, `aw`, `monitor`, `alienware-native`, `monitor-native` and `monitor-5k2k`
all select native 5K2K. Optional aliases are `alienware-330` / `monitor-330` and
`alienware-4k` / `monitor-4k`. The tray exposes those same three monitor choices.
Examples document available commands; they are not a request to execute them.

## Optional-mode prerequisites and limits

330 Hz requires the monitor to advertise 2560x1080/330 using Dell's
OSD Other Modes selection. Hold the joystick inward four seconds to open the
shortcut and choose 39-inch 21:9 2560x1080/330. The hardware change exposes a new
EDID; Windows can restore an extended topology and leave HDR off on the new identity.
Applying the explicit monitor profile disables the other outputs and enables HDR again.
The controller does not operate the OSD or invent a
custom timing. Returning to native 5K2K can require turning that hardware mode off.
If native is unavailable during recovery, the existing TV/native-monitor/VDD
fallback sequence can reach VDD; it does not select an optional resolution instead.

The intended 4K approach is NVIDIA/GPU centering with the monitor remaining in
native mode. The implementation requests centered 3840x2160 inside an existing
5120x2160/~165 Hz target signal through Windows CCD. It preserves the native
target timing and checks the driver's reported source size, signal, HDR and
centering. On the observed NVIDIA driver, CCD reports identity scaling despite
GPU no-scaling. A read-only NVAPI query must confirm GPU-centered/no-scaling before
that specific report is accepted; geometry/timing/HDR checks still apply.
It does not select Dell's built-in 4K mode, alter NVIDIA global
preferences, create custom timings, or toggle DSC. Driver acceptance is not a
measured guarantee of GPU scaling, image quality, VRR or latency; confirm these
in the attended test. No automatic Dell-mode fallback is implemented.

The centering request is rejected if the original state/configuration is missing
or inconsistent. Reapply/select a standard profile first. This matters after the
temporary monitor session whose actual display and saved TV preference differ.
If Windows reports stretching or changes the expected presentation, the new
transition fails and attempts to restore the matching previous profile/configuration.

## HDR and Dolby Vision

The user will manually disable Dolby Vision for this Windows setup; its benefit
does not justify the complexity. Keep Windows HDR enabled and target standard HDR10.
No DV preference was changed by source development. The existing Windows
advanced-color toggle enables HDR but cannot choose or verify HDR10 versus DV.
DV automation and metadata handling are out of scope. DSC remains accepted.

## Defaults preserved

TV remains the default physical profile. Recovery still tries TV, native Alienware,
then VDD. A correctly selected monitor does not switch to a newly available TV.
Manual optional profiles remain selected on reapply; they are never automatic
recovery candidates. Existing rollback can temporarily restore a previously chosen
optional mode between failed recovery attempts.

The watchdog remains topology-only with the existing timers. It now rechecks
topology under the operation lock so an action queued during a completed slow
transition does not reapply it unnecessarily. It does not enforce resolution/HDR/caps
continuously. Streaming remains owned by Sunshine prep/undo; no socket inference.

Sunshine still reads `SUNSHINE_CLIENT_WIDTH`, `HEIGHT`, `FPS`, `HDR`, accepts 1080p
and 4K at 60/90/120, and uses the requested FPS cap and HDR state. Stop Stream
uses the existing recovery order. Audio selection remains Sunshine's responsibility.
No Sunshine config, audio, startup, firmware, driver, DV, or TV auto-return change
is included in this source update.

## Build, checks and deployment

See [SOURCE_GUIDE.md](SOURCE_GUIDE.md) for source paths, compiler and RTSS dependencies.
`./build.ps1` compiles console/tray into ignored `outputs/DisplayController`.
`./test-recovery.ps1` runs isolated checks using synthetic configurations and
process-local test variables; it does not call the live display/HDR/RTSS APIs or
write controller state. The latest source pass has 101 checks passing and both
application builds compiling. The build is deployed for attended manual testing.

The original September/October deployed setup passed its historical staged tests;
that is not verification of this new monitor source. The dated baseline remains in
[BASELINE_BACKUP.md](BASELINE_BACKUP.md). Existing binaries in `backups/` are archival.
The installed path remains `C:/StreamingTools/DisplayController` until deployment
is separately authorized. Source paths are relative to this project folder.

## TV cable swap

Never move the GPU HDMI cable between the CX and C2/soundbar while Windows is
running. Fully shut down Windows, move the connection, turn on the selected TV
and soundbar, then boot. The controller does not automate this physical swap.
