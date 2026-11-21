# Host display recovery — October 31, 2026

Installed updated DisplayController.exe and DisplayControllerTray.exe in C:\StreamingTools\DisplayController. Source and build remain in C:\Users\micha\Documents\Codex\2026-09-05\why-x20.

Stream-end now tries TV, Alienware, then VDD. Unavailable targets are skipped; a failed profile advances to the next target. Successful recovery saves a non-streaming profile and clears stream metadata. If all recovery profiles fail, it reports failure and clears any finished Stream/Transition marker, retaining the rollback topology as a virtual-display safety state.

Failed stream setup, incomplete transition recovery, and the watchdog's unavailable-physical-display recovery use the same order. A correctly selected Alienware or VDD does not automatically switch to a newly available TV. Frame-rate validation remains 60, 90, or 120 FPS; RTSS remains host-managed.

Validation:

- Both release executables built successfully; deployed SHA-256 hashes match build outputs.
- Nine isolated recovery checks passed: TV preference, unavailable/failed physical targets, VDD fallback, total failure, and completed stream metadata cleanup.
- Live host controller lifecycle exercised the Sunshine environment request 3840x2160@120 HDR. VDD became active and RTSS Global recorded Limit=120.
- Stream-end skipped the unavailable TV, restored Alienware 3440x1440@175 HDR, saved RTSS Limit=172, and cleared StreamClient/StreamFps and Mode=Stream.
- Sunshine's active stream was terminated by restarting its service at the user's request. Sunshine then reported currentgame=0. This controller lifecycle check was a host simulation, not a new client VRR or sustained gaming benchmark.
- The new tray controller was restarted successfully. TV recovery with a physically available TV and the neither-physical-display case were covered by isolated policy checks, not physical cable/power changes.

Backup: backups/display-controller-pre-fallback-20261031-132610 contains original deployed binaries, source, README, runbook, and state snapshot. The state snapshot was taken during streaming and must not be restored as part of an executable rollback.

Rollback, if needed: wait for Sunshine to be idle, stop only the verified C:\StreamingTools\DisplayController\DisplayControllerTray.exe process, copy the two original executable files from that backup to C:\StreamingTools\DisplayController, and restart DisplayControllerTray.exe tray. Restore original project source/docs if rebuilding the old version. Leave the live state.ini in place; reverting the old binaries reinstates the missing-TV recovery bug.
