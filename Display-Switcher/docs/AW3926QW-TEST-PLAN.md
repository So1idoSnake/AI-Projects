# AW3926QW attended validation plan

Source development only is complete for this checkpoint. Every step below is
deferred until the user is home and authorizes deployment/testing. No hardware,
HDR/DV/DSC setting, RTSS value, service or startup entry was changed during coding.

## Acceptance targets

| Profile | Desktop | Signal / presentation | Cap |
|---|---|---|---|
| Standard monitor | 5120x2160 / 165 Hz | Native ultrawide | 162 |
| Explicit 330 | 2560x1080 / 330 Hz | Dell hardware dual-mode prerequisite | 327 |
| Explicit 4K | 3840x2160 / 165 Hz | Intended NVIDIA/GPU-centered native 5120x2160 signal; 640 black pixels each side | 162 |
| TV | 3840x2160 / 120 Hz | Existing TV profile | 117 |

All physical profiles request Windows HDR on. The user will disable Dolby Vision
manually and use HDR10. That preference has not yet been verified on the machine.

## What the source checks establish

The console and tray compile. 86 isolated checks cover old recovery priority,
monitor aliases/parameters/identity, state parsing/cloning, unchanged stream
request limits, native-structure layout, synthetic centering and rollback
comparisons, snapshot/state consistency, settling retries and timeout failure.
These checks do not run the live transition/rollback pipeline, start RTSS, query
real devices, exercise the timer or verify a game's behavior.

State/recovery and CCD code received independent reviews. Focused fixes were made
for saved-state/snapshot mismatches, stale HDR capture, full presentation polling,
dynamic-refresh comparison and queued-watchdog rechecks. They still need live
validation. No claim is made that CCD can force NVIDIA's control-panel scaling
choice, keep VRR/DSC working, or provide a measured latency advantage.

## Preparation and recovery access

1. Ensure the user can see/control the PC and no Sunshine stream is active.
2. Inspect actual state rather than assuming the last known temporary monitor
   session is unchanged. The old tray was stopped for that session and state said TV.
3. Preserve installed binaries, state and relevant config in a new dated private
   backup. Keep the committed November 20 baseline unchanged.
4. Record native monitor ID, connector/cable, firmware, advertised modes, exact
   refresh, HDR, G-SYNC/VRR and DSC settings. Firmware installation is a separate decision.
5. Disable DV manually for Windows use and verify HDR remains enabled. Prefer the
   per-display Windows DV choice if available; do not change unrelated TV inputs/apps.
6. Only after explicit authorization: stop the old tray if it is running, deploy
   the reviewed build, and test manually before enabling the new tray/watchdog.
   Do not create duplicate tray instances or startup entries.

## Native and TV first

Start with Dell Other Modes off/native. Select `set monitor` first to align the
saved profile with the actual display; confirm native 5K2K/165, HDR, cap162 and
RTSS plus its 64-bit hook loader. Inspect an actual game for limiter enforcement.
Check that the TV and VDD are inactive. Return to TV and verify its unchanged
4K/120/HDR/cap117 behavior, then return to native monitor.

Office C2/soundbar and loft CX testing must use the existing full-shutdown cable
swap procedure. A soundbar input-selection glitch does not by itself establish
a controller bug. Record exact timing/errors rather than changing EDID matching.

## NVIDIA/GPU-centered 4K

Keep the monitor's hardware mode native. Start from a confirmed standard profile
whose saved state matches the actual display. Select `set monitor-4k` and check:

- Windows desktop is 3840x2160/~165; signal remains 5120x2160/~165.
- Image is 16:9, centered and pixel-sharp, with black side bars and no stretching.
- NVIDIA Control Panel reports/retains the intended GPU/no-scaling behavior; compare
  with explicit NVIDIA configuration if the CCD request is unavailable or ambiguous.
- Windows HDR remains on, DV is disabled as intended, and the game behaves as HDR10.
- G-SYNC/VRR, DSC and 10-bit output remain usable on the actual connection.
- RTSS cap162, fullscreen/borderless, Alt-Tab and return-to-native work reliably.

The source verifies the driver's reported geometry/scaling/HDR and expected signal
and rejects substitution; visual behavior still must be checked. Do not add an
EDID/custom resolution or choose Dell's built-in 4K mode to hide a failed test.
If needed, compare those alternatives deliberately and record the result before
changing the implementation. The preferred route remains NVIDIA/GPU management.

## Manual 330 Hz

Switch Dell Other Modes to 39-inch 2560x1080/330 as required, then use
`set monitor-330`. Check advertised mode, exact refresh, HDR, VRR, cap327 and
game stability. Confirm monitor identity remains recognized through the OSD change.
Test `reapply` retains this explicitly selected profile.
Returning to native may require turning the Dell hardware dual mode off first.
The controller cannot return to 5K2K if the monitor no longer advertises it.
Do not work around that by silently selecting the optional mode during recovery.

## Watchdog, failure and stream regression checks

After manual profiles pass, enable one tray instance under supervision. Confirm:

- Turning on a TV does not take over a correctly selected monitor profile.
- A slow HDR/mode transition does not trigger repeated switching from stale queued
  watchdog work after the desired topology is already correct.
- Power loss/disconnect uses existing TV -> native monitor -> VDD priority.
- Unsupported modes or rejected centering leave the original selected profile
  restored, with matching state/cap, or produce a clear logged failure.
- A finished stream clears Stream metadata and allows a fresh subsequent launch.
- Real handheld and Xbox sessions still request their existing resolution/FPS/HDR,
  use the VDD, switch audio through Sunshine, and restore via the same recovery policy.

Use Moonlight Stop Stream and start a fresh application session after changing
client settings. Test default/native recovery after optional selections. If Dell
330 hardware mode prevents native recovery while the TV is unavailable, verify
the VDD safety path and document the manual OSD return needed.

No timer/cap/VRR/HDR behavior is inferred solely from a status command. A reboot
startup test is a later user-controlled step, not part of source verification.

## Rollback and deployment boundary

Keep recovery access through the working TV and VDD. If reverting to the old
controller, first choose a compatible standard state such as TV using the new
controller where possible; the old binary cannot understand optional enum names
or recognize the new monitor. Stop the new tray before replacing binaries.
Do not restore an old Stream state or overwrite personal configs from a snapshot.
No deployment/rollback script is executed or implied by this document.

## HDR10 versus Dolby Vision references

Windows general advanced-color enablement is separate from the per-display DV
choice. The controller intentionally does not detect/toggle DV. Monitor/TV HDMI
DV support alone does not establish Windows DV support through a soundbar.
Sunshine's existing HEVC Main10 HDR path is not a promise of DV metadata passthrough.

- [Windows HDR settings and DV option](https://support.microsoft.com/en-us/windows/hardware/display-graphics/hdr-settings-in-windows)
- [Dell monitor manual](https://dl.dell.com/content/manual37488095-alienware-39-5k-oled-gaming-monitor-aw3926qw-user-s-guide.pdf?language=en-us)
- [Dolby monitor requirements](https://dolby.my.site.com/accesssupport/s/article/Dolby-Vision-Monitors---Everything-you-need-to-know)
- [Windows desktop scaling intent](https://learn.microsoft.com/en-us/windows-hardware/drivers/display/scaling-the-desktop-image)
