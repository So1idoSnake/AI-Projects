# Hogwarts Legacy Steam-to-Xbox save investigation

Status: unresolved manual-save compatibility. This is a recovery/research record, not a supported converter.

Imported gameplay loaded in some attempts and autosaves worked, but Xbox's manual-slot metadata
remained incompatible. Patching/replacing the profile repeatedly caused the game to rebuild it,
sometimes hiding the character. The latest reviewed instruction was to initialize a new native
Xbox character on this PC, finish Sorting/reach the common room, create a visible manual save,
then close the game before any further import. Do not claim this last step was completed.

## Fresh-PC workflow

Install the game, establish a working native profile and manual save, exit fully and back up both
Steam and Xbox/WGS data before attempting migration. Account/container paths must be discovered
on the target PC. Never copy another account's WGS indexes blindly. Do not automate cloud-conflict
choices or change live saves from this runbook.

The original private work folder is copied into ignored local-private/original-work:
C:/Users/micha/Documents/Codex/2026-08-30/referenced-chatgpt-conversation-this-is-an/work.
It contains timestamped backups, native-profile rebuild attempts, and staged paired imports.
Preserve these originals; avoid interpreting intermediate payloads as known-good current saves.
Private session code is partial historical evidence only. No maintained standalone converter
source was recovered. Do not replay its ad hoc binary patch commands as a fresh-PC installation.
