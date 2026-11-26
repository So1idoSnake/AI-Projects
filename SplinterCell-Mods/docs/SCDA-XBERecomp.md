# Original Xbox Double Agent recomp setup

Installation and rebuild guide for
[TGP482/SCDA-XBERecomp](https://github.com/TGP482/SCDA-XBERecomp), a third-party
native Windows port of **Splinter Cell: Double Agent for the original Xbox**.
The upstream application source is maintained by TGP482. No application source,
game data, or downloaded runtime payloads are copied into this repository.

## Verified status — November 22, 2026

- Official release V3 downloaded; archive SHA-256 matched the published digest.
- User-provided original Xbox ISO format and required root entries checked.
- Full ISO copied locally; destination SHA-256 matched the source stream.
- First-run compilation produced both `SCDA.dll` and `SCDA_Versus.dll` and their
  completion stamps after compiling 194 files, eight at a time.
- A graphical launch on the desktop's NVIDIA GeForce RTX 4090 rendered the
  animated title screen and Enter prompt. Initial graphics startup took about
  62 seconds during that check. The test instance was closed with Alt+F4.
- Campaign gameplay, profile creation, keyboard/controller behavior beyond the
  title screen, performance, co-op, and Versus have **not** been validated.

This is evidence from the November 22 setup, not a new live test performed during
this documentation update or a compatibility guarantee for another machine.

## Source and deployment boundaries

| Location | Purpose |
|---|---|
| This guide and [release manifest](../reference/SCDA-XBERecomp-V3.json) | GitHub-maintained setup instructions and provenance |
| `C:/Github/SCDA-XBERecomp-Setup` | Original desktop's separate local setup/deployment repository; no remote configured |
| `game/SCDA.exe` under that deployment | Story/co-op application to launch |
| `game/SCDA_Versus.exe` | Versus application; compiled but not launch-tested |
| `game/assets/` | Local user-supplied ISO or extracted Xbox game files |
| `game/compiler/` | Bundled compiler, generated code, and compilation cache |
| `game/logs/` | Local application and compile logs |
| `game/artwork/` | Ignored Steam cover, wide tile, banner, source manifest, and banner prompt |

The deployment's README contains local operational details. Its Git exclusions
cover the entire `downloads/` and `game/` directories. Pushing this documentation
does not back up the installation, game files, settings, or saves. No original
standalone installer script was created; setup used bounded download, checksum,
copy, extraction, and application-launch commands.

## Steam library presentation — November 25, 2026

The existing non-Steam shortcut was renamed from `SCDA` to
**Splinter Cell: Double Agent (Xbox Recomp)**. Its custom sort name is
`Tom Clancy's Splinter Cell: Double Agent (Xbox Recomp)` to group it with the
other Splinter Cell titles. Its target remains `game/SCDA.exe` and its working
directory remains `game/`; the application filename and native PC Steam entry
were left unchanged.

Steam's **Properties > Customization** was used to apply:

| Steam field | File under `game/artwork/` | Dimensions |
|---|---|---|
| Cover | `splinter-cell-double-agent-xbox-cover.jpg` | 1521 x 2156 |
| Background/banner | `splinter-cell-double-agent-xbox-banner.png` | 2172 x 724 |
| Wide Cover | `splinter-cell-double-agent-xbox-hero.jpg` | 1920 x 1080 |

The cover includes original Xbox branding. The banner was reframed with the
built-in image-generation tool from a monochrome promotional wallpaper to keep
Sam's face visible in Steam's wide crop and leave dark space for the title.
The downloaded source wallpaper is retained as
`splinter-cell-double-agent-wallpaper.jpg`; the exact prompt is in
`banner-prompt.txt`. This is an AI-edited derivative, not an untouched official
banner. No upscaling was applied to the cover or wide-cover source.

Artwork sources: [TheGamesDB Xbox cover](https://thegamesdb.net/game.php?id=6197),
[LaunchBox promotional art](https://gamesdb.launchbox-app.com/games/details/11306-tom-clancys-splinter-cell-double-agent),
and [GoodFon source wallpaper](https://www.goodfon.com/games/wallpaper-splinter-cell-double-agent.html).
`artwork-sources.json` records source URLs, roles, dimensions, and SHA-256 hashes.
The whole artwork directory, including that manifest and prompt, is Git-ignored
by the deployment's existing `/game/` rule. Images are not uploaded to GitHub.

Verified the applied cover, wide-cover preview, displayed banner, and distinct
Xbox/PC Steam names; Steam's three saved artwork files matched their local
source hashes. The game was not launched during this presentation update.
On a fresh Steam profile, add the existing executable as a non-Steam game and
use **Properties > Customization > Change** to choose these files again.
Steam maintains its own per-profile image copies, separate from the artwork
source folder. Use each field's **Reset** control to revert that field later.

## Required game files

Use a legally acquired copy of **Double Agent for the original Xbox**. PC/Steam,
Xbox 360, PlayStation 2, and GameCube versions are different inputs.

The release accepts either:

1. An original Xbox disc image with the `.iso` extension in `game/assets/`.
2. Complete extracted Xbox game files in `game/assets/`, with `default.xbe`,
   `sc4_adv.xbe`, `Default/`, `Media/`, and `adv/` alongside each other.

`.xbe` is an original Xbox executable. A lone executable is insufficient. Extract
archive containers such as `.zip`, `.7z`, or `.rar` first. Other emulator formats
were not tested and should not be assumed to work directly.

The tested USA/Europe multilingual ISO was a 7,825,162,240-byte full-disc image.
The Xbox volume signature was verified at partition base `0x18300000`, following
the V3 [disc reader](https://github.com/TGP482/SCDA-XBERecomp/blob/V3/src/kernel/disc.cpp),
and both executables and all three required directories were present.
Its SHA-256 was
`c5336cc8bce847dff0b83a37cfa629329389184aca18aaa1111b170361b0324d`.
The application accepted `default.xbe` with SHA-1
`91290baf93c2f6ae735a3106c48734665c826d84`.
These identify the tested copy; they are not a claim that every other dump is invalid.
Rediscover the user's archive path on another machine rather than assuming a
particular mapped drive exists.

## Prerequisites and fresh-machine setup

The V3 upstream minimum is 64-bit Windows 10, a CPU with SSE4.2 and POPCNT,
a Direct3D 12 GPU, and 4 GB RAM. Use a writable deployment folder outside this
repository. This tested ISO alone uses about 7.29 GiB; allow additional space for
the release, compiled code, cache, settings, and saves. Retaining the compiler
cache is useful for recovery. The release route does not require Visual Studio.

1. Download `SCDA-XBERecomp.zip` from the official
   [V3 release](https://github.com/TGP482/SCDA-XBERecomp/releases/tag/V3), or review
   newer upstream instructions before deliberately choosing a later version.
2. For the pinned V3 archive, verify its size is 73,479,363 bytes and its SHA-256 is
   `3f1bb078a2d9857ad0a29ed793c14a79b1fa747ca9420cccc828db591fa8416a`.
   In PowerShell, run `Get-FileHash -LiteralPath '<downloaded-archive.zip>' -Algorithm SHA256`.
3. Extract the entire archive into a separate game folder, retaining the bundled
   DLLs and compiler directory. Do not extract over an existing installation
   without first reviewing and preserving its settings and saves.
4. Copy the ISO, or the complete extracted game contents, into that folder's
   `assets/` directory. Preserve the source copy and verify the local copy.
5. Run `SCDA.exe` from the extracted game folder. The first start compiles the
   game code and offers a Start button when ready. Let compilation finish;
   completed objects are retained if preparation is interrupted.
6. Check for the title screen, then perform an attended input/profile/gameplay
   test before treating the installation as fully playable. Settings open with
   `F10`. Stop at a blocker and record it instead of changing machine settings.

On the original desktop the launch target is
`C:/Github/SCDA-XBERecomp-Setup/game/SCDA.exe`. Installation and saves remain
independent of the Steam Double Agent directory and its widescreen mod.

Building upstream from source is a separate workflow: the V3 instructions require
extracted assets and Visual Studio 2022 C++/Clang/CMake components. ISO input is
supported for playing the release, not for the upstream source-build procedure.
No source build or custom game patch was performed here.

## Verification, recovery, and remaining work

Check `game/logs/scda.log` for image detection and graphics startup, and
`game/logs/compile.log` for compilation diagnostics. A copied executable or a
successful compile alone is not evidence of working gameplay.
The observed DLL sizes were 21,750,272 bytes for `SCDA.dll` and 12,466,688 bytes
for `SCDA_Versus.dll`; these are historical outputs, not portable checksums.

For an update or reinstall, close the game and make a separate private backup of
the existing installation's settings and saves before replacing anything. Keep
the old deployment intact until its replacement is verified. Obtain runtime
files again from upstream; an AI-Projects clone supplies only this guide and
release metadata. Removing or changing the separate deployment should not modify
the Steam installation or the source archive copy.

Remaining checks: profile creation, input and controller behavior, a campaign
mission, frame pacing, co-op, and the Versus executable. A compiled Versus DLL
does not establish working multiplayer.

## Upstream provenance and licensing

- Release: [SCDA XBERecomp V3](https://github.com/TGP482/SCDA-XBERecomp/releases/tag/V3), published October 29, 2026.
- V3 source commit: `ee5b44027bb99cdc11692b466f01d7930912240d`.
- Instructions: [V3 README](https://github.com/TGP482/SCDA-XBERecomp/blob/V3/README.md).
- License: [upstream MIT license](https://github.com/TGP482/SCDA-XBERecomp/blob/V3/LICENSE).
- Bundled compiler licensing: preserve the release's `compiler/LLVM-LICENSE.txt` and other upstream notices.

The upstream software license does not grant rights to redistribute the game's
ISO, extracted assets, or derived game code. Keep those files and personal runtime
state outside the GitHub documentation repository.
