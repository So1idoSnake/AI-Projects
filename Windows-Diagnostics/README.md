# Windows hardware and stability investigations

Historical cases, not an automatic optimizer. No permanent diagnostic service or standalone
repair application was recovered. Private session-code captures retain some original inspection
commands; they are not instructions to uninstall drivers or change a fresh PC.

| Case | Historical evidence | Status / rebuild implication |
|---|---|---|
| Xbox wireless adapter | Adapter on a VIA/generic hub; later USB adapter stayed present while controller child link disappeared repeatedly | Try direct motherboard placement, power/battery and second-controller comparisons; failing RF hardware suspected, not proven |
| Astro A50 X microphone | Correct default input and volume 100, Windows mute active, test 0% | Unmute the intended endpoint; no hardware replacement conclusion |
| NETIO.SYS crash | Network stack crash with Mullvad split-tunnel and AdGuard filters loaded | Mullvad was removed and reboot/removal verified; attribution was not proven. Do not automatically uninstall AdGuard |
| Forced-shutdown recovery | User entered Windows recovery then Continue normally | Separate from display-controller development; no general repair inferred |

## Fresh-PC use

Install correct chipset, USB, audio, network and GPU drivers. Reproduce the actual symptom and
collect time-bounded evidence before changing settings. Kernel-Power records alone do not identify
root cause. Avoid carrying old device instances, account IDs or crash-specific conclusions across PCs.
Use a new timestamp and distinguish observation from hypotheses. No current re-diagnosis was done.
