# Sharing the code

The Display-Switcher folder contains the actual C# implementation, build script, recovery-test
source, icon-generation script and assets, plus a separate exact-binary snapshot.
Windows-App-Startup/scripts contains a reusable adaptation of the successful documented shortcut
procedure. It was syntax-checked only; it was not installed or live-tested during this import.

For each other project, the README says whether executable code was recovered or the work was
performed with one-off commands, UI interaction, or third-party packages. Historical commands
are reference material and may be incomplete or unsafe to replay.

[The Double Agent recomp guide](SplinterCell-Mods/docs/SCDA-XBERecomp.md) records
an installation of TGP482's third-party port, not an original game implementation.
Share its runbook and release metadata; obtain binaries and source from upstream.
Xbox disc images, extracted game files, generated game code/DLLs, saves, compiler
work directories, and raw runtime logs are outside this GitHub backup. The local
deployment's separate Git repository has no remote; its ignored assets are not
protected by pushing this repository. Preserve upstream licensing when obtaining
or redistributing upstream components.
Custom Steam artwork and its source/prompt manifest also remain in the ignored
deployment; the shared runbook records attribution and the AI-reframed banner's
provenance without publishing image payloads.

Before publishing or sharing:
- Share source and runbooks; omit local-private folders, personal exports, saves and raw diagnostics.
- Paths, app IDs and monitor EDIDs in historical material may need adjustment.
- Read THIRD_PARTY.md where present. Downloaded Nexus mods are not original project code.
- The Codey-like display icon is not a claim of affiliation with OpenAI.
- No repository-wide redistribution license has been chosen; decide one deliberately before
  offering others formal reuse rights. Preserve upstream licenses on third-party components.

No remote push or public release was performed by this import.
