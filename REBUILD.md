# Rebuild on a new Windows PC

This repository contains reusable source and historical context. It is not a full Windows image,
an unattended installer, or an assurance that old driver/app settings still apply.

## 1. Bring over source and private backups separately

Clone the repository, then read the project index. Restore personal saves, exports, and account
evidence from a separate private backup only when that project's procedure calls for them.
Ignored local-private folders are not included in a clone. The original source workspaces were
retained except for the explicitly relocated inactive helper scripts listed in MIGRATION.md.

## 2. Install and discover prerequisites

Install Windows updates, the motherboard/chipset/network/audio drivers and the GPU vendor driver.
Install the apps you actually need: Steam, Xbox, ChatGPT, Gemini, RTSS and Sunshine.
Use official installers and current supported versions; the versions observed in old notes are
historical evidence, not pinned requirements. Do not restore tokens, auth databases or cookies.
Sign in through each app normally.

Connect the monitor/TV. Inventory exact monitor IDs, supported resolutions, refresh rates,
HDR and VRR support. The old controller's EDID IDs, installed paths and hardcoded monitor
profile are specific to the original PC; review them before building or deploying.
The AW3926QW 5K2K/330Hz/4K profile changes have NOT been implemented.

## 3. Build and configure the display project

Read Display-Switcher/README.md and Display-Switcher/BASELINE_BACKUP.md.
Change into Display-Switcher before using its scripts. The source relative to that folder is
src/DisplayController/DisplayController.cs, and build.ps1 produces both EXEs.
The build expects the Windows .NET Framework C# compiler and x86 RTSS hooks.
Confirm the compiler path and both RTSS path constants described in Display-Switcher/SOURCE_GUIDE.md.
Install/configure MikeTheTech VDD with the expected MTT1337 identity and the mode sample in
Sunshine-Streaming/config/vdd_settings.xml before relying on virtual-display fallback.
Recheck that sample against the installed VDD version. Build first; deploy only as a separate requested step.
Do not simply drop the old executable snapshot onto a different machine.

## 4. Configure streaming after local displays work

Follow Sunshine-Streaming/README.md. Review the config sample rather than overwrite the installed
configuration blindly. Configure a fresh Sunshine login/pairing and verify each real Moonlight
client's request. Display switching, cap, HDR capture, client HDR and audio restoration are separate
checks. No old saved Stream state should be restored onto a fresh installation.

## 5. Set sign-in apps last

Windows-App-Startup has a reusable setup script; it previews by default and changes shortcuts
only with -Apply. Resolve the new machine's installed app IDs. Read its original guide for
obsolete-entry cleanup and testing. RTSS and the display tray remain separate startup components.

## 6. Optional projects

For the handheld, AutoHDRForceTouch contains its original C# source and a local
build command; build it before launching the registry-editing GUI.
RetroBat-Configuration records ROM paths, controls, graphics, shader dependencies
and the previous iCloud save-junction migration. Rediscover paths and hardware
before applying those settings. [SplinterCell-Mods](SplinterCell-Mods/README.md)
records upstream Steam fixes and the separate
[original Xbox Double Agent recomp setup](SplinterCell-Mods/docs/SCDA-XBERecomp.md).
For the recomp, download the official release and supply your own original Xbox
ISO or complete extracted game files; the Steam/PC game files cannot replace them.
Keep the deployment outside this repository and allow its first-run compilation
to finish. The November 22 desktop check reached the title screen; gameplay and
multiplayer remain untested. No game/mod payloads or saves are bundled here.

Install games first; then use the individual mod/save guides. Hogwarts migration remains unresolved
for ordinary manual-save slots. Never replay old WGS patch commands. Memory/history projects need
new authorized source exports, not credentials from this PC. AI-App-Configuration is historical:
check the new runtime's accepted schema instead of copying old keys.

## Verification checklist for a later rebuild session

- Local monitor/TV: correct identity, only intended output, resolution/Hz, HDR and VRR.
- RTSS: actual process and hook loader running; correct profile and in-game enforcement.
- Streaming: real client start and Stop Stream; resolution, HDR, cap and audio restored.
- Startup: shortcut registration and a real sign-in/reboot verification.
- Mods/saves: backed up first, game version compatible, then verify in game.
These checks were not run during repository organization.
