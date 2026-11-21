# Display Switcher baseline backup

Snapshot date: 2026-11-20 (America/New_York).

This is the existing implementation before adding AW3926QW monitor profiles.
Source, scripts, assets, deployment samples, and existing documentation were
copied without changing their contents. No build or hardware tests were run.

## Exact deployed build

`backups/2026-11-20/` contains the two deployed executables, the installed
Sunshine configuration, RTSS global profile, and saved controller state.
These are reference snapshots, not live configuration or an automatic restore
mechanism. The executable snapshots are deliberately tracked for rollback.

SHA-256:

- DisplayController.exe: `924C374FB5A2CEBFE159180822F3EAC13163ECB70700BED3CA0802639C115333`
- DisplayControllerTray.exe: `7C7F0EC9C51151A8720605F33388472C0C6A7684A2D3CA4C732F11BFDE3FA2EE`

Both deployed binaries match their corresponding workspace output byte for byte.
The current source includes the October 31 recovery order TV -> Alienware -> VDD
and finished-stream state cleanup. Some existing documentation retains older
verification dates; this backup does not reconcile or rewrite it.

## Runtime situation at backup

The AW3926QW was temporarily activated at 5120x2160/165 Hz, HDR enabled, with
RTSS set to 162 FPS. The old tray watchdog was stopped for that session because
the installed controller does not yet recognize the new monitor. The saved
controller state still says TV. Returning to TV should also resume the tray.
This temporary session does not constitute an implemented AW3926QW profile.

The deployed Alienware profile still recognizes DELA1E4 (AW3423DW) and uses
3440x1440/175 Hz with a 172 FPS cap. The new monitor is DELA2F9 (AW3926QW).
The installed Sunshine configuration differs from its workspace sample: the
sample's dd_configuration_option key is absent from the installed configuration.
Both versions are preserved; neither was applied or edited.

## Pending work, not implemented

- Standard monitor profile: 5120x2160/165 Hz, HDR on, proposed cap 162.
- Explicit competitive profile: 2560x1080/330 Hz, proposed cap 327.
- Explicit 16:9 profile: 3840x2160/165 Hz, black side bars, proposed cap 162.
- Prefer evaluating NVIDIA GPU/no scaling for the 4K option before custom timing
  or the monitor's built-in 4K mode. DSC is an accepted requirement.
- Preserve the existing TV, streaming, audio, startup, and recovery defaults;
  native 5K2K is the standard monitor selection and monitor recovery profile.

Firmware, monitor-menu control, link behavior, HDR/VRR, and all new modes remain
unverified on the live hardware. No monitor implementation, installation,
firmware update, startup change, or GitHub push is part of this backup.

Runtime logs, Sunshine credentials/certificates/client pairing databases, and
unrelated PC files are intentionally excluded.
