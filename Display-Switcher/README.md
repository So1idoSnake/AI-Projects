# Gaming Display Controller

Local display state controller for the gaming PC. It provides explicit TV, Alienware, and virtual-display profiles and consumes Sunshine client mode/HDR requests. The TV profile recognizes both the loft LG-native endpoint and the office C2's exact Samsung-soundbar passthrough endpoint (`SAM721B`).

The physical, virtual, handheld, and Xbox profiles have completed their staged verification. Sunshine integration and the topology-only tray controller are enabled.

## Physical TV swap safety

Do not hot-swap the GPU's HDMI connection between the LG CX and LG C2 while Windows is running. A TV-to-TV cable change requires a full Windows shutdown first:

1. Shut down Windows completely.
2. Move the GPU HDMI connection from one LG TV to the other.
3. Turn on the selected TV and boot the PC.
4. Run `DisplayController.exe status`, then verify/apply the shared `tv` profile.

The CX and C2 intentionally share one logical LG profile. The controller must never attempt to automate the physical cable swap.

## Commands

```text
DisplayController.exe status [--json]
DisplayController.exe validate
DisplayController.exe set-cap <fps>
DisplayController.exe validate-stream-request
DisplayController.exe set tv|alienware|vdd
DisplayController.exe stream-start
DisplayController.exe stream-end
DisplayController.exe restore-lg
DisplayController.exe reapply
DisplayController.exe tray
```

`stream-start` reads the width, height, FPS, and HDR `SUNSHINE_CLIENT_*` variables provided by Sunshine. This installed Sunshine build does not expose a client-name variable, so selection is intentionally request-based: 1080p SDR/HDR for a handheld-class client and 4K HDR for the Xbox-class client. Unsupported resolutions or refresh rates fail closed and leave the current profile unchanged. Failed display setup uses the same available-display recovery order as stream-end.

## Normal use

- TV is the default physical profile: 3840x2160 at 120 Hz, HDR on, RTSS 117 FPS.
- Streaming is automatic. Sunshine activates only the VDD from the Moonlight request. Stop Stream restores the TV when available, otherwise the Alienware, then the VDD safety profile. A failed profile also advances to the next available display.
- Choose **Alienware only** from the Gaming Display Controller tray icon or ask ChatGPT to switch it. It is also the automatic recovery fallback when the TV is unavailable. This selects 3440x1440 at 175 Hz, HDR on, RTSS 172 FPS.
- Choose **TV only (LG CX / LG C2)** from the tray icon to return from Alienware manually.

The tray enforces the saved display topology after a five-second mismatch. Recovery tries TV, Alienware, then VDD when the selected physical path becomes unavailable. It does not infer streaming state from network sockets; Sunshine owns stream lifetime and runs the `stream-end` undo command on Moonlight disconnect. Completed stream metadata is cleared even if every recovery profile fails, so a finished stream cannot block the next launch as already active.

The tray starts through the per-user Windows Startup shortcut `Gaming Display Controller.lnk`, targeting:

```text
"C:\StreamingTools\DisplayController\DisplayControllerTray.exe" tray
```

RTSS's in-app **Start with Windows** setting is enabled. A direct per-user Startup shortcut is also retained as a fallback because the in-app toggle did not expose a Windows `Run` entry during verification.
