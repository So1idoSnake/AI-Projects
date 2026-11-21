# Gaming Display Controller Runbook

Last verified: September 6, 2026

## Purpose

This gaming PC uses one controlled display output at a time:

- An LG CX television in the loft
- An LG C2 television in the office, connected through a Samsung HDMI 2.1 soundbar
- A Dell Alienware AW3423DW monitor
- A virtual display used only for Sunshine/Moonlight streaming

The controller prevents powered-off or newly powered-on physical displays from taking over the desktop, gives each use case the correct resolution, refresh rate, HDR state, and frame-rate cap, and restores the TV profile when a Moonlight stream ends.

## Hard safety rule

Never move the GPU's HDMI cable between the LG CX and LG C2 while Windows is running.

To change TVs:

1. Shut Windows down completely. Do not merely restart it.
2. Move the single GPU HDMI cable to the other TV/soundbar path.
3. Turn on the selected TV and any intervening soundbar.
4. Boot the PC.
5. Confirm the TV profile with the tray menu or the `status` command.

The controller intentionally does not automate this physical cable swap. Live HDMI/GPU hot-swapping has previously caused instability.

## Operating model

Only one profile should be active at a time.

| Profile | Active display | Mode | HDR | RTSS cap | Selection |
|---|---|---:|---:|---:|---|
| TV | Connected LG CX or LG C2/soundbar | 3840×2160 at 120 Hz | On | 117 FPS | Default and manual |
| Alienware | Dell AW3423DW | 3440×1440 at 175 Hz | On | 172 FPS | Manual only |
| Handheld stream | Virtual display | 1920×1080 at 120 Hz | Off for the current handheld | 120 FPS | Automatic from Moonlight |
| Xbox stream | Virtual display | 3840×2160 at 120 Hz | On | 120 FPS | Automatic from Moonlight |
| Manual VDD | Virtual display | 1920×1080 at 120 Hz | Off | 120 FPS | Manual/safety use |

The two LG televisions share one logical `TV` profile because the GPU has one HDMI output and only one TV is connected to it at a time. The office path is recognized by the Samsung soundbar's `SAM721B` endpoint; the loft path is recognized by its native LG/GSM endpoint.

## Normal use

### Playing on either LG TV

The TV is the default physical output. It should be the only active display at 4K/120 Hz with HDR enabled and RTSS capped at 117 FPS.

Use **TV only (LG CX / LG C2)** from the tray menu if another profile was previously selected.

### Playing on the Alienware monitor

Alienware mode is deliberately manual. Select **Alienware only** from the tray menu or ask ChatGPT to switch it.

The controller will:

- Disable the TV and virtual display paths
- Select 3440×1440 at 175 Hz
- Enable HDR
- Change RTSS to 172 FPS

Turning on a connected TV while Alienware mode is selected must not take over the desktop. Return manually with **TV only (LG CX / LG C2)** when finished.

### Streaming to the handheld

Start the session normally from Moonlight with the client configured for 1920×1080 at 120 FPS.

Sunshine will call `stream-start`, which:

- Disables both physical display paths
- Activates only the virtual display
- Applies the client-requested 1920×1080 at 120 Hz mode
- Leaves HDR off for the current non-HDR handheld
- Sets RTSS to the requested 120 FPS

Use Moonlight's **Stop Stream** action when finished. Sunshine then runs `stream-end`, waits briefly for the streaming session to settle, restores the selected LG TV at 4K/120 HDR, and returns RTSS to 117 FPS.

### Streaming to Xbox

Start Moonlight with the Xbox client configured for 3840×2160 at 120 FPS with HDR enabled.

Sunshine uses the same virtual-display flow but applies 4K/120 HDR. The verified stream used HEVC 10-bit HDR with Rec. 2020 color and SMPTE 2084 PQ.

Moonlight's **Stop Stream** action restores the TV and 117 FPS cap.

## Resolution and HDR selection

Sunshine does not decide between hard-coded device names. It passes the Moonlight request to the controller through these variables:

```text
SUNSHINE_CLIENT_WIDTH
SUNSHINE_CLIENT_HEIGHT
SUNSHINE_CLIENT_FPS
SUNSHINE_CLIENT_HDR
```

This Sunshine build does not expose `SUNSHINE_CLIENT_NAME`, so the controller distinguishes use cases by the requested mode:

- 1920×1080 requests are treated as handheld-class streams.
- 3840×2160 HDR requests are treated as Xbox-class streams.
- Supported refresh requests are 60, 90, and 120 FPS.
- Unsupported requests fail closed rather than applying an unknown display configuration.

HDR follows the client's Moonlight request. The current handheld requests SDR; the future OLED handheld should request HDR and must be verified once with a real session. No Windows HDR calibration or per-display luminance profile is automated. Games can use their own HDR calibration, with the LG televisions treated as approximately 1000-nit-class displays.

## Audio behavior

The display controller does not change audio devices. Sunshine owns streaming audio.

Both Sunshine **Audio Sink** and **Virtual Sink** are intentionally blank, enabling automatic device selection:

- During a stream, Sunshine selects `Moonlight (Steam Streaming Speakers)` and configures the verified 48 kHz 7.1 virtual sink.
- When the stream ends, normal Console and Multimedia audio returns to the physical LG HDMI endpoint.
- Windows' separate Default Communications role was observed pointing to Moonlight Speakers after a stream. Applications such as Discord that explicitly use the communications default may need their output selected manually.

## G-SYNC, V-SYNC, FreeSync, and caps

The host configuration uses:

- NVIDIA global Monitor Technology: G-SYNC Compatible
- NVIDIA global Vertical Sync: On
- NVIDIA global Max Frame Rate: Off
- RTSS as the single managed frame limiter

The physical caps leave a small margin below maximum refresh:

- TVs: 117 FPS for 120 Hz
- Alienware: 172 FPS for 175 Hz

During streaming, host G-SYNC/FreeSync is not the synchronization mechanism because Sunshine captures the virtual display at the requested fixed rate. The client may use its own adaptive-refresh capability for local presentation, but that is client-side behavior. The controller caps the host to the stream request, normally 120 FPS.

## Tray controller behavior

The blue gaming Codey icon opens this menu:

- **TV only (LG CX / LG C2)**
- **Alienware only**
- **Virtual display only**
- **Reapply current lock**
- **Show status**
- **Exit controller**

The tray checks the saved topology every two seconds. If the active topology is wrong continuously for five seconds, it reapplies the selected profile. If the selected physical path becomes unavailable, it can use the virtual display as a safety output.

The watchdog is topology-only. It does not guess whether a stream is running from Sunshine's network sockets. Sunshine owns the stream lifetime and invokes the verified start and undo commands.

## Sunshine integration

Installed configuration:

```text
dd_config_revert_on_disconnect = enabled
dd_configuration_option = disabled
encoder = nvenc
global_prep_cmd = [{"do":"C:\\StreamingTools\\DisplayController\\DisplayController.exe stream-start","elevated":false,"undo":"C:\\StreamingTools\\DisplayController\\DisplayController.exe stream-end"}]
hevc_mode = 3
min_log_level = 1
nvenc_preset = 4
```

Sunshine's own display-device configuration is disabled so it cannot race the controller. A stale fixed display GUID and stale fixed virtual-audio-sink value were removed.

## Startup configuration

The following startup paths have passed reboot verification:

- Gaming Display Controller: per-user Startup shortcut `Gaming Display Controller.lnk`
- RTSS: in-app **Start with Windows** enabled, with `RivaTuner Statistics Server.lnk` retained as a fallback
- ChatGPT/Codex: `Launch ChatGPT at Logon.vbs`
- Sunshine: its installed background service/application startup

The controller shortcut targets:

```text
"C:\StreamingTools\DisplayController\DisplayControllerTray.exe" tray
```

After the final reboot, the controller, Sunshine, RTSS, and `RTSSHooksLoader64` were running; the LG TV returned at 4K/120 HDR; and the RTSS global profile retained 117 FPS.

## Files and locations

### Deployed controller

```text
C:\StreamingTools\DisplayController\DisplayController.exe
C:\StreamingTools\DisplayController\DisplayControllerTray.exe
C:\StreamingTools\DisplayController\data\state.ini
C:\StreamingTools\DisplayController\data\controller.log
```

The pre-icon controller executables are retained beside the deployed files with the `.pre-icon` suffix.

### Sunshine

```text
C:\Program Files\Sunshine\config\sunshine.conf
C:\Program Files\Sunshine\config\sunshine.log
```

The pre-controller Sunshine configuration is preserved in this project as `deployment\sunshine.conf.pre-controller`.

### RTSS

```text
C:\Program Files (x86)\RivaTuner Statistics Server\RTSS.exe
C:\Program Files (x86)\RivaTuner Statistics Server\Profiles\Global
```

The global profile's `[Framerate]` section contains the persistent `Limit` value.

## Command reference

Run commands from `C:\StreamingTools\DisplayController` or use the full executable path.

```text
DisplayController.exe status [--json]
DisplayController.exe validate
DisplayController.exe set-cap <fps>
DisplayController.exe validate-stream-request
DisplayController.exe set tv
DisplayController.exe set alienware
DisplayController.exe set vdd
DisplayController.exe stream-start
DisplayController.exe stream-end
DisplayController.exe restore-lg
DisplayController.exe reapply
DisplayControllerTray.exe tray
```

Do not run `stream-start` manually without valid `SUNSHINE_CLIENT_*` variables. For ordinary manual changes, use `set tv`, `set alienware`, or the tray menu.

## Quick troubleshooting

### Tray icon is missing

1. Check whether `DisplayControllerTray.exe` is running.
2. Check the latest `controller.log` entry for `Tray controller started.`
3. Verify `Gaming Display Controller.lnk` exists in the per-user Startup folder.
4. Restart Windows if Explorer has retained stale notification-area state.

### RTSS is missing or the cap is wrong

1. Check that `RTSS.exe` and `RTSSHooksLoader64.exe` are running.
2. Open `Profiles\Global` and verify `Limit=117` for TV use.
3. Use `DisplayController.exe set-cap 117` to reapply the TV cap.
4. Confirm RTSS **Start with Windows** remains enabled.

### A Moonlight stream uses the wrong resolution or HDR state

1. Check the resolution, FPS, and HDR options in that Moonlight client.
2. Check Sunshine's log for the received client request and encoder mode.
3. Check `controller.log` for the recorded stream request and verified transition.
4. Do not restore a fixed Sunshine display GUID; selection must remain request-based.

### Stop Stream does not restore the TV

1. Wait several seconds for Sunshine's undo command and settle delay.
2. Select **TV only (LG CX / LG C2)** from the tray menu.
3. Alternatively run `DisplayController.exe restore-lg`.
4. Inspect both the controller and Sunshine logs.

### The office TV is black or reports no signal

The Samsung soundbar sometimes needs its HDMI input selected again. Correct the soundbar input first. Do not change controller identification for that known hardware quirk.

### A powered-on display tries to take over

Use **Reapply current lock**. The watchdog should also correct a persistent topology mismatch after five seconds. Alienware remains manual-only, so turning on a TV must not select it while Alienware mode is saved.

## Verified test matrix

- LG CX TV-only: passed at 3840×2160, 120 Hz, HDR on, 117 FPS
- LG C2 through Samsung soundbar: passed at 3840×2160, 120 Hz, HDR on, 117 FPS
- Alienware-only: passed at 3440×1440, 175 Hz, HDR on, 172 FPS
- TV power-state change while Alienware selected: passed without takeover
- Manual return from Alienware to TV: passed
- Virtual-display-only topology: passed
- Simulated handheld request and rollback: passed
- Real handheld 1080p/120 SDR stream and Stop Stream restoration: passed
- Real Xbox 4K/120 HDR stream, HEVC 10-bit HDR, audio, and Stop Stream restoration: passed
- Controller tray startup and custom icon after reboot: passed
- RTSS startup, hook loader, and persistent 117 FPS cap after reboot: passed

## Current status

The controller implementation is complete. All staged physical-display, virtual-display, streaming, HDR, audio, frame-cap, power-state, disconnect-restoration, tray-startup, and RTSS-startup tests have passed.
