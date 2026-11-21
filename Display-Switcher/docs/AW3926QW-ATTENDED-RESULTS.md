# AW3926QW attended results — 2026-11-21

The user authorized deployment and testing while present. Work is on
`projects/aw3926qw-profiles`; main is unchanged.

## Deployment and recovery

Before replacing binaries, the complete installed controller directory and RTSS
global profile were copied to ignored private storage:
`local-private/display-switcher/pre-aw3926qw-deployment-2026-11-21-083315`.
The original EXEs matched the committed baseline SHA-256 values.
The new console and tray EXEs are installed under `C:/StreamingTools/DisplayController`.
The tray/watchdog stayed stopped during the initial manual profile tests.
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

## Passed: native -> hardware330 -> native, HDR and reapply

- Selecting Other Modes 39-inch 2560x1080/330 changed the monitor EDID from
  DELA2F9 to DELA2FA, with PnP name Alienware AW3926QW(DisplayPort DUAL).
  The initial build did not recognize this second identity. Windows restored
  active TV and VDD outputs as well; the monitor was at330 but HDR was off.
- Added exact native/dual product-ID matching, preserving exclusion of the old
  AW3423DW. Both EXEs compiled; 101 isolated checks passed, including dual EDID,
  case-insensitivity and rejection of an unrelated product-ID prefix.
- Before deployment, the prior installed build/data were privately copied to
  `local-private/display-switcher/pre-dual-edid-2026-11-21-085805`.
- `set monitor-330` verified at08:58:07: monitor-only2560x1080/330/HDRon/cap327,
  TV and VDD inactive. User confirmed it worked, including the HDR correction.
- `reapply` retained Alienware330 with those settings.
- User selected Other Modes Off and confirmed the picture returned. `set monitor`
  verified at09:00:33: nativeDELA2F9, monitor-only5120x2160/165/HDRon/cap162.
  User then confirmed the full ultrawide picture was correct.
- Watchdog-active OSD workflow, actual game cap and VRR remain unverified.
  Do not describe routine330 operation with the tray running as complete yet.

## Initial tray startup

- After all manual round trips passed, verified no existing controller process and
  correct saved/actual native state, then started one updated tray at09:01:54.
- After8seconds it remained running, with native5120x2160/165/HDR, TV/VDD inactive,
  and no additional transitions logged. User screenshot confirmed the icon/menu
  and native,330 and centered4K entries are present.
- The active-watchdog OSD interaction still needs validation: a hardware EDID change
  can restore multiple outputs before the user reapplies a profile. Existing recovery
  could then try the previously selected profile while its timing is unavailable.
- User accidentally changed Smart HDR and manually restored HDR Peak1300Bright.
  A subsequent read-only check confirmed native5120x2160/165/HDR remained active,
  with one tray instance and no additional controller transitions. This verifies
  Windows HDR state; the OSD preset is user-reported, not programmatically queried.

## Passed: tray native -> centered4K -> native

- User selected centered4K from the tray; transition verified at09:04:41.
  User confirmed a correct centered image. Read-only checks found desktop3840x2160,
  unchanged native5120x2160/165 signal, HDRon, NVIDIA GPU-centered/no-scaling3,
  RTSS global cap162, and TV/VDD inactive.
- User selected native from the tray; transition verified at09:05:56.
  User confirmed full ultrawide. Read-only checks found source and signal5120x2160/165,
  HDRon, source position0,0, identity scaling, RTSS global cap162, and TV/VDD inactive.
- One tray instance remained running. No new transition errors appeared during this round trip.

## Passed: hardware-only native/330 synchronization

- User found that OSD-only toggles restored picture/HDR seamlessly. Read-only status
  confirmed native5120x2160/165/HDR but savedAlienware330 and RTSS cap327. This established
  the need to follow hardware state rather than require another tray click.
- User authorized this change and expects to use330 rarely. Added a stable hardware-mode
  observer, scoped to already selected monitor profiles. It waits at least two seconds,
  rechecks live state under the operation mutex, and uses a six-second retry cooldown.
  TV/VDD/Stream/Transition selections and explicit native-EDID4K are protected.
- Correct resolution is retained; only extra-output cleanup, HDR, cap and saved state
  are updated as needed. Returning from DUAL to a native EDID that remembers4K restores
  stock5K2K. The automatic recovery candidate list is unchanged.
- Both EXEs compiled and136 isolated checks passed, including transient, stale-state,
  cooldown, ambiguous identity and TV/stream/4K exclusion cases.
- Previous deployed build/data and RTSS profile are privately backed up under
  `local-private/display-switcher/pre-hardware-sync-2026-11-21-091750`.
- New tray started09:17:51 and automatically corrected the existing stale330/native
  mismatch by09:17:57: native5120x2160/165/HDR/cap162, TV/VDD inactive.
- User used only the monitor menu to select330. At09:18:30, tray automatically saved
  Alienware330; readback confirmed2560x1080/~329.988Hz/HDR/cap327 with other outputs inactive.
- User used only Other Modes Off to return. At09:19:26, tray automatically saved
  Alienware; readback confirmed5120x2160/165/HDR/cap162. User confirmed full ultrawide.

## Passed: explicit4K preservation, reapply and exact rollback

- With the new hardware follower active, explicitly selected centered4K at09:20:36.
  It remained selected after eight seconds; reapply succeeded at09:20:46.
- An intentional negative test requested330 while native hardware mode was selected.
  This produced the expected advertisement-check error at09:20:46, exit10, then
  exact rollback to centered4K at09:20:47. Readback confirmed desktop3840x2160 inside
  unchanged5120x2160/165 signal, HDR, NVIDIA GPU-centered3, cap162, TV/VDD inactive.
- The test intentionally returned to stock native at09:20:49. The user's observed
  back-and-forth picture changes matched this announced test sequence; no new fault
  was identified from those changes. Further unattended live switching was not started.

## Remaining

- Physical power-loss/fallback behavior and reboot startup.
- Actual game cap/HDR/VRR checks; no measured latency or DSC-health claim is made.
- Real handheld and Xbox streaming regressions after physical profiles are stable.
- Dolby Vision disablement is user intent; HDR-on verification does not prove DV is off.

At this checkpoint native Alienware is selected and one tray instance is running. Re-read
live state when resuming. The earlier source checkpoint and baseline are historical.
