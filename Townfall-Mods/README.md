# Silent Hill Townfall mod setup

Installation recipe for the original Steam installation; this is not an original game or mod engine.

## Components and original sources

- UE4SS for Townfall, including its custom StaticConstructObject signature:
  https://www.nexusmods.com/silenthilltownfall/mods/4
- Infinite Durability: https://www.nexusmods.com/silenthilltownfall/mods/7
- No Intro: https://www.nexusmods.com/silenthilltownfall/mods/3
- No Post-Processing: https://www.nexusmods.com/silenthilltownfall/mods/5
- The Weight Stops Stalking, 50% Slow variant (F8):
  https://www.nexusmods.com/silenthilltownfall/mods/84

Original staging: C:/Users/micha/Documents/Codex/2026-10-07/i-am-x20/work/townfall-mods-20261007.
This staging tree is preserved under ignored local-private/vendor-staging for personal recovery.
It includes downloaded Lua/config source as well as binaries. See THIRD_PARTY.md before sharing.

## Rebuild on another PC

Install the compatible Steam game version and close it. Download the components from their
authors and review current installation instructions; no automatic installer is supplied.
The original layout placed the loader/core under Townfall/Binaries/Win64, UE4SS mods under
that directory's ue4ss/Mods, and No Intro packages under Townfall/Content/Paks/~mods.
The post-processing INI was placed under the current user's local Townfall Saved/Config/Windows.
Merge/review existing settings and back them up first; don't blindly replace a new PC's INI.

The original installed files were hash-checked against their downloads. In-game startup showed
UE4SS initialized and Infinite Durability hooks registered. SlowStalker used fallback slowdown
mechanisms after its preferred hook failed; real F8 behavior remained to be checked in game.
The user chose to leave the debug console visible. The original Engine.ini was read-only.
Those are historical findings, not guarantees after game updates.

No standalone first-party mod generator or installer source was found. The recovered code belongs
to downloaded upstream components; private session-command history supplies the original operations.
