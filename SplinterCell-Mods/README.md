# Splinter Cell mods and Xbox recomp setup

Reference for the six Steam games configured on the handheld on October 17,
2026, and the original Xbox Double Agent recomp configured on the desktop on
November 22, 2026. This folder records upstream sources, setup instructions,
and verification limits; it contains no downloaded mods, game files, saves,
staging tree or installer.

## Original Xbox Double Agent recomp

See the [SCDA-XBERecomp setup and rebuild guide](docs/SCDA-XBERecomp.md) for the
separate [TGP482 port](https://github.com/TGP482/SCDA-XBERecomp). Release V3 was
downloaded and checksum-verified, the user's original Xbox ISO was copied and
verified locally, both game DLLs compiled, and the animated title screen was
observed on November 22. Gameplay, profile creation, and multiplayer remain
untested. The test instance was closed afterward.

This uses the original Xbox version of Double Agent. The PC game and its
widescreen fix below are a separate installation and cannot supply its assets.
The deployment remains at `C:/Github/SCDA-XBERecomp-Setup`; this repository keeps
the runbook and [release provenance](reference/SCDA-XBERecomp-V3.json).

On November 25, its non-Steam shortcut was named **Splinter Cell: Double Agent
(Xbox Recomp)** and given an Xbox-branded cover, wide tile, and fitted banner.
Artwork sources and the AI banner-reframing prompt stay in the deployment's
ignored `game/artwork/` folder. The [runbook](docs/SCDA-XBERecomp.md#steam-library-presentation--november-25-2026)
records the files, provenance, and Steam customization procedure.

## Mods and completed work

| Game | Installed package | Purpose recorded during setup |
|---|---|---|
| Splinter Cell | [Enhanced SC v1.4b](https://github.com/Joshhhuaaa/EnhancedSC) | Gameplay/compatibility improvements and modern controller support |
| Pandora Tomorrow | ThirteenAG Widescreen Fix | Widescreen, controller/XInput, EAX/audio and 60 FPS fixes |
| Chaos Theory | ThirteenAG Widescreen Fix | Widescreen, controller/XInput, EAX/audio and 60 FPS fixes |
| Double Agent | ThirteenAG Widescreen Fix | Widescreen, controller/XInput, audio and stability fixes |
| Conviction | ThirteenAG Fusion Fix | Controller and modern-system fixes |
| Blacklist | ThirteenAG Fusion Fix | Widescreen/QOL fixes; native controller support retained |

ThirteenAG's official [game/package index](https://github.fusionfix.io/wfp) and
[source repository](https://github.com/ThirteenAG/WidescreenFixesPack) provide the
individual downloads and installation instructions. These are upstream projects;
we installed/configured them rather than wrote them. Links were checked during
the November 21 documentation import. Current packages may differ from those
downloaded in October; follow their version-specific instructions.

The original archives were extracted, their game-folder payloads inspected, and
copied into the corresponding Steam installations. Automatic resolution detection
and the older games' 60 FPS defaults were reviewed. Installation recorded 578,
19, 10, 12, 59 and 14 files respectively: **692 files total**. The original
post-install comparison reported no missing files or SHA-256 mismatches.
Nine existing Enhanced SC files were retained locally before being overwritten.
This is historical file-install evidence, not proof that every game launched or
every feature worked in gameplay. No games were launched for this import.

## Later Conviction notes

In November the user reported Conviction launching, followed by microstutter at
higher frame rates. The review recorded a 120 Hz desktop, a 60 Hz fullscreen
game profile and an RTSS global cap at 116 FPS. A 60 FPS per-game test and a
120 Hz in-game refresh comparison were proposed; no smooth-above-60 result
was established. RTSS's saved global 116 FPS limit and sign-in startup task were
verified in that later task. These are original-machine observations, not
required values for a fresh installation.

## Recreate or remove

Install each owned Steam game, obtain its fix from the upstream links, check
compatibility and follow that package's instructions. Review controller settings
and resolution/frame-rate options before testing in game. Steam's **Verify
integrity of game files** can restore overwritten Steam-managed files; separately
remove added mod files according to upstream instructions, because verification
does not necessarily delete extra files.

The original install manifest remains locally at
`Documents/Codex/2026-10-17/i-m-downloading-games-on-steam/mod-backups/20261017-142808/install-manifest.json`.
Its counts and verification outcome are summarized above; the packages and
machine-specific manifest are intentionally not copied into Git.
