# AW3926QW draft checkpoint — paused 2026-11-20

User requested a pause before the session limit. Source changes are on
projects/aw3926qw-profiles, based on d0b7553. No deployment, application launch,
display/HDR/RTSS changes, firmware installation or live hardware tests occurred.
The installed controller remains the old baseline; its temporary monitor session
and paused watchdog must not be changed without user direction.

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

## Verification at pause

Final draft compiled as console and tray. The isolated test executable passed
70 checks without calling live display/HDR/RTSS APIs or writing controller state.
The console/tray binaries were not launched. Outputs are under the original
workspace work/aw3926qw-code/compiled and are not deployed or committed.
Passing synthetic tests does not establish real DSC/VRR, HDR, centering, OSD or rollback behavior.

Two agents reviewed state/recovery and CCD structure/scaling. Their concrete
rollback-verification corrections were incorporated before the last test pass.
The final strict presentation comparison was compile/test-checked but has not
received a second independent review. Recovery's existing rollback may briefly
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

## Resume here

1. Read this note and inspect the feature branch diff before more changes.
2. Finish independent review of strict CCD comparisons, mode-transition/rollback
   edge cases and whether non-virtual-aware snapshots are accepted on this driver.
3. Reconcile README.md, AGENTS.md, SOURCE_GUIDE.md and root REBUILD.md with draft-source
   status. Those documents still describe the pre-monitor baseline; do not mistake
   their old profile values for the new source or the draft for deployed behavior.
4. Add the attended validation plan and DV/HDR limitations to maintained docs.
5. Only when the user is home and authorizes deployment, verify native165,
   manual330/OSD, centered4K, HDR/VRR/DSC, caps, rollback, TV transitions and real
   handheld/Xbox streams. Check scaling before calling the 4K implementation complete.
6. Preserve the existing default/recovery semantics; do not bundle new auto-return,
   DV preference, audio, startup or firmware behavior.

No GitHub push is claimed for this checkpoint; scheduled CLI authentication is
still separate from the previously successful manual Desktop sync.
