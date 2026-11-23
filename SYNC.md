# Periodic GitHub sync

Configured on 2026-11-20 as the local app automation **Sync AI project branches**
(automation ID: sync-ai-project-branches), daily at 7 PM America/New_York.

Repository: C:/Github/AI-Projects.
Remote: https://github.com/So1idoSnake/AI-Projects.git.

Only committed work on backup/* and projects/* branches is eligible for ordinary
same-name, fast-forward pushes. The automation must not change main/master, switch
the current checkout, force-push, merge/rebase, or commit unreviewed working edits.
Uncommitted changes are reported for a deliberate commit. All local-private folders
and excluded personal data remain local and require a separate private backup.

Splinter Cell setup documentation is maintained under `SplinterCell-Mods/`,
including the [Xbox Double Agent recomp runbook](SplinterCell-Mods/docs/SCDA-XBERecomp.md).
The separate local deployment at `C:/Github/SCDA-XBERecomp-Setup` has no GitHub
remote. This repository's sync covers the runbook and release metadata, not that
deployment, the Xbox ISO, compiled game code, settings, or saves.

Git runs noninteractively with GIT_TERMINAL_PROMPT=0, GCM_INTERACTIVE=never, and
credential.interactive=never. Missing authentication is reported rather than
opening a sign-in dialog. Network attempts should be time-bounded.

## Verified manual sync and remaining limitation

On 2026-11-20, GitHub Desktop successfully published the existing branches:

- backup/display-switcher-2026-11-20 at 149d2a24901da581dd508e59415a0009a835fdcb.
- projects/consolidate-existing-work at bfea130c908394524d7e61aa3fb8891ca6e45e8d,
  followed by the documentation update recording this sync.

GitHub Desktop reported successful pushes and fetched matching remote-tracking
commit hashes. The original checkout stayed on projects/consolidate-existing-work;
the backup branch was published through an isolated local clone. Main was unchanged.

The GitHub plugin authenticated as So1idoSnake, but reported no repository
installations or accessible repositories. Noninteractive command-line Git still
had no usable credentials. Desktop's successful manual sync does not establish
that the daily automation can authenticate. Configure a supported noninteractive
Git authentication method and verify a scheduled sync before treating automatic
backup as operational. Never paste or commit passwords, tokens, cookies or
credential-store contents.

The schedule is stored by the app, not installed by cloning this repository.
On a new computer, configure authentication and recreate the daily automation
with the rules above. Then verify the remote branch commit hashes after a push.

## November 21 handheld checkout

The handheld imported its work from a separate checkout under
`Documents/Codex/2026-11-21/https-github-com-so1idosnake-ai-projects`.
After the user completed Git sign-in, command-line fetch of the existing
`projects/consolidate-existing-work` branch succeeded. This does not verify or
change the desktop's daily automation or its credential setup. Report a source
backup as pushed only after comparing the remote head with the local commit.
