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

Git runs noninteractively with GIT_TERMINAL_PROMPT=0, GCM_INTERACTIVE=never, and
credential.interactive=never. Missing authentication is reported rather than
opening a sign-in dialog. Network attempts should be time-bounded.

## Verified manual sync history

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
that the daily automation can authenticate. This describes the November 20 state;
the authentication limitation was addressed below. Never paste or commit passwords,
tokens, cookies or credential-store contents.

## Command-line authentication verified — 2026-11-22

Retried the failed scheduled sync with the user present. The existing Git Credential
Manager 2.9.1 browser login completed for So1idoSnake. A subsequent bounded Git
request succeeded with GIT_TERMINAL_PROMPT=0, GCM_INTERACTIVE=never and
credential.interactive=never, followed by a successful project/backup-only fetch.
The system's configured manager helper is used; no credentials were stored in this repo.

At that comparison, the backup branch matched origin. The consolidation branch had
zero unpublished local commits and was one commit behind origin; it was left untouched.
The display feature branch projects/aw3926qw-profiles contained the unpublished source,
tests and documentation being synced. No merge, rebase or checkout change is needed
to publish that independent feature branch.

Interactive sign-in is only a one-time setup step. The scheduled job remains strictly
noninteractive, and should report failure rather than opening a login dialog if access
expires. This manual retry verifies the current Git context; the next scheduled run
must still succeed before claiming an actual scheduled sync was verified. Always
confirm matching remote head hashes before reporting a completed backup.

The schedule is stored by the app, not installed by cloning this repository.
On a new computer, configure authentication and recreate the daily automation
with the rules above. Then verify the remote branch commit hashes after a push.
