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
The client provides SUNSHINE_CLIENT_WIDTH, HEIGHT, FPS and HDR. Supported stream requests are
1080p/4K at 60, 90 or 120 FPS. HEVC mode 3 permits 10-bit HDR; NVENC preset 4 was retained.
A fixed stale display GUID previously broke detection and should not be restored.
config/vdd_settings.xml preserves the original shareable VDD mode configuration: 1080p and 4K
at 60/90/120 Hz. Use the MikeTheTech driver exposing MTT1337 and verify mode availability on
the new driver version before applying this sample. It is not an installer or live deployment.

## Verified history and limitations

The September setup passed real handheld 1080p/120 SDR and Xbox 4K/120 HDR tests, including audio.
On October 31 a missing-TV restore bug was fixed: TV -> Alienware -> VDD fallback, clear completed
stream metadata even if recovery fails. See docs/HOST-FALLBACK-CHANGE.md for exact scope.
Changing Moonlight settings while resuming an existing application can bypass prep; use Stop Stream
and start a fresh session when testing client-mode changes.
Host virtual-display refresh does not prove client VRR operation.
Historical logs showed ~119.88 client FPS versus a 120 Hz desktop; pacing remains a separate check.
Observed low instantaneous bitrate was content-dependent, not proof of a fixed 60 Mbps cap.

scripts/restart-sunshine.ps1 is the original helper, relocated unchanged. It restarts SunshineService
and writes its result beside itself; running it terminates active streams. This import did not run it.
The installed config omits dd_configuration_option; an older sample explicitly says disabled.
A prior host inspection found the installed build's default disabled. Re-check on a new version.

## Handheld StreamLight client

The November 21 import adds the original
[client settings notes](docs/handheld/STREAMING-SETTINGS.md) and
[October 31 VRR verification](docs/handheld/VRR-VERIFICATION-20261031.md).
Read the verification first: it supersedes the settings document's earlier
116 FPS experiment. The successful Docked request was 3840x2160 at 120 FPS,
100 Mbps, HEVC HDR, hardware decoding, YUV 4:4:4 off, VSync and VRR on.
The handheld work also saved Portable at 1920x1080/120 FPS, 40 Mbps, HEVC HDR
4:2:0 and stereo audio, with VSync/VRR enabled. That preset had not yet been
tested in a new undocked stream. Its historical rollback script is preserved,
without the private registry export.

The Docked session and the user's TV refresh readout supported active VRR.
Its desktop-content average was about 42.7 FPS, so it did not establish sustained
4K120 gameplay performance. TV-reported Dolby Vision did not establish a Dolby
Vision encoded stream. These are historical observations, not new tests.

The two original client rollback scripts are kept separately in
[backups/handheld-2026-10-31](backups/handheld-2026-10-31/README.md).
They restore earlier settings and should not be run as setup scripts. The raw
logs, pairing data, complete registry exports and installed client are excluded.
