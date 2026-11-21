# Gaming display controller implementation checkpoint

Last updated: 2026-09-06

## Hard safety rule

Never hot-swap the GPU HDMI connection between the LG CX and LG C2 while Windows is running. Fully shut down Windows, move the HDMI connection, turn on the selected TV, and then boot the PC. The controller does not automate this physical swap.

## Verified

- LG CX is identified through its native LG/GSM endpoint.
- The office LG C2 is identified through the Samsung HDMI 2.1 soundbar passthrough endpoint `SAM721B`.
- Both endpoints resolve to the same TV-only controller profile.
- TV-only Windows topology validates and applies on both the loft CX and the office C2/soundbar path.
- LG CX runs at 3840x2160, 120 Hz, HDR enabled.
- LG C2 through the soundbar runs at 3840x2160, 120 Hz, HDR enabled.
- NVIDIA global Monitor Technology is G-SYNC Compatible.
- NVIDIA global Vertical Sync is On.
- NVIDIA's duplicate global Max Frame Rate limiter is Off.
- RTSS is the single managed limiter and persists the TV cap at 117 FPS.
- Alienware-only mode applies at 3440x1440, 175 Hz, HDR enabled, with RTSS capped at 172 FPS.
- Powering the office TV/soundbar endpoint off and back on while Alienware mode is selected does not activate it or take over the desktop.
- Explicit restoration from Alienware mode returns to the TV-only 4K/120 HDR profile and restores the 117 FPS cap.
- After cold-swapping back to the loft, the LG CX again boots as the sole 3840x2160, 120 Hz, HDR-enabled TV output.
- RTSS's in-app **Start with Windows** setting is enabled. A direct per-user Startup-folder shortcut targeting `RTSS.exe` is retained as a fallback because the in-app toggle did not expose a Windows `Run` entry during verification.
- RTSS is running in the interactive user session with `RTSSHooksLoader64`, its saved global profile cap is 117 FPS, and the repaired startup path has passed a reboot verification.
- ChatGPT launched successfully after the reboot through the existing `Launch ChatGPT at Logon.vbs` Startup item.
- Startup items must be checked for hidden `StartupApproved` disabled flags and verified after reboot.
- Alienware-only and VDD-only topologies pass Windows validation.
- A simulated handheld request successfully activated VDD-only at 1920x1080, 120 Hz, HDR off, with RTSS at 120 FPS; `stream-end` then restored the loft LG TV-only profile at 3840x2160, 120 Hz, HDR on, with RTSS at 117 FPS.
- The verified controller binaries are deployed at `C:\StreamingTools\DisplayController`.
- Sunshine now calls the controller directly with `stream-start` and `stream-end`; the old AutoHDR PowerShell preparation commands are no longer active.
- Sunshine's stale fixed display GUID is cleared, and Sunshine's own display-device switching is disabled so it cannot race the controller.
- Sunshine is configured with `hevc_mode = 3`; its post-restart diagnostics successfully created an HEVC 10-bit HDR encoder on the RTX 4090.
- Sunshine's post-restart display inventory detects the VDD, Alienware, and active loft LG TV without the former fixed-display detection failure.
- The pre-controller Sunshine configuration is preserved at `deployment\sunshine.conf.pre-controller`. The old AutoHDR scripts remain on disk but are not referenced.
- Production controller state and logs now live in the shared deployed `data` directory beside the executable, so Sunshine preparation commands, manual commands, and the future tray/watchdog use the same state across Windows accounts.
- The real handheld test passed at 1920x1080, 120 Hz, SDR 8-bit on VDD-only with RTSS at 120 FPS.
- Moonlight Stop Stream ran the Sunshine undo command and restored the loft LG TV-only profile at 3840x2160, 120 Hz, HDR on, with RTSS at 117 FPS.
- Sunshine's stale fixed virtual-audio-sink value was cleared. Automatic selection successfully switched streaming audio to the virtual 48 kHz 7.1 sink without the previous warning.
- The real Xbox tests passed at 3840x2160, 120 Hz, HDR enabled on VDD-only with RTSS at 120 FPS.
- Sunshine captured the Xbox stream as HEVC 10-bit HDR (Rec. 2020 / SMPTE 2084 PQ), sent HDR mode to the client, and used the automatic 48 kHz 7.1 virtual audio sink.
- Both Xbox Stop Stream tests restored the loft LG TV-only 4K/120 HDR profile and RTSS 117 FPS successfully; the duplicate start/stop pair was intentional audio verification.
- The tray watchdog is topology-only. Sunshine owns stream lifetime and runs the verified undo command; the unreliable Sunshine UDP-socket inference was removed to prevent false restores.
- `GamingDisplayController` starts from the per-user Startup shortcut `Gaming Display Controller.lnk`, targeting `"C:\StreamingTools\DisplayController\DisplayControllerTray.exe" tray`, and is running in the interactive session.
- The custom blue gaming Codey tray icon appeared correctly after reboot.

## Reboot smoke-test result

1. Windows rebooted with the loft LG connected.
2. The TV returned as the sole 3840x2160, 120 Hz, HDR-enabled output.
3. ChatGPT, Sunshine, and Gaming Display Controller launched successfully; the custom tray icon appeared.
4. RTSS did not initially relaunch. Its in-app startup setting and a direct Startup-folder fallback are now enabled, RTSS is running, and the saved 117 FPS cap has been re-verified.
5. A second reboot confirmed that RTSS and `RTSSHooksLoader64` launch automatically with the saved 117 FPS cap.

All staged TV, Alienware, VDD, handheld, Xbox, HDR, audio, disconnect-restoration, tray startup, and RTSS startup tests have passed. Sunshine integration and the topology-only tray controller are fully enabled.

Sunshine build note: the installed build exposes `SUNSHINE_CLIENT_WIDTH`, `SUNSHINE_CLIENT_HEIGHT`, `SUNSHINE_CLIENT_FPS`, and `SUNSHINE_CLIENT_HDR`, but not `SUNSHINE_CLIENT_NAME`. Stream profile selection must remain request-based rather than client-name-based.
