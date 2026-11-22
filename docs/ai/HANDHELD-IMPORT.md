# Handheld source and reference import

Date: November 21, 2026, on the handheld. Added to the existing
`projects/consolidate-existing-work` branch after fetching its remote state.

The requested backup is original code and useful READMEs. Third-party mod
packages and machine installations are represented by references and completed
work, not bulk file copies. This is not a general chat archive or a Windows backup.

| Destination | Recovered material |
|---|---|
| AutoHDRForceTouch | Original C# application, tests, manifest/runtime config, build/icon scripts, icon artwork and usage notes |
| RetroBat-Configuration | Original read-only PE inspection script, 15 custom shader selector YAML files and a curated setup/rollback README |
| SplinterCell-Mods | README with six upstream mod references, historical install verification and later Conviction/RTSS limitations |
| Sunshine-Streaming | Original client settings/VRR notes and two separately labeled historical rollback scripts |
| Windows-Personalization | Two original edited-wallpaper bitmap outputs and provenance |

Sources remain under the original Documents/Codex workspaces listed in
[PROJECT_INDEX.md](../../PROJECT_INDEX.md). No original workspace file was moved
or changed. AutoHDRForceTouch's build default was adapted from the old task-output
layout to project-local `dist/`; its application/test/icon-tool source is unchanged.
The RetroBat README replaces the old handoff's obsolete claim that Auto HDR had
not yet been implemented; the completed application is now linked directly.

Downloaded archives, vendor staging, game files, ROMs/BIOS, saves, executables,
registry exports, logs, pairing/account data and raw conversation histories were
excluded. The Splinter Cell install script stays in its original local workspace;
the user requested just a reference for that work.

Validation records copy integrity and syntax/build checks; it does not re-run
installers, apply old settings, launch games, restore registry data or change
display/service/startup state. No periodic sync schedule was created or changed.

Completed checks:

- SHA-256 comparison matched 32 unchanged imported source/configuration,
  documentation and artwork files against the original workspaces.
- AutoHDRForceTouch built with compiler warnings treated as errors and
  `-SkipTests`. Its original test harness also compiled with warnings as errors;
  the tests and GUI were not executed during this import.
- PowerShell parsing passed for all five imported scripts.
- Relative Markdown links resolved across 18 relevant documents.
- Generated build/dist files were verified ignored; staged paths were reviewed
  for excluded packages, binaries and private data.
