# AW3926QW checkpoint — attended testing in progress

## Current status — 2026-11-21

The user is home and authorized deployment/testing. See
[attended results](docs/AW3926QW-ATTENDED-RESULTS.md) for current state and remaining work.
Both EXEs are deployed; the tray/watchdog remains stopped during manual tests.
NVIDIA readback and dual-mode identity fixes pass 101 isolated checks. Native -> centered 4K
-> native 5120x2160/165/HDR passed controller checks and user visual confirmation.
The source-only checkpoint below is historical; its no-deployment statements describe
the earlier work, not the current installation. The office TV/native-monitor round trip
and TV-off retention also passed. 330 entry/HDR/cap327 and reapply passed after recognizing
the new DELA2FA EDID. Return from hardware330 to native remains pending, as do
streaming/startup/game tests and OSD workflow with the watchdog active.

## Earlier source-only checkpoint

Source development resumed after the earlier pause. Changes are on
projects/aw3926qw-profiles, based on d0b7553. No deployment, application launch,
display/HDR/RTSS changes, firmware installation or live hardware tests occurred.
The installed controller remains the old baseline. At the last manual monitor
switch, its watchdog was paused and saved state still said TV; inspect actual
state at the attended session rather than assuming it has remained unchanged.
See [the attended test plan](docs/AW3926QW-TEST-PLAN.md) for the next steps.

## Draft source written

- Recognize AW3926QW DELA2F9; retire DELA1E4 from the new monitor timings.
- Existing monitor/alienware aliases select native 5120x2160/165, HDR on, cap162.
- Explicit alienware-330 / monitor-330 selects 2560x1080/330, HDR on, cap327.
- Explicit alienware-4k / monitor-4k requests centered 3840x2160 at165, HDR on, cap162.
- Append optional enum values; preserve prior state names/numbers and selected variant on reapply.
- Keep TV -> native Alienware -> VDD recovery priority, TV parameters, Sunshine request
  contract, audio ownership, startup and topology-only watchdog unchanged.
- Advertised-mode checks avoid creating unsupported custom timings. 330Hz still
  requires the corresponding Dell hardware mode; no OSD automation was added.
- 4K uses a captured native source/target configuration and CCD centered scaling,
  preserves native target timings, rejects reported stretching, and restores the
  captured configuration on failure. No NVAPI, NVIDIA global setting or DSC toggle.
- Source-only tests cover profile/identity/state logic, original recovery cases,
  unchanged streaming requests, CCD ABI layouts, synthetic centering and rollback comparisons.
- The snapshot must match the saved original identity, desktop, path/signal refresh
  and HDR before exact rollback is used. Entering 4K refuses an inconsistent starting
  state before any transition-state write; leaving 4K can still recover to TV/VDD.
- Centering waits for native timing/HDR to settle and polls the full expected
  presentation afterwards; stale HDR, query errors and timeouts cannot imply success.
- A queued watchdog action rechecks topology under the lock and returns if the
  earlier transition already completed. Timer values and topology-only policy are unchanged.

## Offline verification

Final draft compiled as console and tray. The isolated test executable passed
86 checks without calling live display/HDR/RTSS APIs or writing controller state.
The console/tray binaries were not launched. Outputs are under the original
workspace work/aw3926qw-code/compiled and are not deployed or committed.
Passing synthetic tests does not establish real DSC/VRR, HDR, centering, OSD or rollback behavior.

Two agents reviewed state/recovery and CCD structure/scaling, including the strict
presentation comparison. Their focused corrections and regression cases were
incorporated before the final offline checks. Recovery's existing rollback may briefly
restore a previously explicit optional profile between failed candidate attempts;
the automatic candidate list itself contains only TV, native monitor and VDD.

## Dolby Vision research

An agent reviewed current Microsoft, Dell and Dolby guidance. Keep general Windows
HDR/advanced-color enablement independent of DV. The current SetHdr API cannot
select or verify HDR10 versus Dolby Vision. Windows may auto-select DV on qualifying
monitors; no guarantee has been established for all three modes or TV/soundbar paths.
User decision: the user will manually disable Dolby Vision for this Windows setup;
its benefit does not justify the added complexity. Standard HDR10 remains the target,
with Windows HDR enabled. Do not add Dolby Vision support or automatic DV switching.
This records intent, not a verified setting: nothing was disabled during this session.

- Microsoft: https://support.microsoft.com/en-us/windows/hardware/display-graphics/hdr-settings-in-windows
- Dell manual: https://dl.dell.com/content/manual37488095-alienware-39-5k-oled-gaming-monitor-aw3926qw-user-s-guide.pdf?language=en-us
- Dolby compatibility: https://dolby.my.site.com/accesssupport/s/article/Dolby-Vision-Monitors---Everything-you-need-to-know
- CCD scaling: https://learn.microsoft.com/en-us/windows-hardware/drivers/display/scaling-the-desktop-image

## Next: attended validation, not automatic deployment

1. Read this note, README.md and docs/AW3926QW-TEST-PLAN.md before further action.
2. Verify that this driver's non-virtual-aware snapshots and centered-scaling request
   behave as intended. Fail-closed rejection is possible and must not be bypassed blindly.
3. Only when the user is home and authorizes deployment, verify native165,
   manual330/OSD, centered4K, HDR/VRR/DSC, caps, rollback, TV transitions and real
   handheld/Xbox streams. Check scaling before calling the 4K implementation complete.
4. Preserve the existing default/recovery semantics; do not bundle new auto-return,
   DV preference, audio, startup or firmware behavior.

Source work is prepared; device/driver behavior and real transition/rollback integration
remain unverified. The historical runbook and backups are not the new profile specification.

No GitHub push is claimed for this source checkpoint; scheduled CLI authentication is
still separate from the previously successful manual Desktop sync.
