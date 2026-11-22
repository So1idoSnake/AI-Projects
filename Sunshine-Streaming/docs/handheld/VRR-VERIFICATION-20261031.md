# VRR verification — October 31, 2026

## Result

StreamLight 8.0.0 connected to Sunshine Desktop with the exact launch request **3840×2160×120**. Video, audio and the connection worked. The client ran its D3D11 adaptive VRR backend, and the user reported **VRR active with a varying frame-rate reading on the LG TV**. This is combined runtime and TV-indicator evidence of VRR operation, beyond an enabled checkbox. The user also reported audio working with no interruptions.

The client does **not** require a 116 FPS launch request for VRR. No host-controller change is required to admit this tested 120 FPS request. The previous 116 request originated in the selected frame-rate preference. StreamLight offers/recommends 116 for a 120 Hz screen; explicitly choosing that option sends 116. Keep the saved rate at 120. Recheck the rate after changing VRR options, since the UI can select its recommendation when enabling VRR.

## Saved configuration

Existing host profile: **Docked**. 3840×2160, 120 FPS, 100 Mbps, HEVC HDR, YUV 4:4:4 off, hardware decoding, borderless, V-Sync on, VRR on, frame pacing on/VRR-managed, fractional V-Sync off, Balanced Target timing, Reduce Judder on. Performance overlay remains enabled.

The Docked profile was already at 120 when this retry began. The only saved setting changed in this retry was the global fallback FPS, from 116 to 120, while StreamLight was fully closed. The application was restarted to remove a discrepancy between its in-memory summary and saved Docked settings. No update was installed. No local RTSS process or installation was found in the checked standard locations. Host RTSS remains managed by the host Display Controller; its actual cap was not independently queried from this client.

Pairing was preserved. A private complete configuration backup and single-setting rollback instructions are in `backups/streamlight-pre-120-retry-20261031-131429/`.

## Session evidence

Log: `StreamLight-1793466874.log` in the Windows temporary directory. The raw log contains private session data; use the redacted excerpts below for sharing. Launch began around 13:15 Eastern, with approximately 175 seconds of streaming between first video initialization and the quit event.

```text
Actual launch mode: 3840x2160x120
Sunshine launch response: status_code=200, gamesession=1
Presentation snapshot: V-sync enabled, VRR requested yes, VRR enabled yes,
fractional V-sync off, refresh 120 Hz, window mode 1
D3D11 VRR backend enabled: refresh=120 Hz,
presentation=DXGI (per-frame tearing/synchronized, estimated timing)
VRR pacing: target 120 Hz with 120 FPS stream
(adaptive timestamp playout, frame timing smoothed, timing profile balanced target)
Renderer 'D3D11VA' chosen
FFmpeg-based video decoder chosen
HEVC: Main 10 profile bitstream, hevc_d3d11va2 hardware path
Audio stream has 8 channels
SDL audio driver: wasapi
Received first audio packet after 300 ms
```

The V-Sync-off renderer probes earlier in the log are startup/preflight checks. They do not override the live presentation snapshot and active VRR worker. In the real session the saved V-Sync setting is on; StreamLight's adaptive backend chooses tearing or synchronized presentation per frame. The runtime backend log plus source provide evidence of the allow-tearing-capable adaptive path, not a measured count of actual `DXGI_PRESENT_ALLOW_TEARING` calls. Physical TV behavior is supported by the user's TV readout, rather than inferred from that flag alone.

The TV reportedly displayed **Dolby Vision**. The client reports **HEVC 10-bit HDR**; that does not independently establish a Dolby Vision encoded stream or dynamic metadata. Treat the TV badge as the reported output/display processing mode.

## Final statistics

```text
Video stream: 3840x2160 42.69 FPS (Codec: HEVC 10-bit HDR)
Bitrate: 1.6 Mbps (session average)
Incoming frame rate from network: 42.69 FPS
Decoding frame rate: 42.69 FPS
Rendering frame rate: 42.12 FPS
Host processing latency min/max/average: 5.4/17.8/5.6 ms
Frames dropped by your network connection: 0.00%
Frames dropped by client pacing: 1.34%
Average network latency: 1 ms (variance: 0 ms)
Average decoding time: 0.56 ms (waiting for the decoder 0.09 ms)
Average frame queue delay: 7.38 ms
Average rendering time (including monitor V-sync latency): 1.60 ms
VRR pacing: Active | Smoothness (2m): 98.04% / 99.50% target
Client interval error (1s): 1.081 ms | Tolerance: 0.50 ms | Dropped (30s): 6
```

120 FPS is the requested/negotiated ceiling, not the measured session average. The Desktop content was not a sustained 120 FPS game benchmark, and the low average bitrate does not show that a 100 Mbps motion-heavy stream was exercised. The visible overlay showed delivery varying with Desktop activity. Do not claim that this establishes sustained 4K120 decode/render performance under gameplay load.

The connection had no measured network frame loss, hardware decoding was fast, and the user reported no audible/visible interruptions. Pacing was nevertheless below its own smoothness target, with 1.34% client pacing drops. VRR operation is verified for this session; ideal frame pacing and maximum-load performance are not.

## Cleanup and remaining test

The stream was ended through the client's normal disconnect shortcut. The log records input/audio/video/control streams stopping, ENet acknowledging disconnection, and a subsequent Sunshine **status_code=200, cancel=1** response. StreamLight returned to the host page with **Open**, not a running-session Resume control. The Docked profile remains ready at 4K120 HDR.

For a later performance test, use a repeatable game scene that sustains near 120 incoming FPS for two minutes and compare decode time, queue delay, client pacing drops and the TV refresh readout. Keep the launch request at 120 and let the existing host controller manage its cap. If lower-rate headroom is desired later, decouple the host game cap from the 120 FPS launch request rather than assuming VRR requires a 116 request.
