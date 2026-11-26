# StreamLight settings — October 31, 2026

**Superseded by the successful 4K120 retry:** see [VRR verification](VRR-VERIFICATION-20261031.md). The current Docked profile requests 120 FPS, and the global fallback is also 120. The stream connected, active VRR pacing ran, and the user confirmed VRR active with varying refresh on the TV. The 116 FPS preset and failed-launch findings below describe the earlier attempt.

## Applied balanced Docked preset

StreamLight 8.0.0 at `C:\Program Files\StreamLight\StreamLight.exe` remains installed. No update was installed. The host card's **Profiles → Docked → Video** contains these settings:

| Setting | Value |
|---|---|
| TV output | Existing 3840×2160, 120 Hz; unchanged |
| Stream resolution | 3840×2160 |
| Stream frame rate | 116 FPS, StreamLight's recommendation below a 120 Hz ceiling |
| Bitrate | 100 Mbps |
| Codec | HEVC |
| HDR | On |
| YUV 4:4:4 | Off, requesting 4:2:0 |
| Display mode | Borderless |
| V-Sync | On |
| Frame pacing | On, managed by the VRR worker when active |
| Fractional V-Sync | Off |
| VRR | On |

Global **Settings → Video** keeps the existing Balanced Target VRR timing and Reduce Judder enabled. Global **Settings → Advanced** uses hardware decoding. Global **Settings → Overlay** has detailed performance statistics enabled, including frame rates, network drops, decode, frame queue, rendering, cadence and VRR pacing. Global YUV 4:4:4 is also disabled, so Portable cannot inherit the unsupported 4:4:4 path. Portable's own saved overrides were not edited.

The global settings tabs/labels are described by the installed v8.0.0 source; the Docked profile and its loaded effective summary were directly verified in the UI.

## Verified device and connection

- Handheld previously identified as ASUS ROG Xbox Ally X RC74XA, with internal AMS740LS02-0 OLED, 1920×1080 at 120 Hz. The current target is the external TV.
- Current GPU: AMD Radeon 890M, driver 32.0.32015.7029. AMD Software UI reports version 26.10.1.
- Current Windows video-controller report: 3840×2160 at 120 Hz. This query reports integer refresh, not a precise fractional timing.
- AMD Display UI identifies LG TV SSCR2 and reports AMD FreeSync Premium **Enabled**. Its DisplayPort label describes the GPU-facing USB-C display path and does not identify the TV's physical connector.
- Active client adapter: Realtek USB GbE, wired at 1 Gbps. Keep wired networking for the TV preset. The previously inspected Wi-Fi adapter was MediaTek MT7922 Wi-Fi 6E; Wi-Fi was not active during this setup.
- HEVC Main10 D3D11VA hardware decoding passes the new startup/session-preflight checks. AV1 was not benchmarked in this task; HEVC is selected for the measured baseline.
- Controller/touch/audio settings were preserved: multiple controllers enabled, gamepad mouse enabled, background gamepad input disabled, absolute touch enabled, mouse acceleration disabled, keep-awake enabled, audio 7.1. Use stereo when listening through the handheld speakers or a stereo endpoint; retain surround only with a matching playback system. Rumble response still needs a game test.

## Why 4:4:4 was disabled

The previous real session log, `StreamLight-1793465381.log`, reports HEVC 10-bit HDR 4:4:4, failure of D3D11VA and DXVA2 hardware decode, and fallback to software decode with Vulkan/libplacebo. Its final summary reports:

```text
Video stream: 3840x2160 51.82 FPS (Codec: HEVC 10-bit HDR 4:4:4)
Average decoding time: 45.06 ms (waiting for the decoder 30.94 ms)
Average frame queue delay: 3.50 ms
Average rendering time (including monitor V-sync latency): 4.89 ms
Frames dropped by your network connection: 10.73%
Frames dropped by client pacing: 1.38%
VRR pacing: Inactive
```

These are historical figures before this optimization. The network-drop label alone does not establish a physical network fault while decoding was badly overloaded.

## Verification result and remaining blocker

The application loaded the Docked summary as **4K / 116 FPS / 100 Mbps / HDR / HEVC**. Its profile UI reports Borderless, V-Sync and frame pacing controlled by VRR, and fractional V-Sync off.

A new Desktop launch was attempted. The client preflight passes HEVC Main10 D3D11VA, and the presentation snapshot requests VRR on the 120 Hz display in borderless mode. That snapshot is intent/preflight evidence, not proof of live presentation. Sunshine returned the following before creating a game session:

```text
Launch display mode: 3840x2160x116
Presentation snapshot: V-sync enabled, VRR requested yes, VRR enabled yes,
fractional V-sync off, refresh 120 Hz, window mode 1
Failed to start the specified application
gamesession: 0
```

Source: new `StreamLight-1793466161.log` in the user's temporary directory. No new live stream started, so no post-change decoder latency, queue statistics, runtime VRR pacing result or allow-tearing presentation result is available. Startup's V-Sync-off probes do not describe a live session.

The host's Sunshine log around 13:05–13:06 Eastern on October 31 is needed to identify the failing prep/launch command. The 116 FPS request may be relevant if display automation assumes request FPS equals a supported monitor refresh; that is an unconfirmed hypothesis. The intended host mapping is a **3840×2160, 120 Hz** virtual display with an **RTSS game cap of 116**, while the client requests 116 FPS. Do not create a 116 Hz display mode just to match that request. Confirm the existing automation can decouple these values.

The client GPU uses AMD FreeSync. Host-side G-SYNC does not prove that the TV is using VRR. The host RTSS cap was not inspected or changed from this client.

## Alternatives — recommendations, not applied

| Preset | Resolution / stream FPS | Bitrate | VRR timing |
|---|---|---|---|
| Balanced, applied | 4K / 116 | 100 Mbps HEVC HDR 4:2:0 | Balanced Target |
| Lower latency | 1440p / 116, keep TV 120 Hz | 60 Mbps HEVC, HDR optional | Low Latency, test for pacing drops |
| Higher image quality | 4K / 116 | 150 Mbps HEVC HDR 4:2:0 | Balanced Target |

Change resolution/bitrate in **Profiles → Docked → Video**, and timing in **Settings → Video → VRR timing**. Keep hardware decoding, borderless, V-Sync and VRR enabled for all three. Increase bitrate only after a repeatable two-minute moving scene is stable. AV1 10-bit 4:2:0 can be compared at 80–100 Mbps after confirming D3D11VA, VRR pacing and decoding latency in a real session; it is not assumed faster. Avoid HEVC/AV1 4:4:4 on this measured hardware path.

## Matching host guidance and next test

Keep the established RTX 4090 NVENC HEVC Main10 and HDR capture path. This client setup does not justify changing Sunshine encoders, drivers, global graphics settings, power plans or display-controller software. Resolve the host launch/prep error and confirm the 4K120 / RTSS116 mapping first.

Then stream a repeatable moving game scene for two minutes. Confirm the overlay shows 3840×2160, HEVC 10-bit HDR without 4:4:4, hardware decoding, and **VRR pacing: Active**. Compare incoming/rendering rate with the actual 116-capped game, and record network drops, decoding time, queue delay and cadence. Check the LG TV's live VRR/refresh readout changes with the game rate while HDR remains active. Active app pacing and FreeSync enabled are supporting evidence; neither alone verifies physical TV VRR.

## Exact rollback

Pairing is preserved in the private full backup. Close StreamLight, then run:

```powershell
& 'C:\Users\micha\Documents\Codex\2026-10-25\inspect-this-asus-rog-xbox-ally\backups\streamlight-pre-4k116-20261031-130131\Restore-Settings.ps1'
```

Reopen StreamLight. This removes only the nine newly added Docked overrides and restores the four changed global values. No pairing information is printed. The rollback script's syntax was checked without executing it.

## Primary references checked

- StreamLight v8.0.0 source: https://github.com/FoggyBytes/StreamLight/tree/v8.0.0
- StreamLight v8.0.0 release: https://github.com/FoggyBytes/StreamLight/releases/tag/v8.0.0

The source verifies the persisted configuration keys, profile precedence, 116 FPS recommendation, V-Sync dependency, forced borderless mode, and renderer-level VRR fallback.
