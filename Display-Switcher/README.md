# Gaming Display Controller

Source status: AW3926QW profiles are prepared for attended testing on branch
`projects/aw3926qw-profiles`. They are **not deployed or verified on hardware**.
The installed controller remains the old AW3423DW baseline. Do not run the new
console/tray app or deploy it while the user is away.

Read [WORK_IN_PROGRESS.md](WORK_IN_PROGRESS.md) for the source checkpoint and
[the attended test plan](docs/AW3926QW-TEST-PLAN.md) before deployment.

## Source profiles

| Selection | Desktop mode | HDR intent | RTSS cap | Selection policy |
|---|---|---|---|---|
| TV | 3840x2160 / 120 Hz | On | 117 | Existing default; CX or C2/soundbar |
| Monitor / Alienware | 5120x2160 / 165 Hz | On | 162 | Standard monitor selection and monitor recovery candidate |
| Monitor 330 | 2560x1080 / 330 Hz | On | 327 | Explicit manual selection only |
| Monitor 4K | 3840x2160 / 165 Hz, centered | On | 162 | Explicit manual selection only |
| Manual/safety VDD | 1920x1080 / 120 Hz | Off | 120 | Existing behavior |
| Sunshine stream | Client-requested 1080p or 4K / 60, 90, 120 Hz | Client request | Requested FPS | Existing behavior |

The new monitor identity is AW3926QW `DELA2F9`. The old `DELA1E4` monitor is not
eligible for these new timings. TV identity rules remain LG/GSM and exact Samsung
soundbar `SAM721B`; the virtual display remains `MTT1337`.

## Commands after a future authorized deployment

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
Examples above document future operation; they are not a request to execute them.

## Optional-mode prerequisites and limits

330 Hz requires the monitor to advertise 2560x1080/330, which can require Dell's
OSD Other Modes selection. The controller does not operate the OSD or invent a
custom timing. Returning to native 5K2K can require turning that hardware mode off.
If native is unavailable during recovery, the existing TV/native-monitor/VDD
fallback sequence can reach VDD; it does not select an optional resolution instead.

The intended 4K approach is NVIDIA/GPU centering with the monitor remaining in
native mode. The implementation requests centered 3840x2160 inside an existing
5120x2160/~165 Hz target signal through Windows CCD. It preserves the native
target timing and checks the driver's reported source size, signal, HDR and
centering. It does not select Dell's built-in 4K mode, alter NVIDIA global
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
write controller state. The latest source pass has 86 checks passing and both
application builds compiling; no new application binary has been launched/deployed.

The original September/October deployed setup passed its historical staged tests;
that is not verification of this new monitor source. The dated baseline remains in
[BASELINE_BACKUP.md](BASELINE_BACKUP.md). Existing binaries in `backups/` are archival.
The installed path remains `C:/StreamingTools/DisplayController` until deployment
is separately authorized. Source paths are relative to this project folder.

## TV cable swap

Never move the GPU HDMI cable between the CX and C2/soundbar while Windows is
running. Fully shut down Windows, move the connection, turn on the selected TV
and soundbar, then boot. The controller does not automate this physical swap.
