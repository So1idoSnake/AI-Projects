# Sunshine / Moonlight streaming

Host configuration and recovery work for the RTX 4090 gaming PC, Xbox client and handheld.
The actual display-switching code is maintained once in ../Display-Switcher/src, not duplicated here.

## Rebuild

Install Sunshine, RTSS and a supported VDD; first verify a local physical display.
Build/review Display-Switcher, then deliberately deploy it to a stable path such as
C:/StreamingTools/DisplayController. Review ../Display-Switcher/deployment/sunshine.conf
and its installed snapshot at ../Display-Switcher/backups/2026-11-20/sunshine.installed.conf.
Create fresh Sunshine credentials/pairings.
Do not copy old authentication files or overwrite installed config without comparing supported options.

The current explicit prep/undo commands invoke DisplayController.exe stream-start / stream-end.
As explicitly requested on 2026-11-21, the current controller's stream-end recovery targets
native Alienware, using VDD temporarily if native is unavailable and restoring the monitor
when it returns. It no longer chooses TV automatically. Client mode requests and Sunshine
audio ownership are unchanged. See [current controller behavior](../Display-Switcher/README.md)
and [attended results](../Display-Switcher/docs/AW3926QW-ATTENDED-RESULTS.md).
The client provides SUNSHINE_CLIENT_WIDTH, HEIGHT, FPS and HDR. Supported stream requests are
1080p/4K at 60, 90 or 120 FPS. HEVC mode 3 permits 10-bit HDR; NVENC preset 4 was retained.
A fixed stale display GUID previously broke detection and should not be restored.
config/vdd_settings.xml preserves the original shareable VDD mode configuration: 1080p and 4K
at 60/90/120 Hz. Use the MikeTheTech driver exposing MTT1337 and verify mode availability on
the new driver version before applying this sample. It is not an installer or live deployment.

## Verified history and limitations

The September setup passed real handheld 1080p/120 SDR and Xbox 4K/120 HDR tests, including audio.
On October 31 a missing-TV restore bug was fixed: TV -> Alienware -> VDD fallback, clear completed
stream metadata even if recovery fails. That historical TV-first priority is superseded
by the monitor-target policy above. See docs/HOST-FALLBACK-CHANGE.md for historical scope.
Changing Moonlight settings while resuming an existing application can bypass prep; use Stop Stream
and start a fresh session when testing client-mode changes.
Host virtual-display refresh does not prove client VRR operation.
Historical logs showed ~119.88 client FPS versus a 120 Hz desktop; pacing remains a separate check.
Observed low instantaneous bitrate was content-dependent, not proof of a fixed 60 Mbps cap.

scripts/restart-sunshine.ps1 is the original helper, relocated unchanged. It restarts SunshineService
and writes its result beside itself; running it terminates active streams. This import did not run it.
The installed config omits dd_configuration_option; an older sample explicitly says disabled.
A prior host inspection found the installed build's default disabled. Re-check on a new version.
