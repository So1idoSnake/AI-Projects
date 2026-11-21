# TV / soundbar HDR and Dolby Vision research

Research only. The working PC gaming target is HDR10. The office LG C2 sits behind a Samsung
HDMI soundbar; the loft LG CX uses its native endpoint. Do not confuse Samsung EDID identity
with the panel's manufacturer. The exact soundbar suffix remained unresolved in the reviewed chat.

The original workspace contains current-hdmi-edid-backup.bin,
c2-dolby-vision-lldv-candidate.bin and display-diagnostics.txt. These are copied under ignored
local-private/original-evidence from C:/Users/micha/Documents/Codex/2026-10-31/i-m-on-my-tv-is.
A candidate binary is not evidence that an EDID override was installed or that Dolby Vision worked.

The reviewed discussion corrected an earlier claim: the C2's normal HDMI Dolby Vision capability
does not establish support for the newer Dolby Vision PC setting. Windows/GPU/playback software,
soundbar passthrough and the exact TV mode all matter. Treat unofficial EDID workarounds as
unverified research. Do not install the candidate, copy it to another PC, or flash monitor firmware.

## Fresh-PC use

Start with native EDID and working 4K/120 HDR10/VRR. Record the exact chain and supported modes.
Recheck manufacturer documentation for that model/firmware before considering optional Dolby Vision.
Keep this investigation separate from Display-Switcher implementation and its HDR enable toggle.
No standalone EDID-editing utility source was recovered in this workspace.
