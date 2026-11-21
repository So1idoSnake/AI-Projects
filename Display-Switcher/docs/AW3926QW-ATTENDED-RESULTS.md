# AW3926QW attended results — 2026-11-21

The user authorized deployment and testing while present. Work is on
`projects/aw3926qw-profiles`; main is unchanged.

## Deployment and recovery

Before replacing binaries, the complete installed controller directory and RTSS
global profile were copied to ignored private storage:
`local-private/display-switcher/pre-aw3926qw-deployment-2026-11-21-083315`.
The original EXEs matched the committed baseline SHA-256 values.
The new console and tray EXEs are installed under `C:/StreamingTools/DisplayController`.
The tray/watchdog is deliberately not started during these manual tests.
No startup, Sunshine, audio, NVIDIA global, firmware or calibration settings were changed.

## Passed: native -> centered 4K -> native

- Initial state was native 5120x2160/165/HDR but saved preference TV. `set monitor`
  aligned saved state and selected native/cap162.
- Initial 4K verification rejected CCD scaling=identity and restored the original
  native snapshot successfully. The user saw black bars during the brief attempt.
- A diagnostic request reported source3840x2160 inside an unchanged native
  signal5120x2160/165/HDR. NVIDIA Control Panel showed No scaling on GPU.
- Read-only public NVAPI GetDisplayConfig independently returned scaling3,
  NVIDIA GPU-centered/no-scaling. The controller now requires this confirmation
  when CCD reports identity; it still rejects full-screen stretching, unconfirmed
  identity, wrong geometry/rotation/position/HDR and changed native timings.
- Both EXEs rebuilt and 98 isolated recovery/profile/scaling checks passed.
- Corrected `set monitor-4k` verified at 08:41:42 local time. The user confirmed
  equal side bars and a normally proportioned picture.
- `set monitor` verified the return at 08:43:13: source5120x2160/165/HDR,
  saved state Alienware, TV/VDD inactive. The user confirmed full ultrawide with bars gone.
- RTSS SetLimit reads its property back before returning; cap162 was accepted.
  An actual game's limiter/VRR behavior has not yet been checked.

The read-only scanout-rectangle API exposed a partial-width scanout on this
connection and is not used as proof of complete image placement. No custom timing,
Dell built-in 4K mode or NVIDIA scaling-setting change was needed for this result.

## Passed: monitor -> office TV -> native monitor

- With the TV/soundbar powered on, the monitor briefly blanked a few times but
  remained selected. No new controller transition was logged; the tray was stopped.
  A GPU/link handshake is a possible explanation, not a diagnosed cause.
- `set tv` verified at 08:53:06: office Samsung soundbar/C2 path active at
  3840x2160/120/HDR, cap117, monitor/VDD inactive. User confirmed the TV picture.
- `set monitor` verified at 08:53:28: AW3926QW active at 5120x2160/165/HDR,
  cap162, TV/VDD inactive. User confirmed the full ultrawide picture.
- This checks the office path only; the loft CX was not cable-swapped or retested.
- The user then powered off the TV and confirmed the monitor remained good.
  Read-only status still showed native5120x2160/165/HDR with TV/VDD inactive and
  no further controller transitions logged. EDID availability remained true for the TV.

## Remaining

- Manual Dell hardware330 mode, cap327, and return to native after disabling dual mode.
- 4K reapply and exact rollback from a previously selected 4K state on a failed transition.
- One supervised tray instance, topology/watchdog behavior, reboot startup.
- Actual game cap/HDR/VRR checks; no measured latency or DSC-health claim is made.
- Real handheld and Xbox streaming regressions after physical profiles are stable.
- Dolby Vision disablement is user intent; HDR-on verification does not prove DV is off.

At this checkpoint native Alienware is selected and the tray is stopped. Re-read
live state when resuming. The earlier source checkpoint and baseline are historical.
